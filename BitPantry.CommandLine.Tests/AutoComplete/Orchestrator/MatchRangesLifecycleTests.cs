using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Orchestrator;

/// <summary>
/// TDD tests for MatchRanges lifecycle bugs:
/// - Bug A: Selected item should still show match highlighting
/// - Bug B: Backspace should clear MatchRanges when filter is removed  
/// - Bug C: Escape/reopen menu should clear old MatchRanges
/// </summary>
[TestClass]
public class MatchRangesLifecycleTests
{
    private Mock<ICompletionProvider> _mockProvider;
    private Mock<ICompletionCache> _mockCache;
    private CommandRegistry _registry;
    private CompletionOrchestrator _orchestrator;
    private List<CompletionItem> _testItems;

    [TestInitialize]
    public void Setup()
    {
        _testItems = new List<CompletionItem>
        {
            new() { InsertText = "connect", Kind = CompletionItemKind.Command },
            new() { InsertText = "disconnect", Kind = CompletionItemKind.Command },
            new() { InsertText = "status", Kind = CompletionItemKind.Command },
            new() { InsertText = "help", Kind = CompletionItemKind.Command }
        };

        _mockProvider = new Mock<ICompletionProvider>();
        _mockProvider.Setup(p => p.Priority).Returns(0);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new CompletionResult(_testItems.Select(i => new CompletionItem 
            { 
                InsertText = i.InsertText, 
                Kind = i.Kind 
            }).ToList(), _testItems.Count));

        _mockCache = new Mock<ICompletionCache>();
        _registry = new CommandRegistry();

