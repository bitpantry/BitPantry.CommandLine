using System.Linq;
using BitPantry.CommandLine.Commands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Commands
{
    [TestClass]
    public class VersionCommandTests
    {
        [TestMethod]
        public void VersionCommand_HasCorrectName()
        {
            // Arrange
            var command = new VersionCommand();

            // Act
            var commandType = command.GetType();
            var attribute = commandType.GetCustomAttributes(typeof(BitPantry.CommandLine.API.CommandAttribute), false)
                .FirstOrDefault() as BitPantry.CommandLine.API.CommandAttribute;

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Name.Should().Be("version");
        }

        [TestMethod]
        public void VersionCommand_HasFullOption()
        {
            // Arrange
            var command = new VersionCommand();

            // Act
            var fullProperty = command.GetType().GetProperty("Full");

            // Assert
            fullProperty.Should().NotBeNull();
            fullProperty!.PropertyType.Should().Be(typeof(BitPantry.CommandLine.API.Option));
        }

        [TestMethod]
        public void VersionCommand_HasDescription()
        {
            // Arrange
            var command = new VersionCommand();

            // Act
            var commandType = command.GetType();
            var attribute = commandType.GetCustomAttributes(typeof(BitPantry.CommandLine.API.DescriptionAttribute), false)
                .FirstOrDefault() as BitPantry.CommandLine.API.DescriptionAttribute;

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Description.Should().Contain("version");
        }
    }
}
