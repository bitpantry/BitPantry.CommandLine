using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class CredentialStoreTests
    {
        private string _testDirectory = null!;
        private string _credentialsFilePath = null!;
        private Mock<ILogger<CredentialStore>> _loggerMock = null!;
        private CredentialStore _credentialStore = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"CredentialStoreTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _credentialsFilePath = Path.Combine(_testDirectory, "credentials.enc");
            _loggerMock = new Mock<ILogger<CredentialStore>>();
            _credentialStore = new CredentialStore(_loggerMock.Object, _credentialsFilePath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        [TestMethod]
        public async Task StoreAsync_ShouldStoreCredential()
        {
            // Arrange
            var profileName = "test-profile";
            var apiKey = "test-api-key-12345";

            // Act
            await _credentialStore.StoreAsync(profileName, apiKey);

            // Assert
            var exists = await _credentialStore.ExistsAsync(profileName);
            exists.Should().BeTrue();
        }

        [TestMethod]
        public async Task RetrieveAsync_ShouldReturnStoredCredential()
        {
            // Arrange
            var profileName = "test-profile";
            var apiKey = "test-api-key-12345";
            await _credentialStore.StoreAsync(profileName, apiKey);

            // Act
            var retrieved = await _credentialStore.RetrieveAsync(profileName);

            // Assert
            retrieved.Should().Be(apiKey);
        }

        [TestMethod]
        public async Task RetrieveAsync_ShouldReturnNull_WhenProfileNotFound()
        {
            // Act
            var retrieved = await _credentialStore.RetrieveAsync("nonexistent");

            // Assert
            retrieved.Should().BeNull();
        }

        [TestMethod]
        public async Task RemoveAsync_ShouldRemoveCredential()
        {
            // Arrange
            var profileName = "test-profile";
            var apiKey = "test-api-key-12345";
            await _credentialStore.StoreAsync(profileName, apiKey);

            // Act
            var removed = await _credentialStore.RemoveAsync(profileName);

            // Assert
            removed.Should().BeTrue();
            var exists = await _credentialStore.ExistsAsync(profileName);
            exists.Should().BeFalse();
        }

        [TestMethod]
        public async Task RemoveAsync_ShouldReturnFalse_WhenProfileNotFound()
        {
            // Act
            var removed = await _credentialStore.RemoveAsync("nonexistent");

            // Assert
            removed.Should().BeFalse();
        }

        [TestMethod]
        public async Task ExistsAsync_ShouldReturnFalse_WhenNoCredentials()
        {
            // Act
            var exists = await _credentialStore.ExistsAsync("test-profile");

            // Assert
            exists.Should().BeFalse();
        }

        [TestMethod]
        public async Task StoreAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            var apiKey = "test-api-key";
            await _credentialStore.StoreAsync("TestProfile", apiKey);

            // Act & Assert
            (await _credentialStore.ExistsAsync("testprofile")).Should().BeTrue();
            (await _credentialStore.ExistsAsync("TESTPROFILE")).Should().BeTrue();
            (await _credentialStore.RetrieveAsync("testprofile")).Should().Be(apiKey);
        }

        [TestMethod]
        public async Task StoreAsync_ShouldOverwriteExisting()
        {
            // Arrange
            var profileName = "test-profile";
            await _credentialStore.StoreAsync(profileName, "old-key");
            
            // Act
            await _credentialStore.StoreAsync(profileName, "new-key");

            // Assert
            var retrieved = await _credentialStore.RetrieveAsync(profileName);
            retrieved.Should().Be("new-key");
        }

        [TestMethod]
        public async Task StoreAsync_ShouldThrow_WhenProfileNameEmpty()
        {
            // Act
            var act = () => _credentialStore.StoreAsync("", "api-key");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task StoreAsync_ShouldThrow_WhenApiKeyEmpty()
        {
            // Act
            var act = () => _credentialStore.StoreAsync("profile", "");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
