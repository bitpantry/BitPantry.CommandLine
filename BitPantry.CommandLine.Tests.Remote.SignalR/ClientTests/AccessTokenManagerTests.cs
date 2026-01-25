using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using BitPantry.CommandLine.Tests.Infrastructure.Authentication;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class AccessTokenManagerTests
    {
        private Mock<ILogger<AccessTokenManager>> _loggerMock;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<AccessTokenManager>>();
        }

        [TestMethod]
        public async Task SetAccessToken_ShouldSetTokenAndServerUri()
        {
            // Arrange

            var token = TestJwtTokenService.GenerateAccessToken();
            var mgr = TestAccessTokenManager.Create(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)); // shouldn't send request to get unauthorized

            // Act

            await mgr.SetAccessToken(token, "http://localhost");

            // Assert

            mgr.CurrentToken.Should().Be(token);
        }

        [TestMethod]
        public async Task MonitorToken_ShouldRefreshToken_WhenTokenIsExpired()
        {
            // Arrange

            var expiringToken = TestJwtTokenService.GenerateAccessToken(TimeSpan.FromMilliseconds(1), TimeSpan.FromMinutes(1));
            var newToken = TestJwtTokenService.GenerateAccessToken();

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RefreshAccessTokenResponse
                (
                    newToken.Token,
                    newToken.RefreshToken
                ))
            };

            var mgr = TestAccessTokenManager.Create(response);

            var refreshEvtTcs = new TaskCompletionSource<bool>();

            mgr.OnAccessTokenChanged += async (sender, newToken) =>
            {
                refreshEvtTcs.SetResult(true);
                newToken.Should().NotBeNull();
                await Task.CompletedTask;
            };

            // Act

            await Task.Delay(10); // wait for token to expire
            await mgr.SetAccessToken(expiringToken, "http://localhost");
            var evtFlag = await refreshEvtTcs.Task;

            // Assert

            evtFlag.Should().BeTrue();
            mgr.CurrentToken.Token.Should().Be(newToken.Token);
            mgr.CurrentToken.RefreshToken.Should().Be(newToken.RefreshToken);
        }

        [TestMethod]
        public async Task MonitorToken_ShouldRefreshToken_WhenTokenIsAboutToExpire()
        {
            // Arrange

            var expiringToken = TestJwtTokenService.GenerateAccessToken(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            var newToken = TestJwtTokenService.GenerateAccessToken();

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RefreshAccessTokenResponse
                (
                    newToken.Token,
                    newToken.RefreshToken
                ))
            };

            var mgr = TestAccessTokenManager.Create(TimeSpan.FromMinutes(2), response);

            var refreshEvtTcs = new TaskCompletionSource<bool>();

            mgr.OnAccessTokenChanged += async (sender, newToken) =>
            {
                refreshEvtTcs.SetResult(true);
                newToken.Should().NotBeNull();
                await Task.CompletedTask;
            };

            // Act

            await mgr.SetAccessToken(expiringToken, "http://localhost");
            var evtFlag = await refreshEvtTcs.Task;

            // Assert

            evtFlag.Should().BeTrue();
            mgr.CurrentToken.Token.Should().Be(newToken.Token);
            mgr.CurrentToken.RefreshToken.Should().Be(newToken.RefreshToken);
        }

        [TestMethod]
        public async Task SetAccessToken_ShouldHandleNullToken()
        {
            // Arrange

            var tokenMgr = TestAccessTokenManager.Create(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            // Act

            Func<Task> func = async () => await tokenMgr.SetAccessToken(null, "http://localhost");

            // Assert

            await func.Should().NotThrowAsync<Exception>();
            tokenMgr.CurrentToken.Should().BeNull();
        }

        [TestMethod]
        public async Task MonitorToken_ShouldNotRefreshToken_WhenRefreshTokenIsExpired()
        {
            // Arrange

            var token = TestJwtTokenService.GenerateAccessToken(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)); // expired tokens

            var mgr = TestAccessTokenManager.Create(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var refreshEvtTcs = new TaskCompletionSource<bool>();

            mgr.OnAccessTokenChanged += async (sender, newToken) =>
            {
                newToken.Should().BeNull();
                refreshEvtTcs.SetResult(true);
                await Task.CompletedTask;
            };

            // Act

            await Task.Delay(50); // wait for tokens to expire
            await mgr.SetAccessToken(token, "http://test.com");


            var eventFlag = await refreshEvtTcs.Task;

            // Assert

            eventFlag.Should().BeTrue();
            mgr.CurrentToken.Should().BeNull();
        }
    }
}

