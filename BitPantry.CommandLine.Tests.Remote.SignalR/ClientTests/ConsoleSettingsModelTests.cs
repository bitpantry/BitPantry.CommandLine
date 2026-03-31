using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Testing;
using System.Text;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class ConsoleSettingsModelTests
    {
        [DataTestMethod]
        [DataRow(1)]
        [DataRow(80)]
        [DataRow(120)]
        [DataRow(150)]
        [DataRow(200)]
        [DataRow(500)]
        public void Constructor_CapturesWidth(int expectedWidth)
        {
            // Arrange
            var console = new TestConsole();
            console.Profile.Width = expectedWidth;

            // Act
            var model = new ConsoleSettingsModel(console);

            // Assert
            model.Width.Should().Be(expectedWidth);
        }

        [TestMethod]
        public void Constructor_NullConsole_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new ConsoleSettingsModel(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("console");
        }

        [TestMethod]
        public void Constructor_CapturesAllProperties()
        {
            // Arrange
            var console = new TestConsole();
            console.Profile.Width = 120;
            // Note: TestConsole has limited configuration options compared to real AnsiConsole

            // Act
            var model = new ConsoleSettingsModel(console);

            // Assert
            model.Width.Should().Be(120);
            model.EncodingName.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void DefaultConstructor_HasDefaultWidth()
        {
            // Arrange & Act
            var model = new ConsoleSettingsModel();

            // Assert - Width defaults to 0 for JSON deserialization.
            // Note: Width=0 will cause ArgumentException in SignalRAnsiConsole constructor,
            // so valid models must be constructed from an IAnsiConsole with a positive width.
            model.Width.Should().Be(0);
        }
    }
}

