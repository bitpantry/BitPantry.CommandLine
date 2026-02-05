using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for ProfileManager - profile storage and retrieval.
    /// </summary>
    [TestClass]
    public class ProfileManagerTests
    {
        private MockFileSystem _fileSystem;
        private string _storagePath;
        private ICredentialStore _credentialStore;

        [TestInitialize]
        public void Setup()
        {
            _fileSystem = new MockFileSystem();
            _storagePath = @"C:\Users\TestUser\.bitpantry\commandline\profiles";
            _fileSystem.Directory.CreateDirectory(_storagePath);
            _credentialStore = new CredentialStore(_fileSystem, _storagePath);
        }

        #region GetAllProfiles Tests

        [TestMethod]
        public async Task GetAllProfiles_EmptyStore_ReturnsEmptyList()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);

            // Act
            var profiles = await profileManager.GetAllProfilesAsync();

            // Assert
            profiles.Should().NotBeNull("should return a list, not null");
            profiles.Should().BeEmpty("new profile store should have no profiles");
        }

        [TestMethod]
        public async Task GetAllProfiles_MultipleProfiles_ReturnsAll()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profiles = new[]
            {
                new ServerProfile { Name = "production", Uri = "https://prod.example.com" },
                new ServerProfile { Name = "staging", Uri = "https://staging.example.com" },
                new ServerProfile { Name = "development", Uri = "https://dev.example.com" }
            };

            foreach (var profile in profiles)
            {
                await profileManager.CreateProfileAsync(profile);
            }

            // Act
            var retrievedProfiles = await profileManager.GetAllProfilesAsync();

            // Assert
            retrievedProfiles.Should().HaveCount(3, "all saved profiles should be returned");
            retrievedProfiles.Select(p => p.Name).Should().BeEquivalentTo(new[] { "production", "staging", "development" });
        }

        #endregion

        #region GetProfile Tests

        [TestMethod]
        public async Task GetProfile_ExistingProfile_ReturnsProfile()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile 
            { 
                Name = "production", 
                Uri = "https://prod.example.com",
                ApiKey = "secret-api-key"
            };
            await profileManager.CreateProfileAsync(profile);

            // Act
            var retrieved = await profileManager.GetProfileAsync("production");

            // Assert
            retrieved.Should().NotBeNull("existing profile should be returned");
            retrieved!.Name.Should().Be("production");
            retrieved.Uri.Should().Be("https://prod.example.com");
            retrieved.ApiKey.Should().Be("secret-api-key", "API key should be populated from credential store");
        }

        [TestMethod]
        public async Task GetProfile_NonExistentProfile_ReturnsNull()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);

            // Act
            var profile = await profileManager.GetProfileAsync("nonexistent");

            // Assert
            profile.Should().BeNull("non-existent profile should return null");
        }

        [TestMethod]
        public async Task GetProfile_CaseInsensitive_ReturnsProfile()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "Production", Uri = "https://prod.example.com" };
            await profileManager.CreateProfileAsync(profile);

            // Act
            var retrieved = await profileManager.GetProfileAsync("PRODUCTION");

            // Assert
            retrieved.Should().NotBeNull("profile lookup should be case-insensitive");
            retrieved!.Name.Should().Be("Production");
        }

        #endregion

        #region CreateProfile Tests

        [TestMethod]
        public async Task CreateProfile_NewProfile_PersistsToStorage()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile 
            { 
                Name = "production", 
                Uri = "https://prod.example.com",
                ApiKey = "secret-key"
            };

            // Act
            await profileManager.CreateProfileAsync(profile);

            // Assert - Verify profile persisted to file
            var configPath = Path.Combine(_storagePath, "profiles.json");
            _fileSystem.File.Exists(configPath).Should().BeTrue("profiles.json should be created");
            
            // Verify using a new instance (simulates app restart)
            var newProfileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var retrieved = await newProfileManager.GetProfileAsync("production");
            retrieved.Should().NotBeNull("profile should persist across instances");
            retrieved!.Uri.Should().Be("https://prod.example.com");
        }

        [TestMethod]
        public async Task CreateProfile_ExistingProfile_ThrowsException()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "https://prod.example.com" };
            await profileManager.CreateProfileAsync(profile);

            // Act
            var duplicate = new ServerProfile { Name = "production", Uri = "https://other.example.com" };
            Func<Task> act = async () => await profileManager.CreateProfileAsync(duplicate);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [TestMethod]
        public async Task CreateProfile_SetsCreatedAt_OnNewProfile()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "https://prod.example.com" };
            var beforeCreate = DateTime.UtcNow;

            // Act
            await profileManager.CreateProfileAsync(profile);
            var afterCreate = DateTime.UtcNow;

            // Assert
            var retrieved = await profileManager.GetProfileAsync("production");
            retrieved.Should().NotBeNull();
            retrieved!.CreatedAt.Should().BeOnOrAfter(beforeCreate, "CreatedAt should be set to creation time");
            retrieved.CreatedAt.Should().BeOnOrBefore(afterCreate, "CreatedAt should not be in the future");
            retrieved.ModifiedAt.Should().BeCloseTo(retrieved.CreatedAt, TimeSpan.FromMilliseconds(10), "ModifiedAt should be very close to CreatedAt on new profile");
        }

        #endregion

        #region UpdateProfile Tests

        [TestMethod]
        public async Task UpdateProfile_ExistingProfile_UpdatesProfile()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "https://prod.example.com" };
            await profileManager.CreateProfileAsync(profile);

            // Act
            var updated = new ServerProfile { Name = "production", Uri = "https://new-prod.example.com" };
            await profileManager.UpdateProfileAsync(updated);

            // Assert
            var retrieved = await profileManager.GetProfileAsync("production");
            retrieved.Should().NotBeNull();
            retrieved!.Uri.Should().Be("https://new-prod.example.com");
            retrieved.ModifiedAt.Should().BeAfter(retrieved.CreatedAt, "ModifiedAt should be updated");
        }

        [TestMethod]
        public async Task UpdateProfile_NonExistentProfile_ThrowsException()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);

            // Act
            var profile = new ServerProfile { Name = "nonexistent", Uri = "https://example.com" };
            Func<Task> act = async () => await profileManager.UpdateProfileAsync(profile);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*does not exist*");
        }

        #endregion

        #region DeleteProfile Tests

        [TestMethod]
        public async Task DeleteProfile_ExistingProfile_ReturnsTrue()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "https://prod.example.com" };
            await profileManager.CreateProfileAsync(profile);

            // Act
            var result = await profileManager.DeleteProfileAsync("production");

            // Assert
            result.Should().BeTrue("deleting existing profile should return true");
        }

        [TestMethod]
        public async Task DeleteProfile_NonExistent_ReturnsFalse()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);

            // Act
            var result = await profileManager.DeleteProfileAsync("nonexistent");

            // Assert
            result.Should().BeFalse("deleting non-existent profile should return false");
        }

        [TestMethod]
        public async Task DeleteProfile_RemovesFromStorage()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "https://prod.example.com", ApiKey = "secret" };
            await profileManager.CreateProfileAsync(profile);

            // Act
            await profileManager.DeleteProfileAsync("production");

            // Assert
            var retrieved = await profileManager.GetProfileAsync("production");
            retrieved.Should().BeNull("deleted profile should not be retrievable");
            
            // Verify credential is also removed
            var hasCredential = await profileManager.HasCredentialAsync("production");
            hasCredential.Should().BeFalse("credentials should be removed with profile");
        }

        #endregion

        #region DefaultProfile Tests

        [TestMethod]
        public async Task GetDefaultProfileName_NoneSet_ReturnsNull()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);

            // Act
            var defaultName = await profileManager.GetDefaultProfileNameAsync();

            // Assert
            defaultName.Should().BeNull("no default should be set initially");
        }

        [TestMethod]
        public async Task GetDefaultProfileName_WhenSet_ReturnsName()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "https://prod.example.com" };
            await profileManager.CreateProfileAsync(profile);
            await profileManager.SetDefaultProfileAsync("production");

            // Act
            var defaultName = await profileManager.GetDefaultProfileNameAsync();

            // Assert
            defaultName.Should().Be("production", "default should return the set name");
        }

        [TestMethod]
        public async Task SetDefaultProfile_ValidName_PersistsDefault()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "https://prod.example.com" };
            await profileManager.CreateProfileAsync(profile);

            // Act
            await profileManager.SetDefaultProfileAsync("production");

            // Assert - Verify persistence by using a new instance
            var newProfileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var defaultName = await newProfileManager.GetDefaultProfileNameAsync();
            defaultName.Should().Be("production", "default should persist across instances");
        }

        [TestMethod]
        public async Task SetDefaultProfile_Null_ClearsDefault()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "https://prod.example.com" };
            await profileManager.CreateProfileAsync(profile);
            await profileManager.SetDefaultProfileAsync("production");

            // Act
            await profileManager.SetDefaultProfileAsync(null);

            // Assert
            var defaultName = await profileManager.GetDefaultProfileNameAsync();
            defaultName.Should().BeNull("passing null should clear the default");
        }

        [TestMethod]
        public async Task DeleteProfile_WasDefault_ClearsDefault()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "https://prod.example.com" };
            await profileManager.CreateProfileAsync(profile);
            await profileManager.SetDefaultProfileAsync("production");

            // Act
            await profileManager.DeleteProfileAsync("production");

            // Assert
            var defaultName = await profileManager.GetDefaultProfileNameAsync();
            defaultName.Should().BeNull("deleting the default profile should clear the default setting");
        }

        #endregion

        #region Validation Tests

        [TestMethod]
        public async Task CreateProfile_EmptyName_ThrowsValidation()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "", Uri = "https://example.com" };

            // Act
            Func<Task> act = async () => await profileManager.CreateProfileAsync(profile);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*name*cannot be empty*");
        }

        [TestMethod]
        public async Task CreateProfile_InvalidCharacters_ThrowsValidation()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "my@profile#name", Uri = "https://example.com" };

            // Act
            Func<Task> act = async () => await profileManager.CreateProfileAsync(profile);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*invalid*character*");
        }

        [TestMethod]
        public async Task CreateProfile_TooLongName_ThrowsValidation()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var longName = new string('a', 65); // 65 chars exceeds 64 char limit
            var profile = new ServerProfile { Name = longName, Uri = "https://example.com" };

            // Act
            Func<Task> act = async () => await profileManager.CreateProfileAsync(profile);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*64*characters*");
        }

        [TestMethod]
        public async Task CreateProfile_HyphenInName_Succeeds()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "my-profile", Uri = "https://example.com" };

            // Act
            await profileManager.CreateProfileAsync(profile);

            // Assert
            var retrieved = await profileManager.GetProfileAsync("my-profile");
            retrieved.Should().NotBeNull("hyphen in middle of name should be allowed");
            retrieved!.Name.Should().Be("my-profile");
        }

        [TestMethod]
        public async Task CreateProfile_StartsWithHyphen_ThrowsValidation()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "-profile", Uri = "https://example.com" };

            // Act
            Func<Task> act = async () => await profileManager.CreateProfileAsync(profile);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*cannot start with*hyphen*");
        }

        [TestMethod]
        public async Task CreateProfile_InvalidUri_ThrowsValidation()
        {
            // Arrange
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);
            var profile = new ServerProfile { Name = "production", Uri = "not-a-valid-uri" };

            // Act
            Func<Task> act = async () => await profileManager.CreateProfileAsync(profile);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*URI*invalid*");
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public async Task CreateProfile_CorruptedFile_RecreatesFile()
        {
            // Arrange - Create corrupted profiles.json
            var configPath = _fileSystem.Path.Combine(_storagePath, "profiles.json");
            _fileSystem.File.WriteAllText(configPath, "{ this is not valid json }");
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);

            // Act - Should recreate file and add profile successfully
            var profile = new ServerProfile { Name = "production", Uri = "https://example.com" };
            await profileManager.CreateProfileAsync(profile);

            // Assert
            var retrieved = await profileManager.GetProfileAsync("production");
            retrieved.Should().NotBeNull("corrupted file should be recreated to allow new profile");
        }

        [TestMethod]
        public async Task GetAllProfiles_MissingFile_ReturnsEmpty()
        {
            // Arrange - Ensure no profiles.json exists
            var configPath = _fileSystem.Path.Combine(_storagePath, "profiles.json");
            if (_fileSystem.File.Exists(configPath))
                _fileSystem.File.Delete(configPath);
            var profileManager = new ProfileManager(_fileSystem, _storagePath, _credentialStore);

            // Act
            var profiles = await profileManager.GetAllProfilesAsync();

            // Assert
            profiles.Should().BeEmpty("missing file should return empty list, not throw");
        }

        [TestMethod]
        public async Task CreateProfile_DirectoryNotExists_CreatesDirectory()
        {
            // Arrange - Use a path where directory doesn't exist
            var newPath = @"C:\Users\TestUser\.bitpantry\commandline\newprofiles";
            // Ensure directory doesn't exist
            if (_fileSystem.Directory.Exists(newPath))
                _fileSystem.Directory.Delete(newPath, true);
            var profileManager = new ProfileManager(_fileSystem, newPath, _credentialStore);

            // Act
            var profile = new ServerProfile { Name = "production", Uri = "https://example.com" };
            await profileManager.CreateProfileAsync(profile);

            // Assert
            _fileSystem.Directory.Exists(newPath).Should().BeTrue("directory should be created");
            var retrieved = await profileManager.GetProfileAsync("production");
            retrieved.Should().NotBeNull("profile should be saved in newly created directory");
        }

        #endregion
    }
}
