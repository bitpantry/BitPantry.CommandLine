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

        await env.ConnectToServerAsync();

        env.Cli.Services.GetRequiredService<IServerProxy>().ConnectionState.Should().Be(ServerProxyConnectionState.Connected);
    }

    [TestMethod]
    public async Task ConnectClient_BadApiKey_ClientConnected()
    {
        using var env = TestEnvironment.WithServer();
        await env.ConnectToServerAsync(apiKey: "badKey");

        // Wait a bit for output to appear, then check that the output contains the expected message
        // Join all lines to handle text wrapping in the virtual console
        await Task.Delay(100);
        var output = string.Join("", env.Console.Lines).Replace(" ", "");
        output.Should().Contain("RequestingtokenwithAPIkeyisunauthorized".Replace(" ", ""));
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

        await env.ConnectToServerAsync();
        
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

        await env.ConnectToServerAsync();

        var token = TestJwtTokenService.GenerateAccessToken();
        var tamperedToken = new AccessToken(token.Token + "_tampered", token.RefreshToken, token.RefreshRoute);

        proxy.ConnectionState.Should().Be(ServerProxyConnectionState.Connected);
        await mgr.SetAccessToken(tamperedToken, env.Server.BaseAddress.AbsoluteUri);
        proxy.ConnectionState.Should().Be(ServerProxyConnectionState.Disconnected);

        var proxyLogs = env.GetClientLogs<SignalRServerProxy>();

        proxyLogs[0].Message.Should().Be("OnAccessTokenChanged :: no active connection");
        proxyLogs[2].Message.Should().Be("OnAccessTokenChanged :: rebuilding connection");
        proxyLogs[3].Message.Should().StartWith("An error occured while reconnecting with refreshed access token");
    }
}

