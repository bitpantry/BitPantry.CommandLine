using BitPantry.CommandLine.AutoComplete.Rendering;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using Spectre.Console.Testing;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.AutoComplete.Rendering;

/// <summary>
/// Tests for GhostLiveRenderer - high-level wrapper for ghost text rendering with LiveRenderable pattern.
/// Follows TDD approach - tests written before implementation.
/// </summary>
[TestClass]
public class GhostLiveRendererTests
{
    #region Show Tests

    [TestMethod]
    public void Show_ShouldRenderGhostTextToConsole()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act
        renderer.Show("suggest");

        // Assert
        console.Output.Should().Contain("suggest");
    }

    [TestMethod]
    public void Show_ShouldSetIsVisibleToTrue()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act
        renderer.Show("suggestion");

        // Assert
        renderer.IsVisible.Should().BeTrue();
    }

    [TestMethod]
    public void Show_ShouldSetCurrentShapeWidth()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act
        renderer.Show("hello");

        // Assert
        renderer.CurrentShape.Width.Should().Be(5); // "hello" is 5 chars
    }

    [TestMethod]
    public void Show_ShouldSetCurrentShapeHeight()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act
        renderer.Show("hello");

        // Assert
        renderer.CurrentShape.Height.Should().Be(1); // Single line ghost
    }

    [TestMethod]
    public void Show_ShouldApplyDimStyle()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act
        renderer.Show("dimmed");

        // Assert - Check output contains the text with dim styling
        // GhostTextRenderable applies dim style, check for text presence
        console.Output.Should().Contain("dimmed");
        // The output contains ANSI styling (ESC[2m is dim decoration)
        console.Output.Should().Contain("\u001b[");
    }

    #endregion

    #region Update Tests

    [TestMethod]
    public void Update_ShouldReplaceGhostText()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        renderer.Show("first");

        // Act
        renderer.Update("second");

        // Assert
        console.Output.Should().Contain("second");
    }

    [TestMethod]
    public void Update_WhenNotVisible_ShouldNotCrash()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act - update without show
        var act = () => renderer.Update("text");

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void Update_ShouldUpdateShape()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        renderer.Show("a");

        // Act
        renderer.Update("longer");

        // Assert
        renderer.CurrentShape.Width.Should().BeGreaterThanOrEqualTo(6);
    }

    [TestMethod]
    public void Update_ShouldInflateShape_NotShrink()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        renderer.Show("longertext"); // 10 chars

        // Act
        renderer.Update("ab"); // 2 chars

        // Assert - Inflate pattern: width should stay at max
        renderer.CurrentShape.Width.Should().Be(10);
    }

    #endregion

    #region Hide/Clear Tests

    [TestMethod]
    public void Hide_ShouldClearGhostText()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        renderer.Show("ghost");

        // Act
        renderer.Hide();

        // Assert
        renderer.IsVisible.Should().BeFalse();
    }

    [TestMethod]
    public void Hide_ShouldResetShapeToZero()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        renderer.Show("ghost");

        // Act
        renderer.Hide();

        // Assert
        renderer.CurrentShape.Width.Should().Be(0);
        renderer.CurrentShape.Height.Should().Be(0);
    }

    [TestMethod]
    public void Hide_ShouldEmitClearSequence()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        renderer.Show("ghost");

        // Capture output before clear
        var beforeHide = console.Output;

        // Act
        renderer.Hide();

        // Assert - should emit spaces or clear sequence
        var afterHide = console.Output;
        afterHide.Length.Should().BeGreaterThan(beforeHide.Length);
    }

    [TestMethod]
    public void Hide_WhenNotVisible_ShouldNotCrash()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act
        var act = () => renderer.Hide();

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void Clear_ShouldBeAliasForHide()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        renderer.Show("ghost");

        // Act
        renderer.Clear();

        // Assert
        renderer.IsVisible.Should().BeFalse();
    }

    #endregion

    #region Show/Hide Cycle Tests

    [TestMethod]
    public void ShowHideCycle_ShouldNotAccumulateState()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act - Multiple cycles
        for (int i = 0; i < 5; i++)
        {
            renderer.Show($"ghost{i}");
            renderer.Hide();
        }

        // Assert - Clean state
        renderer.IsVisible.Should().BeFalse();
        renderer.CurrentShape.Width.Should().Be(0);
    }

    [TestMethod]
    public void Show_AfterHide_ShouldResetMaxWidth()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        
        // First show with long text
        renderer.Show("verylongghost");
        renderer.CurrentShape.Width.Should().Be(13);
        
        // Hide
        renderer.Hide();

        // Act - Show again with short text
        renderer.Show("ab");

        // Assert - Should reset, not inherit old max
        renderer.CurrentShape.Width.Should().Be(2);
    }

    #endregion

    #region Empty Text Tests

    [TestMethod]
    public void Show_WithEmptyText_ShouldSetZeroWidth()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act
        renderer.Show("");

        // Assert
        renderer.CurrentShape.Width.Should().Be(0);
        renderer.IsVisible.Should().BeTrue(); // Still "visible" state
    }

    [TestMethod]
    public void Show_WithNullText_ShouldNotCrash()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);

        // Act
        var act = () => renderer.Show(null!);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Cursor Position Tests

    [TestMethod]
    public void PositionCursor_ShouldReturnBackspaceSequence()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        renderer.Show("ghost");

        // Act
        var position = renderer.PositionCursor();

        // Assert - Should return sequence to move cursor left
        // Ghost text is rendered after input, so position cursor goes back
        position.Should().Contain("\u001b[");
    }

    [TestMethod]
    public void RestoreCursor_ShouldReturnForwardSequence()
    {
        // Arrange
        var console = new TestConsole().EmitAnsiSequences();
        var renderer = new GhostLiveRenderer(console);
        renderer.Show("ghost");

        // Act
        var restore = renderer.RestoreCursor();

        // Assert
        restore.Should().Contain("\u001b[");
    }

    #endregion
}
