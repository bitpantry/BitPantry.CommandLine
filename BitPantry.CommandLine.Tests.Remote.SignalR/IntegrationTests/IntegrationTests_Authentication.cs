using BitPantry.CommandLine.Remote.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using BitPantry.CommandLine.Tests.Remote.SignalR.Helpers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Tests.Remote.SignalR.Environment.Commands;
using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

[TestClass]
public class IntegrationTests_Authentication
{
    [TestMethod]
    public async Task ConnectClient_ClientConnected()
    {
        using var env = new TestEnvironment();

        await env.Cli.ConnectToServer(env.Server);

        env.Cli.Services.GetRequiredService<IServerProxy>().ConnectionState.Should().Be(ServerProxyConnectionState.Connected);
    }

    [TestMethod]
    public async Task ConnectClient_BadApiKey_ClientConnected()
    {
        using var env = new TestEnvironment();
        await env.Cli.ConnectToServer(server: env.Server, apiKey: "badKey");

        Console.WriteLine($"Buffer: [{env.Console.Buffer}]");
        Console.WriteLine($"Lines count: {env.Console.Lines.Count}");
        for (int i = 0; i < env.Console.Lines.Count; i++)
        {
            Console.WriteLine($"Lines[{i}]: [{env.Console.Lines[i]}]");
        }

        env.Console.Lines.Count.Should().BeGreaterThan(1);
        env.Console.Lines.Should().Contain(l => l.Contains("Requesting token with API key"));
    }

    [TestMethod]
    public async Task RefreshTokenDuringExecution_ExecutionCompletesFirst()
    {
        LongRunningCommand.ResetTcs(); // Reset the TCS for this test run
        
        using var env = new TestEnvironment();
        var token = TestJwtTokenService.GenerateAccessToken();

        await env.Cli.ConnectToServer(env.Server);

        var lrcTask = env.Cli.Run("test lrc"); // start long running command
        
        // Wait for long running task with timeout to avoid hanging
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(LongRunningCommand.Tcs.Task, timeoutTask);
        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("LongRunningCommand did not start within 10 seconds. Command may have failed to resolve.");
        }

        await env.Cli.Services.GetRequiredService<AccessTokenManager>().SetAccessToken(token, env.Server.BaseAddress.AbsoluteUri);

        _ = await lrcTask;

        var lrcLogs = env.GetServerLogs<LongRunningCommand>();
        var proxyLogs = env.GetClientLogs<SignalRServerProxy>();

        lrcLogs[0].Message.Should().Be("Long running command finished");
        proxyLogs[2].Message.Should().Be("OnAccessTokenChanged :: rebuilding connection");
        proxyLogs[2].Timestamp.Subtract(lrcLogs[0].Timestamp).Should().BeGreaterThan(TimeSpan.Zero);
    }

    [TestMethod]
    public async Task RefreshTokenOnExpiration_TokenRefreshes()
    {
        using var env = new TestEnvironment(opts =>
        {
            // Token lives for 2 seconds, start refreshing when 1 second remains
            // This means refresh triggers ~1 second after token creation
            // Monitor checks every 100ms for timely refresh detection
            opts.AccessTokenLifetime = TimeSpan.FromSeconds(2);
            opts.TokenRefreshMonitorInterval = TimeSpan.FromMilliseconds(100);
            opts.TokenRefreshThreshold = TimeSpan.FromSeconds(1);
        });

        var mgr = env.Cli.Services.GetRequiredService<AccessTokenManager>();

        var tokens = new System.Collections.Concurrent.ConcurrentBag<AccessToken>();
        
        mgr.OnAccessTokenChanged += async (sender, newToken) =>
        {
            if (newToken != null)
            {
                tokens.Add(newToken);
            }
            await Task.CompletedTask;
        };

        // Use TestEnvironment's unique API key to ensure parallel test isolation
        await env.ConnectToServer();
        
        // Poll for successful token refresh - wait up to 3 seconds
        // Refresh should occur ~1 second after connection
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline)
        {
            if (tokens.Count >= 2)
            {
                break;
            }
            await Task.Delay(100);
        }

        var mgrLogs = env.GetClientLogs<AccessTokenManager>();

        // Verify we got at least the initial token
        tokens.Should().NotBeEmpty("At least one token should have been received");
        
        // The test is specifically about token REFRESH - verify refresh occurred
        mgrLogs.Should().Contain(l => l.Message == "Successfully refreshed access token",
            "Token should have been successfully refreshed");
        
        // Verify we received at least 2 non-null tokens (initial + refresh)
        tokens.Count.Should().BeGreaterThanOrEqualTo(2, 
            $"Should have received at least 2 tokens (initial + refresh). " +
            $"Tokens received: {tokens.Count}. " +
            $"Logs: {string.Join(", ", mgrLogs.Select(l => l.Message))}");

        mgrLogs[0].Message.Should().Be("Setting access token - current access token is null");
    }

    [TestMethod]
    public async Task ExecuteRemoteCommand_ShouldHandleTamperedToken()
    {
        using var env = new TestEnvironment();

        var mgr = env.Cli.Services.GetRequiredService<AccessTokenManager>();
        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();

        await env.Cli.ConnectToServer(env.Server);

        var token = TestJwtTokenService.GenerateAccessToken();
        var tamperedToken = new AccessToken(token.Token + "_tampered", token.RefreshToken, token.RefreshRoute);

        proxy.ConnectionState.Should().Be(ServerProxyConnectionState.Connected);
        await mgr.SetAccessToken(tamperedToken, env.Server.BaseAddress.AbsoluteUri);
        proxy.ConnectionState.Should().Be(ServerProxyConnectionState.Disconnected);

        var proxyLogs = env.GetClientLogs<SignalRServerProxy>();

        proxyLogs[0].Message.Should().Be("OnAccessTokenChanged :: no active connection");
        proxyLogs[2].Message.Should().Be("OnAccessTokenChanged :: rebuilding connection");
        proxyLogs[3].Message.Should().Be("An error occured while reconnecting with refreshed access token");
    }
}
