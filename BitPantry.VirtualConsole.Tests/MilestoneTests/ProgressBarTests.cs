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
    [TestMethod]
    public void ProgressBar_AtZeroPercent_ShouldShowEmptyBar()
    {
        var console = new VirtualConsole(50, 5);
        
        // Typical progress bar: [          ] 0%
        console.Write("Downloading...\r\n");
        console.Write("[          ] 0%");
        
        var row = console.GetRow(1);
        row.GetText().Should().StartWith("[          ] 0%");
    }

    [TestMethod]
    public void ProgressBar_AtFiftyPercent_ShouldShowHalfFilled()
    {
        var console = new VirtualConsole(50, 5);
        
        // Progress bar at 50%: [█████     ] 50%
        console.Write("[");
        console.Write("\x1b[42m     \x1b[0m");  // 5 green filled
        console.Write("     ");                  // 5 empty
        console.Write("] 50%");
        
        var row = console.GetRow(0);
        var cells = row.GetCells().ToList();
        
        // Check filled portion has green background
        cells[1].Style.BackgroundColor.Should().Be(System.ConsoleColor.DarkGreen);
        cells[5].Style.BackgroundColor.Should().Be(System.ConsoleColor.DarkGreen);
        
        // Check empty portion has no background
        cells[6].Style.BackgroundColor.Should().BeNull();
    }

    [TestMethod]
    public void ProgressBar_Complete_ShouldShowFullBar()
    {
        var console = new VirtualConsole(50, 5);
        
        // Complete: [██████████] 100%
        console.Write("[");
        console.Write("\x1b[42m          \x1b[0m");  // 10 green filled
        console.Write("] 100%");
        
        var row = console.GetRow(0);
        row.GetText().Should().StartWith("[          ] 100%");
        
        // All 10 positions should have green background
        for (int i = 1; i <= 10; i++)
        {
            console.GetCell(0, i).Style.BackgroundColor.Should().Be(System.ConsoleColor.DarkGreen);
        }
    }

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
        row.GetText().Should().NotContain("5%");  // Old value overwritten (using 5% since "20%" contains "0%")
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

    [TestMethod]
    public void ProgressBar_WithETA_ShouldDisplayTimeRemaining()
    {
        var console = new VirtualConsole(70, 5);
        
        console.Write("Installing packages...\r\n");
        console.Write("[████████████████████          ] 67% - ETA: 2m 30s");
        
        var row = console.GetRow(1);
        row.GetText().Should().Contain("67%");
        row.GetText().Should().Contain("ETA");
        row.GetText().Should().Contain("2m 30s");
    }
}
