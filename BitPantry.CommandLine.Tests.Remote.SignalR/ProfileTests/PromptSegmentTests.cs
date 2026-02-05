using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Prompt;
using BitPantry.CommandLine.Client;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ProfileTests
{
    [TestClass]
    public class PromptSegmentTests
    {
        private Mock<IServerProxy> _serverProxyMock;
        private Mock<IProfileConnectionState> _profileConnectionStateMock;
        private ProfilePromptSegment _segment;

        [TestInitialize]
        public void Setup()
        {
            _serverProxyMock = new Mock<IServerProxy>();
            _profileConnectionStateMock = new Mock<IProfileConnectionState>();
            _segment = new ProfilePromptSegment(_serverProxyMock.Object, _profileConnectionStateMock.Object);
        }

        [TestMethod]
        public void ConnectedViaProfile_PromptShowsProfileName()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _profileConnectionStateMock.Setup(p => p.ConnectedProfileName).Returns("myprofile");

            // Act
            var result = _segment.Render();

            // Assert
            result.Should().Contain("myprofile");
        }

        [TestMethod]
        public void ConnectedViaProfile_PromptShowsBracketFormat()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _profileConnectionStateMock.Setup(p => p.ConnectedProfileName).Returns("testprofile");

            // Act
            var result = _segment.Render();

            // Assert
            result.Should().Be("[testprofile]");
        }

        [TestMethod]
        public void NotConnected_PromptHidesProfileSegment()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
            _profileConnectionStateMock.Setup(p => p.ConnectedProfileName).Returns("myprofile");

            // Act
            var result = _segment.Render();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ConnectedDirectUri_PromptHidesProfileSegment()
        {
            // Arrange
            _serverProxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _profileConnectionStateMock.Setup(p => p.ConnectedProfileName).Returns((string)null);

            // Act
            var result = _segment.Render();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Order_ShouldBe110()
        {
            // Assert - Order 110 is in connection state range (100-199),
            // after ServerConnectionSegment at 100
            _segment.Order.Should().Be(110);
        }
    }
}
