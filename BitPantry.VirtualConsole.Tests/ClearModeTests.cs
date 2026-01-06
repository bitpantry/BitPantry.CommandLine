using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class ClearModeTests
{
    [TestMethod]
    public void ToEnd_ShouldBeZero()
    {
        ClearMode.ToEnd.Should().Be((ClearMode)0);
    }

    [TestMethod]
    public void ToBeginning_ShouldBeOne()
    {
        ClearMode.ToBeginning.Should().Be((ClearMode)1);
    }

    [TestMethod]
    public void All_ShouldBeTwo()
    {
        ClearMode.All.Should().Be((ClearMode)2);
    }

    [TestMethod]
    public void AllValues_ShouldBeDistinct()
    {
        var values = new[] { ClearMode.ToEnd, ClearMode.ToBeginning, ClearMode.All };
        values.Should().OnlyHaveUniqueItems();
    }
}
