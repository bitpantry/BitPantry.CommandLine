using BitPantry.CommandLine.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Tests for StyledSegment record type.
/// </summary>
[TestClass]
public class StyledSegmentTests
{
    // Implements: SETUP-001
    [TestMethod]
    public void StyledSegment_CreatedWithProperties_AllPropertiesAccessible()
    {
        // Arrange
        var style = new Style(foreground: Color.Cyan);

        // Act
        var segment = new StyledSegment("test", 0, 4, style);

        // Assert - all properties should be accessible and match inputs
        segment.Text.Should().Be("test");
        segment.Start.Should().Be(0);
        segment.End.Should().Be(4);
        segment.Style.Should().Be(style);
    }

    [TestMethod]
    public void StyledSegment_IsImmutable_RecordEquality()
    {
        // Arrange
        var style = new Style(foreground: Color.Cyan);
        var segment1 = new StyledSegment("test", 0, 4, style);
        var segment2 = new StyledSegment("test", 0, 4, style);

        // Assert - records with same values should be equal
        segment1.Should().Be(segment2);
    }
}
