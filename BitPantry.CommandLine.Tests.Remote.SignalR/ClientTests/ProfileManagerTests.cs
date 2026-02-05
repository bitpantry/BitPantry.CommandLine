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
    }
}
