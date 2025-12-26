using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.PromptSegments;
using FluentAssertions;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class ServerConnectionSegmentTests
    {
        private Mock<IServerProxy> _serverProxyMock;

        [TestInitialize]
        public void Setup()
        {
            _serverProxyMock = new Mock<IServerProxy>();
        }

        [TestMethod]
        public void Order_Returns100()
        {
            // Arrange
            var segment = new ServerConnectionSegment(_serverProxyMock.Object);

            // Act & Assert
            segment.Order.Should().Be(100);
        }

        [TestMethod]
        public void Render_ReturnsNull_WhenDisconnected()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
            var segment = new ServerConnectionSegment(_serverProxyMock.Object);

            // Act
            var result = segment.Render();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Render_ReturnsNull_WhenConnecting()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connecting);
            var segment = new ServerConnectionSegment(_serverProxyMock.Object);

            // Act
            var result = segment.Render();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Render_ReturnsNull_WhenReconnecting()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Reconnecting);
            var segment = new ServerConnectionSegment(_serverProxyMock.Object);

            // Act
            var result = segment.Render();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Render_ReturnsHostname_WhenConnected()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _serverProxyMock.Setup(p => p.ConnectionUri).Returns(new Uri("https://api.example.com"));
            var segment = new ServerConnectionSegment(_serverProxyMock.Object);

            // Act
            var result = segment.Render();

            // Assert
            result.Should().Be("@api.example.com");
        }

        [TestMethod]
        public void Render_HandlesNullUri_WhenConnected()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _serverProxyMock.Setup(p => p.ConnectionUri).Returns((Uri)null);
            var segment = new ServerConnectionSegment(_serverProxyMock.Object);

            // Act
            var result = segment.Render();

            // Assert
            result.Should().Be("@");
        }

        [TestMethod]
        public void Render_ExtractsHostFromUri()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _serverProxyMock.Setup(p => p.ConnectionUri).Returns(new Uri("https://server.domain.com:8443/api/hub"));
            var segment = new ServerConnectionSegment(_serverProxyMock.Object);

            // Act
            var result = segment.Render();

            // Assert
            result.Should().Be("@server.domain.com");
        }
    }
}
