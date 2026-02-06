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

    // Implements: CV-002
    [TestMethod]
    public void Command_ReturnsDefaultStyle()
    {
        // Act
        var style = SyntaxColorScheme.Command;

        // Assert - verify the style is default (no foreground/background/decorations)
        style.Should().Be(Style.Plain);
    }

    // Implements: CV-003
    [TestMethod]
    public void ArgumentName_ReturnsYellowStyle()
    {
        // Act
        var style = SyntaxColorScheme.ArgumentName;

        // Assert - verify yellow foreground
        var expectedStyle = new Style(foreground: Color.Yellow);
        style.Should().Be(expectedStyle);
    }

    // Implements: CV-004
    [TestMethod]
    public void ArgumentAlias_ReturnsYellowStyle()
    {
        // Act
        var style = SyntaxColorScheme.ArgumentAlias;

        // Assert - verify yellow foreground (same as ArgumentName)
        var expectedStyle = new Style(foreground: Color.Yellow);
        style.Should().Be(expectedStyle);
    }

    // Implements: CV-005
    [TestMethod]
    public void ArgumentValue_ReturnsPurpleStyle()
    {
        // Act
        var style = SyntaxColorScheme.ArgumentValue;

        // Assert - verify purple foreground
        var expectedStyle = new Style(foreground: Color.Purple);
        style.Should().Be(expectedStyle);
    }

    // Implements: CV-006
    [TestMethod]
    public void GhostText_ReturnsDimStyle()
    {
        // Act
        var style = SyntaxColorScheme.GhostText;

        // Assert - verify dim decoration
        style.Decoration.Should().Be(Decoration.Dim);
    }

    // Implements: CV-007
    [TestMethod]
    public void Default_ReturnsDefaultStyle()
    {
        // Act
        var style = SyntaxColorScheme.Default;

        // Assert - verify default (no foreground/background/decorations)
        style.Should().Be(Style.Plain);
    }
}
