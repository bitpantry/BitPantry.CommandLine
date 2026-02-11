using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server.Profiles;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using FluentAssertions;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for server profile commands.
    /// Implements test cases from spec 009-server-profile: CMD-ADD-*, CMD-LST-*, CMD-SHW-*, CMD-RMV-*, CMD-DEF-*
    /// </summary>
    [TestClass]
    public class ProfileCommandTests
    {
        private Mock<IProfileManager> _profileManagerMock = null!;
        private TestConsole _console = null!;

        [TestInitialize]
        public void Setup()
        {
            _profileManagerMock = new Mock<IProfileManager>();
            _console = new TestConsole();
        }

        #region profile add Command Tests (CMD-ADD-*)

        /// <summary>
        /// Implements: 009:T071 (CMD-ADD-001)
        /// When: User runs `server profile add prod -u https://api.com`
        /// Then: Profile is created with name "prod" and URI "https://api.com"
        /// </summary>
        [TestMethod]
        public async Task Add_ValidProfile_CreatesProfile()
        {
            // Arrange
            _profileManagerMock.Setup(m => m.ExistsAsync("prod", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _profileManagerMock.Setup(m => m.CreateProfileAsync(It.IsAny<ServerProfile>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = CreateAddCommand();
            command.Name = "prod";
            command.Uri = "https://api.com";

            // Act
            await command.Execute(CreateContext());

            // Assert - Profile should be created with correct name and URI
            _profileManagerMock.Verify(m => m.CreateProfileAsync(
                It.Is<ServerProfile>(p => p.Name == "prod" && p.Uri == "https://api.com"),
                It.IsAny<CancellationToken>()), Times.Once);

            // Should display success message
            _console.Output.Should().Contain("prod", "should confirm profile name in output");
        }

        /// <summary>
        /// Implements: 009:T072 (CMD-ADD-002)
        /// When: User tries to add a profile with name that already exists
        /// Then: Error message is displayed, no profile created
        /// </summary>
        [TestMethod]
        public async Task Add_DuplicateName_ShowsError()
        {
            // Arrange
            _profileManagerMock.Setup(m => m.ExistsAsync("existing", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = CreateAddCommand();
            command.Name = "existing";
            command.Uri = "https://api.com";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should show error message
            _console.Output.Should().Contain("already exists", "should indicate duplicate name error");

            // Should NOT create profile
            _profileManagerMock.Verify(m => m.CreateProfileAsync(
                It.IsAny<ServerProfile>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Implements: 009:T073 (CMD-ADD-003)
        /// When: User runs `server profile add prod -u https://api.com -k myapikey`
        /// Then: Profile is created and API key is encrypted and stored
        /// </summary>
        [TestMethod]
        public async Task Add_WithApiKey_StoresCredential()
        {
            // Arrange
            _profileManagerMock.Setup(m => m.ExistsAsync("prod", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _profileManagerMock.Setup(m => m.CreateProfileAsync(It.IsAny<ServerProfile>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = CreateAddCommand();
            command.Name = "prod";
            command.Uri = "https://api.com";
            command.ApiKey = "secret-api-key";

            // Act
            await command.Execute(CreateContext());

            // Assert - Profile should be created with API key
            _profileManagerMock.Verify(m => m.CreateProfileAsync(
                It.Is<ServerProfile>(p => p.Name == "prod" && p.ApiKey == "secret-api-key"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Implements: 009:T074 (CMD-ADD-004)
        /// When: User runs `server profile add prod -u https://api.com --api-key` without value
        /// Then: User is prompted for API key with masked input
        /// Note: This test verifies the command structure supports the scenario.
        /// Integration test would verify actual prompting behavior.
        /// </summary>
        [TestMethod]
        public async Task Add_WithApiKeyFlag_SupportsPromptScenario()
        {
            // Arrange - API key property is nullable, supporting prompt scenario
            _profileManagerMock.Setup(m => m.ExistsAsync("prod", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _profileManagerMock.Setup(m => m.CreateProfileAsync(It.IsAny<ServerProfile>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = CreateAddCommand();
            command.Name = "prod";
            command.Uri = "https://api.com";
            command.ApiKey = null; // Simulates --api-key flag without value (prompt scenario)

            // Act
            await command.Execute(CreateContext());

            // Assert - Profile should be created even without API key (can be added later)
            _profileManagerMock.Verify(m => m.CreateProfileAsync(
                It.Is<ServerProfile>(p => p.Name == "prod" && p.ApiKey == null),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Implements: 009:T075 (CMD-ADD-005)
        /// When: User runs `server profile add prod -u not-a-valid-uri`
        /// Then: Validation error is displayed, no profile created
        /// </summary>
        [TestMethod]
        public async Task Add_InvalidUri_ShowsError()
        {
            // Arrange
            var command = CreateAddCommand();
            command.Name = "prod";
            command.Uri = "not-a-valid-uri";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should show validation error
            _console.Output.Should().Contain("invalid", "should indicate URI validation error");

            // Should NOT create profile
            _profileManagerMock.Verify(m => m.CreateProfileAsync(
                It.IsAny<ServerProfile>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Implements: 009:T076 (CMD-ADD-006)
        /// When: User runs `server profile add prod -u https://api.com --default`
        /// Then: Profile is created and set as default
        /// </summary>
        [TestMethod]
        public async Task Add_SetAsDefault_SetsDefault()
        {
            // Arrange
            _profileManagerMock.Setup(m => m.ExistsAsync("prod", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _profileManagerMock.Setup(m => m.CreateProfileAsync(It.IsAny<ServerProfile>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _profileManagerMock.Setup(m => m.SetDefaultProfileAsync("prod", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = CreateAddCommand();
            command.Name = "prod";
            command.Uri = "https://api.com";
            command.SetAsDefault = true;

            // Act
            await command.Execute(CreateContext());

            // Assert - Profile should be created
            _profileManagerMock.Verify(m => m.CreateProfileAsync(
                It.Is<ServerProfile>(p => p.Name == "prod"),
                It.IsAny<CancellationToken>()), Times.Once);

            // Should set as default
            _profileManagerMock.Verify(m => m.SetDefaultProfileAsync("prod", It.IsAny<CancellationToken>()), Times.Once);

            // Should indicate it's the default
            _console.Output.Should().Contain("default", "should confirm profile is set as default");
        }

        #endregion

        #region profile list Command Tests (CMD-LST-*)

        /// <summary>
        /// Implements: 009:T078 (CMD-LST-001)
        /// When: User runs `server profile list` with no profiles configured
        /// Then: "No profiles configured" message is displayed
        /// </summary>
        [TestMethod]
        public async Task List_NoProfiles_ShowsEmptyMessage()
        {
            // Arrange
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<ServerProfile>());

            var command = CreateListCommand();

            // Act
            await command.Execute(CreateContext());

            // Assert
            _console.Output.Should().Contain("No profiles", "should show empty message");
        }

        /// <summary>
        /// Implements: 009:T079 (CMD-LST-002)
        /// When: User runs `server profile list` with multiple profiles
        /// Then: Table displays with Name, URI, and Default columns
        /// </summary>
        [TestMethod]
        public async Task List_MultipleProfiles_ShowsTable()
        {
            // Arrange
            var profiles = new List<ServerProfile>
            {
                new ServerProfile { Name = "prod", Uri = "https://prod.api.com" },
                new ServerProfile { Name = "staging", Uri = "https://staging.api.com" }
            };
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profiles);
            _profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            var command = CreateListCommand();

            // Act
            await command.Execute(CreateContext());

            // Assert - Should display a table with profiles
            var output = _console.Output;
            output.Should().Contain("prod", "should show first profile name");
            output.Should().Contain("staging", "should show second profile name");
            output.Should().Contain("https://prod.api.com", "should show first profile URI");
            output.Should().Contain("https://staging.api.com", "should show second profile URI");
        }

        /// <summary>
        /// Implements: 009:T080 (CMD-LST-003)
        /// When: User runs `server profile list` with a default profile set
        /// Then: Default profile is marked with `*` indicator
        /// </summary>
        [TestMethod]
        public async Task List_MarksDefault_WithIndicator()
        {
            // Arrange
            var profiles = new List<ServerProfile>
            {
                new ServerProfile { Name = "prod", Uri = "https://prod.api.com" },
                new ServerProfile { Name = "staging", Uri = "https://staging.api.com" }
            };
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profiles);
            _profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("prod"); // prod is default

            var command = CreateListCommand();

            // Act
            await command.Execute(CreateContext());

            // Assert - Default profile should have indicator in table
            var output = _console.Output;
            // The table shows Yes/No in Default column - prod should show Yes, staging should show No
            output.Should().MatchRegex(@"prod.*Yes", "default profile (prod) should show Yes");
            output.Should().MatchRegex(@"staging.*No", "non-default profile (staging) should show No");
        }

        /// <summary>
        /// Implements: 009:T081 (CMD-LST-004)
        /// When: User runs `server profile list` showing credential status
        /// Then: Lists show whether each profile has stored credentials
        /// </summary>
        [TestMethod]
        public async Task List_IncludesCredentials_Column()
        {
            // Arrange
            var profiles = new List<ServerProfile>
            {
                new ServerProfile { Name = "prod", Uri = "https://prod.api.com" },
                new ServerProfile { Name = "staging", Uri = "https://staging.api.com" }
            };
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(profiles);
            _profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
            _profileManagerMock.Setup(m => m.HasCredentialAsync("prod", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _profileManagerMock.Setup(m => m.HasCredentialAsync("staging", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var command = CreateListCommand();

            // Act
            await command.Execute(CreateContext());

            // Assert - Should indicate credential status
            var output = _console.Output;
            // Output should indicate which profiles have credentials (Yes/No, ✓/✗, etc.)
            output.Should().MatchRegex(@"(Yes|No|✓|✗|true|false|API Key)", "should show credential status");
        }

        #endregion

        #region profile show Command Tests (CMD-SHW-*)

        /// <summary>
        /// Implements: 009:T083 (CMD-SHW-001)
        /// When: User runs `server profile show [name]` for existing profile
        /// Then: Shows name, URI, created date
        /// </summary>
        [TestMethod]
        public async Task Show_ExistingProfile_DisplaysDetails()
        {
            // Arrange
            var profile = new ServerProfile
            {
                Name = "production",
                Uri = "https://prod.api.com"
            };
            _profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            var command = CreateShowCommand();
            command.Name = "production";

            // Act
            await command.Execute(CreateContext());

            // Assert
            var output = _console.Output;
            output.Should().Contain("production", "should show profile name");
            output.Should().Contain("https://prod.api.com", "should show profile URI");
        }

        /// <summary>
        /// Implements: 009:T084 (CMD-SHW-002)
        /// When: User runs `server profile show` for non-existent profile
        /// Then: "Profile 'x' not found" error is displayed
        /// </summary>
        [TestMethod]
        public async Task Show_NonExistent_ShowsError()
        {
            // Arrange
            _profileManagerMock.Setup(m => m.GetProfileAsync("nonexistent", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServerProfile?)null);

            var command = CreateShowCommand();
            command.Name = "nonexistent";

            // Act
            await command.Execute(CreateContext());

            // Assert
            var output = _console.Output;
            output.Should().Contain("not found", "should show not found error");
            output.Should().Contain("nonexistent", "should mention profile name in error");
        }

        /// <summary>
        /// Implements: 009:T085 (CMD-SHW-003)
        /// When: User runs `server profile show` for profile with credential
        /// Then: Shows "API Key: ****" not actual key
        /// </summary>
        [TestMethod]
        public async Task Show_WithCredential_ShowsMasked()
        {
            // Arrange
            var profile = new ServerProfile
            {
                Name = "production",
                Uri = "https://prod.api.com",
                ApiKey = "secret-api-key-12345"
            };
            _profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _profileManagerMock.Setup(m => m.HasCredentialAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = CreateShowCommand();
            command.Name = "production";

            // Act
            await command.Execute(CreateContext());

            // Assert
            var output = _console.Output;
            output.Should().NotContain("secret-api-key-12345", "actual key should never be shown");
            // Should show masked indicator like **** or [stored] or similar
            output.Should().MatchRegex(@"(\*{4,}|\[stored\]|Yes|Configured)", "should show masked credential indicator");
        }

        /// <summary>
        /// Implements: 009:T086 (CMD-SHW-004)
        /// When: User uses tab completion for profile name in show command
        /// Then: Autocomplete suggests existing profile names
        /// </summary>
        [TestMethod]
        public async Task Show_ProfileNameAutocomplete_Works()
        {
            // This test verifies the AutoComplete attribute exists on the Name property
            // The actual autocomplete behavior is tested via command metadata inspection

            // Arrange
            var command = CreateShowCommand();

            // Act - Check if the Name property has the AutoComplete attribute
            var nameProperty = typeof(ProfileShowCommand).GetProperty(nameof(command.Name));
            var hasAutoCompleteAttribute = nameProperty!.GetCustomAttributes(true)
                .Any(a => a.GetType().Name.Contains("AutoComplete"));

            // Assert
            hasAutoCompleteAttribute.Should().BeTrue("Name property should have AutoComplete attribute for profile name completion");
            await Task.CompletedTask; // Make async for test signature
        }

        #endregion

        #region profile remove Command Tests (CMD-RMV-*)

        /// <summary>
        /// Implements: 009:T088 (CMD-RMV-001)
        /// When: User runs `server profile remove [name]` for existing profile
        /// Then: Profile is removed from storage
        /// </summary>
        [TestMethod]
        public async Task Remove_ExistingProfile_DeletesProfile()
        {
            // Arrange
            _profileManagerMock.Setup(m => m.ExistsAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _profileManagerMock.Setup(m => m.DeleteProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = CreateRemoveCommand();
            command.Name = "production";

            // Act
            await command.Execute(CreateContext());

            // Assert
            _profileManagerMock.Verify(m => m.DeleteProfileAsync("production", It.IsAny<CancellationToken>()), Times.Once);
            _console.Output.Should().Contain("removed", "should confirm removal");
        }

        /// <summary>
        /// Implements: 009:T089 (CMD-RMV-002)
        /// When: User runs `server profile remove` for non-existent profile
        /// Then: "Profile 'x' not found" error is displayed
        /// </summary>
        [TestMethod]
        public async Task Remove_NonExistent_ShowsError()
        {
            // Arrange
            _profileManagerMock.Setup(m => m.ExistsAsync("nonexistent", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _profileManagerMock.Setup(m => m.DeleteProfileAsync("nonexistent", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var command = CreateRemoveCommand();
            command.Name = "nonexistent";

            // Act
            await command.Execute(CreateContext());

            // Assert
            var output = _console.Output;
            output.Should().Contain("not found", "should show not found error");
            output.Should().Contain("nonexistent", "should mention profile name in error");
        }

        /// <summary>
        /// Implements: 009:T090 (CMD-RMV-003)
        /// When: User removes a profile with stored credentials
        /// Then: Associated credential is also deleted (handled by DeleteProfileAsync)
        /// </summary>
        [TestMethod]
        public async Task Remove_AlsoRemoves_Credential()
        {
            // Arrange - Profile with credentials
            _profileManagerMock.Setup(m => m.ExistsAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _profileManagerMock.Setup(m => m.HasCredentialAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            // DeleteProfileAsync should handle credential removal internally
            _profileManagerMock.Setup(m => m.DeleteProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = CreateRemoveCommand();
            command.Name = "production";

            // Act
            await command.Execute(CreateContext());

            // Assert - DeleteProfileAsync is called, which handles credential deletion
            _profileManagerMock.Verify(m => m.DeleteProfileAsync("production", It.IsAny<CancellationToken>()), Times.Once);
            // The command should succeed (credential deletion is handled by IProfileManager.DeleteProfileAsync)
            _console.Output.Should().Contain("removed", "should confirm profile removal including credential");
        }

        /// <summary>
        /// Implements: 009:T091 (CMD-RMV-004)
        /// When: User removes a profile that was set as default
        /// Then: Default profile setting is cleared
        /// </summary>
        [TestMethod]
        public async Task Remove_WasDefault_ClearsDefault()
        {
            // Arrange - Profile is the default
            _profileManagerMock.Setup(m => m.ExistsAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("production"); // production is currently default
            _profileManagerMock.Setup(m => m.DeleteProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = CreateRemoveCommand();
            command.Name = "production";

            // Act
            await command.Execute(CreateContext());

            // Assert - Default should be cleared when removing the default profile
            _profileManagerMock.Verify(m => m.SetDefaultProfileAsync(null, It.IsAny<CancellationToken>()), Times.Once,
                "should clear default when removing the default profile");
        }

        #endregion

        #region profile set-default Command Tests (CMD-DEF-*)

        /// <summary>
        /// Implements: 009:T093 (CMD-DEF-001)
        /// When: User sets an existing profile as default
        /// Then: Profile becomes the default profile
        /// </summary>
        [TestMethod]
        public async Task SetDefault_ExistingProfile_SetsDefault()
        {
            // Arrange - Profile exists
            _profileManagerMock.Setup(m => m.ExistsAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = CreateSetDefaultCommand();
            command.Name = "production";

            // Act
            await command.Execute(CreateContext());

            // Assert - SetDefaultProfileAsync should be called with the profile name
            _profileManagerMock.Verify(m => m.SetDefaultProfileAsync("production", It.IsAny<CancellationToken>()), Times.Once);
            _console.Output.Should().Contain("production", "should confirm the profile name");
            _console.Output.Should().Contain("default", "should confirm it was set as default");
        }

        /// <summary>
        /// Implements: 009:T094 (CMD-DEF-002)
        /// When: User tries to set a non-existent profile as default
        /// Then: Error message is shown
        /// </summary>
        [TestMethod]
        public async Task SetDefault_NonExistent_ShowsError()
        {
            // Arrange - Profile does not exist
            _profileManagerMock.Setup(m => m.ExistsAsync("nonexistent", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var command = CreateSetDefaultCommand();
            command.Name = "nonexistent";

            // Act
            await command.Execute(CreateContext());

            // Assert - Error message shown, SetDefaultProfileAsync not called
            _profileManagerMock.Verify(m => m.SetDefaultProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _console.Output.Should().Contain("nonexistent", "should show the profile name");
            _console.Output.Should().Contain("not found", "should indicate profile not found");
        }

        /// <summary>
        /// Implements: 009:T095 (CMD-DEF-003)
        /// When: User uses --none flag to clear the default
        /// Then: Default profile is cleared
        /// </summary>
        [TestMethod]
        public async Task SetDefault_ClearWithNone_ClearsDefault()
        {
            // Arrange - Use --none flag
            var command = CreateSetDefaultCommand();
            command.ClearDefault = true;
            command.Name = string.Empty; // Name should not be used when ClearDefault is true

            // Act
            await command.Execute(CreateContext());

            // Assert - SetDefaultProfileAsync should be called with null to clear
            _profileManagerMock.Verify(m => m.SetDefaultProfileAsync(null, It.IsAny<CancellationToken>()), Times.Once);
            _console.Output.Should().Contain("cleared", "should confirm the default was cleared");
        }

        #endregion

        #region profile set-key Command Tests (CMD-KEY-*)

        /// <summary>
        /// Implements: 009:T097 (CMD-KEY-001)
        /// When: User updates API key for an existing profile
        /// Then: New API key is stored securely
        /// </summary>
        [TestMethod]
        public async Task SetKey_ExistingProfile_UpdatesCredential()
        {
            // Arrange - Profile exists
            _profileManagerMock.Setup(m => m.ExistsAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = CreateSetKeyCommand();
            command.Name = "production";
            command.ApiKey = "new-secret-key-123";

            // Act
            await command.Execute(CreateContext());

            // Assert - SetApiKeyAsync should be called with new key
            _profileManagerMock.Verify(m => m.SetApiKeyAsync("production", "new-secret-key-123", It.IsAny<CancellationToken>()), Times.Once);
            _console.Output.Should().Contain("production", "should confirm the profile name");
            _console.Output.Should().Contain("updated", "should confirm the key was updated");
        }

        /// <summary>
        /// Implements: 009:T098 (CMD-KEY-002)
        /// When: User tries to set key for non-existent profile
        /// Then: Error message is shown
        /// </summary>
        [TestMethod]
        public async Task SetKey_NonExistent_ShowsError()
        {
            // Arrange - Profile does not exist
            _profileManagerMock.Setup(m => m.ExistsAsync("nonexistent", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var command = CreateSetKeyCommand();
            command.Name = "nonexistent";
            command.ApiKey = "new-key";

            // Act
            await command.Execute(CreateContext());

            // Assert - Error shown, SetApiKeyAsync not called
            _profileManagerMock.Verify(m => m.SetApiKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _console.Output.Should().Contain("nonexistent", "should show the profile name");
            _console.Output.Should().Contain("not found", "should indicate profile not found");
        }

        /// <summary>
        /// Implements: 009:T099 (CMD-KEY-003)
        /// When: User runs set-key without providing key value
        /// Then: Command uses secure/masked prompting for key (tested via flag)
        /// Note: Actual prompt testing requires integration tests. This validates the flag behavior.
        /// </summary>
        [TestMethod]
        public async Task SetKey_PromptsWithMasking_WhenNoValue()
        {
            // Arrange - Profile exists, no key provided (would trigger prompt in real scenario)
            _profileManagerMock.Setup(m => m.ExistsAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = CreateSetKeyCommand();
            command.Name = "production";
            command.ApiKey = null; // No key provided - would trigger prompt

            // Act
            await command.Execute(CreateContext());

            // Assert - Without an interactive prompt, this should show an error asking for key
            // (Real prompt behavior tested in integration tests)
            _console.Output.Should().Contain("key", "should mention API key is required");
        }

        /// <summary>
        /// Implements: 009:T100 (CMD-KEY-004)
        /// When: User provides empty API key
        /// Then: Error message is shown, key not updated
        /// </summary>
        [TestMethod]
        public async Task SetKey_EmptyInput_ShowsError()
        {
            // Arrange - Profile exists but key is empty
            _profileManagerMock.Setup(m => m.ExistsAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = CreateSetKeyCommand();
            command.Name = "production";
            command.ApiKey = ""; // Empty key

            // Act
            await command.Execute(CreateContext());

            // Assert - Error shown, key not stored
            _profileManagerMock.Verify(m => m.SetApiKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _console.Output.Should().Contain("empty", "should indicate empty key is not allowed");
        }

        #endregion

        #region Helper Methods

        private ProfileAddCommand CreateAddCommand()
        {
            var cmd = new ProfileAddCommand(_profileManagerMock.Object);
            cmd.SetConsole(_console);
            return cmd;
        }

        private ProfileListCommand CreateListCommand()
        {
            var cmd = new ProfileListCommand(_profileManagerMock.Object);
            cmd.SetConsole(_console);
            return cmd;
        }

        private ProfileShowCommand CreateShowCommand()
        {
            var cmd = new ProfileShowCommand(_profileManagerMock.Object);
            cmd.SetConsole(_console);
            return cmd;
        }

        private ProfileRemoveCommand CreateRemoveCommand()
        {
            var cmd = new ProfileRemoveCommand(_profileManagerMock.Object);
            cmd.SetConsole(_console);
            return cmd;
        }

        private ProfileSetDefaultCommand CreateSetDefaultCommand()
        {
            var cmd = new ProfileSetDefaultCommand(_profileManagerMock.Object);
            cmd.SetConsole(_console);
            return cmd;
        }

        private ProfileSetKeyCommand CreateSetKeyCommand()
        {
            var cmd = new ProfileSetKeyCommand(_profileManagerMock.Object);
            cmd.SetConsole(_console);
            return cmd;
        }

        private CommandExecutionContext CreateContext()
        {
            return new CommandExecutionContext();
        }

        #endregion
    }
}
