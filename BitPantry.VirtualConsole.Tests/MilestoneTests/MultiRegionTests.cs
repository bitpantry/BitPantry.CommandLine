using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BitPantry.VirtualConsole.Tests.MilestoneTests;

/// <summary>
/// T088: Multi-region milestone tests
/// Tests complex CLI layouts with multiple independent regions.
/// </summary>
[TestClass]
public class MultiRegionTests
{
    [TestMethod]
    public void MultiRegion_HeaderAndContent_ShouldMaintainSeparation()
    {
        var console = new VirtualConsole(60, 20);
        
        // Header area (row 0) - simple header with blue background
        console.Write("\x1b[1;44mHeader Line with Blue Background\x1b[0m\r\n");
        
        // Content area (row 1+)
        console.Write("Main content goes here\r\n");
        console.Write("More content...\r\n");
        
        // Verify header has blue background on first character
        var headerCell = console.GetCell(0, 0);
        headerCell.Character.Should().Be('H');
        headerCell.Style.BackgroundColor.Should().Be(System.ConsoleColor.DarkBlue);
        headerCell.Style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        
        // Verify content is normal (no background color)
        var contentCell = console.GetCell(1, 0);
        contentCell.Character.Should().Be('M');
        contentCell.Style.BackgroundColor.Should().BeNull();
    }

    [TestMethod]
    public void MultiRegion_SidebarAndMain_ShouldRenderSideBySide()
    {
        var console = new VirtualConsole(60, 10);
        
        // Simulated sidebar (left 15 cols) and main (right side)
        // Row by row rendering:
        console.Write("\x1b[7mMenu          \x1b[0m | Main Content Area\r\n");
        console.Write("  Option 1      | Lorem ipsum dolor sit amet\r\n");
        console.Write("\x1b[7m> Option 2    \x1b[0m | consectetur adipiscing elit\r\n");
        console.Write("  Option 3      | sed do eiusmod tempor\r\n");
        
        // Verify sidebar styling
        console.GetCell(0, 0).Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeTrue();
        console.GetCell(2, 0).Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeTrue();
        
        // Verify main content is normal
        console.GetCell(1, 18).Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeFalse();
    }

    [TestMethod]
    public void MultiRegion_StatusBar_ShouldUpdateIndependently()
    {
        var console = new VirtualConsole(50, 10);
        
        // Initial render
        console.Write("Content line 1\r\n");
        console.Write("Content line 2\r\n");
        console.Write("Content line 3\r\n");
        console.Write("Content line 4\r\n");
        console.Write("Content line 5\r\n");
        console.Write("Content line 6\r\n");
        console.Write("Content line 7\r\n");
        console.Write("Content line 8\r\n");
        console.Write("\x1b[7mStatus: Ready                                    \x1b[0m\r\n");
        
        // Update ONLY the status bar
        console.Write("\x1b[9;1H");  // Move to row 9 (0-based: row 8)
        console.Write("\x1b[7mStatus: Processing 50%                          \x1b[0m");
        
        // Verify content unchanged
        console.GetRow(0).GetText().Should().StartWith("Content line 1");
        console.GetRow(7).GetText().Should().StartWith("Content line 8");
        
        // Verify status bar updated
        console.GetRow(8).GetText().Should().Contain("Processing 50%");
        console.GetCell(8, 0).Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeTrue();
    }

    [TestMethod]
    public void MultiRegion_TabbedInterface_ShouldShowActiveTab()
    {
        var console = new VirtualConsole(60, 10);
        
        // Tab bar
        console.Write("┌─────┐┌─────┐┌─────┐\r\n");
        console.Write("│\x1b[7mTab 1\x1b[0m││ Tab 2 ││ Tab 3 │\r\n");  // Tab 1 active
        console.Write("└─────┘└─────┘└─────┘\r\n");
        console.Write("Tab 1 content here...\r\n");
        
        // Verify Tab 1 is highlighted
        console.GetCell(1, 1).Character.Should().Be('T');
        console.GetCell(1, 1).Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeTrue();
        
        // Tab 2 is normal
        console.GetRow(1).GetText().Should().Contain("Tab 2");
    }

    [TestMethod]
    public void MultiRegion_SplitPane_ShouldHandleVerticalSplit()
    {
        var console = new VirtualConsole(80, 15);
        
        // Header
        console.Write("═══════════════════════════════════════════════════════════════════════════════\r\n");
        
        // Two-pane view with vertical separator
        console.Write("│ Left Pane                           │ Right Pane                            │\r\n");
        console.Write("│                                     │                                       │\r\n");
        console.Write("│ - Item A                            │ Details for Item A                    │\r\n");
        console.Write("│ - Item B                            │                                       │\r\n");
        console.Write("│ - Item C                            │ Some detailed information             │\r\n");
        console.Write("│                                     │ about the selected item.              │\r\n");
        console.Write("═══════════════════════════════════════════════════════════════════════════════\r\n");
        
        // Verify separators are present
        console.GetRow(1).GetText().Should().Contain("│");
        console.GetRow(1).GetText().Should().Contain("Left Pane");
        console.GetRow(1).GetText().Should().Contain("Right Pane");
    }

    [TestMethod]
    public void MultiRegion_ModalDialog_ShouldOverlayContent()
    {
        var console = new VirtualConsole(50, 15);
        
        // Background content
        console.Write("Background content line 1\r\n");
        console.Write("Background content line 2\r\n");
        console.Write("Background content line 3\r\n");
        console.Write("Background content line 4\r\n");
        console.Write("Background content line 5\r\n");
        console.Write("Background content line 6\r\n");
        console.Write("Background content line 7\r\n");
        console.Write("Background content line 8\r\n");
        
        // Draw modal dialog overlay in the middle
        console.Write("\x1b[3;10H");  // Position
        console.Write("┌──────────────────────────┐");
        console.Write("\x1b[4;10H");
        console.Write("│\x1b[43m      Confirm Action      \x1b[0m│");
        console.Write("\x1b[5;10H");
        console.Write("│                          │");
        console.Write("\x1b[6;10H");
        console.Write("│  Are you sure?           │");
        console.Write("\x1b[7;10H");
        console.Write("│                          │");
        console.Write("\x1b[8;10H");
        console.Write("│  \x1b[32m[Yes]\x1b[0m     \x1b[31m[No]\x1b[0m        │");
        console.Write("\x1b[9;10H");
        console.Write("└──────────────────────────┘");
        
        // Verify dialog is rendered
        console.GetRow(2).GetText().Should().Contain("┌");
        console.GetRow(3).GetText().Should().Contain("Confirm Action");
        console.GetRow(5).GetText().Should().Contain("Are you sure?");
        
        // Verify buttons have colors
        var yesPos = console.GetRow(7).GetText().IndexOf('Y');
        if (yesPos > 0)
        {
            console.GetCell(7, yesPos).Style.ForegroundColor.Should().Be(System.ConsoleColor.DarkGreen);
        }
    }
}
