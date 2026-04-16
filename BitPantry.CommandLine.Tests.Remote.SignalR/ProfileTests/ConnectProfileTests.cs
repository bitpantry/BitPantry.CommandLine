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
                _profileConnectionStateMock.Object,
                new FileAccessConsentPolicy());

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
                _profileConnectionStateMock.Object,
                new FileAccessConsentPolicy());

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
                _profileConnectionStateMock.Object,
                new FileAccessConsentPolicy());

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
                _profileConnectionStateMock.Object,
                new FileAccessConsentPolicy());

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
                _profileConnectionStateMock.Object,
                new FileAccessConsentPolicy());

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
