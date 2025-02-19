using Moq;
using BitPantry.CommandLine.Remote.SignalR.Server.Auth;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests.Auth
{
    [TestClass]
    public class JwtServiceTests
    {
        [TestMethod]
        public void GenerateAccessToken_ShouldReturnValidToken()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            var token = service.GenerateAccessToken("client-id");

            // Assert
            token.Should().NotBeNull();
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            jwtToken.Subject.Should().Be("client-id");
        }

        [TestMethod]
        public async Task GenerateRefreshToken_ShouldStoreToken()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            var refreshToken = await service.GenerateRefreshToken("client-id");

            // Assert
            refreshToken.Should().NotBeNull();
            mockRefreshTokenStore.Verify(store => store.StoreRefreshTokenAsync("client-id", refreshToken), Times.Once);
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnTrue_WhenTokenIsValid()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var validToken = service.GenerateAccessToken("client-id");

            // Act
            var result = await service.ValidateToken(validToken);

            // Assert
            result.Item1.Should().BeTrue();
            result.Item2.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenTokenIsInvalid()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            var result = await service.ValidateToken("invalid-token");

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public async Task RevokeRefreshTokenAsync_ShouldRevokeToken()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            await service.RevokeRefreshTokenAsync("client-id");

            // Assert
            mockRefreshTokenStore.Verify(store => store.RevokeRefreshTokenAsync("client-id"), Times.Once);
        }

        [TestMethod]
        public async Task RotateRefreshTokenAsync_ShouldRevokeAndGenerateNewToken()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            var newRefreshToken = await service.RotateRefreshTokenAsync("client-id");

            // Assert
            newRefreshToken.Should().NotBeNull();
            mockRefreshTokenStore.Verify(store => store.RevokeRefreshTokenAsync("client-id"), Times.Once);
            mockRefreshTokenStore.Verify(store => store.StoreRefreshTokenAsync("client-id", newRefreshToken), Times.Once);
        }

        [TestMethod]
        public void GenerateAccessToken_ShouldThrowException_WhenClientIdIsNull()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            Action act = () => service.GenerateAccessToken(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void GenerateAccessToken_ShouldThrowException_WhenClientIdIsEmpty()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            Action act = () => service.GenerateAccessToken(string.Empty);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task GenerateRefreshToken_ShouldThrowException_WhenClientIdIsNull()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            Func<Task> act = async () => await service.GenerateRefreshToken(null);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task GenerateRefreshToken_ShouldThrowException_WhenClientIdIsEmpty()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            Func<Task> act = async () => await service.GenerateRefreshToken(string.Empty);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenTokenIsNull()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            var result = await service.ValidateToken(null);

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenTokenIsEmpty()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            var result = await service.ValidateToken(string.Empty);

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenTokenIsExpired()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var expiredToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                issuer: tokenAuthSettings.Issuer,
                audience: tokenAuthSettings.Audience,
                expires: DateTime.UtcNow.AddHours(-1),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenAuthSettings.Key)), SecurityAlgorithms.HmacSha256)
            ));

            // Act
            var result = await service.ValidateToken(expiredToken);

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public async Task RevokeRefreshTokenAsync_ShouldThrowException_WhenClientIdIsNull()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            Func<Task> act = async () => await service.RevokeRefreshTokenAsync(null);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task RevokeRefreshTokenAsync_ShouldThrowException_WhenClientIdIsEmpty()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            Func<Task> act = async () => await service.RevokeRefreshTokenAsync(string.Empty);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task RotateRefreshTokenAsync_ShouldThrowException_WhenClientIdIsNull()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            Func<Task> act = async () => await service.RotateRefreshTokenAsync(null);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task RotateRefreshTokenAsync_ShouldThrowException_WhenClientIdIsEmpty()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience"); var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            // Act
            Func<Task> act = async () => await service.RotateRefreshTokenAsync(string.Empty);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }


        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenAccessTokenIsExpired()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var accessToken = service.GenerateAccessToken("client-id");

            // Wait for the token to expire
            await Task.Delay(1100);

            // Act
            var result = await service.ValidateToken(accessToken);

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenRefreshTokenIsExpired()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var refreshToken = await service.GenerateRefreshToken("client-id");

            // Wait for the token to expire
            await Task.Delay(1100);

            // Act
            var result = await service.ValidateToken(refreshToken);

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnTrue_WhenRefreshTokenIsValid()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var refreshToken = await service.GenerateRefreshToken("client-id");

            // Mock the refresh token store to return the generated refresh token
            mockRefreshTokenStore.Setup(store => store.TryGetRefreshTokenAsync("client-id", out refreshToken)).ReturnsAsync(true);

            // Act
            var result = await service.ValidateToken(refreshToken);

            // Assert
            result.Item1.Should().BeTrue();
            result.Item2.Should().NotBeNull();
            result.Item2.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("client-id");
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenTokenHasInvalidSignature()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("invalidkey-somereallylongstringthatmeetsthe128byterequirement"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: tokenAuthSettings.Issuer,
                audience: tokenAuthSettings.Audience,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var invalidToken = tokenHandler.WriteToken(token);

            // Act
            var result = await service.ValidateToken(invalidToken);

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenTokenHasIncorrectIssuer()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenAuthSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "incorrectIssuer",
                audience: tokenAuthSettings.Audience,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var invalidToken = tokenHandler.WriteToken(token);

            // Act
            var result = await service.ValidateToken(invalidToken);

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenTokenHasIncorrectAudience()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenAuthSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: tokenAuthSettings.Issuer,
                audience: "incorrectAudience",
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var invalidToken = tokenHandler.WriteToken(token);

            // Act
            var result = await service.ValidateToken(invalidToken);

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public async Task ValidateToken_ShouldReturnFalse_WhenRefreshTokenDoesNotMatchStoredToken()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var refreshToken = await service.GenerateRefreshToken("client-id");

            // Mock the refresh token store to return a different token
            mockRefreshTokenStore.Setup(store => store.TryGetRefreshTokenAsync("client-id", out refreshToken)).ReturnsAsync(false);

            // Act
            var result = await service.ValidateToken(refreshToken);

            // Assert
            result.Item1.Should().BeFalse();
            result.Item2.Should().BeNull();
        }

        [TestMethod]
        public void GenerateAccessToken_ShouldIncludeCustomClaims()
        {
            // Arrange
            var mockRefreshTokenStore = new Mock<IRefreshTokenStore>();
            var mockLogger = new Mock<ILogger<JwtTokenService>>();
            var tokenAuthSettings = new TokenAuthenticationSettings(
                "somereallylongstringthatmeetsthe128byterequirement",
                "/token",
                "/refresh",
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(30),
                TimeSpan.Zero,
                "issuer",
                "audience");
            var service = new JwtTokenService(mockLogger.Object, tokenAuthSettings, mockRefreshTokenStore.Object);

            var customClaims = new List<Claim> { new Claim("customClaimType", "customClaimValue") };

            // Act
            var token = service.GenerateAccessToken("client-id", customClaims);

            // Assert
            token.Should().NotBeNull();
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            jwtToken.Subject.Should().Be("client-id");
            jwtToken.Claims.Should().Contain(c => c.Type == "customClaimType" && c.Value == "customClaimValue");
        }


    }
}
