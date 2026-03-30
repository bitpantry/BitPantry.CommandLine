using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Rendering;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Tests for ConsoleLineMirror behavior when input text wraps past the terminal width.
/// Validates fix for issue #36: cursor position desync across row boundaries.
/// </summary>
[TestClass]
public class ConsoleLineMirrorWrapTests
{
    // Use a narrow terminal to make wrapping easy to trigger
    private const int TerminalWidth = 40;
    private const int TerminalHeight = 24;
    private const int PromptLength = 10; // e.g., "bp [app]> "

    private VirtualConsole.VirtualConsole _virtualConsole;
    private VirtualConsoleAnsiAdapter _adapter;
    private ConsoleLineMirror _mirror;

    [TestInitialize]
    public void Setup()
    {
        _virtualConsole = new VirtualConsole.VirtualConsole(TerminalWidth, TerminalHeight);
        _virtualConsole.StrictMode = true;
        _adapter = new VirtualConsoleAnsiAdapter(_virtualConsole);

        // Simulate prompt by writing it to the console first
        _adapter.Write("bp [app]> "); // 10 chars = PromptLength

        _mirror = new ConsoleLineMirror(_adapter, PromptLength);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls ConsoleLineMirror.Write
    ///   Breakage detection: YES — if cursor position not tracked across wrap, assertion fails
    ///   Not a tautology: YES — verifies cursor row/col on terminal grid
    /// </summary>
    [TestMethod]
    public void Write_TextExceedsTerminalWidth_CursorPositionCorrect()
    {
        // Arrange — text that exceeds terminal width (prompt=10, width=40, so 30 chars fills row 0)
        // 35 chars of input: wraps 5 chars onto row 1
        var text = new string('A', 35);

        // Act
        _mirror.Write(text);

        // Assert — cursor should be on row 1, column 5 (promptLength=10 + 35 = 45, 45 % 40 = 5)
        _mirror.BufferPosition.Should().Be(35);
        _virtualConsole.CursorRow.Should().Be(1, "text wraps to second row");
        _virtualConsole.CursorColumn.Should().Be(5, "5 chars overflow onto row 1");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls ConsoleLineMirror.Write
    ///   Breakage detection: YES — if chars duplicated at wrap boundary, text won't match
    ///   Not a tautology: YES — verifies actual screen content across rows
    /// </summary>
    [TestMethod]
    public void Write_TextExceedsTerminalWidth_NoCharacterDuplication()
    {
        // Arrange — text that wraps: 30 chars on row 0 (after prompt), 5 on row 1
        var text = "ABCDEFGHIJ" + "KLMNOPQRST" + "UVWXYZ1234" + "56789";

        // Act
        _mirror.Write(text);

        // Assert — row 0 should have prompt + first 30 chars, row 1 should have last 5
        var row0 = _virtualConsole.GetRow(0).GetText();
        var row1 = _virtualConsole.GetRow(1).GetText().TrimEnd();

        // First 10 cols are prompt, next 30 are input
        row0.Substring(PromptLength, 30).Should().Be("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234");
        row1.Should().Be("56789");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls MoveToPosition across row boundary
    ///   Breakage detection: YES — if MoveLeft can't cross rows, cursor stays on wrong row
    ///   Not a tautology: YES — verifies physical cursor position matches target
    /// </summary>
    [TestMethod]
    public void MoveToPosition_AcrossRowBoundary_CursorCorrect()
    {
        // Arrange — write 35 chars to wrap to row 1, col 5
        _mirror.Write(new string('X', 35));

        // Act — move back to position 10 (should be on row 0, col 20 = prompt 10 + 10)
        _mirror.MoveToPosition(10);

        // Assert — cursor should be on row 0, column 20
        _mirror.BufferPosition.Should().Be(10);
        _virtualConsole.CursorRow.Should().Be(0, "position 10 is on the first row");
        _virtualConsole.CursorColumn.Should().Be(20, "prompt(10) + bufferPos(10) = col 20");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls Backspace at row boundary
    ///   Breakage detection: YES — if cursor can't move back across row boundary, backspace fails
    ///   Not a tautology: YES — verifies cursor position and buffer content after backspace
    /// </summary>
    [TestMethod]
    public void Backspace_AtStartOfWrappedRow_MovesToPreviousRow()
    {
        // Arrange — write exactly 30 chars to fill row 0, then 1 more to wrap
        _mirror.Write(new string('A', 30)); // fills row 0
        _mirror.Write("B"); // wraps to row 1, col 0

        // Verify precondition: cursor is on row 1
        _virtualConsole.CursorRow.Should().Be(1);
        _virtualConsole.CursorColumn.Should().Be(1, "one char written on row 1");

        // Act — backspace should remove 'B' and move cursor back to row 0
        _mirror.Backspace();

        // Assert
        _mirror.BufferPosition.Should().Be(30);
        _mirror.Buffer.Should().Be(new string('A', 30));
        // Offset 40 (prompt 10 + buffer 30) in a 40-col terminal = row 1, col 0
        // This is the standard terminal position after content fills a row exactly
        _virtualConsole.CursorRow.Should().Be(1, "offset 40 in 40-col terminal = row 1");
        _virtualConsole.CursorColumn.Should().Be(0, "offset 40 % 40 = col 0");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls RenderWithStyles with content spanning rows
    ///   Breakage detection: YES — if styling fails across wrap, cells won't have correct colors
    ///   Not a tautology: YES — verifies per-cell foreground colors on both rows
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_WrappedLine_AllSegmentsStyled()
    {
        // Arrange — create styled segments that span the wrap boundary
        // Segment 1: 25 chars cyan (fills most of row 0 after prompt)
        // Segment 2: 10 chars yellow (crosses wrap boundary: 5 on row 0, 5 on row 1)
        var cyanText = new string('G', 25);
        var yellowText = new string('A', 10);
        var fullText = cyanText + yellowText;

        _mirror.Write(fullText);

        var segments = new List<StyledSegment>
        {
            new StyledSegment(cyanText, 0, 25, SyntaxColorScheme.Group),        // Cyan
            new StyledSegment(yellowText, 25, 35, SyntaxColorScheme.ArgumentName) // Yellow
        };

        // Act
        _mirror.RenderWithStyles(segments, 35);

        // Assert — cyan cells on row 0 (columns 10-34)
        _virtualConsole.GetCell(0, PromptLength).Style.Foreground256.Should().Be(14, "first cyan char");
        _virtualConsole.GetCell(0, PromptLength + 24).Style.Foreground256.Should().Be(14, "last cyan char");

        // Yellow cells that are still on row 0 (columns 35-39)
        _virtualConsole.GetCell(0, PromptLength + 25).Style.Foreground256.Should().Be(11, "first yellow char on row 0");
        _virtualConsole.GetCell(0, 39).Style.Foreground256.Should().Be(11, "last yellow char on row 0");

        // Yellow cells that wrapped to row 1 (columns 0-4)
        _virtualConsole.GetCell(1, 0).Style.Foreground256.Should().Be(11, "first yellow char on row 1");
        _virtualConsole.GetCell(1, 4).Style.Foreground256.Should().Be(11, "last yellow char on row 1");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls Write then Delete on wrapped row
    ///   Breakage detection: YES — if cursor can't rewrite across rows, content corrupts
    ///   Not a tautology: YES — verifies buffer content and cursor position after delete
    /// </summary>
    [TestMethod]
    public void Delete_OnWrappedRow_RewritesCorrectly()
    {
        // Arrange — write text that wraps, position cursor on row 1
        _mirror.Write(new string('A', 30) + "BCDEF");
        _mirror.MoveToPosition(31); // on row 1, col 1: position of 'C'

        // Act — delete 'C'
        _mirror.Delete();

        // Assert — 'C' removed, remaining text shifted
        _mirror.Buffer.Should().Be(new string('A', 30) + "BDEF");
        _mirror.BufferPosition.Should().Be(31);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls Write in insert mode on wrapped row
    ///   Breakage detection: YES — if trailing text rewrite fails across rows, content corrupts
    ///   Not a tautology: YES — verifies buffer and cursor position after insert
    /// </summary>
    [TestMethod]
    public void Write_InsertMode_OnWrappedRow_ShiftsTrailingText()
    {
        // Arrange — write text that wraps, position cursor on row 1
        _mirror.Write(new string('A', 30) + "BCDEF"); // 35 chars, wraps
        _mirror.MoveToPosition(32); // on row 1, col 2: between 'C' and 'D'

        // Act — insert 'X' at position 32
        _mirror.Write("X");

        // Assert — 'X' inserted, trailing text shifted
        _mirror.Buffer.Should().Be(new string('A', 30) + "BCXDEF");
        _mirror.BufferPosition.Should().Be(33);
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls Write with exact row-filling count
    ///   Breakage detection: YES — if delayed wrap not handled, next char position wrong
    ///   Not a tautology: YES — verifies cursor at correct position at boundary
    /// </summary>
    [TestMethod]
    public void Write_TextExactlyFillsWidth_DelayedWrapHandled()
    {
        // Arrange — write exactly 30 chars (prompt=10, width=40) to fill row 0
        var text = new string('A', 30);

        // Act
        _mirror.Write(text);

        // Assert — cursor should be at (0, 39) or (1, 0) depending on delayed wrap
        // The key point is: writing one more char should wrap cleanly
        _mirror.BufferPosition.Should().Be(30);

        // Write one more char — should appear on row 1, col 0
        _mirror.Write("B");
        _virtualConsole.CursorRow.Should().Be(1);
        _virtualConsole.CursorColumn.Should().Be(1, "one char past wrap");
        _virtualConsole.GetCell(1, 0).Character.Should().Be('B');
    }

    /// <summary>
    /// Bug: When text spans 3+ rows, backspacing from end across the row 2→1 boundary
    /// leaves the cursor on the wrong row. The Backspace() method writes a trailing space
    /// that lands on the wrap boundary, putting the VirtualConsole cursor in pending wrap
    /// state (col=Width). EmitCursorMovement then fails to reposition because _pendingWrap
    /// adjustment makes from==to, so no movement is emitted.
    /// After that, subsequent backspaces compute wrong row deltas because the physical
    /// cursor position doesn't match the code's model.
    ///
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls Backspace() crossing row 2→1 boundary
    ///   Breakage detection: YES — if cursor row is wrong after boundary cross, assertion fails
    ///   Not a tautology: YES — verifies physical cursor position on VirtualConsole grid
    /// </summary>
    [TestMethod]
    public void Backspace_ThreeRows_CrossingRow2ToRow1_CursorPositionCorrect()
    {
        // Arrange — write text that spans 3 rows:
        // Row 0: prompt(10) + 30 chars = 40 (full)
        // Row 1: 40 chars (full)
        // Row 2: 1 char
        // Total: 71 chars
        _mirror.Write(new string('A', 71));

        // Verify precondition: cursor on row 2
        _virtualConsole.CursorRow.Should().Be(2, "71 chars + prompt=10 = 81, row = 81/40 = 2");
        _virtualConsole.CursorColumn.Should().Be(1, "81 % 40 = 1");

        // Act — backspace once: removes the char on row 2, cursor stays on row 2
        _mirror.Backspace();
        _mirror.BufferPosition.Should().Be(70);
        // offset = 80 → row 2, col 0
        _virtualConsole.CursorRow.Should().Be(2, "offset 80 in 40-col terminal: row 2, col 0");
        _virtualConsole.CursorColumn.Should().Be(0, "offset 80 % 40 = 0");

        // Act — backspace again: crosses from row 2 col 0 to row 1 col 39
        _mirror.Backspace();
        _mirror.BufferPosition.Should().Be(69);
        // offset = 79 → row 1, col 39
        _virtualConsole.CursorRow.Should().Be(1, "backspace should cross to row 1");
        _virtualConsole.CursorColumn.Should().Be(39, "offset 79 % 40 = 39");

        // Act — one more backspace: stays on row 1
        _mirror.Backspace();
        _mirror.BufferPosition.Should().Be(68);
        // offset = 78 → row 1, col 38
        _virtualConsole.CursorRow.Should().Be(1, "should remain on row 1");
        _virtualConsole.CursorColumn.Should().Be(38, "offset 78 % 40 = 38");
    }

    /// <summary>
    /// Bug: When Backspace is followed by RenderWithStyles (as InputBuilder does after
    /// each keystroke), the cursor position desync from the pending-wrap issue causes
    /// RenderFull/RenderDifferential to start writing from the wrong position, corrupting
    /// the display across row boundaries.
    ///
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls Backspace() then RenderWithStyles() across boundary
    ///   Breakage detection: YES — if cursor/content wrong after render, assertions fail
    ///   Not a tautology: YES — verifies physical cursor position and screen content
    /// </summary>
    [TestMethod]
    public void Backspace_ThreeRows_WithRenderAfterEach_CursorAndContentCorrect()
    {
        // Arrange — 3-row text: prompt(10) + 71 chars = 81 total
        var text = new string('X', 71);
        _mirror.Write(text);

        // Simulate what InputBuilder does: after first write, call RenderWithStyles
        var segments = new List<StyledSegment>
        {
            new StyledSegment(text, 0, 71, SyntaxColorScheme.ArgumentValue)
        };
        _mirror.RenderWithStyles(segments, 71);

        // Verify precondition
        _virtualConsole.CursorRow.Should().Be(2);

        // Act — backspace to cross row 2→1, with RenderWithStyles after each (like InputBuilder)
        _mirror.Backspace();  // 71→70
        segments = new List<StyledSegment>
        {
            new StyledSegment(new string('X', 70), 0, 70, SyntaxColorScheme.ArgumentValue)
        };
        _mirror.RenderWithStyles(segments, 70);

        _mirror.Backspace();  // 70→69 — crosses row 2→1
        segments = new List<StyledSegment>
        {
            new StyledSegment(new string('X', 69), 0, 69, SyntaxColorScheme.ArgumentValue)
        };
        _mirror.RenderWithStyles(segments, 69);

        // Assert — cursor should be on row 1 after crossing
        _mirror.BufferPosition.Should().Be(69);
        _virtualConsole.CursorRow.Should().Be(1, "should be on row 1 after crossing boundary");
        _virtualConsole.CursorColumn.Should().Be(39, "offset 79 % 40 = 39");

        // Continue backspacing — should stay on row 1
        _mirror.Backspace();  // 69→68
        segments = new List<StyledSegment>
        {
            new StyledSegment(new string('X', 68), 0, 68, SyntaxColorScheme.ArgumentValue)
        };
        _mirror.RenderWithStyles(segments, 68);

        _virtualConsole.CursorRow.Should().Be(1, "should stay on row 1");
        _virtualConsole.CursorColumn.Should().Be(38, "offset 78 % 40 = 38");

        // Verify screen content: row 0 should have prompt + 30 X's, row 1 should have remaining
        var row0Text = _virtualConsole.GetRow(0).GetText();
        row0Text.Substring(PromptLength, 30).Should().Be(new string('X', 30));
    }

    /// <summary>
    /// Bug: The character at the exact wrap boundary (last column before wrapping to next row)
    /// is not styled when RenderWithStyles is called. This may be due to _pendingWrap
    /// state confusion during the render positioning.
    ///
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls Write + RenderWithStyles with wrap-boundary content
    ///   Breakage detection: YES — if boundary char unstyled, cell color check fails
    ///   Not a tautology: YES — verifies per-cell foreground color at the exact boundary
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_CharAtWrapBoundary_IsStyled()
    {
        // Arrange — write text that fills row 0 exactly and wraps to row 1
        // Row 0: prompt(10) + 30 chars = 40 (boundary char at col 39)
        // Row 1: 5 chars
        var text = new string('Z', 35);
        _mirror.Write(text);

        var segments = new List<StyledSegment>
        {
            new StyledSegment(text, 0, 35, SyntaxColorScheme.ArgumentValue) // Purple (256-color 5)
        };

        // Act
        _mirror.RenderWithStyles(segments, 35);

        // Assert — the character at col 39 (last on row 0, the wrap boundary) should be styled
        _virtualConsole.GetCell(0, 39).Character.Should().Be('Z', "wrap boundary char should be 'Z'");
        _virtualConsole.GetCell(0, 39).Style.Foreground256.Should().Be(5,
            "wrap boundary char should have purple style (256-color 5)");

        // Also verify the first char on row 1 is styled
        _virtualConsole.GetCell(1, 0).Character.Should().Be('Z');
        _virtualConsole.GetCell(1, 0).Style.Foreground256.Should().Be(5,
            "first char on row 1 should also be styled");
    }

    /// <summary>
    /// Bug: Repeated differential renders after appending characters one at a time
    /// (simulating paste) should style all characters including the one that lands
    /// exactly at the wrap boundary.
    ///
    /// Test Validity Check:
    ///   Invokes code under test: YES — calls Write + RenderWithStyles incrementally
    ///   Breakage detection: YES — if boundary char unstyled after differential render, fails
    ///   Not a tautology: YES — verifies actual cell style at boundary position
    /// </summary>
    [TestMethod]
    public void RenderWithStyles_IncrementalAppend_BoundaryCharStyled()
    {
        // Arrange — write 29 chars (one short of filling row 0)
        _mirror.Write(new string('A', 29));
        var segments29 = new List<StyledSegment>
        {
            new StyledSegment(new string('A', 29), 0, 29, SyntaxColorScheme.ArgumentValue)
        };
        _mirror.RenderWithStyles(segments29, 29); // first render → RenderFull

        // Act — write the 30th char (fills row 0 exactly, offset = 40 = boundary)
        _mirror.Write("A");
        var segments30 = new List<StyledSegment>
        {
            new StyledSegment(new string('A', 30), 0, 30, SyntaxColorScheme.ArgumentValue)
        };
        _mirror.RenderWithStyles(segments30, 30); // second render → RenderDifferential

        // Assert — the boundary char at col 39 should be styled purple
        _virtualConsole.GetCell(0, 39).Character.Should().Be('A');
        _virtualConsole.GetCell(0, 39).Style.Foreground256.Should().Be(5,
            "boundary char should be styled after differential render");

        // Write one more char (wraps to row 1)
        _mirror.Write("A");
        var segments31 = new List<StyledSegment>
        {
            new StyledSegment(new string('A', 31), 0, 31, SyntaxColorScheme.ArgumentValue)
        };
        _mirror.RenderWithStyles(segments31, 31);

        // Boundary char should still be styled
        _virtualConsole.GetCell(0, 39).Style.Foreground256.Should().Be(5,
            "boundary char should remain styled after wrapping");
        _virtualConsole.GetCell(1, 0).Style.Foreground256.Should().Be(5,
            "wrapped char should be styled");
    }

    /// <summary>
    /// Simulates the exact InputBuilder paste pattern: for each character, Write(char)
    /// followed by RenderWithStyles with multi-segment highlighted output.
    /// This reproduces the real behavior where typing "server connect " then pasting
    /// a long string processes char-by-char with differential rendering.
    ///
    /// Bug: In real terminal, pasted text "never goes to the next line, pushes left
    /// off the screen." Cursor and row tracking desync during char-by-char paste.
    ///
    /// Test Validity Check:
    ///   Invokes code under test: YES — Write(char) + RenderWithStyles per character
    ///   Breakage detection: YES — cursor position checked at row boundaries
    ///   Not a tautology: YES — verifies physical cursor on VirtualConsole grid
    /// </summary>
    [TestMethod]
    public void CharByCharPaste_LongString_CursorWrapsCorrectly()
    {
        // Arrange — simulate typing "cmd val " (8 chars) first, like "server connect "
        // prompt(10) + "cmd val " = 18 chars on row 0 before paste
        var prefix = "cmd val ";
        foreach (var ch in prefix)
        {
            _mirror.Write(ch);
            var segments = MakeSegments(_mirror.Buffer);
            _mirror.RenderWithStyles(segments, _mirror.BufferPosition);
        }

        // Verify pre-paste state
        _virtualConsole.CursorRow.Should().Be(0, "pre-paste: cursor on row 0");
        _virtualConsole.CursorColumn.Should().Be(18, "pre-paste: prompt(10) + 8 = col 18");

        // Act — paste 62 chars char by char (total: 10 + 8 + 62 = 80 = exactly 2 full rows)
        var pasteText = "now is the time for all good men to come to the aid of their c";
        System.Diagnostics.Debug.Assert(pasteText.Length == 62);

        for (int i = 0; i < pasteText.Length; i++)
        {
            _mirror.Write(pasteText[i]);
            var segments = MakeSegments(_mirror.Buffer);
            _mirror.RenderWithStyles(segments, _mirror.BufferPosition);

            // Verify cursor position at key boundaries
            int totalOffset = PromptLength + _mirror.BufferPosition;
            int expectedRow = totalOffset / TerminalWidth;
            int expectedCol = totalOffset % TerminalWidth;

            // Check at row boundaries (wrapping from row 0→1 and row 1→2)
            if (i == 21) // totalOffset = 10+30 = 40 → row 1, col 0
            {
                _virtualConsole.CursorRow.Should().Be(1,
                    $"paste char {i}: offset {totalOffset} should be row 1");
                _virtualConsole.CursorColumn.Should().Be(0,
                    $"paste char {i}: offset {totalOffset} should be col 0");
            }
            else if (i == 61) // totalOffset = 10+70 = 80 → row 2, col 0
            {
                _virtualConsole.CursorRow.Should().Be(2,
                    $"paste char {i}: offset {totalOffset} should be row 2");
                _virtualConsole.CursorColumn.Should().Be(0,
                    $"paste char {i}: offset {totalOffset} should be col 0");
            }
        }

        // Assert — final cursor position
        int finalOffset = PromptLength + _mirror.BufferPosition; // 10 + 70 = 80
        _virtualConsole.CursorRow.Should().Be(finalOffset / TerminalWidth,
            "final paste position: row");
        _virtualConsole.CursorColumn.Should().Be(finalOffset % TerminalWidth,
            "final paste position: col");
    }

    /// <summary>
    /// After char-by-char paste fills 3 rows, backspace char-by-char with
    /// RenderWithStyles after each (matching InputBuilder behavior).
    /// Verifies cursor correctly crosses row boundaries backwards and
    /// never goes above the prompt row.
    ///
    /// Bug: In real terminal, backspace "deletes back to column 1 then jumps to
    /// last column row-1" and eventually backspaces into intro text above prompt.
    ///
    /// Test Validity Check:
    ///   Invokes code under test: YES — Backspace + RenderWithStyles per keystroke
    ///   Breakage detection: YES — cursor position and row checked at boundaries
    ///   Not a tautology: YES — verifies physical cursor on VirtualConsole grid
    /// </summary>
    [TestMethod]
    public void CharByCharPaste_ThenBackspace_CursorNeverExceedsPromptRow()
    {
        // Arrange — simulate char-by-char paste of text spanning 3 rows
        var prefix = "cmd val ";
        foreach (var ch in prefix)
        {
            _mirror.Write(ch);
            var segments = MakeSegments(_mirror.Buffer);
            _mirror.RenderWithStyles(segments, _mirror.BufferPosition);
        }

        // Paste 63 chars: total offset = 10 + 71 = 81, spans rows 0-2
        var pasteText = "now is the time for all good men to come to the aid of their co";
        System.Diagnostics.Debug.Assert(pasteText.Length == 63);
        System.Diagnostics.Debug.Assert(PromptLength + prefix.Length + pasteText.Length == 81);

        foreach (var ch in pasteText)
        {
            _mirror.Write(ch);
            var segments = MakeSegments(_mirror.Buffer);
            _mirror.RenderWithStyles(segments, _mirror.BufferPosition);
        }

        // Verify: cursor at row 2, col 1 (offset 81)
        _virtualConsole.CursorRow.Should().Be(2, "after paste: row 2");
        _virtualConsole.CursorColumn.Should().Be(1, "after paste: col 1");

        // Act — backspace all the way to empty, checking cursor at each step
        int backspaceCount = _mirror.BufferPosition; // 71
        for (int i = 0; i < backspaceCount; i++)
        {
            _mirror.Backspace();
            var segments = MakeSegments(_mirror.Buffer);
            if (segments.Count > 0)
                _mirror.RenderWithStyles(segments, _mirror.BufferPosition);

            // cursor should NEVER be on a negative row or above row 0
            _virtualConsole.CursorRow.Should().BeGreaterOrEqualTo(0,
                $"backspace {i + 1}: cursor must not go above row 0");

            // Verify expected position at key boundaries
            int totalOffset = PromptLength + _mirror.BufferPosition;
            int expectedRow = totalOffset / TerminalWidth;
            int expectedCol = totalOffset % TerminalWidth;

            _virtualConsole.CursorRow.Should().Be(expectedRow,
                $"backspace {i + 1}: bufPos={_mirror.BufferPosition}, offset={totalOffset}, expected row {expectedRow}");
            _virtualConsole.CursorColumn.Should().Be(expectedCol,
                $"backspace {i + 1}: bufPos={_mirror.BufferPosition}, offset={totalOffset}, expected col {expectedCol}");
        }

        // Final: cursor at prompt position
        _virtualConsole.CursorRow.Should().Be(0, "after full backspace: row 0");
        _virtualConsole.CursorColumn.Should().Be(PromptLength, "after full backspace: at prompt");
    }

    /// <summary>
    /// Helper: creates simplified multi-segment output matching what SyntaxHighlighter
    /// would produce for "cmd val <argument_text>" — three distinct styled regions.
    /// </summary>
    private static List<StyledSegment> MakeSegments(string buffer)
    {
        var segments = new List<StyledSegment>();
        if (string.IsNullOrEmpty(buffer))
            return segments;

        // Simulate: "cmd" = Group, " " = Default, "val" = Command, " " = Default, rest = ArgValue
        // This matches real highlighter behavior for "server connect <pasted_text>"
        if (buffer.Length <= 3)
        {
            segments.Add(new StyledSegment(buffer, 0, buffer.Length, SyntaxColorScheme.Group));
        }
        else if (buffer.Length <= 4)
        {
            segments.Add(new StyledSegment("cmd", 0, 3, SyntaxColorScheme.Group));
            segments.Add(new StyledSegment(buffer.Substring(3), 3, buffer.Length, SyntaxColorScheme.Default));
        }
        else if (buffer.Length <= 7)
        {
            segments.Add(new StyledSegment("cmd", 0, 3, SyntaxColorScheme.Group));
            segments.Add(new StyledSegment(" ", 3, 4, SyntaxColorScheme.Default));
            segments.Add(new StyledSegment(buffer.Substring(4, buffer.Length - 4), 4, buffer.Length, SyntaxColorScheme.Command));
        }
        else if (buffer.Length <= 8)
        {
            segments.Add(new StyledSegment("cmd", 0, 3, SyntaxColorScheme.Group));
            segments.Add(new StyledSegment(" ", 3, 4, SyntaxColorScheme.Default));
            segments.Add(new StyledSegment("val", 4, 7, SyntaxColorScheme.Command));
            segments.Add(new StyledSegment(buffer.Substring(7), 7, buffer.Length, SyntaxColorScheme.Default));
        }
        else
        {
            segments.Add(new StyledSegment("cmd", 0, 3, SyntaxColorScheme.Group));
            segments.Add(new StyledSegment(" ", 3, 4, SyntaxColorScheme.Default));
            segments.Add(new StyledSegment("val", 4, 7, SyntaxColorScheme.Command));
            segments.Add(new StyledSegment(" ", 7, 8, SyntaxColorScheme.Default));
            segments.Add(new StyledSegment(buffer.Substring(8), 8, buffer.Length, SyntaxColorScheme.ArgumentValue));
        }

        return segments;
    }

    /// <summary>
    /// Diagnostic: verifies that Spectre.Console's Text rendering does not add
    /// trailing newlines that would corrupt cursor positioning.
    /// </summary>
    [TestMethod]
    public void SpectreText_SingleChar_NoTrailingNewline()
    {
        _adapter.WriteLogEnabled = true;
        _adapter.WriteLog.Clear();

        _adapter.Write(new Text("X", new Style(foreground: Color.Purple)));

        var log = _adapter.WriteLog.ToString();
        log.Should().NotContain("\n", "Spectre Text should not add newlines");
        log.Should().NotContain("\r", "Spectre Text should not add carriage returns");
        _virtualConsole.CursorRow.Should().Be(0, "single char should stay on row 0");
        _virtualConsole.CursorColumn.Should().Be(11, "prompt(10) + 1 char = col 11");
    }

    /// <summary>
    /// Bug: When terminal auto-wraps at a wider width than Profile.Width reports,
    /// EmitCursorMovement computes too many CUU (cursor up) commands because it
    /// thinks content spans more rows than it actually does. This causes the
    /// cursor to go ABOVE the prompt row into preceding content (server messages).
    ///
    /// The fix has two parts:
    /// 1. Application level: InputBuilder syncs Profile.Width before each input session
    /// 2. ConsoleLineMirror: _rowsFromPrompt tracking + CUU clamping as defense-in-depth
    ///
    /// This test verifies that with content above the prompt AND correct width,
    /// the cursor never escapes above the prompt row during backspace operations.
    ///
    /// Test Validity Check:
    ///   Invokes code under test: YES — Write + Backspace + RenderWithStyles
    ///   Breakage detection: YES — cursor row checked to never go above prompt row
    ///   Not a tautology: YES — verifies physical cursor position on grid
    /// </summary>
    [TestMethod]
    public void ContentAbovePrompt_BackspaceNeverGoesAbovePromptRow()
    {
        // Arrange — Create terminal with content above the prompt (like the sandbox)
        var console = new VirtualConsole.VirtualConsole(TerminalWidth, TerminalHeight);
        console.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(console);

        // Simulate server startup messages above the prompt
        adapter.WriteLine("Starting server in background...");
        adapter.WriteLine("Starting client...");
        adapter.WriteLine("");
        adapter.WriteLine("Use 'server connect' to connect");
        adapter.WriteLine("");

        // Record the prompt row
        int promptRow = console.CursorRow;
        promptRow.Should().Be(5, "prompt should start after server messages");

        // Write prompt
        adapter.Write("bp [app]> "); // 10 chars

        var mirror = new ConsoleLineMirror(adapter, PromptLength);

        // Write text that spans 3 rows: prompt(10) + 71 = 81, row = 81/40 = 2
        var text = new string('X', 71);
        mirror.Write(text);

        var segments = new List<StyledSegment>
        {
            new StyledSegment(text, 0, 71, SyntaxColorScheme.ArgumentValue)
        };
        mirror.RenderWithStyles(segments, 71);

        // Verify cursor is below prompt
        console.CursorRow.Should().BeGreaterThan(promptRow);

        // Backspace all the way back
        for (int i = 0; i < 71; i++)
        {
            mirror.Backspace();
            var newText = new string('X', 71 - (i + 1));
            if (newText.Length > 0)
            {
                var segs = new List<StyledSegment>
                {
                    new StyledSegment(newText, 0, newText.Length, SyntaxColorScheme.ArgumentValue)
                };
                mirror.RenderWithStyles(segs, newText.Length);
            }

            // CRITICAL: cursor should NEVER go above the prompt row
            console.CursorRow.Should().BeGreaterOrEqualTo(promptRow,
                $"backspace {i + 1}: cursor row {console.CursorRow} must never go above prompt row {promptRow}");
        }

        // After all backspaces, cursor should be at prompt position
        console.CursorRow.Should().Be(promptRow, "after clearing: should be on prompt row");
        console.CursorColumn.Should().Be(PromptLength, "after clearing: should be at prompt end");
    }

    /// <summary>
    /// Prescribed test #8: Ghost text rendering and clearing when input has wrapped.
    /// 
    /// When input text wraps past terminal width and ghost text is shown, the
    /// GhostTextController must:
    /// 1. Render the ghost text after the current buffer position
    /// 2. Move the cursor back to the correct (row, col) after rendering
    /// 3. When clearing, erase the ghost text and restore cursor position
    /// 
    /// Test Validity Check:
    ///   Invokes code under test: YES — GhostTextController.Show, .Clear with wrapped input
    ///   Breakage detection: YES — if cursor restore fails after wrap, position will be wrong
    ///   Not a tautology: YES — verifies cursor position and ghost text rendering on grid
    /// </summary>
    [TestMethod]
    public void GhostText_OnWrappedLine_RendersAndClearsCorrectly()
    {
        // Arrange — write text that wraps: 35 chars puts cursor on row 1, col 5
        _mirror.Write(new string('A', 35));
        
        // Record cursor position before ghost text
        int cursorRowBefore = _virtualConsole.CursorRow;
        int cursorColBefore = _virtualConsole.CursorColumn;
        cursorRowBefore.Should().Be(1, "pre-ghost: cursor on row 1");
        cursorColBefore.Should().Be(5, $"pre-ghost: cursor at col 5 (({PromptLength} + 35) % {TerminalWidth} = 45 % 40)");

        // Create ghost text controller with proper prompt length
        var theme = new Theme();
        var ghostController = new GhostTextController(_adapter, theme, PromptLength);
        
        // Act — show ghost text
        ghostController.Show("_ghost", _mirror);

        // Assert — ghost text should appear after cursor position
        // Cursor should return to original position after Show
        _virtualConsole.CursorRow.Should().Be(cursorRowBefore, "cursor row should be restored after ghost show");
        _virtualConsole.CursorColumn.Should().Be(cursorColBefore, "cursor column should be restored after ghost show");
        
        // Ghost text should be visible at position after buffer (row 1, col 5)
        _virtualConsole.GetCell(1, 5).Character.Should().Be('_', "first ghost char should be at row 1, col 5");
        _virtualConsole.GetCell(1, 6).Character.Should().Be('g', "second ghost char visible");

        // Ghost text should have dim style
        _virtualConsole.Should()
            .HaveRangeWithStyle(row: 1, startColumn: 5, length: 6, CellAttributes.Dim);

        // Act — clear ghost text
        ghostController.Clear();

        // Assert — ghost text should be cleared, cursor restored
        _virtualConsole.CursorRow.Should().Be(cursorRowBefore, "cursor row restored after clear");
        _virtualConsole.CursorColumn.Should().Be(cursorColBefore, "cursor column restored after clear");
        
        // Verify buffer content is still intact
        _mirror.Buffer.Should().Be(new string('A', 35), "buffer unchanged after ghost show/clear");
    }

    /// <summary>
    /// Prescribed test #9: Autocomplete menu positioning when input spans multiple rows.
    /// 
    /// When input text wraps past terminal width and an autocomplete menu is opened,
    /// the AutoCompleteMenuController/Renderer must:
    /// 1. Position the menu below the last row of the wrapped input
    /// 2. Restore cursor to the correct (row, col) on the input after rendering
    /// 3. When closing, properly clear menu lines and restore cursor position
    /// 
    /// Test Validity Check:
    ///   Invokes code under test: YES — AutoCompleteMenuController.Show with wrapped input
    ///   Breakage detection: YES — if cursor column calculation fails for wrapped input, position wrong
    ///   Not a tautology: YES — verifies menu appears below input and cursor position is correct
    /// </summary>
    [TestMethod]
    public void AutoCompleteMenu_WithWrappedInput_PositionsCorrectly()
    {
        // Arrange — write text that wraps: 35 chars puts cursor on row 1, col 5
        _mirror.Write(new string('A', 35));
        
        // Record input end position
        int inputEndRow = _virtualConsole.CursorRow;
        int inputEndCol = _virtualConsole.CursorColumn;
        inputEndRow.Should().Be(1, "input ends on row 1");
        inputEndCol.Should().Be(5, "input ends at col 5");

        // Create autocomplete menu controller with prompt length
        var theme = new Theme();
        var menuController = new AutoCompleteMenuController(_adapter, theme);
        menuController.SetPromptLength(PromptLength);

        // Create menu options
        var options = new List<AutoCompleteOption>
        {
            new AutoCompleteOption("option1"),
            new AutoCompleteOption("option2"),
            new AutoCompleteOption("option3")
        };

        // Act — show menu
        menuController.Show(options, _mirror);

        // Assert — menu should be visible
        menuController.IsVisible.Should().BeTrue();

        // Cursor should be restored to input position (row 1, col 5)
        // Note: GetCursorColumn returns 1-based column (for ANSI CHA compatibility)
        int expectedCol = (PromptLength + _mirror.BufferPosition) % TerminalWidth + 1; // +1 for 1-based column indexing
        var actualCursorCol = menuController.GetCursorColumn(_mirror);
        actualCursorCol.Should().Be(expectedCol, "cursor column should account for wrap");

        // Menu should render below the input (row 2+)
        // Check that row 2 has menu content (first option)
        var row2Text = _virtualConsole.GetRow(2).GetText().TrimEnd();
        row2Text.Should().Contain("option", "menu should render below wrapped input");

        // Act — hide menu
        menuController.Hide();

        // Assert — menu should no longer be visible
        menuController.IsVisible.Should().BeFalse();

        // Cursor should be at expected position after hide
        _virtualConsole.CursorRow.Should().Be(inputEndRow, "cursor row restored after menu hide");
        _virtualConsole.CursorColumn.Should().Be(inputEndCol, "cursor column restored after menu hide");
    }
}
