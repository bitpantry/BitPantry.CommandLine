using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Tests for ConsoleLineMirror.RenderWithStyles functionality.
/// </summary>
[TestClass]
public class ConsoleLineMirrorTests
{
    private VirtualConsole.VirtualConsole _virtualConsole;
    private VirtualConsoleAnsiAdapter _adapter;
    private ConsoleLineMirror _mirror;

    [TestInitialize]
    public void Setup()
    {
        _virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        _virtualConsole.StrictMode = true;
        _adapter = new VirtualConsoleAnsiAdapter(_virtualConsole);
        _mirror = new ConsoleLineMirror(_adapter);
    }

    // Implements: CV-040
    [TestMethod]
    public void RenderWithStyles_SingleSegment_DisplaysCorrectColor()
    {
        // Arrange - single segment with cyan style
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group) // Cyan
        };

        // Act
        _mirror.RenderWithStyles(segments, 6);

        // Assert - text should display in cyan
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server");
        // Color.Cyan = 256-color index 14
        _virtualConsole.GetCell(0, 0).Style.Foreground256.Should().Be(14, "Group style is Cyan (256-color index 14)");
    }

    // Implements: CV-041
    [TestMethod]
    public void RenderWithStyles_MultipleSegments_DisplaysCorrectColors()
    {
        // Arrange - multiple segments: group (cyan), command (default), argument (yellow)
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),      // Cyan
            new StyledSegment("start", 7, 12, SyntaxColorScheme.Command),    // Default
            new StyledSegment("--port", 13, 19, SyntaxColorScheme.ArgumentName) // Yellow
        };

        // Act
        _mirror.RenderWithStyles(segments, 19);

        // Assert - text should display with correct colors
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("serverstart--port");
        // Color.Cyan = index 14
        _virtualConsole.GetCell(0, 0).Style.Foreground256.Should().Be(14, "server should be Cyan");
        // Default style = null
        _virtualConsole.GetCell(0, 6).Style.Foreground256.Should().BeNull("start should be default");
        // Color.Yellow = index 11
        _virtualConsole.GetCell(0, 11).Style.Foreground256.Should().Be(11, "--port should be Yellow");
    }

    // Implements: CV-042
    [TestMethod]
    public void RenderWithStyles_CursorAtEnd_PositionedCorrectly()
    {
        // Arrange - single segment, cursor at end
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group)
        };

        // Act
        _mirror.RenderWithStyles(segments, 6); // cursor at end

        // Assert - cursor should be at position 6
        _mirror.BufferPosition.Should().Be(6);
        _virtualConsole.CursorColumn.Should().Be(6);
    }

    // Implements: CV-043
    [TestMethod]
    public void RenderWithStyles_CursorMidLine_PositionedCorrectly()
    {
        // Arrange - segments with cursor in the middle
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),
            new StyledSegment("start", 7, 12, SyntaxColorScheme.Command)
        };

        // Act - cursor at position 3 (middle of "server")
        _mirror.RenderWithStyles(segments, 3);

        // Assert - cursor should be at position 3
        _mirror.BufferPosition.Should().Be(3);
        _virtualConsole.CursorColumn.Should().Be(3);
    }

    // Implements: CV-044
    [TestMethod]
    public void RenderWithStyles_ClearsExistingContent()
    {
        // Arrange - write some initial content
        _mirror.Write("initial content here");
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("initial content here");

        // New shorter content
        var segments = new List<StyledSegment>
        {
            new StyledSegment("hello", 0, 5, SyntaxColorScheme.Group)
        };

        // Act
        _mirror.RenderWithStyles(segments, 5);

        // Assert - old content should be cleared, only new content visible
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("hello");
        _mirror.Buffer.Should().Be("hello");
    }

    // Implements: UX-010
    [TestMethod]
    public void RenderWithStyles_MidLineEdit_CursorReturnsToSamePosition()
    {
        // Arrange - simulate user typing "server start" with cursor at position 10 (middle of "start")
        // Initial typing
        _mirror.Write("server start");
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server start");
        _mirror.BufferPosition.Should().Be(12); // cursor at end after typing

        // Move cursor to position 10 (middle of "start")
        _mirror.MoveToPosition(10);
        _mirror.BufferPosition.Should().Be(10);
        _virtualConsole.CursorColumn.Should().Be(10);

        // Simulate inserting a character 'x' at position 10, making "server stxart"
        // In real scenario, the input would be re-highlighted and re-rendered
        var newSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),
            new StyledSegment(" ", 6, 7, SyntaxColorScheme.Default),
            new StyledSegment("stxart", 7, 13, SyntaxColorScheme.Default)
        };

        // Act - re-render with cursor at position 11 (after the inserted 'x')
        _mirror.RenderWithStyles(newSegments, 11);

        // Assert - cursor should be at position 11 after re-render
        _mirror.BufferPosition.Should().Be(11);
        _virtualConsole.CursorColumn.Should().Be(11);
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server stxart");
    }

    #region Differential Rendering Tests

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles twice
    ///   Breakage detection: YES - verifies only changed portion is rewritten via WriteLog
    ///   Not a tautology: YES - verifies actual console output and write patterns
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_AppendChar_OnlyRewritesChangedSegments()
    {
        // Arrange - enable write logging to track what gets written
        _adapter.WriteLogEnabled = true;
        
        // Initial render
        var initialSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(initialSegments, 6);
        
        // Clear write log to track only the second render
        _adapter.WriteLog.Clear();
        
        // Act - append one character (common case: typing at end)
        var updatedSegments = new List<StyledSegment>
        {
            new StyledSegment("servers", 0, 7, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(updatedSegments, 7);
        
        // Assert - should have written just the appended character, not the full line
        // The log should NOT contain the full "servers" text written from position 0
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("servers");
        _mirror.Buffer.Should().Be("servers");
        _mirror.BufferPosition.Should().Be(7);
        
        // Verify differential rendering: the log should contain the new character 's'
        var writeLog = _adapter.WriteLog.Contents;
        writeLog.Should().Contain("s", "Differential path should write the appended character");
        // The differential path should NOT rewrite the unchanged prefix
        writeLog.Should().NotContain("server", "Differential path should not rewrite unchanged 'server' prefix");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles twice
    ///   Breakage detection: YES - verifies style change triggers rewrite
    ///   Not a tautology: YES - verifies actual console output and colors
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_StyleChangeOnExistingSegment_RewritesAffectedSegment()
    {
        // Arrange - initial render with default style
        var initialSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, Style.Plain)
        };
        _mirror.RenderWithStyles(initialSegments, 6);
        
        // Verify initial state - no color
        _virtualConsole.GetCell(0, 0).Style.Foreground256.Should().BeNull("initial should be plain");
        
        // Act - re-render with different style (same text)
        var updatedSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group) // Now Cyan
        };
        _mirror.RenderWithStyles(updatedSegments, 6);
        
        // Assert - text unchanged but style should be updated
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server");
        _mirror.Buffer.Should().Be("server");
        // Color.Cyan = 256-color index 14
        _virtualConsole.GetCell(0, 0).Style.Foreground256.Should().Be(14, "style should change to Cyan");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles twice with identical segments
    ///   Breakage detection: YES - verifies no console writes on identical content
    ///   Not a tautology: YES - verifies actual console output remains unchanged
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_NoChange_MinimalConsoleWrites()
    {
        // Arrange - enable write logging
        _adapter.WriteLogEnabled = true;
        
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(segments, 6);
        
        // Clear write log
        _adapter.WriteLog.Clear();
        
        // Act - render identical segments
        var sameSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(sameSegments, 6);
        
        // Assert - minimal or no output (just cursor positioning if needed)
        var writeLog = _adapter.WriteLog.Contents;
        writeLog.Should().NotContain("server", "Should not rewrite identical content");
        // Buffer and position should remain correct
        _mirror.Buffer.Should().Be("server");
        _mirror.BufferPosition.Should().Be(6);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles twice
    ///   Breakage detection: YES - verifies trailing characters are erased
    ///   Not a tautology: YES - verifies actual console output shows shorter content
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_ShorterContent_ClearsTrailingCharacters()
    {
        // Arrange - render longer content first
        var longerSegments = new List<StyledSegment>
        {
            new StyledSegment("server connect", 0, 14, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(longerSegments, 14);
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server connect");
        
        // Act - render shorter content
        var shorterSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(shorterSegments, 6);
        
        // Assert - old trailing content should be erased
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server");
        _mirror.Buffer.Should().Be("server");
        _mirror.BufferPosition.Should().Be(6);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles for mid-line change scenario
    ///   Breakage detection: YES - verifies clean redraw for complex changes
    ///   Not a tautology: YES - verifies actual console output is correct
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_MidLineInsertion_RedrawsCorrectly()
    {
        // Arrange - initial content
        var initialSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),
            new StyledSegment(" ", 6, 7, Style.Plain),
            new StyledSegment("connect", 7, 14, SyntaxColorScheme.Command)
        };
        _mirror.RenderWithStyles(initialSegments, 14);
        
        // Act - simulate mid-line insertion (adding "x" in middle of "connect")
        var modifiedSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),
            new StyledSegment(" ", 6, 7, Style.Plain),
            new StyledSegment("conxnect", 7, 15, SyntaxColorScheme.Command)
        };
        _mirror.RenderWithStyles(modifiedSegments, 15);
        
        // Assert - content should be correctly updated
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server conxnect");
        _mirror.Buffer.Should().Be("server conxnect");
        _mirror.BufferPosition.Should().Be(15);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles with completely different content
    ///   Breakage detection: YES - verifies clean redraw for replacement
    ///   Not a tautology: YES - verifies actual console output is correct
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_CompleteReplacement_RedrawsCleanly()
    {
        // Arrange - initial content
        var initialSegments = new List<StyledSegment>
        {
            new StyledSegment("server connect", 0, 14, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(initialSegments, 14);
        
        // Act - completely different content (e.g., history recall)
        var replacementSegments = new List<StyledSegment>
        {
            new StyledSegment("help", 0, 4, SyntaxColorScheme.Command)
        };
        _mirror.RenderWithStyles(replacementSegments, 4);
        
        // Assert - new content should be displayed, old content cleared
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("help");
        _mirror.Buffer.Should().Be("help");
        _mirror.BufferPosition.Should().Be(4);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles
    ///   Breakage detection: YES - verifies ANSI erase is used instead of space loop
    ///   Not a tautology: YES - verifies actual ANSI sequence in write log
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_FullRedraw_UsesAnsiErase()
    {
        // Arrange - enable write logging
        _adapter.WriteLogEnabled = true;
        
        // Initial render
        var initialSegments = new List<StyledSegment>
        {
            new StyledSegment("server connect", 0, 14, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(initialSegments, 14);
        _adapter.WriteLog.Clear();
        
        // Act - complete replacement triggers full redraw
        var replacementSegments = new List<StyledSegment>
        {
            new StyledSegment("help", 0, 4, SyntaxColorScheme.Command)
        };
        _mirror.RenderWithStyles(replacementSegments, 4);
        
        // Assert - should use ANSI erase (CSI K = \x1B[K) instead of space-writing loop
        var writeLog = _adapter.WriteLog.Contents;
        writeLog.Should().Contain("\x1B[K", "Full redraw should use ANSI erase-to-end-of-line");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles
    ///   Breakage detection: YES - verifies buffer matches segments
    ///   Not a tautology: YES - verifies actual buffer content
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_DifferentialPath_BufferMatchesSegments()
    {
        // Arrange - initial render
        var initialSegments = new List<StyledSegment>
        {
            new StyledSegment("serve", 0, 5, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(initialSegments, 5);
        
        // Act - append via differential path
        var updatedSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(updatedSegments, 6);
        
        // Assert - buffer should exactly match concatenated segment text
        var expectedContent = string.Concat(updatedSegments.Select(s => s.Text));
        _mirror.Buffer.Should().Be(expectedContent);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles
    ///   Breakage detection: YES - verifies cursor position after differential render
    ///   Not a tautology: YES - verifies actual cursor position
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_DifferentialPath_CursorAtCorrectPosition()
    {
        // Arrange - initial render
        var initialSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(initialSegments, 6);
        
        // Act - append via differential path, cursor at end
        var updatedSegments = new List<StyledSegment>
        {
            new StyledSegment("server ", 0, 7, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(updatedSegments, 7);
        
        // Assert - cursor should be at requested position
        _mirror.BufferPosition.Should().Be(7);
        _virtualConsole.CursorColumn.Should().Be(7);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles once (no cache)
    ///   Breakage detection: YES - verifies first render works correctly
    ///   Not a tautology: YES - verifies actual console output
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_FirstRender_WorksCorrectly()
    {
        // Arrange - segments for first render (no cached state)
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),
            new StyledSegment(" ", 6, 7, Style.Plain),
            new StyledSegment("connect", 7, 14, SyntaxColorScheme.Command)
        };
        
        // Act - first render (no previous state)
        _mirror.RenderWithStyles(segments, 14);
        
        // Assert - all segments rendered correctly
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server connect");
        _mirror.Buffer.Should().Be("server connect");
        _mirror.BufferPosition.Should().Be(14);
        _virtualConsole.GetCell(0, 0).Style.Foreground256.Should().Be(14, "server should be Cyan");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles with empty segments
    ///   Breakage detection: YES - verifies line is fully cleared
    ///   Not a tautology: YES - verifies actual console output is empty
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_SegmentsToEmpty_ClearsEntireLine()
    {
        // Arrange - render initial content
        var initialSegments = new List<StyledSegment>
        {
            new StyledSegment("server connect", 0, 14, SyntaxColorScheme.Group)
        };
        _mirror.RenderWithStyles(initialSegments, 14);
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server connect");
        
        // Act - render empty segments
        var emptySegments = new List<StyledSegment>();
        _mirror.RenderWithStyles(emptySegments, 0);
        
        // Assert - line should be cleared
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().BeEmpty();
        _mirror.Buffer.Should().BeEmpty();
        _mirror.BufferPosition.Should().Be(0);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls RenderWithStyles with multiple segment changes
    ///   Breakage detection: YES - verifies all changed segments are rewritten correctly
    ///   Not a tautology: YES - verifies actual console output and colors
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_MultipleSegmentStyleChanges_UpdatesCorrectly()
    {
        // Arrange - initial render with all plain style
        var initialSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, Style.Plain),
            new StyledSegment(" ", 6, 7, Style.Plain),
            new StyledSegment("connect", 7, 14, Style.Plain)
        };
        _mirror.RenderWithStyles(initialSegments, 14);
        
        // Act - update with styled segments (simulating highlighting after recognition)
        var styledSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),    // Now Cyan
            new StyledSegment(" ", 6, 7, Style.Plain),
            new StyledSegment("connect", 7, 14, SyntaxColorScheme.Command) // Default
        };
        _mirror.RenderWithStyles(styledSegments, 14);
        
        // Assert - text unchanged, styles updated
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server connect");
        _virtualConsole.GetCell(0, 0).Style.Foreground256.Should().Be(14, "server should now be Cyan");
    }

    #endregion
}
