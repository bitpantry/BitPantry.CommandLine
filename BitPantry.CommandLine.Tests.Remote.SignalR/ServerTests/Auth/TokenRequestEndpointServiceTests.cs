using Moq;
using FluentAssertions;
using System.Security.Claims;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests.Auth
{
    [TestClass]
    public class TokenRequestEndpointServiceTests
    {
        private Mock<ITokenService> _mockTokenService;
        private Mock<ApiKeyService> _mockApiKeyService;
        private TokenRequestEndpointService _service;

        [TestInitialize]
        public void Setup()
        {
            _mockTokenService = new Mock<ITokenService>();
            _mockApiKeyService = new Mock<ApiKeyService>(Mock.Of<IApiKeyStore>());
            _service = new TokenRequestEndpointService(_mockTokenService.Object, _mockApiKeyService.Object);
        }

        [TestMethod]
        public async Task HandleTokenRequest_ShouldReturnBadRequest_WhenApiKeyIsNull()
        {
            // Arrange
            var request = new TokenRequestModel { ApiKey = null };

            // Act
            var result = await _service.HandleTokenRequest(request, "/refresh");

            // Assert
            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>()
                .Which.Value.Should().Be("API key is required.");
        }

        [TestMethod]
        public async Task HandleTokenRequest_ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
        {
            // Arrange
            var request = new TokenRequestModel { ApiKey = "invalid-api-key" };
            _mockApiKeyService.Setup(svc => svc.ValidateKey(request.ApiKey, out It.Ref<string>.IsAny)).ReturnsAsync(false);

            // Act
            var result = await _service.HandleTokenRequest(request, "/refresh");

            // Assert
            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
        }

        [TestMethod]
        public async Task HandleTokenRequest_ShouldReturnOk_WhenApiKeyIsValid()
        {
            // Arrange
            var request = new TokenRequestModel { ApiKey = "valid-api-key" };
            var clientId = "client-id";
            _mockApiKeyService.Setup(svc => svc.ValidateKey(request.ApiKey, out clientId)).ReturnsAsync(true);
            _mockTokenService.Setup(svc => svc.GenerateAccessToken(clientId, null)).Returns("access-token");
            _mockTokenService.Setup(svc => svc.GenerateRefreshToken(clientId)).ReturnsAsync("refresh-token");

            // Act
            var result = await _service.HandleTokenRequest(request, "/refresh");

            // Assert
            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<TokenResponseModel>>()
                .Which.Value.Should().BeEquivalentTo(new TokenResponseModel("access-token", "refresh-token", "/refresh"));
        }

        [TestMethod]
        public async Task HandleTokenRefreshRequest_ShouldReturnBadRequest_WhenRefreshTokenIsNull()
        {
            // Arrange
            var request = new TokenRefreshRequestModel { RefreshToken = null };

            // Act
            var result = await _service.HandleTokenRefreshRequest(request);

            // Assert
            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>()
                .Which.Value.Should().Be("Refresh token is required.");
        }

        [TestMethod]
        public async Task HandleTokenRefreshRequest_ShouldReturnUnauthorized_WhenRefreshTokenIsInvalid()
        {
            // Arrange
            var request = new TokenRefreshRequestModel { RefreshToken = "invalid-refresh-token" };
            _mockTokenService.Setup(svc => svc.ValidateToken(request.RefreshToken)).ReturnsAsync(Tuple.Create(false, (ClaimsPrincipal)null));

            // Act
            var result = await _service.HandleTokenRefreshRequest(request);

            // Assert
            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
        }

        [TestMethod]
        public async Task HandleTokenRefreshRequest_ShouldReturnOk_WhenRefreshTokenIsValid()
        {
            // Arrange
            var request = new TokenRefreshRequestModel { RefreshToken = "valid-refresh-token" };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "client-id") }));
            _mockTokenService.Setup(svc => svc.ValidateToken(request.RefreshToken)).ReturnsAsync(Tuple.Create(true, claimsPrincipal));
            _mockTokenService.Setup(svc => svc.GenerateAccessToken("client-id", null)).Returns("new-access-token");
            _mockTokenService.Setup(svc => svc.GenerateRefreshToken("client-id")).ReturnsAsync("new-refresh-token");

            // Act
            var result = await _service.HandleTokenRefreshRequest(request);

            // Assert
            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<TokenResponseModel>>()
                .Which.Value.Should().BeEquivalentTo(new TokenResponseModel("new-access-token", "valid-refresh-token", null));
        }

        [TestMethod]
        public async Task HandleTokenRefreshRequest_ShouldReturnUnauthorized_WhenRefreshTokenIsExpired()
        {
            // Arrange
            var request = new TokenRefreshRequestModel { RefreshToken = "expired-refresh-token" };
            _mockTokenService.Setup(svc => svc.ValidateToken(request.RefreshToken)).ReturnsAsync(Tuple.Create(false, (ClaimsPrincipal)null));

            // Act
            var result = await _service.HandleTokenRefreshRequest(request);

            // Assert
            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
        }
    }
}
