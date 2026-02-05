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

        [TestMethod]
        public async Task Retrieve_StoredKey_DecryptsCorrectly_Libsodium()
        {
            // Arrange - Force libsodium encryption for cross-platform testing
            var credentialStore = new CredentialStore(_fileSystem, _storagePath, EncryptionProvider.Libsodium);
            var profileName = "production";
            var apiKey = "secret-api-key-for-libsodium-test";
            await credentialStore.StoreAsync(profileName, apiKey);

            // Act - Create new instance and retrieve (simulates app restart)
            var newCredentialStore = new CredentialStore(_fileSystem, _storagePath, EncryptionProvider.Libsodium);
            var retrievedKey = await newCredentialStore.RetrieveAsync(profileName);

            // Assert
            retrievedKey.Should().Be(apiKey, "libsodium-encrypted API key should decrypt correctly across instances");
        }

        [TestMethod]
        public void CredentialStoreException_ContainsInstallInstructions()
        {
            // Arrange - Create exception that would be thrown when libsodium fails
            var innerException = new Exception("Native library not found");
            
            // Act - Create the exception with instructions
            var exception = CredentialStore.CreateLibsodiumUnavailableException(innerException);

            // Assert - Should contain helpful install instructions
            exception.Message.Should().Contain("libsodium", "should mention libsodium");
            exception.Message.Should().Contain("install", "should contain install instructions");
            exception.InnerException.Should().Be(innerException, "should preserve inner exception");
        }

        #endregion

        #region Validation Tests

        [TestMethod]
        public async Task Store_EmptyApiKey_ThrowsValidation()
        {
            // Arrange
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);
            var profileName = "production";

            // Act
            Func<Task> act = async () => await credentialStore.StoreAsync(profileName, "");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*empty*")
                .WithParameterName("apiKey");
        }

        [TestMethod]
        public async Task Store_WhitespaceApiKey_ThrowsValidation()
        {
            // Arrange
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);
            var profileName = "production";

            // Act
            Func<Task> act = async () => await credentialStore.StoreAsync(profileName, "   ");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("apiKey");
        }

        [TestMethod]
        public async Task Store_MultipleProfiles_IndependentStorage()
        {
            // Arrange
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);
            var profiles = new[]
            {
                ("production", "prod-api-key-abc123"),
                ("staging", "stage-api-key-def456"),
                ("development", "dev-api-key-ghi789")
            };

            // Act - Store all profiles
            foreach (var (name, key) in profiles)
            {
                await credentialStore.StoreAsync(name, key);
            }

            // Assert - Each profile should have its own independent credential
            foreach (var (name, expectedKey) in profiles)
            {
                var retrievedKey = await credentialStore.RetrieveAsync(name);
                retrievedKey.Should().Be(expectedKey, $"profile '{name}' should have its own API key");
            }
        }

        [TestMethod]
        public async Task Remove_AlsoRemovesFromProfile_OnDelete()
        {
            // Arrange - Create multiple profiles
            var credentialStore = new CredentialStore(_fileSystem, _storagePath);
            await credentialStore.StoreAsync("production", "prod-key");
            await credentialStore.StoreAsync("staging", "stage-key");

            // Pre-condition: both exist
            (await credentialStore.ExistsAsync("production")).Should().BeTrue("production should exist before deletion");
            (await credentialStore.ExistsAsync("staging")).Should().BeTrue("staging should exist before deletion");

            // Act - Remove one profile's credentials
            await credentialStore.RemoveAsync("production");

            // Assert - Removed profile should no longer exist
            (await credentialStore.ExistsAsync("production")).Should().BeFalse("production credential should be removed");
            (await credentialStore.RetrieveAsync("production")).Should().BeNull("production key should return null");
            
            // Assert - Other profile should be unaffected
            (await credentialStore.ExistsAsync("staging")).Should().BeTrue("staging should still exist");
            (await credentialStore.RetrieveAsync("staging")).Should().Be("stage-key", "staging key should be unchanged");
        }

        #endregion
    }
}
