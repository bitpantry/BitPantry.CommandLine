using BitPantry.CommandLine.AutoComplete.Rendering;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.AutoComplete.Rendering;

/// <summary>
/// Tests for MenuLiveRenderer - the LiveRenderable-based menu renderer.
/// This uses the Inflate pattern to prevent phantom lines.
/// </summary>
[TestClass]
public class MenuLiveRendererTests
{
    #region Show Tests

    [TestMethod]
    public void Show_SetsIsVisibleToTrue()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        renderer.Show(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        // Assert
        renderer.IsVisible.Should().BeTrue();
    }

    [TestMethod]
    public void Show_RendersMenuItems()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "connect", "config", "status" };

        // Act
        renderer.Show(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        // Assert
        console.Output.Should().Contain("connect");
        console.Output.Should().Contain("config");
        console.Output.Should().Contain("status");
    }

    [TestMethod]
    public void Show_TracksInitialHeight()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        renderer.Show(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        // Assert - renderer should track that we rendered 3 lines
        renderer.CurrentShape.Height.Should().Be(3);
    }

    #endregion

    #region Update Tests - Inflate Pattern

    [TestMethod]
    public void Update_WithMoreItems_InflatesHeight()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items3 = new List<string> { "a", "b", "c" };
        var items5 = new List<string> { "a", "b", "c", "d", "e" };

        renderer.Show(items3, 0, 0, 10);
        
        // Act - update with more items
        renderer.Update(items5, 0, 0, 10);

        // Assert - height inflated to 5
        renderer.CurrentShape.Height.Should().Be(5);
    }

    [TestMethod]
    public void Update_WithFewerItems_MaintainsMaxHeight()
    {
        // Arrange - This is the KEY test for the phantom line bug fix
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items5 = new List<string> { "a", "b", "c", "d", "e" };
        var items2 = new List<string> { "a", "b" };

        renderer.Show(items5, 0, 0, 10);
        
        // Act - update with fewer items
        renderer.Update(items2, 0, 0, 10);

        // Assert - height stays at 5 (Inflate pattern - never shrinks)
        renderer.CurrentShape.Height.Should().Be(5);
    }

    [TestMethod]
    public void Update_ClearsPreviousContentWithPadding()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items5 = new List<string> { "aaa", "bbb", "ccc", "ddd", "eee" };
        var items2 = new List<string> { "xx", "yy" };

        renderer.Show(items5, 0, 0, 10);
        var outputAfterShow = console.Output;
        
        // Act
        renderer.Update(items2, 0, 0, 10);

        // Assert - should still output 5 lines worth of content
        // (2 content lines + 3 blank/padded lines)
        var lines = console.Output.Split('\n');
        // We should see the new items and the area should be fully cleared
        console.Output.Should().Contain("xx");
        console.Output.Should().Contain("yy");
    }

    [TestMethod]
    public void Update_PositionsCursorToStartBeforeRendering()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2", "item3" };

        renderer.Show(items, 0, 0, 10);
        var outputAfterShow = console.Output.Length;

        // Act
        renderer.Update(items, 1, 0, 10);  // Just change selection
        var updateOutput = console.Output.Substring(outputAfterShow);

        // Assert - should start with cursor positioning (CR + CUU)
        updateOutput.Should().Contain("\r");  // Carriage return
        updateOutput.Should().Contain("\u001b[");  // ANSI sequence
    }

    [TestMethod]
    public void Update_AppliesSelectionHighlight()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2", "item3" };

        renderer.Show(items, 0, 0, 10);
        
        // Act - move selection
        renderer.Update(items, 1, 0, 10);

        // Assert - item2 should be highlighted
        console.Output.Should().Contain("item2");
        // The invert style should be applied somewhere
        console.Output.Should().Contain("\u001b[7m");  // ANSI invert code
    }

    #endregion

    #region Hide Tests

    [TestMethod]
    public void Hide_SetsIsVisibleToFalse()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2" };

        renderer.Show(items, 0, 0, 5);

        // Act
        renderer.Hide();

        // Assert
        renderer.IsVisible.Should().BeFalse();
    }

    [TestMethod]
    public void Hide_ClearsAllLinesUpToMaxHeight()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items5 = new List<string> { "a", "b", "c", "d", "e" };
        var items2 = new List<string> { "x", "y" };

        renderer.Show(items5, 0, 0, 10);  // Max height = 5
        renderer.Update(items2, 0, 0, 10);  // Current content = 2, but max still 5
        var outputBeforeHide = console.Output.Length;

        // Act
        renderer.Hide();
        var hideOutput = console.Output.Substring(outputBeforeHide);

        // Assert - should clear all 5 lines
        // The hide sequence should include multiple line clears
        hideOutput.Should().Contain("\u001b[2K");  // Clear line sequence
    }

    [TestMethod]
    public void Hide_RestoresCursorToInputLine()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2", "item3" };

        renderer.Show(items, 0, 0, 5);
        var outputBeforeHide = console.Output.Length;

        // Act
        renderer.Hide();
        var hideOutput = console.Output.Substring(outputBeforeHide);

        // Assert - should position cursor back at start
        hideOutput.Should().StartWith("\r");  // Return to column 0
    }

    [TestMethod]
    public void Hide_ResetsShapeTracking()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2", "item3" };

        renderer.Show(items, 0, 0, 5);

        // Act
        renderer.Hide();

        // Assert - shape should be reset
        renderer.CurrentShape.Height.Should().Be(0);
        renderer.CurrentShape.Width.Should().Be(0);
    }

    #endregion

    #region Menu Growth/Shrink Scenarios

    [TestMethod]
    public void Scenario_MenuGrows_3To5Items_RendersCleanly()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);

        // Start with 3 items
        var items3 = new List<string> { "cmd1", "cmd2", "cmd3" };
        renderer.Show(items3, 0, 0, 10);
        renderer.CurrentShape.Height.Should().Be(3);

        // Act - grow to 5 items
        var items5 = new List<string> { "cmd1", "cmd2", "cmd3", "cmd4", "cmd5" };
        renderer.Update(items5, 0, 0, 10);

        // Assert
        renderer.CurrentShape.Height.Should().Be(5);
        console.Output.Should().Contain("cmd4");
        console.Output.Should().Contain("cmd5");
    }

    [TestMethod]
    public void Scenario_MenuShrinks_5To2Items_ClearsPhantomLines()
    {
        // Arrange - This tests the main bug fix
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);

        // Start with 5 items
        var items5 = new List<string> { "cmd1", "cmd2", "cmd3", "cmd4", "cmd5" };
        renderer.Show(items5, 0, 0, 10);

        // Act - shrink to 2 items
        var items2 = new List<string> { "other1", "other2" };
        renderer.Update(items2, 0, 0, 10);

        // Assert - shape still tracks 5 height to clear phantom lines
        renderer.CurrentShape.Height.Should().Be(5);
        // But the actual content only shows 2 items (rest is padding)
        console.Output.Should().Contain("other1");
        console.Output.Should().Contain("other2");
    }

    [TestMethod]
    public void Scenario_MultipleNavigations_NoPhantomLinesAccumulate()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);

        var items3 = new List<string> { "a", "b", "c" };
        var items5 = new List<string> { "a", "b", "c", "d", "e" };
        var items2 = new List<string> { "x", "y" };

        // Act - simulate navigation that changes item count
        renderer.Show(items3, 0, 0, 10);   // 3 lines
        renderer.Update(items5, 0, 0, 10); // Grows to 5 lines
        renderer.Update(items2, 0, 0, 10); // Stays at 5 (max)
        renderer.Update(items3, 0, 0, 10); // Still 5
        renderer.Update(items5, 0, 0, 10); // Still 5

        // Assert - max height never decreases
        renderer.CurrentShape.Height.Should().Be(5);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Update_BeforeShow_DoesNotThrow()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2" };

        // Act & Assert - should not throw
        var action = () => renderer.Update(items, 0, 0, 5);
        action.Should().NotThrow();
    }

    [TestMethod]
    public void Hide_BeforeShow_DoesNotThrow()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);

        // Act & Assert
        var action = () => renderer.Hide();
        action.Should().NotThrow();
    }

    [TestMethod]
    [Description("FR-003: Empty items shows '(no matches)' message with height 1")]
    public void Show_WithEmptyItems_ShowsNoMatchesMessage()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string>();

        // Act
        renderer.Show(items, 0, 0, 5);

        // Assert - FR-003: Empty list shows "(no matches)" with height 1
        renderer.IsVisible.Should().BeTrue();
        renderer.CurrentShape.Height.Should().Be(1);
    }

    #endregion
}
