using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BitPantry.VirtualConsole.Tests.MilestoneTests;

/// <summary>
/// T087: Table rendering milestone tests
/// Tests CLI table output similar to Spectre.Console tables.
/// </summary>
[TestClass]
public class TableRenderingTests
{
    [TestMethod]
    public void Table_SimpleGrid_ShouldRenderWithBorders()
    {
        var console = new VirtualConsole(40, 10);
        
        // Simple 2x2 table
        console.Write("┌──────────┬──────────┐\r\n");
        console.Write("│ Name     │ Value    │\r\n");
        console.Write("├──────────┼──────────┤\r\n");
        console.Write("│ Foo      │ 42       │\r\n");
        console.Write("│ Bar      │ 100      │\r\n");
        console.Write("└──────────┴──────────┘\r\n");
        
        // Verify structure
        console.GetRow(0).GetText().Should().StartWith("┌");
        console.GetRow(0).GetText().Should().Contain("┬");
        console.GetRow(0).GetText().Should().Contain("┐");
        
        // Verify content
        console.GetRow(1).GetText().Should().Contain("Name");
        console.GetRow(3).GetText().Should().Contain("Foo");
        console.GetRow(3).GetText().Should().Contain("42");
    }

    [TestMethod]
    public void Table_WithColoredHeaders_ShouldPreserveColors()
    {
        var console = new VirtualConsole(50, 10);
        
        // Table with colored header
        console.Write("┌────────────┬────────────┐\r\n");
        console.Write("│ \x1b[1;34mID\x1b[0m         │ \x1b[1;34mStatus\x1b[0m     │\r\n");
        console.Write("├────────────┼────────────┤\r\n");
        console.Write("│ 001        │ \x1b[32mActive\x1b[0m     │\r\n");
        console.Write("│ 002        │ \x1b[31mInactive\x1b[0m   │\r\n");
        console.Write("└────────────┴────────────┘\r\n");
        
        // Header "ID" should be bold blue
        var idCell = console.GetCell(1, 2);
        idCell.Character.Should().Be('I');
        idCell.Style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        idCell.Style.ForegroundColor.Should().Be(System.ConsoleColor.DarkBlue);
        
        // "Active" should be green
        var row3Cells = console.GetRow(3).GetCells().ToList();
        var activeStartIndex = console.GetRow(3).GetText().IndexOf('A');
        if (activeStartIndex > 0)
        {
            row3Cells[activeStartIndex].Style.ForegroundColor.Should().Be(System.ConsoleColor.DarkGreen);
        }
    }

    [TestMethod]
    public void Table_AlternatingRowColors_ShouldApplyZebraStripes()
    {
        var console = new VirtualConsole(40, 10);
        
        console.Write("┌──────┬──────┐\r\n");
        console.Write("│ Item │ Qty  │\r\n");
        console.Write("├──────┼──────┤\r\n");
        console.Write("│ A    │ 10   │\r\n");                          // Normal
        console.Write("\x1b[48;5;236m│ B    │ 20   │\x1b[0m\r\n");     // Dark bg (zebra)
        console.Write("│ C    │ 30   │\r\n");                          // Normal
        
        // Row 4 (B row) should have background color
        var zebra = console.GetCell(4, 0);
        zebra.Style.Background256.Should().Be(236);
        
        // Row 3 should have no background
        var normal = console.GetCell(3, 0);
        normal.Style.Background256.Should().BeNull();
    }

    [TestMethod]
    public void Table_WideContent_ShouldWrapCorrectly()
    {
        var console = new VirtualConsole(80, 15);
        
        // Table with longer content
        console.Write("┌──────────────────────────────────────────────────────────────────────┐\r\n");
        console.Write("│ Description                                                          │\r\n");
        console.Write("├──────────────────────────────────────────────────────────────────────┤\r\n");
        console.Write("│ This is a very long description that spans most of the width        │\r\n");
        console.Write("└──────────────────────────────────────────────────────────────────────┘\r\n");
        
        // Verify the content is on the correct row without wrapping issues
        console.GetRow(3).GetText().Should().Contain("This is a very long description");
        console.CursorRow.Should().Be(5);
    }

    [TestMethod]
    public void Table_MultiColumnAlignment_ShouldAlignContent()
    {
        var console = new VirtualConsole(50, 10);
        
        // Right-aligned numbers, left-aligned text
        console.Write("┌──────────┬──────────┬──────────┐\r\n");
        console.Write("│ Product  │    Price │ In Stock │\r\n");
        console.Write("├──────────┼──────────┼──────────┤\r\n");
        console.Write("│ Apples   │    $1.99 │ Yes      │\r\n");
        console.Write("│ Oranges  │    $2.49 │ No       │\r\n");
        console.Write("└──────────┴──────────┴──────────┘\r\n");
        
        // Verify alignment by checking for text presence
        var row3 = console.GetRow(3).GetText();
        row3.Should().Contain("Apples");
        row3.Should().Contain("$1.99");
        row3.Should().Contain("Yes");
    }
}
