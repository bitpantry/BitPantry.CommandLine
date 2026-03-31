using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Testing;
using System.Text;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    [TestClass]
    public class ConsoleSettingsModelTests
    {
        [TestMethod]
        public void Constructor_CapturesWidth()
        {
            // Arrange
            var console = new TestConsole();
            console.Profile.Width = 150;

            // Act
            var model = new ConsoleSettingsModel(console);

            // Assert
            model.Width.Should().Be(150);
        }

        [TestMethod]
        public void Constructor_CapturesVariousWidths()
        {
            // Test different valid width values
            var widths = new[] { 80, 120, 200, 1, 500 };

            foreach (var expectedWidth in widths)
            {
                // Arrange
                var console = new TestConsole();
                console.Profile.Width = expectedWidth;

                // Act
                var model = new ConsoleSettingsModel(console);

                // Assert
                model.Width.Should().Be(expectedWidth, $"Width should be {expectedWidth}");
            }
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

            // Assert - Width should default to 0 when using parameterless constructor
            model.Width.Should().Be(0);
        }
    }
}

