using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Tests.VirtualConsole;

namespace BitPantry.CommandLine.Tests.VirtualConsole;

/// <summary>
/// Tests for CursorTracker that parses ANSI escape sequences to track cursor position.
/// This is extracted from VirtualAnsiConsole to be reusable with ConsolidatedTestConsole.
/// </summary>
[TestClass]
public class CursorTrackerTests
{
    [TestMethod]
    public void Initial_Position_Should_Be_Zero_Zero()
    {
        var tracker = new CursorTracker();

        tracker.Column.Should().Be(0);
        tracker.Line.Should().Be(0);
    }

    [TestMethod]
    public void Write_Plain_Text_Should_Advance_Column()
    {
        var tracker = new CursorTracker();

        tracker.ProcessText("hello");

        tracker.Column.Should().Be(5);
        tracker.Line.Should().Be(0);
    }

    [TestMethod]
    public void CarriageReturn_Should_Reset_Column_To_Zero()
    {
        var tracker = new CursorTracker();
        tracker.ProcessText("hello");

        tracker.ProcessText("\r");

        tracker.Column.Should().Be(0);
        tracker.Line.Should().Be(0);
    }

    [TestMethod]
    public void Newline_Should_Increment_Line()
    {
        var tracker = new CursorTracker();
        tracker.ProcessText("hello");

        tracker.ProcessText("\n");

        tracker.Column.Should().Be(5);  // Column doesn't change on \n alone
        tracker.Line.Should().Be(1);
    }

    [TestMethod]
    public void CursorUp_CUU_Should_Move_Cursor_Up()
    {
        var tracker = new CursorTracker();
        tracker.ProcessText("line1\nline2\nline3");
        tracker.Line.Should().Be(2);

        // ESC[2A moves cursor up 2 lines
        tracker.ProcessText("\u001b[2A");

        tracker.Line.Should().Be(0);
    }

    [TestMethod]
    public void CursorDown_CUD_Should_Move_Cursor_Down()
    {
        var tracker = new CursorTracker();

        // ESC[3B moves cursor down 3 lines
        tracker.ProcessText("\u001b[3B");

        tracker.Line.Should().Be(3);
    }

    [TestMethod]
    public void CursorForward_CUF_Should_Move_Cursor_Right()
    {
        var tracker = new CursorTracker();

        // ESC[5C moves cursor forward 5 columns
        tracker.ProcessText("\u001b[5C");

        tracker.Column.Should().Be(5);
    }

    [TestMethod]
    public void CursorBack_CUB_Should_Move_Cursor_Left()
    {
        var tracker = new CursorTracker();
        tracker.ProcessText("hello world");
        tracker.Column.Should().Be(11);

        // ESC[4D moves cursor back 4 columns
        tracker.ProcessText("\u001b[4D");

        tracker.Column.Should().Be(7);
    }

    [TestMethod]
    public void CursorUp_Without_Number_Should_Default_To_One()
    {
        var tracker = new CursorTracker();
        tracker.ProcessText("line1\nline2");
        tracker.Line.Should().Be(1);

        // ESC[A without number defaults to 1
        tracker.ProcessText("\u001b[A");

        tracker.Line.Should().Be(0);
    }

    [TestMethod]
    public void CursorUp_Should_Not_Go_Below_Zero()
    {
        var tracker = new CursorTracker();

        // Try to move up when already at top
        tracker.ProcessText("\u001b[5A");

        tracker.Line.Should().Be(0);
    }

    [TestMethod]
    public void CursorBack_Should_Not_Go_Below_Zero()
    {
        var tracker = new CursorTracker();

        // Try to move left when already at column 0
        tracker.ProcessText("\u001b[5D");

        tracker.Column.Should().Be(0);
    }

    [TestMethod]
    public void Mixed_Text_And_Escape_Sequences_Should_Track_Correctly()
    {
        var tracker = new CursorTracker();

        // Write some text, move up, write more
        tracker.ProcessText("hello\n");
        tracker.Line.Should().Be(1);
        tracker.Column.Should().Be(5);  // \n doesn't reset column

        tracker.ProcessText("world");
        // After "world" column should be at 10 (5 from previous + 5 from "world")
        tracker.Column.Should().Be(10);

        tracker.ProcessText("\r");  // CR resets column
        tracker.Column.Should().Be(0);

        tracker.ProcessText("\u001b[1A");  // Move up one line
        tracker.Line.Should().Be(0);
    }

    [TestMethod]
    public void ClearLine_Should_Not_Affect_Position()
    {
        var tracker = new CursorTracker();
        tracker.ProcessText("hello");

        // ESC[2K clears line but doesn't move cursor
        tracker.ProcessText("\u001b[2K");

        tracker.Column.Should().Be(5);
        tracker.Line.Should().Be(0);
    }

    [TestMethod]
    public void Position_Property_Should_Return_Column_And_Line_Tuple()
    {
        var tracker = new CursorTracker();
        tracker.ProcessText("hello\nworld");

        // After "hello\n" column is 5 (no reset), then "world" adds 5 more = 10
        tracker.Position.Should().Be((10, 1));
    }
}
