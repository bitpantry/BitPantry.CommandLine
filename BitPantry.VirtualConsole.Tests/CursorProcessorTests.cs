using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.VirtualConsole.AnsiParser;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class CursorProcessorTests
{
    private ScreenBuffer CreateBuffer() => new ScreenBuffer(80, 25);

    // T033: Tests for CursorProcessor handling CUU/CUD/CUF/CUB
    [TestMethod]
    public void ProcessCursorUp_ShouldMoveCursorUp()
    {
        var buffer = CreateBuffer();
        buffer.MoveCursor(5, 10);
        var processor = new CursorProcessor();
        
        processor.Process(new CsiSequence(new[] { 2 }, 'A'), buffer);
        
        buffer.CursorRow.Should().Be(3);
        buffer.CursorColumn.Should().Be(10);
    }

    [TestMethod]
    public void ProcessCursorUp_DefaultParameter_ShouldMoveOne()
    {
        var buffer = CreateBuffer();
        buffer.MoveCursor(5, 10);
        var processor = new CursorProcessor();
        
        processor.Process(new CsiSequence(System.Array.Empty<int>(), 'A'), buffer);
        
        buffer.CursorRow.Should().Be(4);
    }

    [TestMethod]
    public void ProcessCursorDown_ShouldMoveCursorDown()
    {
        var buffer = CreateBuffer();
        buffer.MoveCursor(5, 10);
        var processor = new CursorProcessor();
        
        processor.Process(new CsiSequence(new[] { 3 }, 'B'), buffer);
        
        buffer.CursorRow.Should().Be(8);
    }

    [TestMethod]
    public void ProcessCursorForward_ShouldMoveCursorRight()
    {
        var buffer = CreateBuffer();
        buffer.MoveCursor(5, 10);
        var processor = new CursorProcessor();
        
        processor.Process(new CsiSequence(new[] { 5 }, 'C'), buffer);
        
        buffer.CursorColumn.Should().Be(15);
    }

    [TestMethod]
    public void ProcessCursorBack_ShouldMoveCursorLeft()
    {
        var buffer = CreateBuffer();
        buffer.MoveCursor(5, 10);
        var processor = new CursorProcessor();
        
        processor.Process(new CsiSequence(new[] { 4 }, 'D'), buffer);
        
        buffer.CursorColumn.Should().Be(6);
    }

    // T034: Tests for CursorProcessor handling CUP (cursor position)
    [TestMethod]
    public void ProcessCursorPosition_ShouldSetAbsolutePosition()
    {
        var buffer = CreateBuffer();
        var processor = new CursorProcessor();
        
        // CSI 10;20H - position at row 10, column 20 (1-based in ANSI)
        processor.Process(new CsiSequence(new[] { 10, 20 }, 'H'), buffer);
        
        buffer.CursorRow.Should().Be(9);  // 0-based
        buffer.CursorColumn.Should().Be(19);
    }

    [TestMethod]
    public void ProcessCursorPosition_NoParameters_ShouldMoveToHome()
    {
        var buffer = CreateBuffer();
        buffer.MoveCursor(10, 10);
        var processor = new CursorProcessor();
        
        // CSI H - home position
        processor.Process(new CsiSequence(System.Array.Empty<int>(), 'H'), buffer);
        
        buffer.CursorRow.Should().Be(0);
        buffer.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void ProcessCursorPosition_SingleParameter_ShouldSetRowOnly()
    {
        var buffer = CreateBuffer();
        var processor = new CursorProcessor();
        
        // CSI 5H - row 5, column 1
        processor.Process(new CsiSequence(new[] { 5 }, 'H'), buffer);
        
        buffer.CursorRow.Should().Be(4);  // 0-based
        buffer.CursorColumn.Should().Be(0);
    }

    // T035: Tests for CursorProcessor handling CHA (cursor horizontal absolute)
    [TestMethod]
    public void ProcessCursorHorizontalAbsolute_ShouldSetColumn()
    {
        var buffer = CreateBuffer();
        buffer.MoveCursor(5, 10);
        var processor = new CursorProcessor();
        
        // CSI 25G - column 25 (1-based)
        processor.Process(new CsiSequence(new[] { 25 }, 'G'), buffer);
        
        buffer.CursorRow.Should().Be(5);  // Row unchanged
        buffer.CursorColumn.Should().Be(24);  // 0-based
    }

    [TestMethod]
    public void ProcessCursorHorizontalAbsolute_NoParameter_ShouldMoveToColumn1()
    {
        var buffer = CreateBuffer();
        buffer.MoveCursor(5, 10);
        var processor = new CursorProcessor();
        
        // CSI G - column 1
        processor.Process(new CsiSequence(System.Array.Empty<int>(), 'G'), buffer);
        
        buffer.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void ProcessCursor_OutOfBounds_ShouldClamp()
    {
        var buffer = CreateBuffer();
        var processor = new CursorProcessor();
        
        // Try to move cursor way out of bounds
        processor.Process(new CsiSequence(new[] { 1000 }, 'B'), buffer); // Down 1000
        
        buffer.CursorRow.Should().Be(24); // Max row
    }

    [TestMethod]
    public void CanProcess_ShouldReturnTrueForCursorCommands()
    {
        var processor = new CursorProcessor();
        
        processor.CanProcess('A').Should().BeTrue(); // CUU
        processor.CanProcess('B').Should().BeTrue(); // CUD
        processor.CanProcess('C').Should().BeTrue(); // CUF
        processor.CanProcess('D').Should().BeTrue(); // CUB
        processor.CanProcess('H').Should().BeTrue(); // CUP
        processor.CanProcess('G').Should().BeTrue(); // CHA
        processor.CanProcess('f').Should().BeTrue(); // HVP (same as CUP)
    }

    [TestMethod]
    public void CanProcess_ShouldReturnFalseForNonCursorCommands()
    {
        var processor = new CursorProcessor();
        
        processor.CanProcess('m').Should().BeFalse(); // SGR
        processor.CanProcess('J').Should().BeFalse(); // ED
        processor.CanProcess('K').Should().BeFalse(); // EL
    }
}
