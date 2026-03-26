using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Tests for cursor hide/show behavior in ConsoleLineMirror methods.
/// Verifies that methods that perform multi-step write + cursor-move operations
/// hide the cursor for the duration to prevent visual flicker.
/// </summary>
[TestClass]
public class ConsoleLineMirrorCursorTests
{
    // ANSI escape sequences for cursor visibility
    private const string CursorHide = "\x1B[?25l";  // CSI ? 25 l - Hide cursor
    private const string CursorShow = "\x1B[?25h";  // CSI ? 25 h - Show cursor

    private VirtualConsole.VirtualConsole _virtualConsole = null!;
    private VirtualConsoleAnsiAdapter _adapter = null!;
    private ConsoleLineMirror _mirror = null!;

    [TestInitialize]
    public void Setup()
    {
        _virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        _virtualConsole.StrictMode = true;
        _adapter = new VirtualConsoleAnsiAdapter(_virtualConsole);
        _adapter.WriteLogEnabled = true; // Enable logging to capture ANSI sequences
        _mirror = new ConsoleLineMirror(_adapter);
    }

    /// <summary>
    /// Asserts that cursor hide sequence appears before show sequence,
    /// and that there is content written between them.
    /// </summary>
    private void AssertCursorHideShowBracketsContent(string context)
    {
        var log = _adapter.WriteLog.Contents;
        var hideIndex = log.IndexOf(CursorHide);
        var showIndex = log.IndexOf(CursorShow);

        hideIndex.Should().BeGreaterOrEqualTo(0, $"cursor hide sequence should be present for {context}");
        showIndex.Should().BeGreaterOrEqualTo(0, $"cursor show sequence should be present for {context}");
        hideIndex.Should().BeLessThan(showIndex, $"cursor hide should come before show for {context}");

        // Verify there's content between hide and show
        var contentBetween = log.Substring(hideIndex + CursorHide.Length, showIndex - hideIndex - CursorHide.Length);
        contentBetween.Should().NotBeEmpty($"there should be content written between hide and show for {context}");
    }

