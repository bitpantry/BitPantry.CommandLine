using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Infrastructure.Authentication;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using BitPantry.CommandLine.Tests.Infrastructure.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for ConnectionService.
    /// Covers token endpoint auto-discovery and the ConnectWithAuth flow.
    /// </summary>
    [TestClass]
    public class ConnectionServiceTests
    {
        private Mock<IServerProxy> _proxyMock;
        private Mock<ILogger<ConnectionService>> _loggerMock;
        private ConnectionService _service;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;

        [TestInitialize]
        public void Setup()
        {
            _proxyMock = new Mock<IServerProxy>();
            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            _loggerMock = new Mock<ILogger<ConnectionService>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(HttpStatusCode.Unauthorized));

            _service = new ConnectionService(
                _loggerMock.Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);
        }

        #region DiscoverTokenEndpointAsync

        [TestMethod]
        public async Task DiscoverTokenEndpointAsync_ServerReturns401WithEndpoint_ReturnsEndpoint()
        {
            // Arrange — server returns 401 with token_request_endpoint in body
            // Bug: Currently no DiscoverTokenEndpointAsync method exists.
            // This test drives its creation.
            var responseBody = JsonSerializer.Serialize(new
            {
                error = "Unauthorized",
                message = "Token is missing",
                token_request_endpoint = "/cli-auth/token-request",
                token_format = "Bearer {JWT}"
            });

            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
            };

            _httpClientFactoryMock.Setup(f => f.CreateClient())
                .Returns(TestHttpClient.Create(response));

            // Act
            var endpoint = await _service.DiscoverTokenEndpointAsync("http://localhost:5115/cli");

            // Assert
            endpoint.Should().Be("/cli-auth/token-request");
        }

        [TestMethod]
        public async Task DiscoverTokenEndpointAsync_ServerReturns401WithoutEndpoint_ReturnsNull()
        {
            // Arrange — server returns 401 but body has no token_request_endpoint
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"Unauthorized\"}", System.Text.Encoding.UTF8, "application/json")
            };

            _httpClientFactoryMock.Setup(f => f.CreateClient())
                .Returns(TestHttpClient.Create(response));

            // Act
            var endpoint = await _service.DiscoverTokenEndpointAsync("http://localhost:5115/cli");

            // Assert
            endpoint.Should().BeNull();
        }

        [TestMethod]
        public async Task DiscoverTokenEndpointAsync_ServerReturns200_ReturnsNull()
        {
            // Arrange — server doesn't require auth (returns 200)
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };

            _httpClientFactoryMock.Setup(f => f.CreateClient())
                .Returns(TestHttpClient.Create(response));

            // Act
            var endpoint = await _service.DiscoverTokenEndpointAsync("http://localhost:5115/cli");

            // Assert — no auth required, so no endpoint to discover
            endpoint.Should().BeNull();
        }

        #endregion

        #region ConnectWithAuthAsync — Auto-Discovery

        [TestMethod]
        public async Task ConnectWithAuthAsync_ProfileApiKeyNoExplicitEndpoint_DiscoversAndConnects()
        {
            // Arrange — proxy returns 401 on first connect (SignalR doesn't preserve response body),
            // then discovery probe finds the endpoint, token is acquired, second connect succeeds.
            //
            // Bug: Currently ConnectWithAuthAsync relies on ExtractTokenEndpoint(ex) which
            // fails because SignalR doesn't preserve ex.Data["responseBody"].
            // This test drives the fix to use DiscoverTokenEndpointAsync as fallback.

            var connectAttempt = 0;
            _proxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    connectAttempt++;
                    if (connectAttempt == 1)
                    {
                        // First attempt: 401 WITHOUT response body in Data (simulates SignalR behavior)
                        throw new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);
                    }
                    return Task.CompletedTask; // Second attempt succeeds (token acquired)
                });

            // Discovery probe returns 401 with token endpoint
            var discoveryResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        error = "Unauthorized",
                        message = "Token is missing",
                        token_request_endpoint = "/cli-auth/token-request",
                        token_format = "Bearer {JWT}"
                    }),
                    System.Text.Encoding.UTF8, "application/json")
            };

            // Token acquisition response
            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        accessToken = "test-jwt-token",
                        refreshToken = "test-refresh-token",
                        refreshRoute = "/cli-auth/token-refresh"
                    }),
                    System.Text.Encoding.UTF8, "application/json")
            };

            // HTTP client factory returns clients for discovery + token acquisition
            var callIndex = 0;
            _httpClientFactoryMock.Setup(f => f.CreateClient())
                .Returns(() =>
                {
                    callIndex++;
                    if (callIndex == 1)
                        return TestHttpClient.Create(discoveryResponse);
                    return TestHttpClient.Create(tokenResponse);
                });

            // Need a ConnectionService with a real AccessTokenManager that accepts tokens
            var accessTokenManager = TestAccessTokenManager.Create(tokenResponse);
            var service = new ConnectionService(
                _loggerMock.Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            // Act — should not throw
            await service.ConnectWithAuthAsync(_proxyMock.Object, "http://localhost:5115/cli", "my-api-key");

            // Assert — proxy.Connect called twice: first 401, then success after token acquisition
            _proxyMock.Verify(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task ConnectWithAuthAsync_NoApiKey_401_Throws()
        {
            // Arrange — no API key, proxy returns 401
            _proxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized));

            // Act & Assert — should re-throw for caller to handle (e.g., interactive prompt)
            var act = () => _service.ConnectWithAuthAsync(_proxyMock.Object, "http://localhost:5115/cli", null);
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [TestMethod]
        public async Task ConnectWithAuthAsync_ApiKeyButNoEndpointDiscovered_ThrowsInvalidOperation()
        {
            // Arrange — proxy returns 401, discovery probe also fails to find endpoint
            _proxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized));

            // Discovery returns 401 but with no token_request_endpoint
            var discoveryResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"Unauthorized\"}", System.Text.Encoding.UTF8, "application/json")
            };

            _httpClientFactoryMock.Setup(f => f.CreateClient())
                .Returns(TestHttpClient.Create(discoveryResponse));

            // Act & Assert
            var act = () => _service.ConnectWithAuthAsync(_proxyMock.Object, "http://localhost:5115/cli", "my-api-key");
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*token endpoint*");
        }

        #endregion
    }
}
