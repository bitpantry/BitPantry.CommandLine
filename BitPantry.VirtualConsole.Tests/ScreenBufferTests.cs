using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class ScreenBufferTests
{
    // T016: Tests for ScreenBuffer initialization
    [TestMethod]
    public void Constructor_ShouldInitializeWithSpecifiedDimensions()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.Width.Should().Be(80);
        buffer.Height.Should().Be(25);
    }

    [TestMethod]
    public void Constructor_ShouldInitializeCursorAtOrigin()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.CursorRow.Should().Be(0);
        buffer.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void Constructor_ShouldInitializeAllCellsAsEmpty()
    {
        var buffer = new ScreenBuffer(10, 5);
        
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 10; col++)
            {
                var cell = buffer.GetCell(row, col);
                cell.Character.Should().Be(' ');
                cell.Style.Should().Be(CellStyle.Default);
            }
        }
    }

    [TestMethod]
    public void Constructor_ShouldInitializeWithDefaultStyle()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.CurrentStyle.Should().Be(CellStyle.Default);
    }

    // T017: Tests for WriteChar at cursor position
    [TestMethod]
    public void WriteChar_ShouldWriteAtCursorPosition()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.WriteChar('A');
        
        buffer.GetCell(0, 0).Character.Should().Be('A');
    }

    [TestMethod]
    public void WriteChar_ShouldAdvanceCursor()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.WriteChar('A');
        
        buffer.CursorColumn.Should().Be(1);
    }

    [TestMethod]
    public void WriteChar_ShouldApplyCurrentStyle()
    {
        var buffer = new ScreenBuffer(80, 25);
        buffer.ApplyStyle(CellStyle.Default.WithForeground(ConsoleColor.Blue));
        
        buffer.WriteChar('X');
        
        buffer.GetCell(0, 0).Style.ForegroundColor.Should().Be(ConsoleColor.Blue);
    }

    [TestMethod]
    public void WriteChar_MultipleChars_ShouldWriteSequentially()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.WriteChar('H');
        buffer.WriteChar('i');
        
        buffer.GetCell(0, 0).Character.Should().Be('H');
        buffer.GetCell(0, 1).Character.Should().Be('i');
        buffer.CursorColumn.Should().Be(2);
    }

    // T018: Tests for cursor movement
    [TestMethod]
    public void MoveCursor_ShouldSetPosition()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.MoveCursor(5, 10);
        
        buffer.CursorRow.Should().Be(5);
        buffer.CursorColumn.Should().Be(10);
    }

    [TestMethod]
    public void MoveCursorRelative_ShouldMoveFromCurrentPosition()
    {
        var buffer = new ScreenBuffer(80, 25);
        buffer.MoveCursor(5, 10);
        
        buffer.MoveCursorRelative(2, 3);
        
        buffer.CursorRow.Should().Be(7);
        buffer.CursorColumn.Should().Be(13);
    }

    [TestMethod]
    public void MoveCursorRelative_ShouldAllowNegativeMovement()
    {
        var buffer = new ScreenBuffer(80, 25);
        buffer.MoveCursor(5, 10);
        
        buffer.MoveCursorRelative(-2, -5);
        
        buffer.CursorRow.Should().Be(3);
        buffer.CursorColumn.Should().Be(5);
    }

    // T019: Tests for GetCell and GetRow
    [TestMethod]
    public void GetCell_ShouldReturnCellAtPosition()
    {
        var buffer = new ScreenBuffer(80, 25);
        buffer.MoveCursor(3, 5);
        buffer.WriteChar('Z');
        
        var cell = buffer.GetCell(3, 5);
        
        cell.Character.Should().Be('Z');
    }

    [TestMethod]
    public void GetRow_ShouldReturnRowWrapper()
    {
        var buffer = new ScreenBuffer(80, 25);
        buffer.WriteChar('A');
        buffer.WriteChar('B');
        buffer.WriteChar('C');
        
        var row = buffer.GetRow(0);
        
        row.Should().NotBeNull();
        row.RowIndex.Should().Be(0);
    }

    [TestMethod]
    public void GetRow_Text_ShouldContainWrittenCharacters()
    {
        var buffer = new ScreenBuffer(80, 25);
        buffer.WriteChar('H');
        buffer.WriteChar('e');
        buffer.WriteChar('l');
        buffer.WriteChar('l');
        buffer.WriteChar('o');
        
        var row = buffer.GetRow(0);
        
        row.GetText().TrimEnd().Should().Be("Hello");
    }

    // T020: Tests for cursor boundary clamping
    [TestMethod]
    public void MoveCursor_ShouldClampNegativeRow()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.MoveCursor(-5, 10);
        
        buffer.CursorRow.Should().Be(0);
    }

    [TestMethod]
    public void MoveCursor_ShouldClampNegativeColumn()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.MoveCursor(5, -10);
        
        buffer.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void MoveCursor_ShouldClampRowBeyondHeight()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.MoveCursor(100, 10);
        
        buffer.CursorRow.Should().Be(24); // Height - 1
    }

    [TestMethod]
    public void MoveCursor_ShouldClampColumnBeyondWidth()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        buffer.MoveCursor(5, 200);
        
        buffer.CursorColumn.Should().Be(79); // Width - 1
    }

    [TestMethod]
    public void MoveCursorRelative_ShouldClampAtBoundaries()
    {
        var buffer = new ScreenBuffer(80, 25);
        buffer.MoveCursor(0, 0);
        
        buffer.MoveCursorRelative(-10, -10);
        
        buffer.CursorRow.Should().Be(0);
        buffer.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void GetCell_OutOfBounds_ShouldReturnDefaultCell()
    {
        var buffer = new ScreenBuffer(80, 25);
        
        var cell = buffer.GetCell(100, 200);
        
        cell.Character.Should().Be(' ');
        cell.Style.Should().Be(CellStyle.Default);
    }

    // T071: Line wrapping at width boundary
    [TestMethod]
    public void WriteChar_AtEndOfLine_ShouldWrapToNextLine()
    {
        var buffer = new ScreenBuffer(10, 5);
        
        // Write exactly 10 characters to fill the first line
        for (int i = 0; i < 10; i++)
        {
            buffer.WriteChar((char)('A' + i));
        }
        
        // Cursor should wrap to next line
        buffer.CursorRow.Should().Be(1);
        buffer.CursorColumn.Should().Be(0);
        
        // First line should be full
        buffer.GetRow(0).GetText().Should().Be("ABCDEFGHIJ");
    }

    [TestMethod]
    public void WriteChar_PastEndOfLine_ShouldContinueOnNextLine()
    {
        var buffer = new ScreenBuffer(10, 5);
        
        // Write 15 characters
        for (int i = 0; i < 15; i++)
        {
            buffer.WriteChar('X');
        }
        
        // Should be on second line, column 5
        buffer.CursorRow.Should().Be(1);
        buffer.CursorColumn.Should().Be(5);
        
        // First line full
        buffer.GetRow(0).GetText().Should().Be("XXXXXXXXXX");
        // Second line has 5 X's
        buffer.GetRow(1).GetText().Should().StartWith("XXXXX");
    }

    [TestMethod]
    public void WriteChar_MultipleLineWraps_ShouldTrackCorrectly()
    {
        var buffer = new ScreenBuffer(5, 10);
        
        // Write 23 characters (fills 4 full lines + 3 chars)
        for (int i = 0; i < 23; i++)
        {
            buffer.WriteChar((char)('0' + (i % 10)));
        }
        
        buffer.CursorRow.Should().Be(4);
        buffer.CursorColumn.Should().Be(3);
        
        buffer.GetRow(0).GetText().Should().Be("01234");
        buffer.GetRow(1).GetText().Should().Be("56789");
        buffer.GetRow(2).GetText().Should().Be("01234");
        buffer.GetRow(3).GetText().Should().Be("56789");
        buffer.GetRow(4).GetText().Should().StartWith("012");
    }

    // T072: Content clipping at height boundary
    [TestMethod]
    public void WriteChar_AtBottomRightCorner_ShouldNotCrash()
    {
        var buffer = new ScreenBuffer(10, 5);
        
        // Move to last position
        buffer.MoveCursor(4, 9);
        
        Action action = () => buffer.WriteChar('X');
        
        action.Should().NotThrow();
        buffer.GetCell(4, 9).Character.Should().Be('X');
    }

    [TestMethod]
    public void WriteChar_PastBottomRow_ShouldClipToLastRow()
    {
        var buffer = new ScreenBuffer(10, 5);
        
        // Fill entire screen
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 10; col++)
            {
                buffer.WriteChar((char)('A' + row));
            }
        }
        
        // Try to write one more character - should stay clamped
        buffer.WriteChar('Z');
        
        // Cursor should be clamped to valid position
        buffer.CursorRow.Should().BeLessThan(5);
    }

    [TestMethod]
    public void MoveCursor_BeyondHeight_ShouldClampToValidRow()
    {
        var buffer = new ScreenBuffer(10, 5);
        
        buffer.MoveCursor(100, 0);
        
        buffer.CursorRow.Should().BeLessThan(5);
    }

    [TestMethod]
    public void MoveCursor_BeyondWidth_ShouldClampToValidColumn()
    {
        var buffer = new ScreenBuffer(10, 5);
        
        buffer.MoveCursor(0, 100);
        
        buffer.CursorColumn.Should().BeLessThan(10);
    }
}
