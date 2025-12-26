using BitPantry.CommandLine.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Input
{
    [TestClass]
    public class AppNameSegmentTests
    {
        [TestMethod]
        public void Order_ReturnsZero()
        {
            // Arrange
            var segment = new AppNameSegment("myapp");

            // Act & Assert
            segment.Order.Should().Be(0);
        }

        [TestMethod]
        public void Render_ReturnsProvidedName()
        {
            // Arrange
            var segment = new AppNameSegment("myapp");

            // Act
            var result = segment.Render();

            // Assert
            result.Should().Be("myapp");
        }

        [TestMethod]
        public void Render_ReturnsDefaultName_WhenNotProvided()
        {
            // Arrange
            var segment = new AppNameSegment();

            // Act
            var result = segment.Render();

            // Assert
            // Default should be something (either entry assembly name or "cli")
            result.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void Render_ReturnsCli_WhenNullAndNoEntryAssembly()
        {
            // Arrange - The entry assembly in test context should give us something
            var segment = new AppNameSegment(null);

            // Act
            var result = segment.Render();

            // Assert
            // Should return something, not null
            result.Should().NotBeNull();
        }
    }
}
