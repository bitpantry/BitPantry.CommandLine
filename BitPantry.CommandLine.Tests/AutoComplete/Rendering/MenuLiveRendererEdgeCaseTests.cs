using BitPantry.CommandLine.AutoComplete.Rendering;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using Spectre.Console.Testing;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.AutoComplete.Rendering;

/// <summary>
/// Edge case tests for MenuLiveRenderer (T044.1-T044.8).
/// Tests boundary conditions and unusual scenarios.
/// </summary>
[TestClass]
public class MenuLiveRendererEdgeCaseTests
{
    #region T044.1: Viewport at terminal height boundary

    [TestMethod]
    public void EdgeCase_ViewportAtTerminalHeightBoundary()
    {
        // Arrange - small terminal
        var console = new TestConsole()
            .Size(new Size(80, 5))  // Only 5 lines tall
            .EmitAnsiSequences();

        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2", "item3", "item4", "item5", "item6" };

        // Act - Show menu with more items than terminal height
        renderer.Show(items, selectedIndex: 0, viewportStart: 0, viewportSize: 3);

        // Assert - should render without crashing
        renderer.IsVisible.Should().BeTrue();
        renderer.CurrentShape.Height.Should().BeLessOrEqualTo(5);
    }

    #endregion

    #region T044.3: Rapid key presses (multiple render updates)

    [TestMethod]
    public void EdgeCase_RapidRenderUpdates()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "a", "b", "c", "d", "e" };

        renderer.Show(items, 0, 0, 10);

        // Act - Rapid updates simulating fast key presses
        for (int i = 0; i < 100; i++)
        {
            var selectedIndex = i % items.Count;
            renderer.Update(items, selectedIndex, 0, 10);
        }

        // Assert - should handle without crashing or accumulating state issues
        renderer.IsVisible.Should().BeTrue();
        renderer.CurrentShape.Height.Should().Be(5);
    }

    #endregion

    #region T044.4: Menu items containing ANSI escape sequences

    [TestMethod]
    public void EdgeCase_ItemsContainingAnsiSequences()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        
        // Items with embedded ANSI codes (edge case - shouldn't happen normally)
        var items = new List<string> 
        { 
            "normal",
            "with\u001b[31mred\u001b[0m",  // Embedded ANSI
            "another\r\nwith newline"  // Embedded newline
        };

        // Act
        renderer.Show(items, 0, 0, 10);

        // Assert - should render without crashing
        renderer.IsVisible.Should().BeTrue();
        console.Output.Should().Contain("normal");
    }

    #endregion

    #region T044.5: Menu with more items than terminal height

    [TestMethod]
    public void EdgeCase_MoreItemsThanTerminalHeight()
    {
        // Arrange
        var console = new TestConsole()
            .Size(new Size(80, 10))
            .EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        
        // 50 items but terminal is only 10 lines
        var items = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            items.Add($"item_{i:D2}");
        }

        // Act - Show with viewport limiting visible items
        renderer.Show(items, 0, 0, 8);

        // Assert - should only render viewport size items
        renderer.IsVisible.Should().BeTrue();
        renderer.CurrentShape.Height.Should().BeLessOrEqualTo(10);
    }

    #endregion

    #region T044.6: Very long item text exceeding terminal width

    [TestMethod]
    public void EdgeCase_VeryLongItemText()
    {
        // Arrange
        var console = new TestConsole()
            .Size(new Size(40, 24))  // Narrow terminal
            .EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        
        var items = new List<string> 
        { 
            "short",
            "this_is_a_very_long_item_name_that_exceeds_terminal_width_and_should_wrap_or_truncate",
            "another"
        };

        // Act
        renderer.Show(items, 1, 0, 10);

        // Assert - should render without crashing
        renderer.IsVisible.Should().BeTrue();
        console.Output.Should().Contain("this_is_a_very_long");
    }

    #endregion

    #region T044.7: Menu grows from 3 to 10 items mid-session

    [TestMethod]
    public void EdgeCase_MenuGrowsFromSmallToLarge()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        
        var items3 = new List<string> { "a", "b", "c" };
        var items10 = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };

        // Act - Start small
        renderer.Show(items3, 0, 0, 10);
        renderer.CurrentShape.Height.Should().Be(3);

        // Grow to 10 items
        renderer.Update(items10, 0, 0, 10);

        // Assert - shape should grow
        renderer.CurrentShape.Height.Should().Be(10);
    }

    #endregion

    #region T044.8: Menu shrinks from 10 to 2 items

    [TestMethod]
    public void EdgeCase_MenuShrinksFromLargeToSmall()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        
        var items10 = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };
        var items2 = new List<string> { "x", "y" };

        // Act - Start large
        renderer.Show(items10, 0, 0, 10);
        renderer.CurrentShape.Height.Should().Be(10);

        // Shrink to 2 items (Inflate pattern keeps max height)
        renderer.Update(items2, 0, 0, 10);

        // Assert - shape should maintain max (prevents phantom lines)
        renderer.CurrentShape.Height.Should().Be(10, "Inflate pattern maintains max height");
        
        // But content should only show 2 items
        console.Output.Should().Contain("x");
        console.Output.Should().Contain("y");
    }

    #endregion

    #region Additional: Menu shows and hides repeatedly

    [TestMethod]
    public void EdgeCase_ShowHideRepeatedly()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var items = new List<string> { "item1", "item2", "item3" };

        // Act - Show and hide multiple times
        for (int i = 0; i < 10; i++)
        {
            renderer.Show(items, 0, 0, 10);
            renderer.Hide();
        }

        // Assert - should end in clean state
        renderer.IsVisible.Should().BeFalse();
        renderer.CurrentShape.Height.Should().Be(0);
    }

    #endregion

    #region Additional: Empty items list

    [TestMethod]
    [Description("FR-003: Empty items list renders '(no matches)' message with height 1")]
    public void EdgeCase_EmptyItemsList()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var emptyItems = new List<string>();

        // Act
        renderer.Show(emptyItems, 0, 0, 10);

        // Assert - FR-003: Empty list shows "(no matches)" with height 1
        renderer.IsVisible.Should().BeTrue();
        renderer.CurrentShape.Height.Should().Be(1);
    }

    #endregion

    #region Additional: Single item menu

    [TestMethod]
    public void EdgeCase_SingleItemMenu()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new MenuLiveRenderer(console);
        var singleItem = new List<string> { "only_item" };

        // Act
        renderer.Show(singleItem, 0, 0, 10);

        // Assert
        renderer.IsVisible.Should().BeTrue();
        renderer.CurrentShape.Height.Should().Be(1);
        console.Output.Should().Contain("only_item");
    }

    #endregion
}
