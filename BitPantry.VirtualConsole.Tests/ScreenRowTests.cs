using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class ScreenRowTests
{
    // T021: Tests for ScreenRow wrapper
    [TestMethod]
    public void RowIndex_ShouldReturnCorrectIndex()
    {
        var buffer = new ScreenBuffer(80, 25);
        var row = buffer.GetRow(5);
        
        row.RowIndex.Should().Be(5);
    }

    [TestMethod]
    public void Length_ShouldMatchBufferWidth()
    {
        var buffer = new ScreenBuffer(80, 25);
        var row = buffer.GetRow(0);
        
        row.Length.Should().Be(80);
    }

    [TestMethod]
    public void GetCell_ShouldReturnCellAtColumn()
    {
        var buffer = new ScreenBuffer(80, 25);
        buffer.MoveCursor(0, 5);
        buffer.WriteChar('X');
        
        var row = buffer.GetRow(0);
        
        row.GetCell(5).Character.Should().Be('X');
    }

    [TestMethod]
    public void GetText_ShouldReturnRowText()
    {
        var buffer = new ScreenBuffer(80, 25);
        buffer.WriteChar('T');
        buffer.WriteChar('e');
        buffer.WriteChar('s');
        buffer.WriteChar('t');
        
        var row = buffer.GetRow(0);
        
        row.GetText().Should().StartWith("Test");
    }

    [TestMethod]
    public void GetText_EmptyRow_ShouldReturnSpaces()
    {
        var buffer = new ScreenBuffer(10, 5);
        var row = buffer.GetRow(0);
        
        row.GetText().Should().Be("          "); // 10 spaces
    }

    [TestMethod]
    public void GetCells_ShouldEnumerateAllCells()
    {
        var buffer = new ScreenBuffer(5, 1);
        buffer.WriteChar('A');
        buffer.WriteChar('B');
        buffer.WriteChar('C');
        
        var row = buffer.GetRow(0);
        var cells = row.GetCells().ToList();
        
        cells.Should().HaveCount(5);
        cells[0].Character.Should().Be('A');
        cells[1].Character.Should().Be('B');
        cells[2].Character.Should().Be('C');
        cells[3].Character.Should().Be(' ');
        cells[4].Character.Should().Be(' ');
    }

    // T058: GetCells enumeration with styling
    [TestMethod]
    public void GetCells_ShouldReturnCellsWithStyling()
    {
        var buffer = new ScreenBuffer(5, 1);
        buffer.ApplyStyle(CellStyle.Default.WithForeground(ConsoleColor.Red));
        buffer.WriteChar('R');
        buffer.ApplyStyle(CellStyle.Default.WithForeground(ConsoleColor.Green));
        buffer.WriteChar('G');
        buffer.ApplyStyle(CellStyle.Default.WithForeground(ConsoleColor.Blue));
        buffer.WriteChar('B');
        
        var row = buffer.GetRow(0);
        var cells = row.GetCells().ToList();
        
        cells[0].Style.ForegroundColor.Should().Be(ConsoleColor.Red);
        cells[1].Style.ForegroundColor.Should().Be(ConsoleColor.Green);
        cells[2].Style.ForegroundColor.Should().Be(ConsoleColor.Blue);
    }

    [TestMethod]
    public void GetCells_ShouldPreserveEnumerationOrder()
    {
        var buffer = new ScreenBuffer(10, 1);
        for (int i = 0; i < 10; i++)
        {
            buffer.WriteChar((char)('0' + i));
        }
        
        var row = buffer.GetRow(0);
        var chars = row.GetCells().Select(c => c.Character).ToList();
        
        chars.Should().ContainInOrder('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
    }

    // T059: Row with mixed styling
    [TestMethod]
    public void GetCells_MixedStyles_ShouldTrackIndependently()
    {
        var buffer = new ScreenBuffer(10, 1);
        
        // Write with bold
        buffer.ApplyStyle(CellStyle.Default.WithAttribute(CellAttributes.Bold));
        buffer.WriteChar('A');
        buffer.WriteChar('B');
        
        // Switch to underline (not bold)
        buffer.ApplyStyle(CellStyle.Default.WithAttribute(CellAttributes.Underline));
        buffer.WriteChar('C');
        buffer.WriteChar('D');
        
        // Switch to bold + underline
        buffer.ApplyStyle(CellStyle.Default.WithAttribute(CellAttributes.Bold | CellAttributes.Underline));
        buffer.WriteChar('E');
        
        var row = buffer.GetRow(0);
        var cells = row.GetCells().ToList();
        
        cells[0].Style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        cells[0].Style.Attributes.HasFlag(CellAttributes.Underline).Should().BeFalse();
        
        cells[2].Style.Attributes.HasFlag(CellAttributes.Bold).Should().BeFalse();
        cells[2].Style.Attributes.HasFlag(CellAttributes.Underline).Should().BeTrue();
        
        cells[4].Style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        cells[4].Style.Attributes.HasFlag(CellAttributes.Underline).Should().BeTrue();
    }

    [TestMethod]
    public void GetCells_MixedColors_ShouldPreserveEach()
    {
        var buffer = new ScreenBuffer(5, 1);
        
        buffer.ApplyStyle(CellStyle.Default.WithForeground(ConsoleColor.Red).WithBackground(ConsoleColor.White));
        buffer.WriteChar('1');
        
        buffer.ApplyStyle(CellStyle.Default.WithForeground(ConsoleColor.Yellow).WithBackground(ConsoleColor.Black));
        buffer.WriteChar('2');
        
        buffer.ResetStyle();
        buffer.WriteChar('3');
        
        var row = buffer.GetRow(0);
        var cells = row.GetCells().ToList();
        
        cells[0].Style.ForegroundColor.Should().Be(ConsoleColor.Red);
        cells[0].Style.BackgroundColor.Should().Be(ConsoleColor.White);
        
        cells[1].Style.ForegroundColor.Should().Be(ConsoleColor.Yellow);
        cells[1].Style.BackgroundColor.Should().Be(ConsoleColor.Black);
        
        cells[2].Style.ForegroundColor.Should().BeNull();
        cells[2].Style.BackgroundColor.Should().BeNull();
    }

    [TestMethod]
    public void GetText_MixedStyles_ShouldReturnPlainText()
    {
        var buffer = new ScreenBuffer(10, 1);
        
        buffer.ApplyStyle(CellStyle.Default.WithForeground(ConsoleColor.Red));
        buffer.WriteChar('H');
        buffer.WriteChar('e');
        
        buffer.ApplyStyle(CellStyle.Default.WithForeground(ConsoleColor.Blue));
        buffer.WriteChar('l');
        buffer.WriteChar('l');
        buffer.WriteChar('o');
        
        var row = buffer.GetRow(0);
        
        row.GetText().Should().StartWith("Hello");
    }
}
