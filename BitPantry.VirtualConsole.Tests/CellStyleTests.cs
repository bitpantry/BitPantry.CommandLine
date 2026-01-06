using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class CellStyleTests
{
    [TestMethod]
    public void Default_ShouldHaveNullColors()
    {
        var style = CellStyle.Default;
        
        style.ForegroundColor.Should().BeNull();
        style.BackgroundColor.Should().BeNull();
    }

    [TestMethod]
    public void Default_ShouldHaveNoAttributes()
    {
        var style = CellStyle.Default;
        
        style.Attributes.Should().Be(CellAttributes.None);
    }

    [TestMethod]
    public void WithForeground_ShouldReturnNewInstanceWithColor()
    {
        var original = CellStyle.Default;
        var modified = original.WithForeground(ConsoleColor.Blue);
        
        modified.ForegroundColor.Should().Be(ConsoleColor.Blue);
        original.ForegroundColor.Should().BeNull(); // Original unchanged
    }

    [TestMethod]
    public void WithBackground_ShouldReturnNewInstanceWithColor()
    {
        var original = CellStyle.Default;
        var modified = original.WithBackground(ConsoleColor.Red);
        
        modified.BackgroundColor.Should().Be(ConsoleColor.Red);
        original.BackgroundColor.Should().BeNull(); // Original unchanged
    }

    [TestMethod]
    public void WithAttribute_ShouldAddAttribute()
    {
        var original = CellStyle.Default;
        var modified = original.WithAttribute(CellAttributes.Bold);
        
        modified.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        original.Attributes.Should().Be(CellAttributes.None); // Original unchanged
    }

    [TestMethod]
    public void WithAttribute_ShouldCombineWithExistingAttributes()
    {
        var style = CellStyle.Default
            .WithAttribute(CellAttributes.Bold)
            .WithAttribute(CellAttributes.Italic);
        
        style.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        style.Attributes.HasFlag(CellAttributes.Italic).Should().BeTrue();
    }

    [TestMethod]
    public void WithoutAttribute_ShouldRemoveAttribute()
    {
        var original = CellStyle.Default.WithAttribute(CellAttributes.Bold | CellAttributes.Italic);
        var modified = original.WithoutAttribute(CellAttributes.Bold);
        
        modified.Attributes.HasFlag(CellAttributes.Bold).Should().BeFalse();
        modified.Attributes.HasFlag(CellAttributes.Italic).Should().BeTrue();
    }

    [TestMethod]
    public void ChainedWithMethods_ShouldAccumulateChanges()
    {
        var style = CellStyle.Default
            .WithForeground(ConsoleColor.Green)
            .WithBackground(ConsoleColor.Black)
            .WithAttribute(CellAttributes.Underline);
        
        style.ForegroundColor.Should().Be(ConsoleColor.Green);
        style.BackgroundColor.Should().Be(ConsoleColor.Black);
        style.Attributes.HasFlag(CellAttributes.Underline).Should().BeTrue();
    }

    [TestMethod]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var style1 = CellStyle.Default.WithForeground(ConsoleColor.Blue);
        var style2 = CellStyle.Default.WithForeground(ConsoleColor.Blue);
        
        style1.Should().Be(style2);
        style1.Equals(style2).Should().BeTrue();
    }

    [TestMethod]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var style1 = CellStyle.Default.WithForeground(ConsoleColor.Blue);
        var style2 = CellStyle.Default.WithForeground(ConsoleColor.Red);
        
        style1.Should().NotBe(style2);
    }

    [TestMethod]
    public void GetHashCode_SameValues_ShouldBeSame()
    {
        var style1 = CellStyle.Default.WithForeground(ConsoleColor.Blue).WithAttribute(CellAttributes.Bold);
        var style2 = CellStyle.Default.WithForeground(ConsoleColor.Blue).WithAttribute(CellAttributes.Bold);
        
        style1.GetHashCode().Should().Be(style2.GetHashCode());
    }
}
