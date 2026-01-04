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
/// Tests for MenuLiveRenderable - the low-level LiveRenderable implementation.
/// Tests cursor positioning and restoration sequences.
/// </summary>
[TestClass]
public class MenuLiveRenderableTests
{
    #region PositionCursor Tests

    [TestMethod]
    public void PositionCursor_WithNoShape_ReturnsEmptyControlCode()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);

        // Act
        var control = liveRenderable.PositionCursor();

        // Assert - with no shape, cursor doesn't need repositioning
        console.Write(control);
        console.Output.Should().BeEmpty();
    }

    [TestMethod]
    public void PositionCursor_After3LineRender_ReturnsCRAndCUU2()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);
        var items = new List<string> { "item1", "item2", "item3" };
        var menu = new AutoCompleteMenuRenderable(items, 0, 0, 5);

        // Render to establish shape
        liveRenderable.SetRenderable(menu);
        console.Write(liveRenderable);

        // Clear output to capture just the cursor positioning
        var positionConsole = new TestConsole().EmitAnsiSequences();

        // Act
        var control = liveRenderable.PositionCursor();
        positionConsole.Write(control);

        // Assert - should be CR + CUU(2) to move up 2 lines (height-1)
        // CR = \r
        // CUU(2) = ESC[2A
        positionConsole.Output.Should().Be("\r\u001b[2A");
    }

    [TestMethod]
    public void PositionCursor_After1LineRender_ReturnsCROnly()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);
        var items = new List<string> { "single" };
        var menu = new AutoCompleteMenuRenderable(items, 0, 0, 5);

        liveRenderable.SetRenderable(menu);
        console.Write(liveRenderable);

        var positionConsole = new TestConsole().EmitAnsiSequences();

        // Act
        var control = liveRenderable.PositionCursor();
        positionConsole.Write(control);

        // Assert - height is 1, so CUU(0) = just CR
        // Actually CUU(0) shouldn't be emitted at all
        positionConsole.Output.Should().Be("\r");
    }

    [TestMethod]
    public void PositionCursor_After5LineRender_ReturnsCRAndCUU4()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);
        var items = new List<string> { "a", "b", "c", "d", "e" };
        var menu = new AutoCompleteMenuRenderable(items, 0, 0, 10);

        liveRenderable.SetRenderable(menu);
        console.Write(liveRenderable);

        var positionConsole = new TestConsole().EmitAnsiSequences();

        // Act
        var control = liveRenderable.PositionCursor();
        positionConsole.Write(control);

        // Assert - CR + CUU(4)
        positionConsole.Output.Should().Be("\r\u001b[4A");
    }

    #endregion

    #region RestoreCursor Tests

    [TestMethod]
    public void RestoreCursor_WithNoShape_ReturnsEmptyControlCode()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);

        // Act
        var control = liveRenderable.RestoreCursor();

        // Assert
        console.Write(control);
        console.Output.Should().BeEmpty();
    }

    [TestMethod]
    public void RestoreCursor_After3LineRender_ReturnsClearSequence()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);
        var items = new List<string> { "item1", "item2", "item3" };
        var menu = new AutoCompleteMenuRenderable(items, 0, 0, 5);

        liveRenderable.SetRenderable(menu);
        console.Write(liveRenderable);

        var restoreConsole = new TestConsole().EmitAnsiSequences();

        // Act
        var control = liveRenderable.RestoreCursor();
        restoreConsole.Write(control);

        // Assert - should be: CR + EL(2) + (CUU(1) + EL(2)) * 2
        // CR = \r
        // EL(2) = ESC[2K (clear entire line)
        // CUU(1) = ESC[1A (move up 1)
        var output = restoreConsole.Output;
        output.Should().StartWith("\r\u001b[2K");
        output.Should().Contain("\u001b[1A\u001b[2K");
        // Should have 2 more clear sequences (for 3 lines total)
        output.Count(c => c == 'K').Should().Be(3);  // 3 EL sequences
    }

    [TestMethod]
    public void RestoreCursor_After1LineRender_ReturnsJustCRAndClear()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);
        var items = new List<string> { "single" };
        var menu = new AutoCompleteMenuRenderable(items, 0, 0, 5);

        liveRenderable.SetRenderable(menu);
        console.Write(liveRenderable);

        var restoreConsole = new TestConsole().EmitAnsiSequences();

        // Act
        var control = liveRenderable.RestoreCursor();
        restoreConsole.Write(control);

        // Assert - just CR + EL(2) (no CUU needed for single line)
        restoreConsole.Output.Should().Be("\r\u001b[2K");
    }

    #endregion

    #region SetRenderable Tests

    [TestMethod]
    public void SetRenderable_UpdatesTarget()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);
        var items = new List<string> { "item1", "item2" };
        var menu = new AutoCompleteMenuRenderable(items, 0, 0, 5);

        // Act
        liveRenderable.SetRenderable(menu);

        // Assert
        liveRenderable.Target.Should().BeSameAs(menu);
        liveRenderable.HasRenderable.Should().BeTrue();
    }

    [TestMethod]
    public void SetRenderable_WithNull_ClearsTarget()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);
        var items = new List<string> { "item1" };
        var menu = new AutoCompleteMenuRenderable(items, 0, 0, 5);

        liveRenderable.SetRenderable(menu);

        // Act
        liveRenderable.SetRenderable(null);

        // Assert
        liveRenderable.Target.Should().BeNull();
        liveRenderable.HasRenderable.Should().BeFalse();
    }

    #endregion

    #region Render with Inflate Pattern Tests

    [TestMethod]
    public void Render_WithGrowingContent_InflatesShape()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);

        var items3 = new List<string> { "a", "b", "c" };
        var items5 = new List<string> { "a", "b", "c", "d", "e" };

        // First render with 3 items
        liveRenderable.SetRenderable(new AutoCompleteMenuRenderable(items3, 0, 0, 10));
        console.Write(liveRenderable);
        liveRenderable.CurrentShape.Height.Should().Be(3);

        // Act - render with 5 items
        liveRenderable.SetRenderable(new AutoCompleteMenuRenderable(items5, 0, 0, 10));
        console.Write(liveRenderable);

        // Assert
        liveRenderable.CurrentShape.Height.Should().Be(5);
    }

    [TestMethod]
    public void Render_WithShrinkingContent_MaintainsMaxShape()
    {
        // Arrange - KEY test for inflate pattern
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);

        var items5 = new List<string> { "a", "b", "c", "d", "e" };
        var items2 = new List<string> { "x", "y" };

        // First render with 5 items
        liveRenderable.SetRenderable(new AutoCompleteMenuRenderable(items5, 0, 0, 10));
        console.Write(liveRenderable);
        liveRenderable.CurrentShape.Height.Should().Be(5);

        // Act - render with only 2 items
        liveRenderable.SetRenderable(new AutoCompleteMenuRenderable(items2, 0, 0, 10));
        console.Write(liveRenderable);

        // Assert - shape stays at max
        liveRenderable.CurrentShape.Height.Should().Be(5);
    }

    [TestMethod]
    public void Render_WithNullRenderable_ResetsShape()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);

        var items = new List<string> { "a", "b", "c" };
        liveRenderable.SetRenderable(new AutoCompleteMenuRenderable(items, 0, 0, 10));
        console.Write(liveRenderable);

        // Act
        liveRenderable.SetRenderable(null);
        console.Write(liveRenderable);

        // Assert - shape reset
        liveRenderable.CurrentShape.Height.Should().Be(0);
    }

    #endregion

    #region CurrentShape Property Tests

    [TestMethod]
    public void CurrentShape_Initially_IsZero()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);

        // Assert
        liveRenderable.CurrentShape.Width.Should().Be(0);
        liveRenderable.CurrentShape.Height.Should().Be(0);
    }

    [TestMethod]
    public void CurrentShape_AfterRender_ReflectsContent()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var liveRenderable = new MenuLiveRenderable(console);
        var items = new List<string> { "longeritem", "short" };
        var menu = new AutoCompleteMenuRenderable(items, 0, 0, 5);

        // Act
        liveRenderable.SetRenderable(menu);
        console.Write(liveRenderable);

        // Assert
        liveRenderable.CurrentShape.Height.Should().Be(2);
        liveRenderable.CurrentShape.Width.Should().BeGreaterThanOrEqualTo("longeritem".Length);
    }

    #endregion
}
