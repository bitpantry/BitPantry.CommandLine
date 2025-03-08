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

        _ = await LongRunningCommand.Tcs.Task; // wait for long running task to be running

        await env.Cli.Services.GetRequiredService<AccessTokenManager>().SetAccessToken(token, env.Server.BaseAddress.AbsoluteUri);
        await lrcTask;

        var lrcLogger = env.Server.Services.GetRequiredService<ILogger<LongRunningCommand>>() as TestLogger<LongRunningCommand>;
        var proxyLogger = env.Cli.Services.GetRequiredService<ILogger<SignalRServerProxy>>() as TestLogger<SignalRServerProxy>;

        lrcLogger.LoggedMessages[0].Message.Should().Be("Long running command finished");
        proxyLogger.LoggedMessages[1].Message.Should().Be("OnAccessTokenChanged :: rebuilding connection");
        proxyLogger.LoggedMessages[1].Timestamp.Subtract(lrcLogger.LoggedMessages[0].Timestamp).Should().BeGreaterThan(TimeSpan.Zero);
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

        var mgrLogger = env.Cli.Services.GetRequiredService<ILogger<AccessTokenManager>>() as TestLogger<AccessTokenManager>;

        originalToken.Should().NotBeNull();
        refreshedToken.Should().NotBeNull();

        mgrLogger.LoggedMessages[0].Message.Should().Be("Setting access token - current access token is null");
        mgrLogger.LoggedMessages[1].Message.Should().Be("Successfully refreshed access token");
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

        var proxyLogger = env.Cli.Services.GetRequiredService<ILogger<SignalRServerProxy>>() as TestLogger<SignalRServerProxy>;

        proxyLogger.LoggedMessages[0].Message.Should().Be("OnAccessTokenChanged :: no active connection");
        proxyLogger.LoggedMessages[1].Message.Should().Be("OnAccessTokenChanged :: rebuilding connection");
        proxyLogger.LoggedMessages[2].Message.Should().Be("An error occured while reconnecting with refreshed access token");
    }
}
