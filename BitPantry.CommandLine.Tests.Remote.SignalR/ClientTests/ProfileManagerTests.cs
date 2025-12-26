using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class ProfileManagerTests
    {
        private string _testDirectory = null!;
        private string _profilesFilePath = null!;
        private Mock<ILogger<ProfileManager>> _loggerMock = null!;
        private Mock<ICredentialStore> _credentialStoreMock = null!;
        private ProfileManager _profileManager = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"ProfileManagerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _profilesFilePath = Path.Combine(_testDirectory, "profiles.json");
            _loggerMock = new Mock<ILogger<ProfileManager>>();
            _credentialStoreMock = new Mock<ICredentialStore>();
            _profileManager = new ProfileManager(_loggerMock.Object, _credentialStoreMock.Object, _profilesFilePath);
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
        public async Task GetAllProfilesAsync_ShouldReturnEmpty_WhenNoProfiles()
        {
            // Act
            var profiles = await _profileManager.GetAllProfilesAsync();

            // Assert
            profiles.Should().BeEmpty();
        }

        [TestMethod]
        public async Task SaveProfileAsync_ShouldSaveProfile()
        {
            // Arrange
            var profile = new ServerProfile
            {
                Name = "test-profile",
                Uri = "https://api.example.com"
            };

            // Act
            await _profileManager.SaveProfileAsync(profile);

            // Assert
            var saved = await _profileManager.GetProfileAsync("test-profile");
            saved.Should().NotBeNull();
            saved!.Name.Should().Be("test-profile");
            saved.Uri.Should().Be("https://api.example.com");
        }

        [TestMethod]
        public async Task GetProfileAsync_ShouldReturnNull_WhenNotFound()
        {
            // Act
            var profile = await _profileManager.GetProfileAsync("nonexistent");

            // Assert
            profile.Should().BeNull();
        }

        [TestMethod]
        public async Task GetProfileAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            var profile = new ServerProfile
            {
                Name = "TestProfile",
                Uri = "https://api.example.com"
            };
            await _profileManager.SaveProfileAsync(profile);

            // Act & Assert
            (await _profileManager.GetProfileAsync("testprofile")).Should().NotBeNull();
            (await _profileManager.GetProfileAsync("TESTPROFILE")).Should().NotBeNull();
        }

        [TestMethod]
        public async Task DeleteProfileAsync_ShouldDeleteProfile()
        {
            // Arrange
            var profile = new ServerProfile { Name = "test-profile", Uri = "https://api.example.com" };
            await _profileManager.SaveProfileAsync(profile);

            // Act
            var result = await _profileManager.DeleteProfileAsync("test-profile");

            // Assert
            result.Should().BeTrue();
            (await _profileManager.GetProfileAsync("test-profile")).Should().BeNull();
        }

        [TestMethod]
        public async Task DeleteProfileAsync_ShouldRemoveCredentials()
        {
            // Arrange
            var profile = new ServerProfile { Name = "test-profile", Uri = "https://api.example.com" };
            await _profileManager.SaveProfileAsync(profile);

            // Act
            await _profileManager.DeleteProfileAsync("test-profile");

            // Assert
            _credentialStoreMock.Verify(x => x.RemoveAsync("test-profile"), Times.Once);
        }

        [TestMethod]
        public async Task DeleteProfileAsync_ShouldReturnFalse_WhenNotFound()
        {
            // Act
            var result = await _profileManager.DeleteProfileAsync("nonexistent");

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task SetDefaultProfileAsync_ShouldSetDefault()
        {
            // Arrange
            var profile = new ServerProfile { Name = "test-profile", Uri = "https://api.example.com" };
            await _profileManager.SaveProfileAsync(profile);

            // Act
            await _profileManager.SetDefaultProfileAsync("test-profile");

            // Assert
            var defaultProfile = await _profileManager.GetDefaultProfileAsync();
            defaultProfile.Should().Be("test-profile");
        }

        [TestMethod]
        public async Task SetDefaultProfileAsync_ShouldThrow_WhenProfileNotFound()
        {
            // Act
            var act = () => _profileManager.SetDefaultProfileAsync("nonexistent");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DeleteProfileAsync_ShouldClearDefault_WhenDeletingDefaultProfile()
        {
            // Arrange
            var profile = new ServerProfile { Name = "test-profile", Uri = "https://api.example.com" };
            await _profileManager.SaveProfileAsync(profile);
            await _profileManager.SetDefaultProfileAsync("test-profile");

            // Act
            await _profileManager.DeleteProfileAsync("test-profile");

            // Assert
            var defaultProfile = await _profileManager.GetDefaultProfileAsync();
            defaultProfile.Should().BeNull();
        }

        [TestMethod]
        [DataRow("valid-profile")]
        [DataRow("valid_profile")]
        [DataRow("ValidProfile123")]
        [DataRow("a")]
        [DataRow("profile-with-dashes")]
        [DataRow("profile_with_underscores")]
        public void IsValidProfileName_ShouldReturnTrue_ForValidNames(string name)
        {
            // Act
            var result = _profileManager.IsValidProfileName(name);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("profile with spaces")]
        [DataRow("profile@special")]
        [DataRow("profile.dot")]
        [DataRow("profile/slash")]
        [DataRow("profile\\backslash")]
        public void IsValidProfileName_ShouldReturnFalse_ForInvalidNames(string name)
        {
            // Act
            var result = _profileManager.IsValidProfileName(name);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task SaveProfileAsync_ShouldThrow_ForInvalidName()
        {
            // Arrange
            var profile = new ServerProfile { Name = "invalid@name", Uri = "https://api.example.com" };

            // Act
            var act = () => _profileManager.SaveProfileAsync(profile);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task SaveProfileAsync_ShouldSetCreatedAt_ForNewProfile()
        {
            // Arrange
            var profile = new ServerProfile { Name = "test-profile", Uri = "https://api.example.com" };
            var before = DateTimeOffset.UtcNow;

            // Act
            await _profileManager.SaveProfileAsync(profile);

            // Assert
            var saved = await _profileManager.GetProfileAsync("test-profile");
            saved!.CreatedAt.Should().BeOnOrAfter(before);
            saved.ModifiedAt.Should().BeOnOrAfter(before);
        }

        [TestMethod]
        public async Task SaveProfileAsync_ShouldUpdateModifiedAt_ForExistingProfile()
        {
            // Arrange
            var profile = new ServerProfile { Name = "test-profile", Uri = "https://api.example.com" };
            await _profileManager.SaveProfileAsync(profile);
            var original = await _profileManager.GetProfileAsync("test-profile");
            var originalCreatedAt = original!.CreatedAt;
            
            await Task.Delay(10); // Small delay to ensure different timestamp

            // Act
            profile.Uri = "https://new-api.example.com";
            await _profileManager.SaveProfileAsync(profile);

            // Assert
            var updated = await _profileManager.GetProfileAsync("test-profile");
            updated!.CreatedAt.Should().Be(originalCreatedAt); // CreatedAt should not change
            updated.ModifiedAt.Should().BeAfter(originalCreatedAt);
        }
    }
}
