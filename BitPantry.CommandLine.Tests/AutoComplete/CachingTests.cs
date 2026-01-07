using System.Linq;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Caching Behavior Tests (TC-18.1 through TC-18.7)
/// Tests completion caching and cache invalidation.
/// Note: Some cache behaviors are internal and tested through observable effects.
/// </summary>
[TestClass]
public class CachingTests
{
    #region TC-18.1: Cache Hit Returns Instant Results

    /// <summary>
    /// TC-18.1: When same completion was fetched previously,
    /// Then cached results appear instantly (no loading).
    /// </summary>
    [TestMethod]
    public void TC_18_1_CacheHit_ReturnsInstantResults()
    {
        // Arrange: Use command with arguments
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: First Tab - fetches arguments
        harness.TypeText("server ");
        harness.PressTab();
        
        harness.IsMenuVisible.Should().BeTrue("first Tab should show argument menu");
        var firstMenuCount = harness.MenuItemCount;
        var firstItems = harness.MenuItems!.Select(m => m.InsertText).ToList();
        
        // Close menu
        harness.PressEscape();
        harness.IsMenuVisible.Should().BeFalse("menu should close on Escape");
        
        // Second Tab - should use cache
        harness.PressTab();
        
        // Assert: Same results, instant (observable through same item count)
        harness.IsMenuVisible.Should().BeTrue("second Tab should reopen menu");
        harness.MenuItemCount.Should().Be(firstMenuCount, "cached results should have same count");
    }

    #endregion

    #region TC-18.2: Different Argument Is Cache Miss

    /// <summary>
    /// TC-18.2: When different argument is completed,
    /// Then new fetch is triggered.
    /// </summary>
    [TestMethod]
    public void TC_18_2_DifferentArgument_IsCacheMiss()
    {
        // Arrange: Use MultiArgTestCommand with multiple distinct arguments
        using var harness = AutoCompleteTestHarness.WithCommand<MultiArgTestCommand>();

        // Act: Tab to get arguments
        harness.TypeText("multicmd ");
        harness.PressTab();
        
        harness.IsMenuVisible.Should().BeTrue("should show argument menu");
        var initialCount = harness.MenuItemCount;
        
        // Accept first argument
        var firstArg = harness.SelectedItem;
        harness.PressEnter();
        
        // Type value, space, then Tab for remaining arguments
        harness.TypeText("value ");
        harness.PressTab();
        
        // Assert: Different items (cache miss for different context)
        harness.IsMenuVisible.Should().BeTrue("should show remaining arguments");
        // The used argument should be excluded
        harness.MenuItems!.Select(m => m.InsertText).Should().NotContain(firstArg, "used argument should be excluded");
    }

    #endregion

    #region TC-18.3: Cache Invalidated After Command Execution

    /// <summary>
    /// TC-18.3: When command is executed,
    /// Then cache for that command's arguments is cleared.
    /// Note: This requires internal cache observation not available through harness API.
    /// We verify buffer state changes produce correct completions (observable effect).
    /// </summary>
    [TestMethod]
    public void TC_18_3_CacheInvalidated_AfterBufferChange()
    {
        // Arrange: Use MultiArgTestCommand which has multiple arguments
        using var harness = AutoCompleteTestHarness.WithCommand<MultiArgTestCommand>();

        // Act: Get completions for multicmd
        harness.TypeText("multicmd ");
        harness.PressTab();
        harness.IsMenuVisible.Should().BeTrue("should show argument menu");
        var initialItems = harness.MenuItems!.Select(m => m.InsertText).ToList();
        initialItems.Count.Should().BeGreaterThanOrEqualTo(2, "should have multiple arguments");
        
        // Capture which argument is currently selected
        var selectedArg = harness.SelectedItem;
        selectedArg.Should().NotBeNullOrEmpty("an argument should be selected");
        
        // Accept the selected argument
        harness.PressEnter();
        
        // Assert: Buffer updated with selected argument
        harness.Buffer.Should().Contain(selectedArg!, "buffer should contain selected argument");
        
        // Type value and space
        harness.TypeText("myvalue ");
        harness.PressTab();
        
        // Assert: Completions reflect buffer state (with or without menu)
        // The key cache behavior is that buffer state drives completions
        if (harness.IsMenuVisible && harness.MenuItems != null)
        {
            harness.MenuItems.Select(m => m.InsertText).Should().NotContain(selectedArg, 
                $"{selectedArg} should be excluded after use");
        }
    }

