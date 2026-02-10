using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using BitPantry.CommandLine.Remote.SignalR.Client.Prompt;
using BitPantry.CommandLine.Tests.Infrastructure.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Tests for SignalRAutoConnectHandler.
    /// Validates profile resolution, connection lifecycle, and error invariants.
    /// Uses EnvironmentProfileOverride for env var tests (no process-wide state).
    /// </summary>
    [TestClass]
    public class SignalRAutoConnectHandlerTests
    {
        private Mock<ILogger<SignalRAutoConnectHandler>> _loggerMock;
        private Mock<IProfileManager> _profileManagerMock;
        private Mock<IProfileConnectionState> _profileConnectionStateMock;
        private ConnectionService _connectionService;
        private Mock<IServerProxy> _proxyMock;
        private SignalRAutoConnectHandler _handler;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<SignalRAutoConnectHandler>>();
            _profileManagerMock = new Mock<IProfileManager>();
            _profileConnectionStateMock = new Mock<IProfileConnectionState>();

            // Use a real ConnectionService with mocked dependencies — proxy.Connect is on
            // the interface so we control behavior through _proxyMock
            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));

            _connectionService = new ConnectionService(
                new Mock<ILogger<ConnectionService>>().Object,
                accessTokenManager,
                new Mock<IHttpClientFactory>().Object);

            _proxyMock = new Mock<IServerProxy>();

            _handler = new SignalRAutoConnectHandler(
                _loggerMock.Object,
                _profileManagerMock.Object,
                _profileConnectionStateMock.Object,
                _connectionService);
        }

        #region AutoConnectEnabled = false

        [TestMethod]
        public async Task Disabled_WhenConnected_ReturnsTrue()
        {
            // Arrange
            _handler.AutoConnectEnabled = false;
            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task Disabled_WhenDisconnected_ReturnsFalse()
        {
            // Arrange
            _handler.AutoConnectEnabled = false;
            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Already Connected — Profile Match / Mismatch

        [TestMethod]
        public async Task AlreadyConnected_NoProfileRequested_ReturnsTrue()
        {
            // Arrange — connected, no profile name set, no env var, no default
            _handler.AutoConnectEnabled = true;
            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);

            // No profiles configured
            _profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null);

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert — already connected, null profile means no mismatch check
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task AlreadyConnected_SameProfile_ReturnsTrue()
        {
            // Arrange — connected via "dev" profile, requesting "dev"
            _handler.AutoConnectEnabled = true;
            _handler.RequestedProfileName = "dev";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _profileConnectionStateMock.SetupGet(s => s.ConnectedProfileName).Returns("dev");

            _profileManagerMock.Setup(m => m.GetProfileAsync("dev", It.IsAny<CancellationToken>()))
                .ReturnsAsync(MakeProfile("dev"));

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task AlreadyConnected_SameProfileCaseInsensitive_ReturnsTrue()
        {
            // Arrange — connected via "Dev", requesting "dev"
            _handler.AutoConnectEnabled = true;
            _handler.RequestedProfileName = "dev";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _profileConnectionStateMock.SetupGet(s => s.ConnectedProfileName).Returns("Dev");

            _profileManagerMock.Setup(m => m.GetProfileAsync("dev", It.IsAny<CancellationToken>()))
                .ReturnsAsync(MakeProfile("dev"));

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert — case-insensitive match should succeed
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task AlreadyConnected_DifferentProfile_Throws()
        {
            // Arrange — connected via "staging", requesting "production"
            _handler.AutoConnectEnabled = true;
            _handler.RequestedProfileName = "production";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _profileConnectionStateMock.SetupGet(s => s.ConnectedProfileName).Returns("staging");

            _profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(MakeProfile("production"));

            // Act & Assert
            var act = () => _handler.EnsureConnectedAsync(_proxyMock.Object);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*staging*production*disconnect*");
        }

        #endregion

        #region Profile Not Found — Throws

        [TestMethod]
        public async Task RequestedProfileNotFound_Throws()
        {
            // Arrange — explicit profile name that doesn't exist
            _handler.AutoConnectEnabled = true;
            _handler.RequestedProfileName = "nonexistent";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            _profileManagerMock.Setup(m => m.GetProfileAsync("nonexistent", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServerProfile)null);

            // Act & Assert
            var act = () => _handler.EnsureConnectedAsync(_proxyMock.Object);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*nonexistent*not found*");
        }

        [TestMethod]
        public async Task EnvVarProfileNotFound_Throws()
        {
            // Arrange — env var points to profile that doesn't exist
            _handler.AutoConnectEnabled = true;
            _handler.EnvironmentProfileOverride = "env-profile";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            _profileManagerMock.Setup(m => m.GetProfileAsync("env-profile", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServerProfile)null);

            // Act & Assert
            var act = () => _handler.EnsureConnectedAsync(_proxyMock.Object);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*env-profile*not found*");
        }

        #endregion

        #region Not Connected — No Profile Available

        [TestMethod]
        public async Task NotConnected_NoProfileAvailable_ReturnsFalse()
        {
            // Arrange — disconnected, no profile name, no env var, no default
            _handler.AutoConnectEnabled = true;

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            _profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null);

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Successful Auto-Connect

        [TestMethod]
        public async Task NotConnected_WithRequestedProfile_Connects()
        {
            // Arrange
            _handler.AutoConnectEnabled = true;
            _handler.RequestedProfileName = "production";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var profile = MakeProfile("production", "https://prod.example.com", "key-123");
            _profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            // ConnectionService delegates to proxy.Connect — control via proxy mock
            _proxyMock.Setup(p => p.Connect(profile.Uri, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            string capturedProfileName = null;
            _profileConnectionStateMock.SetupSet(s => s.ConnectedProfileName = It.IsAny<string>())
                .Callback<string>(v => capturedProfileName = v);

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert
            result.Should().BeTrue();
            capturedProfileName.Should().Be("production");
            _proxyMock.Verify(
                p => p.Connect(profile.Uri, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task NotConnected_WithEnvVarProfile_Connects()
        {
            // Arrange
            _handler.AutoConnectEnabled = true;
            _handler.EnvironmentProfileOverride = "env-profile";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var profile = MakeProfile("env-profile", "https://env.example.com", "env-key");
            _profileManagerMock.Setup(m => m.GetProfileAsync("env-profile", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            _proxyMock.Setup(p => p.Connect(profile.Uri, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            string capturedProfileName = null;
            _profileConnectionStateMock.SetupSet(s => s.ConnectedProfileName = It.IsAny<string>())
                .Callback<string>(v => capturedProfileName = v);

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert
            result.Should().BeTrue();
            capturedProfileName.Should().Be("env-profile");
        }

        [TestMethod]
        public async Task NotConnected_WithDefaultProfile_Connects()
        {
            // Arrange — no explicit name, no env var, but default profile exists
            _handler.AutoConnectEnabled = true;

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            _profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("default-prof");

            var profile = MakeProfile("default-prof", "https://default.example.com", "default-key");
            _profileManagerMock.Setup(m => m.GetProfileAsync("default-prof", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            _proxyMock.Setup(p => p.Connect(profile.Uri, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            string capturedProfileName = null;
            _profileConnectionStateMock.SetupSet(s => s.ConnectedProfileName = It.IsAny<string>())
                .Callback<string>(v => capturedProfileName = v);

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert
            result.Should().BeTrue();
            capturedProfileName.Should().Be("default-prof");
        }

        #endregion

        #region Profile Resolution Priority

        [TestMethod]
        public async Task RequestedProfileName_TakesPriority_OverEnvVar()
        {
            // Arrange — both RequestedProfileName and env var override set
            _handler.AutoConnectEnabled = true;
            _handler.RequestedProfileName = "explicit";
            _handler.EnvironmentProfileOverride = "env-profile";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var profile = MakeProfile("explicit", "https://explicit.example.com", "key");
            _profileManagerMock.Setup(m => m.GetProfileAsync("explicit", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            _proxyMock.Setup(p => p.Connect(profile.Uri, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _profileConnectionStateMock.SetupSet(s => s.ConnectedProfileName = It.IsAny<string>());

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert — explicit name should be used, not env var
            result.Should().BeTrue();
            _profileManagerMock.Verify(
                m => m.GetProfileAsync("explicit", It.IsAny<CancellationToken>()),
                Times.Once);
            _profileManagerMock.Verify(
                m => m.GetProfileAsync("env-profile", It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task EnvVar_TakesPriority_OverDefault()
        {
            // Arrange — env var override set, default also exists
            _handler.AutoConnectEnabled = true;
            _handler.EnvironmentProfileOverride = "env-profile";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            _profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("default-prof");

            var profile = MakeProfile("env-profile", "https://env.example.com", "key");
            _profileManagerMock.Setup(m => m.GetProfileAsync("env-profile", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            _proxyMock.Setup(p => p.Connect(profile.Uri, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _profileConnectionStateMock.SetupSet(s => s.ConnectedProfileName = It.IsAny<string>());

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert — env var should be used, default should not even be queried
            result.Should().BeTrue();
            _profileManagerMock.Verify(
                m => m.GetProfileAsync("env-profile", It.IsAny<CancellationToken>()),
                Times.Once);
            _profileManagerMock.Verify(
                m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region Connection Failure

        [TestMethod]
        public async Task ConnectFails_ReturnsFalse()
        {
            // Arrange
            _handler.AutoConnectEnabled = true;
            _handler.RequestedProfileName = "production";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var profile = MakeProfile("production", "https://prod.example.com", "key-123");
            _profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            // proxy.Connect throws — ConnectionService propagates → handler catches
            _proxyMock.Setup(p => p.Connect(profile.Uri, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection refused"));

            // Act
            var result = await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert — failure is caught and returns false
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task ConnectFails_DoesNotSetProfileConnectionState()
        {
            // Arrange
            _handler.AutoConnectEnabled = true;
            _handler.RequestedProfileName = "production";

            _proxyMock.SetupGet(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            var profile = MakeProfile("production", "https://prod.example.com", "key-123");
            _profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            _proxyMock.Setup(p => p.Connect(profile.Uri, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection refused"));

            // Act
            await _handler.EnsureConnectedAsync(_proxyMock.Object);

            // Assert — profile state should not be updated on failure
            _profileConnectionStateMock.VerifySet(
                s => s.ConnectedProfileName = It.IsAny<string>(),
                Times.Never);
        }

        #endregion

        #region Helpers

        private static ServerProfile MakeProfile(string name, string uri = "https://test.example.com", string apiKey = null)
        {
            return new ServerProfile
            {
                Name = name,
                Uri = uri,
                ApiKey = apiKey
            };
        }

        #endregion
    }
}