    /*
     * Test Validity Check:
     *   Invokes code under test: YES - calls RenderWithStyles method
     *   Breakage detection: YES - if Hide/Show calls are removed, test fails
     *   Not a tautology: YES - verifies ordering of escape sequences in output
     */
    [TestMethod]
    public void RenderWithStyles_HidesCursorDuringRendering()
    {
        // Arrange - Create some initial content that will be cleared
        _mirror.Write("initial content");
        _adapter.WriteLog.Clear(); // Clear log so we only capture RenderWithStyles output

        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),
            new StyledSegment(" ", 6, 7, SyntaxColorScheme.Default),
            new StyledSegment("start", 7, 12, SyntaxColorScheme.Command)
        };

        // Act
        _mirror.RenderWithStyles(segments, 5); // cursor mid-line

        // Assert
        AssertCursorHideShowBracketsContent("RenderWithStyles");
    }

    /*
     * Test Validity Check:
     *   Invokes code under test: YES - calls Backspace method
     *   Breakage detection: YES - if Hide/Show calls are removed, test fails
     *   Not a tautology: YES - verifies ordering of escape sequences in output
     */
    [TestMethod]
    public void Backspace_HidesCursorDuringRewrite()
    {
        // Arrange - Write some content first
        _mirror.Write("hello world");
        _adapter.WriteLog.Clear(); // Clear log so we only capture Backspace output

        // Act - Backspace should hide cursor while rewriting remainder
        _mirror.Backspace();

        // Assert
        AssertCursorHideShowBracketsContent("Backspace");
    }

    /*
     * Test Validity Check:
     *   Invokes code under test: YES - calls Delete method
     *   Breakage detection: YES - if Hide/Show calls are removed, test fails
     *   Not a tautology: YES - verifies ordering of escape sequences in output
     */
    [TestMethod]
    public void Delete_HidesCursorDuringRewrite()
    {
        // Arrange - Write content and position cursor mid-line
        _mirror.Write("hello world");
        _mirror.MoveToPosition(5);
        _adapter.WriteLog.Clear(); // Clear log so we only capture Delete output

        // Act - Delete should hide cursor while rewriting remainder
        _mirror.Delete();

        // Assert
        AssertCursorHideShowBracketsContent("Delete");
    }

    /*
     * Test Validity Check:
     *   Invokes code under test: YES - calls Write method in insert mode
     *   Breakage detection: YES - if Hide/Show calls are removed, test fails
     *   Not a tautology: YES - verifies ordering of escape sequences in output
     */
    [TestMethod]
    public void Write_InsertMode_HidesCursorDuringRewrite()
    {
        // Arrange - Write content and position cursor mid-line
        _mirror.Write("hello world");
        _mirror.MoveToPosition(5);
        _adapter.WriteLog.Clear(); // Clear log so we only capture insert Write output

        // Act - Writing in insert mode (not at end) should hide cursor while rewriting
        _mirror.Write("X");

        // Assert
        AssertCursorHideShowBracketsContent("Write (insert mode)");
    }

    /*
     * Test Validity Check:
     *   Invokes code under test: YES - calls Markup method in insert mode
     *   Breakage detection: YES - if Hide/Show calls are removed, test fails
     *   Not a tautology: YES - verifies ordering of escape sequences in output
     */
    [TestMethod]
    public void Markup_InsertMode_HidesCursorDuringRewrite()
    {
        // Arrange - Write content and position cursor mid-line
        _mirror.Write("hello world");
        _mirror.MoveToPosition(5);
        _adapter.WriteLog.Clear(); // Clear log so we only capture insert Markup output

        // Act - Markup in insert mode (not at end) should hide cursor while rewriting
        _mirror.Markup("[green]X[/]");

        // Assert
        AssertCursorHideShowBracketsContent("Markup (insert mode)");
    }

    /*
     * Test Validity Check:
     *   Invokes code under test: YES - calls Clear method
     *   Breakage detection: YES - if Hide/Show calls are removed, test fails
     *   Not a tautology: YES - verifies ordering of escape sequences in output
     */
    [TestMethod]
    public void Clear_HidesCursorDuringRewrite()
    {
        // Arrange - Write content first
        _mirror.Write("hello world");
        _adapter.WriteLog.Clear(); // Clear log so we only capture Clear output

        // Act - Clear should hide cursor while writing spaces and repositioning
        _mirror.Clear();

        // Assert
        AssertCursorHideShowBracketsContent("Clear");
    }

    // Edge case: Backspace at position 0 should be a no-op (no cursor hide/show needed)
    [TestMethod]
    public void Backspace_AtPositionZero_DoesNotEmitCursorSequences()
    {
        // Arrange - Position is already 0
        _adapter.WriteLog.Clear();

        // Act
        _mirror.Backspace();

        // Assert - No cursor hide/show should be emitted for no-op
        var log = _adapter.WriteLog.Contents;
        log.Should().NotContain(CursorHide, "no cursor hide should be emitted for no-op Backspace");
        log.Should().NotContain(CursorShow, "no cursor show should be emitted for no-op Backspace");
    }

    // Edge case: Delete at end of buffer should be a no-op (no cursor hide/show needed)
    [TestMethod]
    public void Delete_AtEndOfBuffer_DoesNotEmitCursorSequences()
    {
        // Arrange - Write content, cursor is at end
        _mirror.Write("hello");
        _adapter.WriteLog.Clear();

        // Act
        _mirror.Delete();

        // Assert - No cursor hide/show should be emitted for no-op
        var log = _adapter.WriteLog.Contents;
        log.Should().NotContain(CursorHide, "no cursor hide should be emitted for no-op Delete");
        log.Should().NotContain(CursorShow, "no cursor show should be emitted for no-op Delete");
    }

    // Edge case: Write at end of buffer (append mode) should not emit cursor sequences
    // since there's no "after" content to rewrite
    [TestMethod]
    public void Write_AppendMode_DoesNotEmitCursorSequences()
    {
        // Arrange - Position is at end (append mode)
        _mirror.Write("hello");
        _adapter.WriteLog.Clear();

        // Act - Writing at end is just appending, no rewrite needed
        _mirror.Write("X");

        // Assert - No cursor hide/show should be emitted for append
        var log = _adapter.WriteLog.Contents;
        log.Should().NotContain(CursorHide, "no cursor hide should be emitted for append Write");
        log.Should().NotContain(CursorShow, "no cursor show should be emitted for append Write");
    }

    // Edge case: Clear with empty buffer should be a no-op (no cursor hide/show needed)
    [TestMethod]
    public void Clear_EmptyBuffer_DoesNotEmitCursorSequences()
    {
        // Arrange - Buffer is empty
        _adapter.WriteLog.Clear();

        // Act
        _mirror.Clear();

        // Assert - No cursor hide/show should be emitted for empty buffer
        var log = _adapter.WriteLog.Contents;
        log.Should().NotContain(CursorHide, "no cursor hide should be emitted for empty Clear");
        log.Should().NotContain(CursorShow, "no cursor show should be emitted for empty Clear");
    }
}
