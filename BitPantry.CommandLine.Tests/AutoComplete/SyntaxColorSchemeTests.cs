using BitPantry.CommandLine.AutoComplete;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Tests for SyntaxColorScheme static class.
/// </summary>
[TestClass]
public class SyntaxColorSchemeTests
{
    // Implements: CV-001
    [TestMethod]
    public void Group_ReturnsCyanStyle()
    {
        // Act
        var style = SyntaxColorScheme.Group;

        // Assert - verify the style is cyan (Color.Cyan maps to "aqua" in Spectre.Console markup)
        var expectedStyle = new Style(foreground: Color.Cyan);
        style.Should().Be(expectedStyle);
    }
}
