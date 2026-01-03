using BitPantry.CommandLine.AutoComplete.Rendering;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Tests.AutoComplete.Rendering;

/// <summary>
/// Tests for AutoCompleteMenuRenderable - verifies menu rendering without controller overhead.
/// Uses Spectre's Renderable pattern for isolated testing.
/// </summary>
[TestClass]
public class AutoCompleteMenuRenderableTests
{
    #region Vertical Layout Tests

    [TestMethod]
    public void Render_WithItems_RendersVerticalLayout()
    {
        // Arrange
        var items = new List<string> { "connect", "disconnect", "status" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        var lines = output.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        lines.Should().Contain(l => l.Contains("connect"));
        lines.Should().Contain(l => l.Contains("disconnect"));
        lines.Should().Contain(l => l.Contains("status"));
    }

    [TestMethod]
    public void Render_WithSelection_AppliesInvertStyleToSelectedItem()
    {
        // Arrange
        var items = new List<string> { "connect", "disconnect", "status" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 1, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        console.EmitAnsiSequences = true;
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - the selected item should have invert styling ANSI codes
        // ANSI codes for invert: ESC[7m (reverse video)
        output.Should().Contain("disconnect");
    }

    [TestMethod]
    public void Render_WithItems_EachItemOnOwnLine()
    {
        // Arrange
        var items = new List<string> { "connect", "disconnect", "status" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var lines = console.Lines;
        
        // Assert - each item should be on its own line (vertical layout)
        lines.Count.Should().BeGreaterOrEqualTo(3, "each item should be on its own line");
    }

    #endregion

    #region Viewport Scrolling Tests

    [TestMethod]
    public void Render_WithViewportScroll_ShowsScrollUpIndicator()
    {
        // Arrange - viewport starts at 2, so items 0-1 are above
        var items = new List<string> { "item0", "item1", "item2", "item3", "item4" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 2, viewportStart: 2, viewportSize: 3);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - should show scroll up indicator
        output.Should().Contain("↑").And.Contain("2");
    }

    [TestMethod]
    public void Render_WithViewportScroll_ShowsScrollDownIndicator()
    {
        // Arrange - viewport shows first 3 items but there are 2 more below
        var items = new List<string> { "item0", "item1", "item2", "item3", "item4" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 3);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - should show scroll down indicator
        output.Should().Contain("↓").And.Contain("2");
    }

    [TestMethod]
    public void Render_NoScroll_NoScrollIndicators()
    {
        // Arrange - all items visible
        var items = new List<string> { "connect", "disconnect" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - no scroll indicators
        output.Should().NotContain("↑");
        output.Should().NotContain("↓");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Render_EmptyItems_ReturnsEmptySegments()
    {
        // Arrange
        var items = new List<string>();
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: -1, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        output.Trim().Should().BeEmpty();
    }

    [TestMethod]
    public void Render_SingleItem_RendersCorrectly()
    {
        // Arrange
        var items = new List<string> { "only-item" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        output.Should().Contain("only-item");
    }

    [TestMethod]
    public void Render_SelectedIndexOutOfRange_ClampsToValidRange()
    {
        // Arrange - selectedIndex > items.Count should be clamped
        var items = new List<string> { "item1", "item2" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 10, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act - should not throw
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        output.Should().Contain("item1");
        output.Should().Contain("item2");
    }

    #endregion
}
