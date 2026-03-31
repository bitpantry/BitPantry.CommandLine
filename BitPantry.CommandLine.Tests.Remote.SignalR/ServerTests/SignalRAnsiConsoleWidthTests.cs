using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using FluentAssertions;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class SignalRAnsiConsoleWidthTests
    {
        private TestClientProxy _proxy;
        private RpcMessageRegistry _rpcMsgReg;

        [TestInitialize]
        public void Setup()
        {
            _proxy = new TestClientProxy();
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            _rpcMsgReg = new RpcMessageRegistry(rpcScopeMock.Object);
        }

        private SignalRAnsiConsoleSettings CreateSettings(int width) => new SignalRAnsiConsoleSettings
        {
            Ansi = true,
            ColorSystem = Spectre.Console.ColorSystem.Standard,
            Interactive = true,
            Width = width
        };

        [TestMethod]
        public void Constructor_WidthFromSettings_AppliesCorrectly()
        {
            // Arrange
            var settings = CreateSettings(width: 150);

            // Act
            using var console = new SignalRAnsiConsole(_proxy, _rpcMsgReg, settings);

            // Assert
            console.Profile.Width.Should().Be(150);
        }

        [TestMethod]
        public void Constructor_ZeroWidth_ThrowsArgumentException()
        {
            // Arrange
            var settings = CreateSettings(width: 0);

            // Act
            Action act = () => new SignalRAnsiConsole(_proxy, _rpcMsgReg, settings);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Width must be a positive value*")
                .And.ParamName.Should().Be("settings");
        }

        [TestMethod]
        public void Constructor_NegativeWidth_ThrowsArgumentException()
        {
            // Arrange
            var settings = CreateSettings(width: -1);

            // Act
            Action act = () => new SignalRAnsiConsole(_proxy, _rpcMsgReg, settings);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Width must be a positive value*")
                .And.ParamName.Should().Be("settings");
        }

        [TestMethod]
        public void Constructor_DifferentWidthValues_AppliesCorrectly()
        {
            // Test with various valid widths
            var widths = new[] { 80, 120, 200, 1 };

            foreach (var width in widths)
            {
                var settings = CreateSettings(width);

                // Act
                using var console = new SignalRAnsiConsole(_proxy, _rpcMsgReg, settings);

                // Assert
                console.Profile.Width.Should().Be(width, $"Width should be {width}");
            }
        }
    }
}
