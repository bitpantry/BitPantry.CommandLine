using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BitPantry.VirtualConsole.Tests.MilestoneTests;

/// <summary>
/// T086: Progress bar milestone tests
/// Tests CLI progress bar rendering with ANSI cursor movement and color.
/// </summary>
[TestClass]
public class ProgressBarTests
{
    #region Consolidated: Progress Percentage Display

    [TestMethod]
    [DataRow("0%", "[          ] 0%", "empty bar at zero percent")]
    [DataRow("67%", "[████████████████████          ] 67% - ETA: 2m 30s", "partial bar with ETA")]
    public void ProgressBar_PercentageDisplay_ShowsCorrectText(string expectedPercent, string barOutput, string scenario)
    {
        // Arrange
        var console = new VirtualConsole(70, 5);
        console.Write("Status message...\r\n");
        
        // Act
        console.Write(barOutput);
        
        // Assert
        var row = console.GetRow(1);
        row.GetText().Should().Contain(expectedPercent, because: scenario);
    }

    #endregion

    #region Consolidated: ANSI Color Rendering

    [TestMethod]
    [DataRow(5, 5, "50% - half filled with green background")]
    [DataRow(10, 0, "100% - fully filled with green background")]
    public void ProgressBar_ColoredFill_RendersWithBackgroundColor(int filledCount, int emptyCount, string scenario)
    {
        // Arrange
        var console = new VirtualConsole(50, 5);
        var filledSpaces = new string(' ', filledCount);
        var emptySpaces = new string(' ', emptyCount);
        
        // Act - Write bar with ANSI green background for filled portion
        console.Write("[");
        console.Write($"\x1b[42m{filledSpaces}\x1b[0m");  // Green background
        if (emptyCount > 0) console.Write(emptySpaces);
        console.Write("]");
        
        // Assert - All filled cells should have green background
        var cells = console.GetRow(0).GetCells().ToList();
        for (int i = 1; i <= filledCount; i++)
        {
            cells[i].Style.BackgroundColor.Should().Be(System.ConsoleColor.DarkGreen, because: scenario);
        }
        
        // Empty cells should have no background
        for (int i = filledCount + 1; i <= filledCount + emptyCount; i++)
        {
            cells[i].Style.BackgroundColor.Should().BeNull(because: $"empty portion should not be colored ({scenario})");
        }
    }

    #endregion

    #region Behavioral: Cursor and Update Mechanics

    [TestMethod]
    public void ProgressBar_InPlaceUpdate_ShouldOverwritePrevious()
    {
        var console = new VirtualConsole(50, 5);
        
        // Initial: 5%
        console.Write("Progress: [          ] 5%  ");
        
        // Update with carriage return (in-place update)
        console.Write("\rProgress: [██        ] 20% ");
        
        var row = console.GetRow(0);
        row.GetText().Should().StartWith("Progress: [");
        row.GetText().Should().Contain("20%");
        row.GetText().Should().NotContain("5%");  // Old value overwritten
    }

    [TestMethod]
    public void ProgressBar_WithSpinner_ShouldAnimateCorrectly()
    {
        var console = new VirtualConsole(50, 5);
        
        // Frame 1: |
        console.Write("Working | [█████     ]");
        
        // Frame 2: / (overwrite spinner)
        console.Write("\x1b[1;9H");  // Move to spinner position
        console.Write("/");
        
        // Verify spinner updated
        console.GetCell(0, 8).Character.Should().Be('/');
        
        // Verify progress bar unchanged
        console.GetRow(0).GetText().Should().Contain("[");
    }

    [TestMethod]
    public void ProgressBar_MultipleLines_ShouldShowParallelDownloads()
    {
        var console = new VirtualConsole(60, 10);
        
        // Multiple concurrent downloads
        console.Write("file1.zip  [████      ] 40%\r\n");
        console.Write("file2.zip  [\x1b[42m██████    \x1b[0m] 60%\r\n");  // Colored
        console.Write("file3.zip  [██████████] Done!\r\n");
        
        console.GetRow(0).GetText().Should().Contain("40%");
        console.GetRow(1).GetText().Should().Contain("60%");
        console.GetRow(2).GetText().Should().Contain("Done!");
        
        // Second bar should have colored fill
        console.GetCell(1, 12).Style.BackgroundColor.Should().Be(System.ConsoleColor.DarkGreen);
    }

    #endregion
}
