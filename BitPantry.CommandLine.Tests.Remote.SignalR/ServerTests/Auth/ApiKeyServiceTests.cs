using Moq;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests.Auth
{
    [TestClass]
    public class ApiKeyServiceTests
    {
        private Mock<IApiKeyStore> _mockApiKeyStore;
        private ApiKeyService _apiKeyService;

        [TestInitialize]
        public void Setup()
        {
            _mockApiKeyStore = new Mock<IApiKeyStore>();
            _apiKeyService = new ApiKeyService(_mockApiKeyStore.Object);
        }

        [TestMethod]
        public async Task ValidateKey_ShouldReturnTrue_WhenApiKeyIsValid()
        {
            // Arrange
            var apiKey = "valid-api-key";
            var clientId = "client-id";
            _mockApiKeyStore.Setup(store => store.TryGetUserIdByApiKey(apiKey, out clientId)).ReturnsAsync(true);

            // Act
            var result = await _apiKeyService.ValidateKey(apiKey, out var returnedClientId);

            // Assert
            result.Should().BeTrue();
            returnedClientId.Should().Be(clientId);
        }

        [TestMethod]
        public async Task ValidateKey_ShouldReturnFalse_WhenApiKeyIsInvalid()
        {
            // Arrange
            var apiKey = "invalid-api-key";
            string clientId = null;
            _mockApiKeyStore.Setup(store => store.TryGetUserIdByApiKey(apiKey, out clientId)).ReturnsAsync(false);

            // Act
            var result = await _apiKeyService.ValidateKey(apiKey, out var returnedClientId);

            // Assert
            result.Should().BeFalse();
            returnedClientId.Should().BeNull();
        }

        [TestMethod]
        public async Task ValidateKey_ShouldThrowException_WhenApiKeyIsNull()
        {
            // Arrange
            string clientId;
            _mockApiKeyStore.Setup(store => store.TryGetUserIdByApiKey(null, out clientId)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await _apiKeyService.Invoking(service => service.ValidateKey(null, out clientId))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ValidateKey_ShouldThrowException_WhenApiKeyIsEmpty()
        {
            // Arrange
            string clientId;
            _mockApiKeyStore.Setup(store => store.TryGetUserIdByApiKey(string.Empty, out clientId)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await _apiKeyService.Invoking(service => service.ValidateKey(string.Empty, out clientId))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ValidateKey_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var apiKey = "valid-api-key";
            var clientId = "client-id";
            _mockApiKeyStore.Setup(store => store.TryGetUserIdByApiKey(apiKey, out clientId)).ReturnsAsync(true);
            var tasks = new List<Task<bool>>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_apiKeyService.ValidateKey(apiKey, out var returnedClientId));
            }
            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllBeEquivalentTo(true);
        }

        [TestMethod]
        public async Task ValidateKey_ShouldReturnFalse_ForRevokedKeys()
        {
            // Arrange
            var apiKey = "revoked-api-key";
            string clientId = null;
            _mockApiKeyStore.Setup(store => store.TryGetUserIdByApiKey(apiKey, out clientId)).ReturnsAsync(false);

            // Act
            var result = await _apiKeyService.ValidateKey(apiKey, out var returnedClientId);

            // Assert
            result.Should().BeFalse();
            returnedClientId.Should().BeNull();
        }
    }
}
