using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using System.Text;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for CredentialStore - secure credential storage.
    /// Tests cover Windows DPAPI encryption actions.
    /// </summary>
    [TestClass]
    public class CredentialStoreTests
    {
        private MockFileSystem _fileSystem;
        private string _storagePath;

        [TestInitialize]
        public void Setup()
        {
            _fileSystem = new MockFileSystem();
            _storagePath = @"C:\Users\TestUser\.bitpantry\commandline\profiles";
            _fileSystem.Directory.CreateDirectory(_storagePath);
        }

        #region Windows DPAPI Tests

        [TestMethod]
        public async Task Store_ValidApiKey_EncryptsWithDPAPI()
        {
            // Arrange
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);
            var profileName = "production";
            var apiKey = "test-api-key-12345";

            // Act
            await credentialStore.StoreAsync(profileName, apiKey);

            // Assert - credential file should exist
            var credentialFile = Path.Combine(_storagePath, "credentials.enc");
            _fileSystem.File.Exists(credentialFile).Should().BeTrue("credential file should be created");

            // Assert - stored data should NOT contain plaintext API key (it should be encrypted)
            var storedBytes = _fileSystem.File.ReadAllBytes(credentialFile);
            var storedAsString = Encoding.UTF8.GetString(storedBytes);
            storedAsString.Should().NotContain(apiKey, "API key should be encrypted, not stored in plaintext");
        }

        [TestMethod]
        public async Task Retrieve_StoredKey_DecryptsCorrectly()
        {
            // Arrange
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);
            var profileName = "production";
            var apiKey = "test-api-key-12345";
            await credentialStore.StoreAsync(profileName, apiKey);

            // Act
            var retrievedKey = await credentialStore.RetrieveAsync(profileName);

            // Assert
            retrievedKey.Should().Be(apiKey, "retrieved API key should match original");
        }

        [TestMethod]
        public async Task Retrieve_NonExistent_ReturnsNull()
        {
            // Arrange
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);

            // Act
            var retrievedKey = await credentialStore.RetrieveAsync("nonexistent-profile");

            // Assert
            retrievedKey.Should().BeNull("non-existent credential should return null");
        }

        [TestMethod]
        public async Task Remove_ExistingCredential_RemovesEntry()
        {
            // Arrange
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);
            var profileName = "production";
            var apiKey = "test-api-key-12345";
            await credentialStore.StoreAsync(profileName, apiKey);

            // Act
            await credentialStore.RemoveAsync(profileName);

            // Assert
            var retrievedKey = await credentialStore.RetrieveAsync(profileName);
            retrievedKey.Should().BeNull("credential should be removed");
        }

        [TestMethod]
        public async Task Exists_StoredCredential_ReturnsTrue()
        {
            // Arrange
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);
            var profileName = "production";
            var apiKey = "test-api-key-12345";
            await credentialStore.StoreAsync(profileName, apiKey);

            // Act
            var exists = await credentialStore.ExistsAsync(profileName);

            // Assert
            exists.Should().BeTrue("stored credential should exist");
        }

        [TestMethod]
        public async Task Exists_NonExistent_ReturnsFalse()
        {
            // Arrange
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);

            // Act
            var exists = await credentialStore.ExistsAsync("nonexistent-profile");

            // Assert
            exists.Should().BeFalse("non-existent credential should not exist");
        }

        #endregion

        #region Linux/macOS libsodium Tests

        [TestMethod]
        public async Task Store_ValidApiKey_EncryptsWithSecretBox()
        {
            // Arrange - Force libsodium encryption mode (works on Windows for testing)
            var credentialStore = new CredentialStore(_fileSystem, _storagePath, EncryptionProvider.Libsodium);
            var profileName = "production";
            var apiKey = "test-api-key-12345";

            // Act
            await credentialStore.StoreAsync(profileName, apiKey);

            // Assert - credential file should exist
            var credentialFile = Path.Combine(_storagePath, "credentials.enc");
            _fileSystem.File.Exists(credentialFile).Should().BeTrue("credential file should be created");

            // Assert - stored data should NOT contain plaintext API key (it should be encrypted)
            var storedBytes = _fileSystem.File.ReadAllBytes(credentialFile);
            var storedAsString = Encoding.UTF8.GetString(storedBytes);
            storedAsString.Should().NotContain(apiKey, "API key should be encrypted with libsodium, not stored in plaintext");

            // Assert - should be able to round-trip decrypt
            var retrieved = await credentialStore.RetrieveAsync(profileName);
            retrieved.Should().Be(apiKey, "should decrypt correctly with libsodium");
        }

        #endregion
    }
}
