using BitPantry.CommandLine.Remote.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using BitPantry.CommandLine.Tests.Infrastructure.Authentication;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Tests.Infrastructure;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

[TestClass]
public class IntegrationTests_Authentication
{
    [TestMethod]
    public async Task ConnectClient_ClientConnected()
    {
        using var env = TestEnvironment.WithServer();

        await env.Cli.ConnectToServer(env.Server);

        env.Cli.Services.GetRequiredService<IServerProxy>().ConnectionState.Should().Be(ServerProxyConnectionState.Connected);
    }

    [TestMethod]
    public async Task ConnectClient_BadApiKey_ClientConnected()
    {
        using var env = TestEnvironment.WithServer();
        await env.Cli.ConnectToServer(server: env.Server, apiKey: "badKey");

        env.Console.Lines[0].Should().StartWith("Requesting token with API key is unathorized");
    }

    [TestMethod]
    public async Task RefreshTokenOnExpiration_TokenRefreshes()
    {
        using var env = TestEnvironment.WithServer(svr =>
        {
            svr.AccessTokenLifetime = TimeSpan.FromSeconds(2);
            svr.TokenRefreshMonitorInterval = TimeSpan.FromMilliseconds(200);
            svr.TokenRefreshThreshold = TimeSpan.FromMilliseconds(2200);
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
                refreshEvtTcs.TrySetResult(true);
            }

            await Task.CompletedTask;
        };

        await env.Cli.ConnectToServer(env.Server);
        
        // Wait for token refresh with timeout
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(refreshEvtTcs.Task, timeoutTask);
        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("Token refresh did not occur within 10 seconds.");
        }

        var mgrLogs = env.GetClientLogs<AccessTokenManager>();

        originalToken.Should().NotBeNull();
        refreshedToken.Should().NotBeNull();

        mgrLogs[0].Message.Should().Be("Setting access token - current access token is null");
        mgrLogs[1].Message.Should().Be("Successfully refreshed access token");
    }

    [TestMethod]
    public async Task ExecuteRemoteCommand_ShouldHandleTamperedToken()
    {
        using var env = TestEnvironment.WithServer();

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

