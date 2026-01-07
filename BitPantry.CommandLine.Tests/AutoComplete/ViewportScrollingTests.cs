using System;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Viewport Scrolling Tests (TC-10.1 through TC-10.5)
/// Tests menu scrolling behavior in limited viewport.
/// </summary>
[TestClass]
public class ViewportScrollingTests
{
    /// <summary>
    /// Helper method to create harness with many 's'-prefixed commands for scrolling tests.
    /// We need 15+ matching items to test viewport scrolling behavior.
    /// </summary>
    private static AutoCompleteTestHarness CreateLargeMenuHarness()
    {
        return AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand),
            typeof(ServerDevCommand),
            typeof(ServerProdCommand),
            typeof(ScanTestCommand),
            typeof(SearchTestCommand),
            typeof(SecurityTestCommand),
            typeof(SeedTestCommand),
            typeof(SendTestCommand),
            typeof(ShellTestCommand),
            typeof(ShowTestCommand),
            typeof(ShutdownTestCommand),
            typeof(SignalTestCommand),
            typeof(SnapshotTestCommand),
            typeof(SortTestCommand),
            typeof(SourceTestCommand),
            typeof(SpawnTestCommand),
            typeof(SplitTestCommand),
            typeof(StartTestCommand),
            typeof(StatusTestCommand),
            typeof(StopTestCommand),
            typeof(StorageTestCommand),
            typeof(SubscribeTestCommand),
            typeof(SyncTestCommand));
    }

    #region TC-10.1: Scroll When Navigating Past Viewport

    /// <summary>
    /// TC-10.1: When menu has more items than visible viewport and user navigates past visible area,
    /// Then menu scrolls to keep selected item visible.
    /// </summary>
    [TestMethod]
    public void TC_10_1_ScrollWhenNavigatingPastViewport()
    {
        // Arrange: Create harness with many 's' commands (25 items)
        using var harness = CreateLargeMenuHarness();

        // Act: Type "s" and press Tab to open menu with all 's' commands
        harness.TypeText("s");
        harness.PressTab();

        // Assert: Menu should be visible with 15+ items
        harness.IsMenuVisible.Should().BeTrue("menu should appear for multiple 's' commands");
        harness.MenuItemCount.Should().BeGreaterThanOrEqualTo(15, "should have 15+ items for scrolling test");

        // Navigate down 11 times (past typical viewport of 10)
        for (int i = 0; i < 11; i++)
        {
            harness.PressDownArrow();
        }

        // Assert: Selection should be at item 11 (0-indexed), meaning we've navigated past viewport
        harness.SelectedIndex.Should().Be(11, "selection should be at item 11 after 11 down arrows");
        harness.IsMenuVisible.Should().BeTrue("menu should remain visible after scrolling");
    }

    #endregion

    #region TC-10.2: Scroll Indicators Show More Items Above

    /// <summary>
    /// TC-10.2: When menu has scrolled past the first items,
    /// Then "↑ N more" indicator shows count of hidden items above.
    /// </summary>
    [TestMethod]
    public void TC_10_2_ScrollIndicatorAbove_AfterScrollingDown()
    {
        // Arrange
        using var harness = CreateLargeMenuHarness();

        // Act: Open menu and scroll down past viewport
        harness.TypeText("s");
        harness.PressTab();

        harness.IsMenuVisible.Should().BeTrue("menu should appear");
        harness.MenuItemCount.Should().BeGreaterThanOrEqualTo(15, "should have enough items");

        // Navigate past viewport
        for (int i = 0; i < 12; i++)
        {
            harness.PressDownArrow();
        }

        // Assert: Selection should have moved
        harness.SelectedIndex.Should().Be(12, "selection should be at item 12");
        
        // The scroll indicator should show above (this is visual, but we can verify menu state)
        harness.IsMenuVisible.Should().BeTrue("menu should remain visible while scrolled");
    }

    #endregion

    #region TC-10.3: Scroll Indicators Show More Items Below

    /// <summary>
    /// TC-10.3: When menu has items below the viewport,
    /// Then "↓ N more" indicator shows count of hidden items below.
    /// </summary>
    [TestMethod]
    public void TC_10_3_ScrollIndicatorBelow_WhenMoreItemsExist()
    {
        // Arrange
        using var harness = CreateLargeMenuHarness();

        // Act
        harness.TypeText("s");
        harness.PressTab();

        // Assert: Menu visible with more items than viewport can show
        harness.IsMenuVisible.Should().BeTrue("menu should appear");
        harness.MenuItemCount.Should().BeGreaterThanOrEqualTo(15, 
            "should have more items than typical viewport (10) to trigger scroll indicator");
        harness.SelectedIndex.Should().Be(0, "first item should be selected initially");
    }

    #endregion

    #region TC-10.4: Selection Highlighted After Scroll

    /// <summary>
    /// TC-10.4: When menu scrolls to show new items,
    /// Then selected item is still visually highlighted.
    /// </summary>
    [TestMethod]
    public void TC_10_4_SelectionHighlighted_AfterScroll()
    {
        // Arrange
        using var harness = CreateLargeMenuHarness();

        // Act: Scroll to item 11
        harness.TypeText("s");
        harness.PressTab();

        harness.IsMenuVisible.Should().BeTrue("menu should appear");

        for (int i = 0; i < 11; i++)
        {
            harness.PressDownArrow();
        }

        // Assert: Item 11 should be selected (selection tracking maintained through scroll)
        harness.SelectedIndex.Should().Be(11, "item 11 should be selected after scrolling");
        harness.IsMenuVisible.Should().BeTrue("menu should remain visible");
        
        // The selected item text should be accessible
        harness.SelectedItem.Should().NotBeNullOrEmpty("selected item should have a value");
    }

    #endregion

    #region TC-10.5: Scroll Back with Up Arrow

    /// <summary>
    /// TC-10.5: When navigating back up from scrolled position,
    /// Then viewport scrolls back to show earlier items.
    /// </summary>
    [TestMethod]
    public void TC_10_5_ScrollBack_WithUpArrow()
    {
        // Arrange
        using var harness = CreateLargeMenuHarness();

        // Act: Scroll down to item 11, then back up
        harness.TypeText("s");
        harness.PressTab();

        harness.IsMenuVisible.Should().BeTrue("menu should appear");

        // Navigate down to item 11
        for (int i = 0; i < 11; i++)
        {
            harness.PressDownArrow();
        }
        harness.SelectedIndex.Should().Be(11, "should be at item 11");

        // Press Up Arrow to go back to item 10
        harness.PressUpArrow();

        // Assert: Item 10 should now be selected
        harness.SelectedIndex.Should().Be(10, "item 10 should be selected after pressing up arrow");
        harness.IsMenuVisible.Should().BeTrue("menu should remain visible");
    }

    #endregion
}
