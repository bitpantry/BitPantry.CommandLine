using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class CursorPositionTests
{
    [TestMethod]
    public void Default_ShouldBeAtZeroZero()
    {
        var position = new CursorPosition();
        
        position.Row.Should().Be(0);
        position.Column.Should().Be(0);
    }

    [TestMethod]
    public void Constructor_ShouldSetRowAndColumn()
    {
        var position = new CursorPosition(5, 10);
        
        position.Row.Should().Be(5);
        position.Column.Should().Be(10);
    }

    [TestMethod]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var pos1 = new CursorPosition(3, 7);
        var pos2 = new CursorPosition(3, 7);
        
        pos1.Should().Be(pos2);
        pos1.Equals(pos2).Should().BeTrue();
        (pos1 == pos2).Should().BeTrue();
    }

    [TestMethod]
    public void Equality_DifferentRow_ShouldNotBeEqual()
    {
        var pos1 = new CursorPosition(3, 7);
        var pos2 = new CursorPosition(4, 7);
        
        pos1.Should().NotBe(pos2);
        (pos1 != pos2).Should().BeTrue();
    }

    [TestMethod]
    public void Equality_DifferentColumn_ShouldNotBeEqual()
    {
        var pos1 = new CursorPosition(3, 7);
        var pos2 = new CursorPosition(3, 8);
        
        pos1.Should().NotBe(pos2);
    }

    [TestMethod]
    public void GetHashCode_SameValues_ShouldBeSame()
    {
        var pos1 = new CursorPosition(3, 7);
        var pos2 = new CursorPosition(3, 7);
        
        pos1.GetHashCode().Should().Be(pos2.GetHashCode());
    }

    [TestMethod]
    public void ToString_ShouldShowRowAndColumn()
    {
        var position = new CursorPosition(5, 10);
        
        position.ToString().Should().Contain("5").And.Contain("10");
    }
}
