using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete.Rendering;

namespace BitPantry.CommandLine.Tests.AutoComplete.Rendering;

/// <summary>
/// Tests for SegmentShape struct that tracks maximum rendered dimensions.
/// The key behavior is Inflate() - dimensions only grow, never shrink.
/// </summary>
[TestClass]
public class SegmentShapeTests
{
    [TestMethod]
    public void Constructor_Should_Set_Width_And_Height()
    {
        var shape = new SegmentShape(10, 5);

        shape.Width.Should().Be(10);
        shape.Height.Should().Be(5);
    }

    [TestMethod]
    public void Inflate_With_Larger_Dimensions_Should_Return_Larger_Shape()
    {
        var original = new SegmentShape(10, 5);
        var larger = new SegmentShape(15, 8);

        var result = original.Inflate(larger);

        result.Width.Should().Be(15);
        result.Height.Should().Be(8);
    }

    [TestMethod]
    public void Inflate_With_Smaller_Dimensions_Should_Keep_Original_Shape()
    {
        var original = new SegmentShape(10, 5);
        var smaller = new SegmentShape(5, 3);

        var result = original.Inflate(smaller);

        // Inflate should only grow, never shrink
        result.Width.Should().Be(10);
        result.Height.Should().Be(5);
    }

    [TestMethod]
    public void Inflate_With_Mixed_Dimensions_Should_Take_Max_Of_Each()
    {
        var shape1 = new SegmentShape(10, 5);
        var shape2 = new SegmentShape(5, 8);

        var result = shape1.Inflate(shape2);

        result.Width.Should().Be(10);  // Max of 10, 5
        result.Height.Should().Be(8);  // Max of 5, 8
    }

    [TestMethod]
    public void Inflate_With_Equal_Dimensions_Should_Return_Same_Shape()
    {
        var shape1 = new SegmentShape(10, 5);
        var shape2 = new SegmentShape(10, 5);

        var result = shape1.Inflate(shape2);

        result.Width.Should().Be(10);
        result.Height.Should().Be(5);
    }

    [TestMethod]
    public void Inflate_Chain_Should_Track_Maximum_Over_Time()
    {
        // Simulates a menu that grows from 3 items to 5 items to 2 items
        // The height should track to 5 (the maximum)
        var initial = new SegmentShape(20, 3);
        var grew = new SegmentShape(20, 5);
        var shrunk = new SegmentShape(20, 2);

        var afterGrow = initial.Inflate(grew);
        afterGrow.Height.Should().Be(5);

        var afterShrink = afterGrow.Inflate(shrunk);
        afterShrink.Height.Should().Be(5);  // Should NOT shrink back to 2
    }

    [TestMethod]
    public void Default_Shape_Should_Have_Zero_Dimensions()
    {
        var shape = default(SegmentShape);

        shape.Width.Should().Be(0);
        shape.Height.Should().Be(0);
    }
}
