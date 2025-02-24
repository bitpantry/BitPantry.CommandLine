using Moq;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests.Auth
{
    [TestClass]
    public class ApiKeyStoreTests
    {
        private Mock<IApiKeyStore> _mockApiKeyStore;

        [TestInitialize]
        public void Setup()
        {
            _mockApiKeyStore = new Mock<IApiKeyStore>();
        }


        [TestMethod]
        public async Task TryGetUserIdByApiKey_ShouldReturnTrue_WhenApiKeyExists()
        {
            // Arrange
            var apiKey = "existing-api-key";
            var clientId = "client-id";
            _mockApiKeyStore.Setup(store => store.TryGetClientIdByApiKey(apiKey, out clientId)).ReturnsAsync(true);

            // Act
            var result = await _mockApiKeyStore.Object.TryGetClientIdByApiKey(apiKey, out var returnedClientId);

            // Assert
            result.Should().BeTrue();
            returnedClientId.Should().Be(clientId);
        }

        [TestMethod]
        public async Task TryGetUserIdByApiKey_ShouldReturnFalse_WhenApiKeyDoesNotExist()
        {
            // Arrange
            var apiKey = "non-existing-api-key";
            string clientId = null;
            _mockApiKeyStore.Setup(store => store.TryGetClientIdByApiKey(apiKey, out clientId)).ReturnsAsync(false);

            // Act
            var result = await _mockApiKeyStore.Object.TryGetClientIdByApiKey(apiKey, out var returnedClientId);

            // Assert
            result.Should().BeFalse();
            returnedClientId.Should().BeNull();
        }


        [TestMethod]
        public async Task TryGetUserIdByApiKey_ShouldThrowException_WhenApiKeyIsNull()
        {
            // Arrange
            string clientId;
            _mockApiKeyStore.Setup(store => store.TryGetClientIdByApiKey(null, out clientId)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await _mockApiKeyStore.Object.Invoking(store => store.TryGetClientIdByApiKey(null, out clientId))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task TryGetUserIdByApiKey_ShouldThrowException_WhenApiKeyIsEmpty()
        {
            // Arrange
            string clientId;
            _mockApiKeyStore.Setup(store => store.TryGetClientIdByApiKey(string.Empty, out clientId)).ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await _mockApiKeyStore.Object.Invoking(store => store.TryGetClientIdByApiKey(string.Empty, out clientId))
                .Should().ThrowAsync<ArgumentNullException>();
        }


        [TestMethod]
        public async Task TryGetUserIdByApiKey_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var apiKey = "existing-api-key";
            var clientId = "client-id";
            _mockApiKeyStore.Setup(store => store.TryGetClientIdByApiKey(apiKey, out clientId)).ReturnsAsync(true);
            var tasks = new List<Task<bool>>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_mockApiKeyStore.Object.TryGetClientIdByApiKey(apiKey, out var returnedClientId));
            }
            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllBeEquivalentTo(true);
        }

        [TestMethod]
        public async Task TryGetUserIdByApiKey_ShouldReturnFalse_ForRevokedKeys()
        {
            // Arrange
            var apiKey = "revoked-api-key";
            string clientId = null;
            _mockApiKeyStore.Setup(store => store.TryGetClientIdByApiKey(apiKey, out clientId)).ReturnsAsync(false);

            // Act
            var result = await _mockApiKeyStore.Object.TryGetClientIdByApiKey(apiKey, out var returnedClientId);

            // Assert
            result.Should().BeFalse();
            returnedClientId.Should().BeNull();
        }
    }
}
