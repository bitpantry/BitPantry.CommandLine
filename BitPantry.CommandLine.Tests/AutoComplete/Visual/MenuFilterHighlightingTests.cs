using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Tests.VirtualConsole;
using System;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Visual tests for menu filter highlighting (spec-010).
/// These tests verify that ANSI highlighting codes are correctly rendered
/// when filtering menu items and navigating with arrow keys.
/// 
/// Bug discovered: Typing a filter shows blue highlighting, but pressing
/// Up/Down arrow keys to change selection causes highlighting to disappear.
/// </summary>
[TestClass]
public class MenuFilterHighlightingTests : VisualTestBase
{
    /// <summary>
    /// T067 RED: Test that filter highlighting persists when navigating with arrow keys.
    /// 
    /// Steps:
    /// 1. Type "server " to get subcommands
    /// 2. Press Tab to open menu
    /// 3. Type "conn" to filter (shows connect, disconnect with "conn" highlighted)
    /// 4. Press Down to change selection
    /// 5. Verify ANSI blue highlighting is still present in output
    /// 
    /// This test MUST FAIL before T068 fix is applied, proving the bug exists.
    /// </summary>
    [TestMethod]
    [TestDescription("Filter highlighting should persist when changing selection with arrow keys")]
    public async Task FilterHighlighting_ShouldPersist_WhenChangingSelectionWithArrowKeys()
    {
        // Arrange
        using var runner = CreateRunner();
        runner.Initialize();

        // Act - type command prefix and open menu
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        
        runner.Should().HaveMenuVisible("menu should open after Tab");
        
        // Type filter text to trigger MatchRanges highlighting
        await runner.TypeText("conn");
        
        // Verify initial highlighting is present (proves filter matching works)
        runner.Should().HaveBlueHighlighting("filter text should be highlighted in blue after typing");
        
        // Press Down to change selection - this is where the bug manifests
        await runner.PressKey(ConsoleKey.DownArrow);
        
        // Assert - highlighting should STILL be present after changing selection
        runner.Should().HaveBlueHighlighting(
            "filter highlighting should persist after changing selection with Down arrow");
    }

    /// <summary>
    /// Additional test: Verify highlighting persists through multiple navigation steps.
    /// </summary>
    [TestMethod]
    [TestDescription("Filter highlighting should persist through multiple arrow key presses")]
    public async Task FilterHighlighting_ShouldPersist_ThroughMultipleArrowKeyPresses()
    {
        // Arrange
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        await runner.TypeText("c"); // Filter to items containing 'c' (connect, disconnect)
        
        // Verify initial state
        runner.Should().HaveMenuVisible();
        runner.Should().HaveBlueHighlighting("initial filter should show highlighting");

        // Act - navigate multiple times
        await runner.PressKey(ConsoleKey.DownArrow);
        runner.Should().HaveBlueHighlighting("highlighting should persist after first Down");
        
        await runner.PressKey(ConsoleKey.DownArrow);
        runner.Should().HaveBlueHighlighting("highlighting should persist after second Down");
        
        await runner.PressKey(ConsoleKey.UpArrow);
        runner.Should().HaveBlueHighlighting("highlighting should persist after Up");
    }

    /// <summary>
    /// Verify that both selected and non-selected items show highlighting.
    /// </summary>
    [TestMethod]
    [TestDescription("Both selected and non-selected menu items should show filter highlighting")]
    public async Task FilterHighlighting_ShouldAppear_OnAllMatchingItems()
    {
        // Arrange
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        
        // Filter to get multiple matches
        await runner.TypeText("c");
        
        // Assert - check that blue highlighting appears (indicates at least one item has highlighting)
        runner.Should().HaveBlueHighlighting(
            "matching items should have blue highlighting for the filter text");
        
        // Also verify menu selection styling is present
        runner.Should().HaveInvertedSelection(
            "selected item should have inverted styling");
    }
}
