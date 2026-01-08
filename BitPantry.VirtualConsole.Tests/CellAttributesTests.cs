using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class CellAttributesTests
{
    [TestMethod]
    public void None_ShouldBeZero()
    {
        CellAttributes.None.Should().Be((CellAttributes)0);
    }

    [TestMethod]
    public void Bold_ShouldBeDistinctFlag()
    {
        CellAttributes.Bold.Should().Be((CellAttributes)1);
    }

    [TestMethod]
    public void Dim_ShouldBeDistinctFlag()
    {
        CellAttributes.Dim.Should().Be((CellAttributes)2);
    }

    [TestMethod]
    public void Italic_ShouldBeDistinctFlag()
    {
        CellAttributes.Italic.Should().Be((CellAttributes)4);
    }

    [TestMethod]
    public void Underline_ShouldBeDistinctFlag()
    {
        CellAttributes.Underline.Should().Be((CellAttributes)8);
    }

    [TestMethod]
    public void Blink_ShouldBeDistinctFlag()
    {
        CellAttributes.Blink.Should().Be((CellAttributes)16);
    }

    [TestMethod]
    public void Reverse_ShouldBeDistinctFlag()
    {
        CellAttributes.Reverse.Should().Be((CellAttributes)32);
    }

    [TestMethod]
    public void Hidden_ShouldBeDistinctFlag()
    {
        CellAttributes.Hidden.Should().Be((CellAttributes)64);
    }

    [TestMethod]
    public void Strikethrough_ShouldBeDistinctFlag()
    {
        CellAttributes.Strikethrough.Should().Be((CellAttributes)128);
    }

    [TestMethod]
    public void MultipleFlags_CanBeCombined()
    {
        var combined = CellAttributes.Bold | CellAttributes.Italic | CellAttributes.Underline;
        
        combined.HasFlag(CellAttributes.Bold).Should().BeTrue();
        combined.HasFlag(CellAttributes.Italic).Should().BeTrue();
        combined.HasFlag(CellAttributes.Underline).Should().BeTrue();
        combined.HasFlag(CellAttributes.Reverse).Should().BeFalse();
    }

    [TestMethod]
    public void Flags_CanBeRemoved()
    {
        var combined = CellAttributes.Bold | CellAttributes.Italic;
        var removed = combined & ~CellAttributes.Bold;
        
        removed.HasFlag(CellAttributes.Bold).Should().BeFalse();
        removed.HasFlag(CellAttributes.Italic).Should().BeTrue();
    }
}
