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
        var env = new TestEnvironment();

        await env.Cli.ConnectToServer(env.Server);

        env.Cli.Services.GetRequiredService<IServerProxy>().ConnectionState.Should().Be(ServerProxyConnectionState.Connected);
    }

    [TestMethod]
    public async Task ConnectClient_BadApiKey_ClientConnected()
    {
        var env = new TestEnvironment();
        await env.Cli.ConnectToServer(server: env.Server, apiKey: "badKey");

        Console.WriteLine(string.Concat(env.Console.Lines));

        env.Console.Lines[1].Should().StartWith("Requesting token with API key is unathorized");
    }

    [TestMethod]
    public async Task RefreshTokenDuringExecution_ExecutionCompletesFirst()
    {
        var env = new TestEnvironment();
        var token = TestJwtTokenService.GenerateAccessToken();

        await env.Cli.ConnectToServer(env.Server);

        var lrcTask = env.Cli.Run("test.lrc"); // start long running command
        await LongRunningCommand.Tcs.Task; // wait for long running task to be running

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
        var env = new TestEnvironment(opts =>
        {
            opts.AccessTokenLifetime = TimeSpan.FromSeconds(2);
            opts.TokenRefreshMonitorInterval = TimeSpan.FromMilliseconds(200);
            opts.TokenRefreshThreshold = TimeSpan.FromMilliseconds(2200);
        });

        var mgr = env.Cli.Services.GetRequiredService<AccessTokenManager>();

        var refreshEvtTcs = new TaskCompletionSource<bool>();

        AccessToken originalToken = null;
        AccessToken refreshedToken = null;
        mgr.OnAccessTokenChanged += async (sender, newToken) =>
        {
            if (originalToken == null)
            {
                originalToken = newToken;
            }
            else if (refreshedToken == null)
            {
                refreshedToken = newToken;
                refreshEvtTcs.SetResult(true);
            }

            await Task.CompletedTask;
        };

        await env.Cli.ConnectToServer(env.Server);
        await refreshEvtTcs.Task;

        var mgrLogs = env.GetClientLogs<AccessTokenManager>();

        originalToken.Should().NotBeNull();
        refreshedToken.Should().NotBeNull();

        mgrLogs[0].Message.Should().Be("Setting access token - current access token is null");
        mgrLogs[1].Message.Should().Be("Successfully refreshed access token");
    }

    [TestMethod]
    public async Task ExecuteRemoteCommand_ShouldHandleTamperedToken()
    {
        var env = new TestEnvironment();

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
