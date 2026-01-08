using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace BitPantry.VirtualConsole.Tests;

/// <summary>
/// Tests for T064-T066: Verifying the public API surface is complete and clean.
/// </summary>
[TestClass]
public class PublicApiTests
{
    // T064: Public API completeness
    [TestMethod]
    public void VirtualConsole_ShouldExposeWriteMethod()
    {
        var console = new VirtualConsole(80, 25);
        
        // Write should be a public method
        Action action = () => console.Write("test");
        
        action.Should().NotThrow();
    }

    [TestMethod]
    public void VirtualConsole_ShouldExposeGetCellMethod()
    {
        var console = new VirtualConsole(80, 25);
        
        // GetCell should return a ScreenCell
        var cell = console.GetCell(0, 0);
        
        cell.Should().NotBeNull();
        cell.Character.Should().Be(' '); // Default cell
    }

    [TestMethod]
    public void VirtualConsole_ShouldExposeGetRowMethod()
    {
        var console = new VirtualConsole(80, 25);
        
        // GetRow should return a ScreenRow
        var row = console.GetRow(0);
        
        row.Should().NotBeNull();
        row.Length.Should().Be(80);
    }

    [TestMethod]
    public void VirtualConsole_ShouldExposeGetScreenTextMethod()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("Hello");
        
        var text = console.GetScreenText();
        
        text.Should().Contain("Hello");
    }

    [TestMethod]
    public void VirtualConsole_ShouldExposeGetScreenContentMethod()
    {
        var console = new VirtualConsole(10, 5);
        console.Write("Line1\nLine2");
        
        var content = console.GetScreenContent();
        
        content.Should().Contain("Line1");
        content.Should().Contain("Line2");
    }

    [TestMethod]
    public void VirtualConsole_ShouldExposeClearMethod()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Something");
        
        Action action = () => console.Clear();
        
        action.Should().NotThrow();
    }

    // T065: Cursor position exposed
    [TestMethod]
    public void VirtualConsole_ShouldExposeCursorRowProperty()
    {
        var console = new VirtualConsole(80, 25);
        
        console.CursorRow.Should().Be(0);
        
        console.Write("\n\n");
        
        console.CursorRow.Should().Be(2);
    }

    [TestMethod]
    public void VirtualConsole_ShouldExposeCursorColumnProperty()
    {
        var console = new VirtualConsole(80, 25);
        
        console.CursorColumn.Should().Be(0);
        
        console.Write("Hello");
        
        console.CursorColumn.Should().Be(5);
    }

    [TestMethod]
    public void VirtualConsole_CursorPosition_ShouldReflectAnsiMovement()
    {
        var console = new VirtualConsole(80, 25);
        
        // Move cursor to row 5, column 10
        console.Write("\x1b[6;11H"); // 1-based in ANSI
        
        console.CursorRow.Should().Be(5);
        console.CursorColumn.Should().Be(10);
    }

    // T066: Screen dimensions queryable
    [TestMethod]
    public void VirtualConsole_ShouldExposeWidthProperty()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Width.Should().Be(80);
    }

    [TestMethod]
    public void VirtualConsole_ShouldExposeHeightProperty()
    {
        var console = new VirtualConsole(80, 25);
        
        console.Height.Should().Be(25);
    }

    [TestMethod]
    public void VirtualConsole_DimensionsShouldMatchConstructorArgs()
    {
        var console = new VirtualConsole(120, 40);
        
        console.Width.Should().Be(120);
        console.Height.Should().Be(40);
    }

    // Additional completeness tests
    [TestMethod]
    public void ScreenCell_ShouldExposeCharacterAndStyle()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[31mX\x1b[0m");
        
        var cell = console.GetCell(0, 0);
        
        cell.Character.Should().Be('X');
        cell.Style.ForegroundColor.Should().Be(ConsoleColor.DarkRed);
    }

    [TestMethod]
    public void ScreenRow_ShouldExposeRowIndexLengthAndCells()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("Test");
        
        var row = console.GetRow(0);
        
        row.RowIndex.Should().Be(0);
        row.Length.Should().Be(80);
        row.GetText().Should().StartWith("Test");
        row.GetCells().Should().HaveCount(80);
    }

    [TestMethod]
    public void CellStyle_ShouldExposeAllColorAndAttributeProperties()
    {
        var console = new VirtualConsole(80, 25);
        // Bold, underline, red foreground on bright white background
        console.Write("\x1b[1;4;31;107mX\x1b[0m");
        
        var style = console.GetCell(0, 0).Style;
        
        // All these properties should be accessible
        style.ForegroundColor.Should().Be(ConsoleColor.DarkRed);
        style.BackgroundColor.Should().Be(ConsoleColor.White);
        style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        style.Attributes.HasFlag(CellAttributes.Underline).Should().BeTrue();
    }

    [TestMethod]
    public void CellStyle_ShouldExposeExtendedColorProperties()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[38;5;196mX\x1b[0m");
        
        var style = console.GetCell(0, 0).Style;
        
        style.Foreground256.Should().Be(196);
    }

    [TestMethod]
    public void CellStyle_ShouldExposeTrueColorProperties()
    {
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[38;2;100;150;200mX\x1b[0m");
        
        var style = console.GetCell(0, 0).Style;
        
        style.ForegroundRgb.Should().NotBeNull();
        style.ForegroundRgb!.Value.R.Should().Be(100);
        style.ForegroundRgb!.Value.G.Should().Be(150);
        style.ForegroundRgb!.Value.B.Should().Be(200);
    }

    [TestMethod]
    public void PublicTypes_ShouldBeAccessibleWithoutInternals()
    {
        // All these types should be public and usable from external code
        var console = new VirtualConsole(80, 25);
        ScreenCell cell = console.GetCell(0, 0);
        ScreenRow row = console.GetRow(0);
        CellStyle style = cell.Style;
        CellAttributes attrs = style.Attributes;
        
        // If this compiles, the public API is accessible
        console.Should().NotBeNull();
        cell.Should().NotBeNull();
        row.Should().NotBeNull();
    }
}
