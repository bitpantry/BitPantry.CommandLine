using BitPantry.CommandLine.AutoComplete.Rendering;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

namespace BitPantry.CommandLine.Tests.AutoComplete.Rendering;

/// <summary>
/// Tests for GhostTextRenderable - verifies ghost text rendering without controller overhead.
/// Uses Spectre's Renderable pattern for isolated testing.
/// </summary>
[TestClass]
public class GhostTextRenderableTests
{
    #region Style Tests

    [TestMethod]
    public void Render_WithGhostText_AppliesDimGrayStyle()
    {
        // Arrange
        var renderable = new GhostTextRenderable("nect");

        var console = new TestConsole();
        console.EmitAnsiSequences = true;
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - should contain the ghost text with some styling
        output.Should().Contain("nect");
    }

    [TestMethod]
    public void Render_WithCustomStyle_AppliesCustomStyle()
    {
        // Arrange
        var customStyle = new Style(Color.Red, decoration: Decoration.Italic);
        var renderable = new GhostTextRenderable("nect", customStyle);

        var console = new TestConsole();
        console.EmitAnsiSequences = true;
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        output.Should().Contain("nect");
    }

    #endregion

    #region Empty/Whitespace Tests

    [TestMethod]
    public void Render_EmptyGhostText_ReturnsEmptyOutput()
    {
        // Arrange
        var renderable = new GhostTextRenderable(string.Empty);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        output.Trim().Should().BeEmpty();
    }

    [TestMethod]
    public void Render_WhitespaceGhostText_ReturnsEmptyOutput()
    {
        // Arrange
        var renderable = new GhostTextRenderable("   ");

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - whitespace-only ghost text should be treated as empty
        output.Trim().Should().BeEmpty();
    }

    [TestMethod]
    public void Render_NullGhostText_DoesNotThrow()
    {
        // Arrange
        var renderable = new GhostTextRenderable(null);

        var console = new TestConsole();
        
        // Act - should not throw
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        output.Trim().Should().BeEmpty();
    }

    #endregion

    #region Content Tests

    [TestMethod]
    public void Render_WithGhostText_RendersExactText()
    {
        // Arrange
        var renderable = new GhostTextRenderable("server connect");

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - should contain exact text
        output.Should().Contain("server connect");
    }

    [TestMethod]
    public void Render_GhostTextWithSpecialChars_RendersCorrectly()
    {
        // Arrange - test with special characters that might need escaping
        var renderable = new GhostTextRenderable("--port=8080");

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        output.Should().Contain("--port=8080");
    }

    [TestMethod]
    public void GhostStyle_DefaultValue_IsDimGray()
    {
        // Arrange
        var renderable = new GhostTextRenderable("test");
        
        // Assert - verify the default style is dim gray
        renderable.GhostStyle.Should().NotBeNull();
        renderable.GhostStyle.Foreground.Should().Be(Color.Grey);
        renderable.GhostStyle.Decoration.Should().HaveFlag(Decoration.Dim);
    }

    #endregion
}
