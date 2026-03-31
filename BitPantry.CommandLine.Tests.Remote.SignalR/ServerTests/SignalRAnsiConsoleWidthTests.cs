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
        [TestMethod]
        public void Constructor_WidthFromSettings_AppliesCorrectly()
        {
            // Arrange
            var proxy = new TestClientProxy();
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var rpcMsgReg = new RpcMessageRegistry(rpcScopeMock.Object);
            var settings = new SignalRAnsiConsoleSettings
            {
                Ansi = true,
                ColorSystem = Spectre.Console.ColorSystem.Standard,
                Interactive = true,
                Width = 150
            };

            // Act
            using var console = new SignalRAnsiConsole(proxy, rpcMsgReg, settings);

            // Assert
            console.Profile.Width.Should().Be(150);
        }

        [TestMethod]
        public void Constructor_ZeroWidth_ThrowsArgumentException()
        {
            // Arrange
            var proxy = new TestClientProxy();
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var rpcMsgReg = new RpcMessageRegistry(rpcScopeMock.Object);
            var settings = new SignalRAnsiConsoleSettings
            {
                Ansi = true,
                ColorSystem = Spectre.Console.ColorSystem.Standard,
                Interactive = true,
                Width = 0
            };

            // Act
            Action act = () => new SignalRAnsiConsole(proxy, rpcMsgReg, settings);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Width must be a positive value*")
                .And.ParamName.Should().Be("settings");
        }

        [TestMethod]
        public void Constructor_NegativeWidth_ThrowsArgumentException()
        {
            // Arrange
            var proxy = new TestClientProxy();
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var rpcMsgReg = new RpcMessageRegistry(rpcScopeMock.Object);
            var settings = new SignalRAnsiConsoleSettings
            {
                Ansi = true,
                ColorSystem = Spectre.Console.ColorSystem.Standard,
                Interactive = true,
                Width = -1
            };

            // Act
            Action act = () => new SignalRAnsiConsole(proxy, rpcMsgReg, settings);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Width must be a positive value*")
                .And.ParamName.Should().Be("settings");
        }

        [TestMethod]
        public void Constructor_DifferentWidthValues_AppliesCorrectly()
        {
            // Arrange
            var proxy = new TestClientProxy();
            var rpcScopeMock = new Mock<IRpcScope>();
            rpcScopeMock.Setup(s => s.GetIdentifier()).Returns("testScope");
            var rpcMsgReg = new RpcMessageRegistry(rpcScopeMock.Object);

            // Test with various valid widths
            var widths = new[] { 80, 120, 200, 1 };

            foreach (var width in widths)
            {
                var settings = new SignalRAnsiConsoleSettings
                {
                    Ansi = true,
                    ColorSystem = Spectre.Console.ColorSystem.Standard,
                    Interactive = true,
                    Width = width
                };

                // Act
                using var console = new SignalRAnsiConsole(proxy, rpcMsgReg, settings);

                // Assert
                console.Profile.Width.Should().Be(width, $"Width should be {width}");
            }
        }
    }
}
