using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BitPantry.VirtualConsole.Tests.MilestoneTests;

/// <summary>
/// T085, T089: Menu rendering milestone tests
/// Tests real-world CLI menu scenarios including the original filter highlighting bug.
/// </summary>
[TestClass]
public class MenuRenderingTests
{
    // T085: Menu rendering with selection highlighting
    [TestMethod]
    public void Menu_WithHighlightedSelection_ShouldRenderCorrectly()
    {
        var console = new VirtualConsole(40, 10);
        
        // Simulate a CLI menu with 3 options, second one highlighted
        console.Write("Select an option:\r\n");
        console.Write("  Option 1 - First choice\r\n");
        console.Write("\x1b[7m> Option 2 - Second choice\x1b[0m\r\n");  // Reverse video for highlight
        console.Write("  Option 3 - Third choice\r\n");
        
        // Verify menu structure
        console.GetRow(0).GetText().Should().StartWith("Select an option:");
        console.GetRow(1).GetText().Should().StartWith("  Option 1");
        console.GetRow(2).GetText().Should().StartWith("> Option 2");
        console.GetRow(3).GetText().Should().StartWith("  Option 3");
        
        // Verify highlighting on row 2
        var highlightedCell = console.GetCell(2, 0);
        highlightedCell.Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeTrue();
        
        // Row 1 should NOT have reverse video
        var normalCell = console.GetCell(1, 2);
        normalCell.Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeFalse();
    }

    [TestMethod]
    public void Menu_WithColoredOptions_ShouldPreserveColors()
    {
        var console = new VirtualConsole(50, 10);
        
        // Menu with different colored options
        console.Write("File Menu:\r\n");
        console.Write("\x1b[32mNew\x1b[0m  ");        // Green
        console.Write("\x1b[33mOpen\x1b[0m  ");       // Yellow
        console.Write("\x1b[31mDelete\x1b[0m  ");     // Red
        console.Write("\x1b[34mHelp\x1b[0m\r\n");     // Blue
        
        // Verify colors
        var row = console.GetRow(1);
        var cells = row.GetCells().ToList();
        
        // "New" should be green (positions 0-2)
        cells[0].Character.Should().Be('N');
        cells[0].Style.ForegroundColor.Should().Be(System.ConsoleColor.DarkGreen);
        
        // "Open" starts around position 5
        cells[5].Character.Should().Be('O');
        cells[5].Style.ForegroundColor.Should().Be(System.ConsoleColor.DarkYellow);
    }

    [TestMethod]
    public void Menu_CursorMovement_ShouldUpdateHighlight()
    {
        var console = new VirtualConsole(40, 10);
        
        // Initial menu
        console.Write("Options:\r\n");
        console.Write("\x1b[7m> Item A\x1b[0m\r\n");
        console.Write("  Item B\r\n");
        console.Write("  Item C\r\n");
        
        // Move cursor back up to row 1 and re-render with new highlight
        console.Write("\x1b[2;1H");  // Move to row 2, col 1 (0-based: row 1, col 0)
        console.Write("  Item A  \r\n");  // Clear old highlight
        console.Write("\x1b[7m> Item B\x1b[0m");  // New highlight
        
        // Verify the highlight moved
        console.GetCell(1, 0).Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeFalse();
        console.GetCell(2, 0).Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeTrue();
    }

    // T089: Filter highlighting regression test (the original bug scenario)
    [TestMethod]
    public void Menu_FilterHighlighting_ShouldHighlightMatchingText()
    {
        var console = new VirtualConsole(60, 15);
        
        // Simulate a filterable menu where typed characters highlight matches
        // This is the original bug scenario that motivated VirtualConsole
        
        // Header
        console.Write("Search: \x1b[33mapp\x1b[0m\r\n");  // User typed "app"
        console.Write("─────────────────────\r\n");
        
        // Results with highlighted matches
        console.Write("  \x1b[43mApp\x1b[0mlication.exe\r\n");      // "App" highlighted (yellow bg)
        console.Write("  Word\x1b[43mApp\x1b[0m.doc\r\n");          // "App" in middle
        console.Write("  No\x1b[43mApp\x1b[0micable.txt\r\n");      // "App" split word
        
        // Verify the filter text is shown
        var searchRow = console.GetRow(0);
        searchRow.GetText().Should().Contain("app");
        
        // Verify highlighting on "App" in "Application"
        var appRow = console.GetRow(2);
        var cells = appRow.GetCells().ToList();
        
        // Position 2 should be 'A' with yellow background
        cells[2].Character.Should().Be('A');
        cells[2].Style.BackgroundColor.Should().Be(System.ConsoleColor.DarkYellow);
        
        // Position 3 should be 'p' with yellow background
        cells[3].Character.Should().Be('p');
        cells[3].Style.BackgroundColor.Should().Be(System.ConsoleColor.DarkYellow);
        
        // Position 5 should be 'l' WITHOUT highlight
        cells[5].Character.Should().Be('l');
        cells[5].Style.BackgroundColor.Should().BeNull();
    }

    [TestMethod]
    public void Menu_ScrollableList_ShouldMaintainVisibleWindow()
    {
        var console = new VirtualConsole(30, 6);  // Small window
        
        // Show "more" indicators
        console.Write("↑ 3 items above\r\n");
        console.Write("  Item 4\r\n");
        console.Write("\x1b[7m> Item 5\x1b[0m\r\n");  // Selected
        console.Write("  Item 6\r\n");
        console.Write("↓ 2 items below\r\n");
        
        // Verify visible items
        console.GetRow(1).GetText().Should().StartWith("  Item 4");
        console.GetRow(2).GetText().Should().StartWith("> Item 5");
        console.GetRow(3).GetText().Should().StartWith("  Item 6");
        
        // Verify scroll indicators
        console.GetRow(0).GetText().Should().Contain("above");
        console.GetRow(4).GetText().Should().Contain("below");
    }
}