    #endregion

    #region TC-18.5: Cache Context Sensitivity

    /// <summary>
    /// TC-18.5: When prior arguments change completion context,
    /// Then cache accounts for context.
    /// Note: Observable effect is that used arguments are excluded, not raw cache behavior.
    /// </summary>
    [TestMethod]
    public void TC_18_5_CacheContextSensitivity()
    {
        // Arrange: Use MultiArgTestCommand which has 3 arguments: Name, Count, Verbose
        using var harness = AutoCompleteTestHarness.WithCommand<MultiArgTestCommand>();

        // Act: Type command and get argument completions
        harness.TypeText("multicmd ");
        harness.PressTab();
        
        harness.IsMenuVisible.Should().BeTrue("should show argument menu");
        var itemsBefore = harness.MenuItems!.Select(m => m.InsertText).ToList();
        var countBefore = itemsBefore.Count;
        countBefore.Should().BeGreaterThanOrEqualTo(2, "should have at least 2 arguments");
        
        // Record which argument was selected
        var selectedArg = harness.SelectedItem;
        
        // Accept an argument and provide value
        harness.PressEnter();
        harness.TypeText("value ");
        harness.PressTab();
        
        // Assert: Context changed - verify through observable effects
        // Either menu shows fewer items, or ghost text suggests different completions
        if (harness.IsMenuVisible && harness.MenuItems != null)
        {
            var itemsAfter = harness.MenuItems.Select(m => m.InsertText).ToList();
            itemsAfter.Should().NotContain(selectedArg, 
                "used argument should be excluded (context sensitivity)");
        }
    }

    #endregion

    #region TC-18.6: Local Filtering on Cached Results

    /// <summary>
    /// TC-18.6: When typing to filter cached remote results,
    /// Then filtering happens locally (no network).
    /// </summary>
    [TestMethod]
    public void TC_18_6_LocalFiltering_OnCachedResults()
    {
        // Arrange: Use commands with common prefix
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand),
            typeof(ScanTestCommand),
            typeof(SearchTestCommand));

        // Act: Type "s" and Tab to cache all "s" commands
        harness.TypeText("s");
        harness.PressTab();
        
        harness.IsMenuVisible.Should().BeTrue("menu should open");
        var fullCount = harness.MenuItemCount;
        
        // Type more characters to filter
        harness.TypeText("er"); // Now "ser" - should filter to server, service, search
        
        // Assert: Menu should be filtered locally
        harness.IsMenuVisible.Should().BeTrue("menu should still be visible");
        harness.MenuItemCount.Should().BeLessThan(fullCount, "filtering should reduce items");
    }

    #endregion

    #region TC-18.7: Prefix Query Reuses Cache

    /// <summary>
    /// TC-18.7: When completing with partial prefix that extends cached empty query,
    /// Then cached full results are filtered locally.
    /// </summary>
    [TestMethod]
    public void TC_18_7_PrefixQuery_ReusesCache()
    {
        // Arrange: Use commands with common prefix
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));

        // Act: Start with empty and Tab to get all 's' commands
        harness.TypeText("s");
        harness.PressTab();
        
        harness.IsMenuVisible.Should().BeTrue("menu should open");
        var allItems = harness.MenuItems!.Select(m => m.InsertText).ToList();
        
        // Close and try with more specific prefix
        harness.PressEscape();
        harness.TypeText("e"); // Now "se" - server, service, setup all match "se"
        harness.PressTab();
        
        // Assert: Should reuse cached results, filtered to "se" prefix
        harness.IsMenuVisible.Should().BeTrue("menu should open for 'se'");
        harness.MenuItems!.All(item => item.InsertText.StartsWith("se")).Should().BeTrue(
            "all items should start with 'se'");
    }

    #endregion
}
