using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete.Rendering;

namespace BitPantry.CommandLine.Tests.AutoComplete.Rendering;

/// <summary>
/// Tests for AnsiCodes static helper class that provides ANSI escape sequence constants and builders.
/// </summary>
[TestClass]
public class AnsiCodesTests
{
    [TestMethod]
    public void HideCursor_Should_Return_Correct_Sequence()
    {
        // ANSI sequence to hide cursor: ESC[?25l
        AnsiCodes.HideCursor.Should().Be("\u001b[?25l");
    }

    [TestMethod]
    public void ShowCursor_Should_Return_Correct_Sequence()
    {
        // ANSI sequence to show cursor: ESC[?25h
        AnsiCodes.ShowCursor.Should().Be("\u001b[?25h");
    }

    [TestMethod]
    public void ClearLine_Should_Return_Correct_Sequence()
    {
        // ANSI sequence to clear entire line: ESC[2K
        AnsiCodes.ClearLine.Should().Be("\u001b[2K");
    }

    [TestMethod]
    public void ClearToEndOfLine_Should_Return_Correct_Sequence()
    {
        // ANSI sequence to clear from cursor to end of line: ESC[K
        AnsiCodes.ClearToEndOfLine.Should().Be("\u001b[K");
    }

    [TestMethod]
    public void CarriageReturn_Should_Return_Correct_Sequence()
    {
        AnsiCodes.CarriageReturn.Should().Be("\r");
    }

    [TestMethod]
    [DataRow(1, "\u001b[1A")]
    [DataRow(3, "\u001b[3A")]
    [DataRow(10, "\u001b[10A")]
    public void CursorUp_Should_Return_Correct_Sequence(int lines, string expected)
    {
        // ANSI sequence to move cursor up N lines: ESC[nA
        AnsiCodes.CursorUp(lines).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(1, "\u001b[1B")]
    [DataRow(3, "\u001b[3B")]
    [DataRow(10, "\u001b[10B")]
    public void CursorDown_Should_Return_Correct_Sequence(int lines, string expected)
    {
        // ANSI sequence to move cursor down N lines: ESC[nB
        AnsiCodes.CursorDown(lines).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(1, "\u001b[1C")]
    [DataRow(5, "\u001b[5C")]
    public void CursorForward_Should_Return_Correct_Sequence(int columns, string expected)
    {
        // ANSI sequence to move cursor forward N columns: ESC[nC
        AnsiCodes.CursorForward(columns).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(1, "\u001b[1D")]
    [DataRow(5, "\u001b[5D")]
    public void CursorBack_Should_Return_Correct_Sequence(int columns, string expected)
    {
        // ANSI sequence to move cursor back N columns: ESC[nD
        AnsiCodes.CursorBack(columns).Should().Be(expected);
    }

    [TestMethod]
    public void EraseLine_Mode2_Should_Clear_Entire_Line()
    {
        // ESC[2K clears entire line
        AnsiCodes.EraseLine(2).Should().Be("\u001b[2K");
    }

    [TestMethod]
    public void EraseLine_Mode0_Should_Clear_To_End()
    {
        // ESC[0K or ESC[K clears from cursor to end of line
        AnsiCodes.EraseLine(0).Should().Be("\u001b[0K");
    }
}
