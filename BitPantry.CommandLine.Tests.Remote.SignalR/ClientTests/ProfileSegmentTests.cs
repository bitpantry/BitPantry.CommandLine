using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.PromptSegments;
using FluentAssertions;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class ProfileSegmentTests
    {
        private Mock<IServerProxy> _serverProxyMock;

        [TestInitialize]
        public void Setup()
        {
            _serverProxyMock = new Mock<IServerProxy>();
        }

        [TestMethod]
        public void Order_Returns110()
        {
            // Arrange
            var segment = new ProfileSegment(_serverProxyMock.Object);

            // Act & Assert
            segment.Order.Should().Be(110);
        }

        [TestMethod]
        public void Render_ReturnsNull_WhenDisconnected()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
            var segment = new ProfileSegment(_serverProxyMock.Object);
            segment.SetCurrentProfile("prod");

            // Act
            var result = segment.Render();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Render_ReturnsNull_WhenNoProfile()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            var segment = new ProfileSegment(_serverProxyMock.Object);

            // Act
            var result = segment.Render();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Render_ReturnsProfileInBrackets_WhenConnectedWithProfile()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            var segment = new ProfileSegment(_serverProxyMock.Object);
            segment.SetCurrentProfile("prod");

            // Act
            var result = segment.Render();

            // Assert
            result.Should().Be("[prod]");
        }

        [TestMethod]
        public void SetCurrentProfile_UpdatesProfileName()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            var segment = new ProfileSegment(_serverProxyMock.Object);
            segment.SetCurrentProfile("dev");

            // Act
            segment.SetCurrentProfile("prod");
            var result = segment.Render();

            // Assert
            result.Should().Be("[prod]");
        }

        [TestMethod]
        public void ClearCurrentProfile_RemovesProfileName()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            var segment = new ProfileSegment(_serverProxyMock.Object);
            segment.SetCurrentProfile("prod");

            // Act
            segment.ClearCurrentProfile();
            var result = segment.Render();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Render_ReturnsNull_WhenProfileIsEmpty()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            var segment = new ProfileSegment(_serverProxyMock.Object);
            segment.SetCurrentProfile("");

            // Act
            var result = segment.Render();

            // Assert
            result.Should().BeNull();
        }
    }
}
