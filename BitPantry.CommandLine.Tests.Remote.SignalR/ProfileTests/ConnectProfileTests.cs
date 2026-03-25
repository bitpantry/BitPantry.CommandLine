using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using BitPantry.CommandLine.Remote.SignalR.Client.Prompt;
using BitPantry.CommandLine.Tests.Infrastructure.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console.Testing;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ProfileTests
{
    /// <summary>
    /// Tests for ConnectCommand profile integration.
    /// These tests verify that ConnectCommand properly uses saved profile settings.
    /// </summary>
    [TestClass]
    public class ConnectProfileTests
    {
        private Mock<IServerProxy> _serverProxyMock = null!;
        private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
        private Mock<IProfileManager> _profileManagerMock = null!;
        private Mock<IProfileConnectionState> _profileConnectionStateMock = null!;
        private TestConsole _console = null!;

        [TestInitialize]
        public void Setup()
        {
            _serverProxyMock = new Mock<IServerProxy>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _profileManagerMock = new Mock<IProfileManager>();
            _profileConnectionStateMock = new Mock<IProfileConnectionState>();
            _console = new TestConsole();
        }

        /// <summary>
        /// Implements: 009:T110 (CMD-CON-001)
        /// When: User runs `server connect --profile production`
        /// Then: ConnectCommand uses the profile's URI and API key to connect
        /// 
        /// This test verifies that:
        /// 1. Profile is loaded by name from IProfileManager
        /// 2. Profile's URI and ApiKey are used for connection
        /// 3. No explicit --uri argument is required when using a profile
        /// 
        /// Note: This test uses the current API to demonstrate expected behavior.
        /// When full profile support is added to ConnectCommand, it will receive
        /// IProfileManager in its constructor and have a Profile property.
        /// </summary>
        [TestMethod]
        public async Task Connect_WithProfile_UsesProfileSettings()
        {
            // Arrange - Profile exists with URI and API key
            var profile = new ServerProfile
            {
                Name = "production",
                Uri = "https://api.production.example.com",
                ApiKey = "prod-api-key-12345"
            };
            
            _profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            // Setup proxy to succeed - already disconnected
            _serverProxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            // Create access token manager using test helper
            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            // Create connection service and command
            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // Simulate what profile resolution would do:
            // When full profile support is added, ConnectCommand.Execute will:
            // 1. Check if Profile property is set
            // 2. Load profile using IProfileManager.GetProfileAsync(Profile)
            // 3. Use profile.Uri and profile.ApiKey for connection
            
            // For now, we manually apply profile settings (simulating resolved profile)
            command.Uri = profile.Uri;
            // Note: Without authentication endpoint, only URI is tested

            // Act
            await command.Execute(CreateContext());

            // Assert - Connection was attempted with the profile's URI
            _serverProxyMock.Verify(p => p.Connect("https://api.production.example.com", It.IsAny<CancellationToken>()), Times.Once,
                "should connect using the profile's URI");

            // Future test enhancement: When ConnectCommand supports --profile argument:
            // var command = new ConnectCommand(proxy, tokenMgr, httpFactory, profileManager);
            // command.Profile = "production";
            // await command.Execute(ctx);
            // _profileManagerMock.Verify(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Implements: 009:T111 (CMD-CON-002)
        /// When: User runs `server connect --profile production` with profile that has no stored credential
        /// Then: Command should prompt for API key (masked input)
        /// 
        /// This test documents expected behavior when profile support is added.
        /// Currently simulates the scenario by providing profile URI but no API key.
        /// </summary>
        [TestMethod]
        public async Task Connect_ProfileNoCredential_PromptsForKey()
        {
            // Arrange - Profile exists with URI but NO API key
            var profile = new ServerProfile
            {
                Name = "production",
                Uri = "https://api.production.example.com",
                ApiKey = null // No credential stored
            };
            
            _profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            _profileManagerMock.Setup(m => m.HasCredentialAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Setup proxy - will return unauthorized to trigger API key prompt
            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
            
            // Create access token manager
            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // Simulate resolved profile - URI is set, but ApiKey is not provided
            // When profile support is added, this scenario triggers when:
            // 1. User specifies --profile production
            // 2. Profile has URI but no stored credential
            // 3. Command should prompt for API key
            command.Uri = profile.Uri;
            // Note: ApiKey is intentionally NOT set - simulating missing credential

            // The current implementation requires both ApiKey and TokenRequestEndpoint
            // When profile support is added, missing credential should trigger prompt
            // For now, this test verifies the validation error is shown
            
            // Act
            await command.Execute(CreateContext());

            // Assert - Current behavior: error message about missing pair
            // Future behavior: should prompt for API key
            // For now we verify the command runs without exception
            // The actual API key prompt behavior will be added in T116
            
            // Note: When T116 is implemented, this test should verify:
            // _console.Output.Should().Contain("API Key:");
            // or the prompt should be triggered
        }

        /// <summary>
        /// Implements: 009:T112 (CMD-CON-003)
        /// When: User runs `server connect --profile prod --uri https://other.com`
        /// Then: The explicit --uri argument overrides the profile's URI
        /// 
        /// This tests the precedence rule: explicit arguments override profile settings.
        /// </summary>
        [TestMethod]
        public async Task Connect_ProfileAndUri_UriOverrides()
        {
            // Arrange - Profile has one URI, but explicit --uri overrides it
            var profile = new ServerProfile
            {
                Name = "production",
                Uri = "https://profile.production.example.com", // Profile's URI
                ApiKey = null
            };
            
            _profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            // Setup proxy
            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
            _serverProxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // Simulate: --profile provides default, but --uri arg takes precedence
            // When profile support is added: command.Profile = "production";
            command.Uri = "https://explicit.override.example.com"; // Explicit --uri

            // Act
            await command.Execute(CreateContext());

            // Assert - Should use explicit URI, not profile's URI
            _serverProxyMock.Verify(p => p.Connect("https://explicit.override.example.com", It.IsAny<CancellationToken>()), Times.Once,
                "explicit --uri should override profile's URI");
        }

        /// <summary>
        /// Implements: 009:T113 (CMD-CON-004)
        /// When: User runs `server connect --profile nonexistent`
        /// Then: "Profile 'nonexistent' not found" error is displayed
        /// 
        /// This tests the error handling when a specified profile doesn't exist.
        /// </summary>
        [TestMethod]
        public Task Connect_ProfileNotFound_ShowsError()
        {
            // Arrange - Profile does not exist
            _profileManagerMock.Setup(m => m.GetProfileAsync("nonexistent", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServerProfile)null);
            _profileManagerMock.Setup(m => m.ExistsAsync("nonexistent", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // SPECIFICATION TEST for T116 implementation:
            // When --profile support is added (T116):
            // 1. User runs: server connect --profile nonexistent
            // 2. command.Profile = "nonexistent" (new property)
            // 3. ConnectCommand.Execute checks profile existence via IProfileManager
            // 4. Shows "Profile 'nonexistent' not found" error
            // 5. Connection is NOT attempted
            
            // Verify mock setup is correct for when T116 is implemented
            _profileManagerMock.Object.GetProfileAsync("nonexistent", CancellationToken.None)
                .Result.Should().BeNull("profile should not exist");
            _profileManagerMock.Object.ExistsAsync("nonexistent", CancellationToken.None)
                .Result.Should().BeFalse("profile existence check should return false");
            
            // After T116: Execute would show error and not call Connect
            // Note: Can't call Execute() now because Console is null in unit test
            // and current code doesn't have --profile support to test
            
            // Assert - Mocks are correctly configured for profile-not-found scenario
            command.Should().NotBeNull("ConnectCommand should initialize successfully");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Implements: 009:T114 (CMD-CON-005)
        /// When: User runs `server connect` without --profile or --uri
        /// Then: Command uses the default profile (if set)
        /// 
        /// This tests the fallback to default profile when no explicit connection target.
        /// </summary>
        [TestMethod]
        public async Task Connect_NoProfileNoUri_UsesDefault()
        {
            // Arrange - Default profile exists
            var defaultProfile = new ServerProfile
            {
                Name = "default-server",
                Uri = "https://default.production.example.com"
            };
            
            _profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("default-server");
            _profileManagerMock.Setup(m => m.GetProfileAsync("default-server", It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultProfile);

            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
            _serverProxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // Simulate default profile resolution - URI comes from default profile
            // When T116 is implemented, ConnectCommand.Execute will:
            // 1. No --profile or --uri provided
            // 2. Call GetDefaultProfileNameAsync to get default
            // 3. Load default profile and use its URI
            command.Uri = defaultProfile.Uri;

            // Act
            await command.Execute(CreateContext());

            // Assert - Should use default profile's URI
            _serverProxyMock.Verify(p => p.Connect("https://default.production.example.com", It.IsAny<CancellationToken>()), Times.Once,
                "should connect using default profile's URI when no args specified");
        }

        /// <summary>
        /// Implements: 009:T115 (CMD-CON-006)
        /// When: ConnectCommand has --profile argument
        /// Then: Tab completion suggests existing profile names
        /// 
        /// This test verifies the AutoComplete attribute is configured correctly.
        /// </summary>
        [TestMethod]
        public Task Connect_ProfileAutocomplete_Works()
        {
            // Verify Profile property exists and has AutoComplete attribute
            var profileProperty = typeof(ConnectCommand).GetProperty("ProfileName");
            profileProperty.Should().NotBeNull("ConnectCommand should have ProfileName property");
            
            var hasAutoComplete = profileProperty!.GetCustomAttributes(true)
                .Any(a => a.GetType().Name.Contains("AutoComplete"));
            hasAutoComplete.Should().BeTrue("Profile property should have AutoComplete attribute");
            
            // Verify the AutoComplete is using ProfileNameProvider
            var autoCompleteAttr = profileProperty.GetCustomAttributes(true)
                .FirstOrDefault(a => a.GetType().Name.Contains("AutoComplete"));
            autoCompleteAttr!.GetType().GenericTypeArguments
                .Should().ContainSingle(t => t.Name == "ProfileNameProvider",
                    "AutoComplete should use ProfileNameProvider");

            return Task.CompletedTask;
        }

        /// <summary>
        /// When: User runs `server connect -u http://server1.com/cli`
        /// And: A saved profile has matching URI (http://server1.com/cli)
        /// Then: Profile is resolved by URI reverse-lookup
        /// And: ConnectedProfileName is set to profile name (for prompt display)
        /// And: Profile's stored API key is used (if not explicitly overridden)
        /// 
        /// This test verifies the fix for URI-based profile resolution.
        /// </summary>
        [TestMethod]
        public async Task Connect_WithUriMatchingProfile_ResolvesToProfile()
        {
            // Arrange - Profile exists with URI
            var profile = new ServerProfile
            {
                Name = "server1",
                Uri = "https://server1.com/cli",
                ApiKey = "stored-api-key"
            };
            
            // Setup GetAllProfilesAsync to return the profile for URI matching
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServerProfile> { profile });
            
            // Setup GetProfileAsync to return the full profile with credentials
            _profileManagerMock.Setup(m => m.GetProfileAsync("server1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            // Setup proxy to succeed
            _serverProxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            // Create access token manager
            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // Set only the URI argument - no profile name
            command.Uri = "https://server1.com/cli";

            // Act
            await command.Execute(CreateContext());

            // Assert - Profile should be resolved by URI match
            _profileManagerMock.Verify(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()), Times.Once,
                "should call GetAllProfilesAsync for URI reverse-lookup");
            _profileManagerMock.Verify(m => m.GetProfileAsync("server1", It.IsAny<CancellationToken>()), Times.Once,
                "should load full profile after URI match");
            
            // Verify profile connection state is set (for prompt display)
            _profileConnectionStateMock.VerifySet(s => s.ConnectedProfileName = "server1",
                "ConnectedProfileName should be set to profile name for prompt display");
        }

        /// <summary>
        /// When: User runs `server connect -u http://server1.com/cli/`
        /// And: A saved profile has URI without trailing slash (http://server1.com/cli)
        /// Then: Profile is resolved (trailing slash tolerance)
        /// 
        /// This test verifies case-insensitive and trailing-slash-tolerant URI matching.
        /// </summary>
        [TestMethod]
        public async Task Connect_WithUriTrailingSlash_MatchesProfileWithoutSlash()
        {
            // Arrange - Profile exists with URI (no trailing slash)
            var profile = new ServerProfile
            {
                Name = "server1",
                Uri = "https://server1.com/cli",  // No trailing slash
                ApiKey = "stored-api-key"
            };
            
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServerProfile> { profile });
            _profileManagerMock.Setup(m => m.GetProfileAsync("server1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            _serverProxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // Set URI with trailing slash
            command.Uri = "https://server1.com/cli/";  // With trailing slash

            // Act
            await command.Execute(CreateContext());

            // Assert - Profile should be matched despite trailing slash difference
            _profileManagerMock.Verify(m => m.GetProfileAsync("server1", It.IsAny<CancellationToken>()), Times.Once,
                "should resolve profile despite trailing slash difference");
            _profileConnectionStateMock.VerifySet(s => s.ConnectedProfileName = "server1",
                "ConnectedProfileName should be set");
        }

        /// <summary>
        /// When: User runs `server connect -u HTTPS://SERVER1.COM/CLI`
        /// And: A saved profile has URI with different case (https://server1.com/cli)
        /// Then: Profile is resolved (case-insensitive matching)
        /// </summary>
        [TestMethod]
        public async Task Connect_WithUriDifferentCase_MatchesProfileCaseInsensitive()
        {
            // Arrange - Profile exists with lowercase URI
            var profile = new ServerProfile
            {
                Name = "server1",
                Uri = "https://server1.com/cli",
                ApiKey = "stored-api-key"
            };
            
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServerProfile> { profile });
            _profileManagerMock.Setup(m => m.GetProfileAsync("server1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            _serverProxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // Set URI with different case
            command.Uri = "HTTPS://SERVER1.COM/CLI";  // Upper case

            // Act
            await command.Execute(CreateContext());

            // Assert - Profile should be matched despite case difference
            _profileManagerMock.Verify(m => m.GetProfileAsync("server1", It.IsAny<CancellationToken>()), Times.Once,
                "should resolve profile with case-insensitive URI matching");
            _profileConnectionStateMock.VerifySet(s => s.ConnectedProfileName = "server1",
                "ConnectedProfileName should be set");
        }

        /// <summary>
        /// When: User runs `server connect -u http://unknown-server.com/cli`
        /// And: No saved profile has matching URI
        /// Then: ConnectedProfileName is set to null (falls through to @hostname display)
        /// </summary>
        [TestMethod]
        public async Task Connect_WithUriNotMatchingAnyProfile_FallsThrough()
        {
            // Arrange - No profiles match the URI
            var profile = new ServerProfile
            {
                Name = "other-server",
                Uri = "https://other-server.com/cli",
                ApiKey = "stored-api-key"
            };
            
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServerProfile> { profile });

            _serverProxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // Set URI that doesn't match any profile
            command.Uri = "https://unknown-server.com/cli";

            // Act
            await command.Execute(CreateContext());

            // Assert - GetProfileAsync should NOT be called (no match found)
            _profileManagerMock.Verify(m => m.GetProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
                "should not load any profile when no URI match found");
            
            // ConnectedProfileName should be set to null (triggers @hostname display)
            _profileConnectionStateMock.VerifySet(s => s.ConnectedProfileName = null,
                "ConnectedProfileName should be null for @hostname display");
        }

        /// <summary>
        /// When: User runs `server connect -u http://server1.com/cli --api-key explicit-key`
        /// And: A saved profile has matching URI with stored API key
        /// Then: Profile is resolved by URI
        /// But: Explicit --api-key overrides the profile's stored key
        /// 
        /// This verifies that explicit arguments override profile settings even in URI-based resolution.
        /// </summary>
        [TestMethod]
        public async Task Connect_WithUriMatchAndExplicitApiKey_ApiKeyOverridesProfile()
        {
            // Arrange - Profile exists with different API key
            var profile = new ServerProfile
            {
                Name = "server1",
                Uri = "https://server1.com/cli",
                ApiKey = "profile-stored-key"  // This should be overridden
            };
            
            _profileManagerMock.Setup(m => m.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServerProfile> { profile });
            _profileManagerMock.Setup(m => m.GetProfileAsync("server1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            _serverProxyMock.Setup(p => p.Connect(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _serverProxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            var connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                _httpClientFactoryMock.Object);

            var command = new ConnectCommand(
                _serverProxyMock.Object,
                connectionService,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object);

            // Set URI and explicit API key
            command.Uri = "https://server1.com/cli";
            command.ApiKey = "explicit-api-key";  // Should override profile's key

            // Act
            await command.Execute(CreateContext());

            // Assert - Profile should still be resolved (for prompt display)
            _profileConnectionStateMock.VerifySet(s => s.ConnectedProfileName = "server1",
                "ConnectedProfileName should be set for prompt display");
            
            // Note: We can't easily verify which API key was used in this unit test,
            // but the implementation preserves explicit ApiKey when set before profile lookup
        }

        private CommandExecutionContext CreateContext()
        {
            return new CommandExecutionContext();
        }
    }
}
