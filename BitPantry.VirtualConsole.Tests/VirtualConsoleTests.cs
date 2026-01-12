using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class VirtualConsoleTests
{
    // T022: Tests for VirtualConsole.Write basic text (no ANSI)
    [TestMethod]
    public void Constructor_ShouldCreateWithDimensions()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Width.Should().Be(80);
        console.Height.Should().Be(25);
    }

    [TestMethod]
    public void Write_PlainText_ShouldWriteToBuffer()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("Hello");
        
        console.GetCell(0, 0).Character.Should().Be('H');
        console.GetCell(0, 1).Character.Should().Be('e');
        console.GetCell(0, 2).Character.Should().Be('l');
        console.GetCell(0, 3).Character.Should().Be('l');
        console.GetCell(0, 4).Character.Should().Be('o');
    }

    [TestMethod]
    public void Write_PlainText_ShouldAdvanceCursor()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("Test");
        
        console.CursorColumn.Should().Be(4);
    }

    [TestMethod]
    public void Write_MultipleWrites_ShouldContinueFromCursor()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("Hello");
        console.Write(" World");
        
        console.GetScreenText().TrimEnd().Should().Be("Hello World");
    }

    // T023: Tests for overwrite scenarios
    [TestMethod]
    public void Write_AfterMovingCursorBack_ShouldOverwrite()
    {
        var console = new VirtualConsole(80, 25);
        var buffer = console.GetScreenBuffer();
        
        console.Write("Hello");
        buffer.MoveCursorRelative(0, -5); // Move back 5 columns
        console.Write("XYZ");
        
        // Should show "XYZlo" - first 3 chars overwritten
        console.GetRow(0).GetText().TrimEnd().Should().Be("XYZlo");
    }

    [TestMethod]
    public void Write_Overwrite_ShouldNotAppend()
    {
        var console = new VirtualConsole(80, 25);
        var buffer = console.GetScreenBuffer();
        
        console.Write("AAAAA");
        buffer.MoveCursor(0, 0); // Move to start
        console.Write("BB");
        
        // Should be "BBAAA" not "BBAAAAA"
        console.GetRow(0).GetText().TrimEnd().Should().Be("BBAAA");
    }

    // T024: Tests for GetScreenText and GetScreenContent
    [TestMethod]
    public void GetScreenText_ShouldReturnAllTextWithoutFormatting()
    {
        var console = new VirtualConsole(10, 2);
        console.Write("Line1");
        console.GetScreenBuffer().MoveCursor(1, 0);
        console.Write("Line2");
        
        var text = console.GetScreenText();
        
        text.Should().Contain("Line1");
        text.Should().Contain("Line2");
    }

    [TestMethod]
    public void GetScreenContent_ShouldReturnTextWithLineBreaks()
    {
        var console = new VirtualConsole(10, 2);
        console.Write("Line1");
        console.GetScreenBuffer().MoveCursor(1, 0);
        console.Write("Line2");
        
        var content = console.GetScreenContent();
        
        content.Should().Contain(Environment.NewLine);
    }

    [TestMethod]
    public void GetScreenContent_EmptyScreen_ShouldReturnSpaces()
    {
        var console = new VirtualConsole(5, 2);
        
        var content = console.GetScreenContent();
        
        content.Should().NotBeNullOrEmpty();
    }

    // T025: Tests for Clear method
    [TestMethod]
    public void Clear_ShouldResetAllCellsToDefault()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("Hello World");
        
        console.Clear();
        
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 10; col++)
            {
                console.GetCell(row, col).Character.Should().Be(' ');
            }
        }
    }

    [TestMethod]
    public void Clear_ShouldResetCursorToHome()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Text that moves cursor");
        
        console.Clear();
        
        console.CursorRow.Should().Be(0);
        console.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void Clear_ShouldPreserveCurrentStyle()
    {
        var console = new VirtualConsole(80, 25);
        console.GetScreenBuffer().ApplyStyle(CellStyle.Default.WithForeground(ConsoleColor.Blue));
        
        console.Clear();
        
        // After clear, write something to verify style is preserved
        console.Write("X");
        console.GetCell(0, 0).Style.ForegroundColor.Should().Be(ConsoleColor.Blue);
    }

    // T036: Tests for carriage return (\r) processing
    [TestMethod]
    public void Write_CarriageReturn_ShouldMoveCursorToStartOfLine()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Hello");
        
        console.Write("\r");
        
        console.CursorColumn.Should().Be(0);
        console.CursorRow.Should().Be(0);
    }

    [TestMethod]
    public void Write_CarriageReturn_ShouldAllowOverwrite()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Hello");
        console.Write("\rWorld");
        
        console.GetRow(0).GetText().TrimEnd().Should().Be("World");
    }

    // T037: Tests for newline (\n) processing
    [TestMethod]
    public void Write_LineFeed_ShouldMoveCursorDown()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Hello\n");
        
        console.CursorRow.Should().Be(1);
    }

    [TestMethod]
    public void Write_LineFeed_ShouldNotResetColumn()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Hello\n");
        
        // After LF, column should be at position after Hello (5)
        // But in Unix style, LF doesn't change column
        console.CursorColumn.Should().Be(5);
    }

    [TestMethod]
    public void Write_CrLf_ShouldMoveToNextLineStart()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Hello\r\n");
        
        console.CursorRow.Should().Be(1);
        console.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void Write_MultipleLines_ShouldPositionCorrectly()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Line1\r\nLine2\r\nLine3");
        
        console.GetRow(0).GetText().TrimEnd().Should().Be("Line1");
        console.GetRow(1).GetText().TrimEnd().Should().Be("Line2");
        console.GetRow(2).GetText().TrimEnd().Should().Be("Line3");
    }

    // T038: Tests for VirtualConsole.Write with embedded cursor sequences
    [TestMethod]
    public void Write_CursorUp_ShouldMoveCursor()
    {
        var console = new VirtualConsole(80, 25);
        console.GetScreenBuffer().MoveCursor(5, 0);
        
        console.Write("\x1b[2A"); // Cursor up 2
        
        console.CursorRow.Should().Be(3);
    }

    [TestMethod]
    public void Write_CursorDown_ShouldMoveCursor()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("Hello\x1b[3B"); // Cursor down 3
        
        console.CursorRow.Should().Be(3);
    }

    [TestMethod]
    public void Write_CursorForward_ShouldMoveCursor()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("\x1b[10C"); // Cursor forward 10
        
        console.CursorColumn.Should().Be(10);
    }

    [TestMethod]
    public void Write_CursorBack_ShouldMoveCursor()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Hello");
        
        console.Write("\x1b[3D"); // Cursor back 3
        
        console.CursorColumn.Should().Be(2);
    }

    [TestMethod]
    public void Write_CursorBack_ShouldEnableOverwrite()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Hello");
        console.Write("\x1b[5D"); // Cursor back 5
        console.Write("XYZ");
        
        console.GetRow(0).GetText().TrimEnd().Should().Be("XYZlo");
    }

    [TestMethod]
    public void Write_CursorPosition_ShouldSetAbsolutePosition()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("\x1b[5;10H"); // Row 5, Column 10 (1-based)
        
        console.CursorRow.Should().Be(4);   // 0-based
        console.CursorColumn.Should().Be(9);
    }

    [TestMethod]
    public void Write_CursorHome_ShouldMoveToOrigin()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Hello World");
        
        console.Write("\x1b[H"); // Cursor home
        
        console.CursorRow.Should().Be(0);
        console.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void Write_MixedTextAndSequences_ShouldProcessCorrectly()
    {
        var console = new VirtualConsole(80, 25);
        
        // Write "Hello", move back 5, write "World"
        console.Write("Hello\x1b[5DWorld");
        
        console.GetRow(0).GetText().TrimEnd().Should().Be("World");
    }

    // T051: Tests for VirtualConsole.Write with color sequences
    [TestMethod]
    public void Write_ColoredText_ShouldApplyForegroundColor()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("\x1b[34mBlue");  // Blue text
        
        console.GetCell(0, 0).Style.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
        console.GetCell(0, 1).Style.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
    }

    [TestMethod]
    public void Write_ColorThenReset_ShouldClearColor()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("\x1b[34mBlue\x1b[0mNormal");
        
        console.GetCell(0, 0).Style.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
        console.GetCell(0, 4).Style.ForegroundColor.Should().BeNull();  // After reset
    }

    [TestMethod]
    public void Write_BackgroundColor_ShouldApply()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("\x1b[41mRed BG\x1b[0m");
        
        console.GetCell(0, 0).Style.BackgroundColor.Should().Be(ConsoleColor.DarkRed);
    }

    [TestMethod]
    public void Write_BoldText_ShouldApplyAttribute()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("\x1b[1mBold\x1b[0m");
        
        console.GetCell(0, 0).Style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
    }

    [TestMethod]
    public void Write_ReverseText_ShouldApplyAttribute()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("\x1b[7mReversed\x1b[0m");
        
        console.GetCell(0, 0).Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeTrue();
    }

    [TestMethod]
    public void Write_CombinedStyles_ShouldApplyAll()
    {
        var console = new VirtualConsole(80, 25);
        
        // Bold + Blue + Red BG
        console.Write("\x1b[1;34;41mStyled\x1b[0m");
        
        var style = console.GetCell(0, 0).Style;
        style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        style.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
        style.BackgroundColor.Should().Be(ConsoleColor.DarkRed);
    }

    // T052: Tests for querying styled cells
    [TestMethod]
    public void GetCell_StyledCell_ShouldReturnCompleteStyle()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[1;4;34mTest\x1b[0m");  // Bold + Underline + Blue
        
        var cell = console.GetCell(0, 0);
        
        cell.Character.Should().Be('T');
        cell.Style.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
        cell.Style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        cell.Style.Attributes.HasFlag(CellAttributes.Underline).Should().BeTrue();
    }

    [TestMethod]
    public void GetCell_UnstyledCell_ShouldReturnDefaultStyle()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Plain");
        
        var cell = console.GetCell(0, 0);
        
        cell.Style.Should().Be(CellStyle.Default);
    }

    [TestMethod]
    public void Write_StylePersistsAfterCursorMove()
    {
        var console = new VirtualConsole(80, 25);
        
        // Set blue, write, move to next line, write more
        console.Write("\x1b[34m");
        console.Write("Blue\r\nStillBlue");
        
        console.GetCell(0, 0).Style.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
        console.GetCell(1, 0).Style.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
    }

    [TestMethod]
    public void Write_OverwriteWithDifferentStyle()
    {
        var console = new VirtualConsole(80, 25);
        
        // Write blue text
        console.Write("\x1b[34mHello\x1b[0m");
        // Move back and overwrite with red
        console.Write("\x1b[5D\x1b[31mWorld\x1b[0m");
        
        console.GetCell(0, 0).Character.Should().Be('W');
        console.GetCell(0, 0).Style.ForegroundColor.Should().Be(ConsoleColor.DarkRed);
    }

    [TestMethod]
    public void Write_BrightColors_ShouldWork()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Write("\x1b[94mBrightBlue\x1b[0m");  // Bright blue (94)
        
        console.GetCell(0, 0).Style.ForegroundColor.Should().Be(ConsoleColor.Blue);
    }

    // T060: Empty region queries
    [TestMethod]
    public void GetCell_EmptyRegion_ShouldReturnSpaceWithDefaultStyle()
    {
        var console = new VirtualConsole(80, 25);
        
        // Write text at row 0, but query an untouched cell
        console.Write("Hello");
        
        var emptyCell = console.GetCell(0, 10);
        
        emptyCell.Character.Should().Be(' ');
        emptyCell.Style.ForegroundColor.Should().BeNull();
        emptyCell.Style.BackgroundColor.Should().BeNull();
        emptyCell.Style.Attributes.Should().Be(CellAttributes.None);
    }

    [TestMethod]
    public void GetCell_UntouchedRow_ShouldReturnDefaultCells()
    {
        var console = new VirtualConsole(80, 25);
        
        // Write only to row 0
        console.Write("First row");
        
        // Query row 5 which was never touched
        var row = console.GetRow(5);
        var cells = row.GetCells().ToList();
        
        cells.Should().AllSatisfy(cell =>
        {
            cell.Character.Should().Be(' ');
            cell.Style.ForegroundColor.Should().BeNull();
            cell.Style.BackgroundColor.Should().BeNull();
        });
    }

    [TestMethod]
    public void GetRow_EmptyScreen_ShouldReturnSpacesWithDefaultStyle()
    {
        var console = new VirtualConsole(10, 5);
        
        var row = console.GetRow(0);
        
        row.GetText().Should().Be("          "); // 10 spaces
        row.GetCells().Should().AllSatisfy(cell =>
        {
            cell.Character.Should().Be(' ');
            cell.Style.Should().Be(CellStyle.Default);
        });
    }

    // T061: GetCell with complete style information
    [TestMethod]
    public void GetCell_StyledCell_ShouldReturnAllStyleProperties()
    {
        var console = new VirtualConsole(80, 25);
        
        // Apply comprehensive styling: bold, red on bright white (107)
        console.Write("\x1b[1;31;107mX\x1b[0m");
        
        var cell = console.GetCell(0, 0);
        
        cell.Character.Should().Be('X');
        cell.Style.ForegroundColor.Should().Be(ConsoleColor.DarkRed);
        cell.Style.BackgroundColor.Should().Be(ConsoleColor.White);
        cell.Style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
    }

    [TestMethod]
    public void GetCell_256ColorCell_ShouldReturnExtendedColor()
    {
        var console = new VirtualConsole(80, 25);
        
        // 38;5;196 = extended color 196 (red)
        console.Write("\x1b[38;5;196mX\x1b[0m");
        
        var cell = console.GetCell(0, 0);
        
        cell.Character.Should().Be('X');
        cell.Style.Foreground256.Should().Be(196);
    }

    [TestMethod]
    public void GetCell_TrueColorCell_ShouldReturnRgbValues()
    {
        var console = new VirtualConsole(80, 25);
        
        // 38;2;100;150;200 = RGB(100, 150, 200)
        console.Write("\x1b[38;2;100;150;200mX\x1b[0m");
        
        var cell = console.GetCell(0, 0);
        
        cell.Character.Should().Be('X');
        var rgb = cell.Style.ForegroundRgb;
        rgb.Should().NotBeNull();
        rgb!.Value.R.Should().Be(100);
        rgb!.Value.G.Should().Be(150);
        rgb!.Value.B.Should().Be(200);
    }

    [TestMethod]
    public void GetCell_MultipleAttributesCell_ShouldReturnCombinedAttributes()
    {
        var console = new VirtualConsole(80, 25);
        
        // Bold (1), Underline (4), Italic (3)
        console.Write("\x1b[1;3;4mX\x1b[0m");
        
        var cell = console.GetCell(0, 0);
        
        cell.Character.Should().Be('X');
        cell.Style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        cell.Style.Attributes.HasFlag(CellAttributes.Underline).Should().BeTrue();
        cell.Style.Attributes.HasFlag(CellAttributes.Italic).Should().BeTrue();
    }

    [TestMethod]
    public void GetRow_MixedStyledText_ShouldPreservePerCellStyling()
    {
        var console = new VirtualConsole(80, 25);
        
        // Write: Red 'A', then Blue 'B', then reset 'C'
        console.Write("\x1b[31mA\x1b[34mB\x1b[0mC");
        
        var row = console.GetRow(0);
        var cells = row.GetCells().ToList();
        
        cells[0].Character.Should().Be('A');
        cells[0].Style.ForegroundColor.Should().Be(ConsoleColor.DarkRed);
        
        cells[1].Character.Should().Be('B');
        cells[1].Style.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
        
        cells[2].Character.Should().Be('C');
        cells[2].Style.ForegroundColor.Should().BeNull();
    }

    // T070: Custom dimensions in constructor
    [TestMethod]
    public void Constructor_CustomDimensions_ShouldCreateCorrectSize()
    {
        var console = new VirtualConsole(40, 10);
        
        console.Width.Should().Be(40);
        console.Height.Should().Be(10);
    }

    [TestMethod]
    public void Constructor_SmallDimensions_ShouldWork()
    {
        var console = new VirtualConsole(1, 1);
        
        console.Width.Should().Be(1);
        console.Height.Should().Be(1);
        console.Write("X");
        console.GetCell(0, 0).Character.Should().Be('X');
    }

    [TestMethod]
    public void Constructor_LargeDimensions_ShouldWork()
    {
        var console = new VirtualConsole(200, 100);
        
        console.Width.Should().Be(200);
        console.Height.Should().Be(100);
    }

    // T073: Dimension validation (must be > 0)
    [TestMethod]
    public void Constructor_ZeroWidth_ShouldThrow()
    {
        Action action = () => new VirtualConsole(0, 25);
        
        action.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("width");
    }

    [TestMethod]
    public void Constructor_ZeroHeight_ShouldThrow()
    {
        Action action = () => new VirtualConsole(80, 0);
        
        action.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("height");
    }

    [TestMethod]
    public void Constructor_NegativeWidth_ShouldThrow()
    {
        Action action = () => new VirtualConsole(-10, 25);
        
        action.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("width");
    }

    [TestMethod]
    public void Constructor_NegativeHeight_ShouldThrow()
    {
        Action action = () => new VirtualConsole(80, -5);
        
        action.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("height");
    }

    // Line wrapping integration test at VirtualConsole level
    [TestMethod]
    public void Write_PastEndOfLine_ShouldWrapToNextLine()
    {
        var console = new VirtualConsole(10, 5);
        
        console.Write("0123456789AB");  // 12 characters, wraps at 10
        
        console.GetRow(0).GetText().Should().Be("0123456789");
        console.GetRow(1).GetText().Should().StartWith("AB");
        console.CursorRow.Should().Be(1);
        console.CursorColumn.Should().Be(2);
    }

    // T079: ED (Erase Display) sequences
    [TestMethod]
    public void Write_ED2_ShouldClearEntireScreen()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("Line 0");
        console.Write("\nLine 1");
        console.Write("\nLine 2");
        
        // ESC [ 2 J = Erase entire display
        console.Write("\x1b[2J");
        
        // All cells should be cleared
        console.GetRow(0).GetText().Should().Be("          ");
        console.GetRow(1).GetText().Should().Be("          ");
        console.GetRow(2).GetText().Should().Be("          ");
    }

    [TestMethod]
    public void Write_ED0_ShouldClearFromCursorToEnd()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("0123456789");  // Row 0
        console.Write("ABCDEFGHIJ");  // Row 1
        console.Write("KLMNOPQRST");  // Row 2
        
        // Move cursor to row 1, column 5
        console.Write("\x1b[2;6H"); // ANSI is 1-based
        
        // ESC [ 0 J (or ESC [ J) = Erase from cursor to end
        console.Write("\x1b[0J");
        
        // Row 0 should be intact
        console.GetRow(0).GetText().Should().Be("0123456789");
        // Row 1 should have first 5 chars, rest cleared
        console.GetRow(1).GetText().Should().StartWith("ABCDE");
        // Row 2 should be cleared
        console.GetRow(2).GetText().Should().Be("          ");
    }

    [TestMethod]
    public void Write_ED1_ShouldClearFromStartToCursor()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("0123456789");  // Row 0
        console.Write("ABCDEFGHIJ");  // Row 1
        console.Write("KLMNOPQRST");  // Row 2
        
        // Move cursor to row 1, column 5
        console.Write("\x1b[2;6H");
        
        // ESC [ 1 J = Erase from start to cursor (inclusive)
        console.Write("\x1b[1J");
        
        // Row 0 should be cleared
        console.GetRow(0).GetText().Should().Be("          ");
        // Row 1: positions 0-5 cleared (cursor was at col 5), positions 6-9 remain "GHIJ"
        console.GetRow(1).GetText().Substring(6).Should().StartWith("GHIJ");
        // Row 2 should be intact
        console.GetRow(2).GetText().Should().Be("KLMNOPQRST");
    }

    // T080: EL (Erase Line) sequences
    [TestMethod]
    public void Write_EL2_ShouldClearEntireLine()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("0123456789");  // Row 0
        console.Write("ABCDEFGHIJ");  // Row 1 (cursor wraps)
        
        // Move cursor to row 0, column 5
        console.Write("\x1b[1;6H");
        
        // ESC [ 2 K = Erase entire line
        console.Write("\x1b[2K");
        
        // Row 0 should be cleared
        console.GetRow(0).GetText().Should().Be("          ");
        // Row 1 should be intact
        console.GetRow(1).GetText().Should().Be("ABCDEFGHIJ");
    }

    [TestMethod]
    public void Write_EL0_ShouldClearFromCursorToEndOfLine()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("0123456789");
        
        // Move cursor to column 5
        console.Write("\x1b[1;6H");
        
        // ESC [ 0 K (or ESC [ K) = Erase from cursor to end of line
        console.Write("\x1b[0K");
        
        console.GetRow(0).GetText().Should().Be("01234     ");
    }

    [TestMethod]
    public void Write_EL1_ShouldClearFromStartOfLineToCursor()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("0123456789");
        
        // Move cursor to column 5
        console.Write("\x1b[1;6H");
        
        // ESC [ 1 K = Erase from start of line to cursor
        console.Write("\x1b[1K");
        
        console.GetRow(0).GetText().Should().Be("      6789");
    }

    [TestMethod]
    public void Write_EL_DefaultParameter_ShouldClearToEnd()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("ABCDEFGHIJ");
        
        // Move cursor to column 3
        console.Write("\x1b[1;4H");
        
        // ESC [ K = EL with default parameter (0)
        console.Write("\x1b[K");
        
        console.GetRow(0).GetText().Should().Be("ABC       ");
    }
}