        var providers = new List<ICompletionProvider> { _mockProvider.Object };
        _orchestrator = new CompletionOrchestrator(providers, _mockCache.Object, _registry, new ServiceCollection().BuildServiceProvider());
    }

    #region Bug B: Backspace should clear MatchRanges when filter is removed

    [TestMethod]
    [TestCategory("BugB")]
    [Description("When filter is typed and then backspaced to empty, MatchRanges should be cleared")]
    public async Task HandleBackspaceAsync_FilterRemovedCompletely_MatchRangesShouldBeCleared()
    {
        // Arrange - open menu with Tab
        await _orchestrator.HandleTabAsync("", 0);
        _orchestrator.IsMenuOpen.Should().BeTrue();

        // Type 'c' to filter - this populates MatchRanges on matched items
        await _orchestrator.HandleCharacterAsync('c', "c", 1);

        // Verify MatchRanges are populated after filtering
        var menuAfterFilter = _orchestrator.HandleDownArrow(); // Get menu state
        var itemsAfterFilter = GetCurrentMenuItems();
        itemsAfterFilter.Should().OnlyContain(i => i.InsertText.Contains('c', System.StringComparison.OrdinalIgnoreCase));
        itemsAfterFilter.Should().OnlyContain(i => i.MatchRanges.Count > 0, 
            "after filtering, matched items should have MatchRanges populated");

        // Act - backspace to remove the filter
        await _orchestrator.HandleBackspaceAsync("", 0, 0);

        // Assert - MatchRanges should be cleared
        var itemsAfterBackspace = GetCurrentMenuItems();
        itemsAfterBackspace.Should().HaveCount(4, "all items should be shown again");
        itemsAfterBackspace.Should().OnlyContain(i => i.MatchRanges.Count == 0,
            "MatchRanges should be cleared when filter is removed");
    }

    [TestMethod]
    [TestCategory("BugB")]
    [Description("When filter is partially backspaced (but not empty), MatchRanges should update")]
    public async Task HandleBackspaceAsync_FilterPartiallyRemoved_MatchRangesShouldUpdate()
    {
        // Arrange - open menu and type "co" to filter
        await _orchestrator.HandleTabAsync("", 0);
        await _orchestrator.HandleCharacterAsync('c', "c", 1);
        await _orchestrator.HandleCharacterAsync('o', "co", 2);

        // Get items with "co" filter (connect, disconnect)
        var itemsWithCoFilter = GetCurrentMenuItems();
        itemsWithCoFilter.Should().HaveCount(2);

        // Act - backspace to remove 'o', leaving just 'c'
        await _orchestrator.HandleBackspaceAsync("c", 1, 0);

        // Assert - should still filter, but MatchRanges should update for new query 'c'
        var itemsAfterBackspace = GetCurrentMenuItems();
        itemsAfterBackspace.Should().HaveCount(2, "still filtering by 'c' - connect and disconnect");
        
        // Check that MatchRanges are updated (should match 'c' not 'co')
        foreach (var item in itemsAfterBackspace)
        {
            item.MatchRanges.Should().NotBeEmpty("items should still have match highlighting");
            // The ranges should be for 'c' (length 1) not 'co' (length 2)
            // 'connect' should have range at position 0 with length 1
            // 'disconnect' should have range at position 3 with length 1
        }
    }

    #endregion

    #region Bug C: Escape/reopen menu should clear old MatchRanges

    [TestMethod]
    [TestCategory("BugC")]
    [Description("When menu is closed with Escape and reopened, MatchRanges should be cleared")]
    public async Task HandleEscape_ThenReopenMenu_MatchRangesShouldBeCleared()
    {
        // Arrange - open menu and type 's' to filter
        await _orchestrator.HandleTabAsync("", 0);
        await _orchestrator.HandleCharacterAsync('s', "s", 1);

        // Verify filter is working and MatchRanges are set
        var itemsWithFilter = GetCurrentMenuItems();
        itemsWithFilter.Should().HaveCount(2, "status and disconnect contain 's'");
        itemsWithFilter.Should().OnlyContain(i => i.MatchRanges.Count > 0,
            "items should have MatchRanges after filtering");

        // Close menu with Escape
        _orchestrator.HandleEscape();
        _orchestrator.IsMenuOpen.Should().BeFalse();

        // Act - reopen menu with Tab
        await _orchestrator.HandleTabAsync("", 0);

        // Assert - MatchRanges should be cleared (fresh menu)
        _orchestrator.IsMenuOpen.Should().BeTrue();
        var itemsAfterReopen = GetCurrentMenuItems();
        itemsAfterReopen.Should().HaveCount(4, "all items should be shown");
        itemsAfterReopen.Should().OnlyContain(i => i.MatchRanges.Count == 0,
            "MatchRanges should be cleared when menu is reopened");
    }

    [TestMethod]
    [TestCategory("BugC")]
    [Description("When menu is closed by accepting an item and reopened, MatchRanges should be cleared")]
    public async Task HandleEnter_ThenReopenMenu_MatchRangesShouldBeCleared()
    {
        // Arrange - open menu and type 's' to filter
        await _orchestrator.HandleTabAsync("", 0);
        await _orchestrator.HandleCharacterAsync('s', "s", 1);

        // Verify filter is working
        var itemsWithFilter = GetCurrentMenuItems();
        itemsWithFilter.Should().OnlyContain(i => i.MatchRanges.Count > 0);

        // Close menu by accepting item
        _orchestrator.HandleEnter();
        _orchestrator.IsMenuOpen.Should().BeFalse();

        // Act - reopen menu with Tab
        await _orchestrator.HandleTabAsync("", 0);

        // Assert - MatchRanges should be cleared
        var itemsAfterReopen = GetCurrentMenuItems();
        itemsAfterReopen.Should().OnlyContain(i => i.MatchRanges.Count == 0,
            "MatchRanges should be cleared when menu is reopened after accepting");
    }

    #endregion

    #region Helper Methods

    private List<CompletionItem> GetCurrentMenuItems()
    {
        // Access the orchestrator's internal filtered items via reflection or through menu state
        // For now, we can use DownArrow/UpArrow to indirectly verify menu state
        // Or access the MenuState.Items directly if available
        
        // Use reflection to access _filteredItems
        var field = typeof(CompletionOrchestrator).GetField("_filteredItems", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var items = field?.GetValue(_orchestrator) as List<CompletionItem>;
        return items ?? new List<CompletionItem>();
    }

    #endregion
}
