using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class CellAttributesTests
{
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
