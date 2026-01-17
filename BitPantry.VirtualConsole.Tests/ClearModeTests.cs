using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.VirtualConsole.Tests;

/// <summary>
/// Tests for ClearMode behavior via ScreenBuffer.ClearScreen API.
/// Validates that each ClearMode produces the correct screen state.
/// </summary>
[TestClass]
public class ClearModeTests
{
    [TestMethod]
    [DataRow(ClearMode.ToEnd, "Cursor to end")]
    [DataRow(ClearMode.ToBeginning, "Beginning to cursor")]
    [DataRow(ClearMode.All, "Entire screen")]
    public void ClearScreen_Mode_ClearsCorrectRegion(ClearMode mode, string description)
    {
        // Arrange - Fill screen with content
        var buffer = new ScreenBuffer(10, 3);
        for (int row = 0; row < 3; row++)
        {
            buffer.MoveCursor(row, 0);
            for (int col = 0; col < 10; col++)
                buffer.WriteChar((char)('A' + row));
        }
        
        // Position cursor at row 1, column 5 (middle of screen)
        buffer.MoveCursor(1, 5);
        
        // Act
        buffer.ClearScreen(mode);
        
        // Assert based on mode
        switch (mode)
        {
            case ClearMode.ToEnd:
                // Row 0 should be intact
                buffer.GetCell(0, 0).Character.Should().Be('A', "row 0 should remain for ToEnd");
                // Row 1 from cursor position onward should be cleared
                buffer.GetCell(1, 5).Character.Should().Be(' ', "cursor position should be cleared for ToEnd");
                // Row 2 should be cleared entirely
                buffer.GetCell(2, 0).Character.Should().Be(' ', "row 2 should be cleared for ToEnd");
                break;
                
            case ClearMode.ToBeginning:
                // Row 0 should be cleared
                buffer.GetCell(0, 0).Character.Should().Be(' ', "row 0 should be cleared for ToBeginning");
                // Row 1 up to cursor should be cleared
                buffer.GetCell(1, 0).Character.Should().Be(' ', "row 1 start should be cleared for ToBeginning");
                // Row 2 should be intact
                buffer.GetCell(2, 0).Character.Should().Be('C', "row 2 should remain for ToBeginning");
                break;
                
            case ClearMode.All:
                // All rows should be cleared
                buffer.GetCell(0, 0).Character.Should().Be(' ', "row 0 should be cleared for All");
                buffer.GetCell(1, 0).Character.Should().Be(' ', "row 1 should be cleared for All");
                buffer.GetCell(2, 0).Character.Should().Be(' ', "row 2 should be cleared for All");
                break;
        }
    }

    [TestMethod]
    [DataRow(ClearMode.ToEnd, "Cursor to end of line")]
    [DataRow(ClearMode.ToBeginning, "Beginning of line to cursor")]
    [DataRow(ClearMode.All, "Entire line")]
    public void ClearLine_Mode_ClearsCorrectRegion(ClearMode mode, string description)
    {
        // Arrange - Fill a line with content
        var buffer = new ScreenBuffer(10, 3);
        buffer.MoveCursor(1, 0);
        for (int col = 0; col < 10; col++)
            buffer.WriteChar((char)('0' + col));
        
        // Position cursor at column 5
        buffer.MoveCursor(1, 5);
        
        // Act
        buffer.ClearLine(mode);
        
        // Assert based on mode
        switch (mode)
        {
            case ClearMode.ToEnd:
                // Characters before cursor should remain
                buffer.GetCell(1, 0).Character.Should().Be('0', "before cursor should remain for ToEnd");
                buffer.GetCell(1, 4).Character.Should().Be('4', "before cursor should remain for ToEnd");
                // Cursor position and after should be cleared
                buffer.GetCell(1, 5).Character.Should().Be(' ', "cursor position should be cleared for ToEnd");
                buffer.GetCell(1, 9).Character.Should().Be(' ', "end of line should be cleared for ToEnd");
                break;
                
            case ClearMode.ToBeginning:
                // Characters before and at cursor should be cleared
                buffer.GetCell(1, 0).Character.Should().Be(' ', "start of line should be cleared for ToBeginning");
                buffer.GetCell(1, 5).Character.Should().Be(' ', "cursor position should be cleared for ToBeginning");
                // Characters after cursor should remain
                buffer.GetCell(1, 6).Character.Should().Be('6', "after cursor should remain for ToBeginning");
                break;
                
            case ClearMode.All:
                // Entire line should be cleared
                buffer.GetCell(1, 0).Character.Should().Be(' ', "start should be cleared for All");
                buffer.GetCell(1, 5).Character.Should().Be(' ', "middle should be cleared for All");
                buffer.GetCell(1, 9).Character.Should().Be(' ', "end should be cleared for All");
                break;
        }
    }
}
