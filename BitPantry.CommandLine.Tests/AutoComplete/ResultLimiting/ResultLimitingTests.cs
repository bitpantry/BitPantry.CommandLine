using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;
using Microsoft.Extensions.DependencyInjection;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BitPantry.CommandLine.Tests.AutoComplete.ResultLimiting;

/// <summary>
/// Tests for result limiting behavior (RL-001 to RL-005).
/// Ensures the menu properly handles large result sets.
/// </summary>
[TestClass]
public class ResultLimitingTests
{
    private Mock<ICompletionProvider> _mockProvider;
    private Mock<ICompletionCache> _mockCache;
    private CompletionOrchestrator _orchestrator;
    private CommandRegistry _registry;

    [TestInitialize]
    public void Setup()
    {
        _mockProvider = new Mock<ICompletionProvider>();
        _mockProvider.Setup(p => p.Priority).Returns(100);
        
        _mockCache = new Mock<ICompletionCache>();
        _mockCache.Setup(c => c.Get(It.IsAny<CacheKey>())).Returns((CompletionResult)null);
        
        _registry = new CommandRegistry();
        
        _orchestrator = new CompletionOrchestrator(
            new[] { _mockProvider.Object },
            _mockCache.Object,
            _registry,
            new ServiceCollection().BuildServiceProvider());
    }

    #region RL-001: Maximum 100 results displayed

    [TestMethod]
    [Description("RL-001: When provider returns 150 items, all are available to menu")]
    public async Task LargeResultSet_AllItemsAvailable()
    {
        // The orchestrator doesn't limit results - the UI layer does
        // This test verifies the orchestrator passes through all items
        
        // Given: Provider returns 150 items
        var items = CreateItems(150);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: All 150 items are in state for UI to use
        action.MenuState.Items.Should().HaveCount(150);
        action.MenuState.TotalCount.Should().Be(150);
    }

    #endregion

    #region RL-002: Truncation indicator

    [TestMethod]
    [Description("RL-002: UI can determine if results exceed display limit")]
    public async Task LargeResultSet_TotalCountIndicatesFullSize()
    {
        // Given: Provider returns more items than typical display limit
        var items = CreateItems(120);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: TotalCount indicates full number for UI to show "and N more"
        action.MenuState.TotalCount.Should().Be(120);
    }

    #endregion

    #region RL-003: Scrolling through limited set

    [TestMethod]
    [Description("RL-003: Can scroll through entire result set")]
    public async Task LargeResultSet_CanScrollThroughAll()
    {
        // Given: Menu with 50 items
        var items = CreateItems(50);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // When: Scroll to end
        for (int i = 0; i < 49; i++)
        {
            _orchestrator.HandleDownArrow();
        }

        // Then: At last item
        _orchestrator.MenuState.SelectedIndex.Should().Be(49);
    }

    #endregion

    #region RL-004: Filtering reduces displayed count

    [TestMethod]
    [Description("RL-004: Filtering reduces the count")]
    public async Task Filtering_ReducesCount()
    {
        // Given: 100 items, half starting with 'a', half with 'b'
        var items = new List<CompletionItem>();
        for (int i = 0; i < 50; i++)
        {
            items.Add(new CompletionItem { DisplayText = $"alpha{i}", InsertText = $"alpha{i}" });
        }
        for (int i = 0; i < 50; i++)
        {
            items.Add(new CompletionItem { DisplayText = $"beta{i}", InsertText = $"beta{i}" });
        }
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("a", 1, CancellationToken.None);

        // When: Typing filters to 'al'
        var action = await _orchestrator.HandleCharacterAsync('l', "al", 2, CancellationToken.None);

        // Then: Only 'alpha' items match (50 items)
        action.MenuState.Items.Should().OnlyContain(i => i.DisplayText.StartsWith("alpha"));
        action.MenuState.Items.Should().HaveCount(50);
    }

    #endregion

    #region RL-005: DisplayedCount vs TotalCount

    [TestMethod]
    [Description("RL-005: Menu state includes both displayed and total counts")]
    public async Task MenuState_IncludesBothDisplayedAndTotalCount()
    {
        // Given: 75 items
        var items = CreateItems(75);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: Items count matches total
        action.MenuState.Items.Should().HaveCount(75);
        action.MenuState.TotalCount.Should().Be(75);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    [Description("Empty result set has zero counts")]
    public async Task EmptyResult_HasZeroCounts()
    {
        // Given: No items
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletionResult.Empty);

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("xyz", 3, CancellationToken.None);

        // Then: Zero items
        action.Type.Should().Be(CompletionActionType.NoMatches);
    }

    [TestMethod]
    [Description("Single item auto-accepts")]
    public async Task SingleItem_AutoAccepts()
    {
        // Given: One item
        var items = CreateItems(1);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: Auto-accepts (InsertText for single item)
        action.Type.Should().Be(CompletionActionType.InsertText);
    }

    [TestMethod]
    [Description("Exactly 100 items does not trigger truncation")]
    public async Task Exactly100Items_NoTruncation()
    {
        // Given: Exactly 100 items
        var items = CreateItems(100);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: All 100 available, TotalCount matches
        action.MenuState.Items.Should().HaveCount(100);
        action.MenuState.TotalCount.Should().Be(100);
    }

    #endregion

    #region Helper Methods

    private List<CompletionItem> CreateItems(int count)
    {
        var items = new List<CompletionItem>();
        for (int i = 0; i < count; i++)
        {
            items.Add(new CompletionItem
            {
                DisplayText = $"item{i:D3}",
                InsertText = $"item{i:D3}"
            });
        }
        return items;
    }

    #endregion
}
