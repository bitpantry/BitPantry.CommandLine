using BitPantry.CommandLine.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Input
{
    [TestClass]
    public class AppNameSegmentTests
    {
        #region CV-009: Constructed with name returns that name

        /// <summary>
        /// Implements: CV-009
        /// When AppNameSegment constructed with name, Then Render returns that name
        /// </summary>
        [TestMethod]
        public void Render_WithCustomName_ReturnsCustomName()
        {
            // Arrange
            var segment = new AppNameSegment("myapp");

            // Act
            var result = segment.Render();

            // Assert
            result.Should().Be("myapp");
        }

        /// <summary>
        /// Implements: CV-009 (markup support)
        /// When AppNameSegment constructed with Spectre markup, Then Render returns markup unchanged
        /// </summary>
        [TestMethod]
        public void Render_WithMarkupName_ReturnsMarkupName()
        {
            // Arrange
            var segment = new AppNameSegment("[bold cyan]myapp[/]");

            // Act
            var result = segment.Render();

            // Assert
            result.Should().Be("[bold cyan]myapp[/]");
        }

        #endregion

        #region CV-010: Constructed with null returns default name

        /// <summary>
        /// Implements: CV-010
        /// When AppNameSegment constructed with null, Then Render returns entry assembly name (lowercase) or "cli" fallback
        /// </summary>
        [TestMethod]
        public void Render_WithNullName_ReturnsFallbackName()
        {
            // Arrange
            var segment = new AppNameSegment(null);

            // Act
            var result = segment.Render();

            // Assert
            // In test context, entry assembly is "testhost" or similar, or "cli" if null
            // The key behavior: it must return a lowercase string, never null/empty
            result.Should().NotBeNullOrEmpty();
            result.Should().Be(result.ToLowerInvariant(), "default name should be lowercase");
        }

        /// <summary>
        /// Implements: CV-010 (default constructor)
        /// When AppNameSegment constructed with default constructor, Then Render returns a valid default name
        /// </summary>
        [TestMethod]
        public void Render_WithDefaultConstructor_ReturnsFallbackName()
        {
            // Arrange
            var segment = new AppNameSegment();

            // Act
            var result = segment.Render();

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Be(result.ToLowerInvariant(), "default name should be lowercase");
        }

        #endregion

        #region CV-011: Order returns 0 (core segment range)

        /// <summary>
        /// Implements: CV-011
        /// When AppNameSegment.Order accessed, Then returns 0 (core segment range)
        /// </summary>
        [TestMethod]
        public void Order_ReturnsZero()
        {
            // Arrange
            var segment = new AppNameSegment("test");

            // Act
            var order = segment.Order;

            // Assert
            order.Should().Be(0);
        }

        /// <summary>
        /// Implements: CV-011 (convention validation)
        /// When AppNameSegment.Order accessed, Then value is in core range (0-99)
        /// </summary>
        [TestMethod]
        public void Order_IsInCoreRange()
        {
            // Arrange
            var segment = new AppNameSegment();

            // Act
            var order = segment.Order;

            // Assert - Core range is 0-99 per IPromptSegment convention
            order.Should().BeInRange(0, 99);
        }

        #endregion
    }
}
