using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class ScreenCellTests
{
    [TestMethod]
    public void Default_ShouldHaveSpaceCharacter()
    {
        var cell = new ScreenCell();
        
        cell.Character.Should().Be(' ');
    }

    [TestMethod]
    public void Default_ShouldHaveDefaultStyle()
    {
        var cell = new ScreenCell();
        
        cell.Style.Should().Be(CellStyle.Default);
    }

    [TestMethod]
    public void Constructor_WithCharacterAndStyle_ShouldSetBoth()
    {
        var style = CellStyle.Default.WithForeground(ConsoleColor.Blue);
        var cell = new ScreenCell('X', style);
        
        cell.Character.Should().Be('X');
        cell.Style.Should().Be(style);
    }

    [TestMethod]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var style = CellStyle.Default.WithForeground(ConsoleColor.Red);
        var cell1 = new ScreenCell('A', style);
        var cell2 = new ScreenCell('A', style);
        
        cell1.Should().Be(cell2);
        cell1.Equals(cell2).Should().BeTrue();
    }

    [TestMethod]
    public void Equality_DifferentCharacter_ShouldNotBeEqual()
    {
        var cell1 = new ScreenCell('A', CellStyle.Default);
        var cell2 = new ScreenCell('B', CellStyle.Default);
        
        cell1.Should().NotBe(cell2);
    }

    [TestMethod]
    public void Equality_DifferentStyle_ShouldNotBeEqual()
    {
        var cell1 = new ScreenCell('A', CellStyle.Default.WithForeground(ConsoleColor.Blue));
        var cell2 = new ScreenCell('A', CellStyle.Default.WithForeground(ConsoleColor.Red));
        
        cell1.Should().NotBe(cell2);
    }

    [TestMethod]
    public void GetHashCode_SameValues_ShouldBeSame()
    {
        var style = CellStyle.Default.WithForeground(ConsoleColor.Blue);
        var cell1 = new ScreenCell('X', style);
        var cell2 = new ScreenCell('X', style);
        
        cell1.GetHashCode().Should().Be(cell2.GetHashCode());
    }
}
