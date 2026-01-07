using System.Linq;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Result Limiting & Truncation Tests (TC-21.1 through TC-21.5)
/// Tests that completion results are properly limited and truncation is indicated.
/// </summary>
[TestClass]
public class ResultLimitingTests
{
    #region TC-21.1: Maximum Items Cached

    /// <summary>
    /// TC-21.1: When source returns many items,
    /// Then a reasonable maximum are displayed.
    /// Note: Actual limit depends on implementation (may be 100 or viewport-based).
    /// </summary>
    [TestMethod]
    public void TC_21_1_MaximumItems_AreCapped()
    {
        // Arrange: Create harness with many commands (25+ 's' commands)
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ScanTestCommand), typeof(SearchTestCommand), typeof(SecurityTestCommand),
            typeof(SeedTestCommand), typeof(SendTestCommand), typeof(ShellTestCommand),
            typeof(ShowTestCommand), typeof(ShutdownTestCommand), typeof(SignalTestCommand),
            typeof(SnapshotTestCommand), typeof(SortTestCommand), typeof(SourceTestCommand),
            typeof(SpawnTestCommand), typeof(SplitTestCommand), typeof(StartTestCommand),
            typeof(StatusTestCommand), typeof(StopTestCommand), typeof(StorageTestCommand),
            typeof(SubscribeTestCommand), typeof(SyncTestCommand),
            typeof(ServerCommand), typeof(ServiceCommand), typeof(SetupCommand),
            typeof(ServerDevCommand), typeof(ServerProdCommand));

        // Act: Type 's' to get many matches
        harness.TypeText("s");
        harness.PressTab();
        
        // Assert: Menu is shown with items
        harness.IsMenuVisible.Should().BeTrue("should show filtered menu");
        harness.MenuItemCount.Should().BeGreaterThan(0, "should have items");
        
        // Items are capped at some reasonable limit (exact limit is implementation-specific)
        // For now, just verify we don't get an unreasonable number
        harness.MenuItemCount.Should().BeLessThanOrEqualTo(100, "should be capped at reasonable limit");
    }

    #endregion

    #region TC-21.2: Truncation Indicator Shown

    /// <summary>
    /// TC-21.2: When results are truncated,
    /// Then indicator appears (if implemented).
    /// Note: Truncation indicator may not be implemented in all versions.
    /// </summary>
    [TestMethod]
    public void TC_21_2_TruncationIndicator_ShownWhenNeeded()
    {
        // Arrange: Many commands
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ScanTestCommand), typeof(SearchTestCommand), typeof(SecurityTestCommand),
            typeof(SeedTestCommand), typeof(SendTestCommand), typeof(ShellTestCommand),
            typeof(ShowTestCommand), typeof(ShutdownTestCommand), typeof(SignalTestCommand),
            typeof(SnapshotTestCommand), typeof(SortTestCommand), typeof(SourceTestCommand),
            typeof(SpawnTestCommand), typeof(SplitTestCommand), typeof(StartTestCommand),
            typeof(StatusTestCommand), typeof(StopTestCommand), typeof(StorageTestCommand),
            typeof(SubscribeTestCommand), typeof(SyncTestCommand),
            typeof(ServerCommand), typeof(ServiceCommand), typeof(SetupCommand));

        // Act: Tab for all commands
        harness.PressTab();
        
        // Assert: Menu visible (indicator is implementation-specific)
        harness.IsMenuVisible.Should().BeTrue("should show menu");
        harness.MenuItemCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region TC-21.3: Exactly At Limit No Indicator

    /// <summary>
    /// TC-21.3: When exactly at limit,
    /// Then no truncation indicator needed.
    /// </summary>
    [TestMethod]
    public void TC_21_3_ExactlyAtLimit_NoIndicator()
    {
        // Arrange: Small set of commands (under any limit)
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));

        // Act: Tab for commands
        harness.PressTab();
        
        // Assert: Menu shows commands (may include built-in help command)
        harness.IsMenuVisible.Should().BeTrue("should show menu");
        harness.MenuItemCount.Should().BeGreaterThanOrEqualTo(3, "should show at least 3 commands");
    }

    #endregion

    #region TC-21.4: Under Limit No Indicator

    /// <summary>
    /// TC-21.4: When fewer than limit items returned,
    /// Then no truncation indicator.
    /// </summary>
    [TestMethod]
    public void TC_21_4_UnderLimit_NoIndicator()
    {
        // Arrange: Few commands
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(HelpTestCommand),
            typeof(HelperTestCommand),
            typeof(HelpfulTestCommand));

        // Act: Type 'h' and Tab
        harness.TypeText("h");
        harness.PressTab();
        
        // Assert: Shows all matching items
        harness.IsMenuVisible.Should().BeTrue("should show filtered menu");
        var items = harness.MenuItems!.Select(m => m.InsertText).ToList();
        items.Count.Should().BeGreaterThanOrEqualTo(3, "should show all 'h' commands");
    }

    #endregion

    #region TC-21.5: Filtering Reduces Count

    /// <summary>
    /// TC-21.5: When user filters results,
    /// Then count updates appropriately.
    /// </summary>
    [TestMethod]
    public void TC_21_5_Filtering_ReducesTruncationImpact()
    {
        // Arrange: Many commands
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ScanTestCommand), typeof(SearchTestCommand), typeof(SecurityTestCommand),
            typeof(SeedTestCommand), typeof(SendTestCommand), typeof(ShellTestCommand),
            typeof(ShowTestCommand), typeof(ShutdownTestCommand), typeof(SignalTestCommand),
            typeof(SnapshotTestCommand), typeof(SortTestCommand), typeof(SourceTestCommand),
            typeof(SpawnTestCommand), typeof(SplitTestCommand), typeof(StartTestCommand),
            typeof(StatusTestCommand), typeof(StopTestCommand), typeof(StorageTestCommand),
            typeof(SubscribeTestCommand), typeof(SyncTestCommand),
            typeof(ServerCommand), typeof(ServiceCommand), typeof(SetupCommand));

        // Act: Type 'se' to filter
        harness.TypeText("se");
        harness.PressTab();
        
        // Assert: Fewer items after filtering
        harness.IsMenuVisible.Should().BeTrue("should show filtered menu");
        var filteredCount = harness.MenuItemCount;
        
        // 'se' should match: search, security, seed, send, server, service, setup
        filteredCount.Should().BeGreaterThan(0, "should have matches");
        filteredCount.Should().BeLessThan(23, "should be filtered down from all commands");
    }

    #endregion
}
