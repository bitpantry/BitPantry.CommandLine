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

namespace BitPantry.CommandLine.Tests.AutoComplete.Feedback;

/// <summary>
/// Tests for completion feedback indicators (no-matches, count display).
/// Covers User Stories 8 and 9 from the specification.
/// </summary>
[TestClass]
public class FeedbackTests
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

    #region US8: No Matches Indicator

    [TestMethod]
    [Description("US8-AC1: Tab with no matches shows NoMatches action type")]
    public async Task NoMatches_TabWithNoResults_ReturnsNoMatchesAction()
    {
        // Given: No matching commands
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletionResult.Empty);

        // When: User types gibberish and presses Tab
        var action = await _orchestrator.HandleTabAsync("xyzabc123", 9, CancellationToken.None);

        // Then: NoMatches action returned
        action.Type.Should().Be(CompletionActionType.NoMatches);
    }

    [TestMethod]
    [Description("US8-AC2: NoMatches action does not open menu")]
    public async Task NoMatches_DoesNotOpenMenu()
    {
        // Given: No matching commands
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletionResult.Empty);

        // When: User presses Tab
        await _orchestrator.HandleTabAsync("nonexistent", 11, CancellationToken.None);

        // Then: Menu is not open
        _orchestrator.IsMenuOpen.Should().BeFalse();
        _orchestrator.MenuState.Should().BeNull();
    }

    [TestMethod]
    [Description("US8-AC3: Filter to zero results returns NoMatches")]
    public async Task NoMatches_FilterToZeroResults_ReturnsNoMatchesAction()
    {
        // Given: Menu open with items
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "alpha", InsertText = "alpha" },
            new() { DisplayText = "beta", InsertText = "beta" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);
        _orchestrator.IsMenuOpen.Should().BeTrue();

        // When: User types to filter to zero results
        var action = await _orchestrator.HandleCharacterAsync('z', "z", 1, CancellationToken.None);

        // Then: NoMatches returned
        action.Type.Should().Be(CompletionActionType.NoMatches);
    }

    [TestMethod]
    [Description("US8-AC4: NoMatches after menu was open closes menu")]
    public async Task NoMatches_AfterMenuOpen_ClosesMenu()
    {
        // Given: Menu was open with multiple items
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "item1", InsertText = "item1" },
            new() { DisplayText = "item2", InsertText = "item2" },
            new() { DisplayText = "item3", InsertText = "item3" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("i", 1, CancellationToken.None);
        _orchestrator.IsMenuOpen.Should().BeTrue("menu should be open with multiple items");

        // When: Filter to zero matches
        await _orchestrator.HandleCharacterAsync('x', "ix", 2, CancellationToken.None);

        // Then: Menu closed
        _orchestrator.IsMenuOpen.Should().BeFalse();
    }

    #endregion

    #region US9: Count Indicator

    [TestMethod]
    [Description("US9-AC1: Menu shows total count of matches")]
    public async Task CountIndicator_MenuShowsTotalCount()
    {
        // Given: 12 matching options
        var items = CreateItems(12);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Menu opens
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: TotalCount is 12
        action.MenuState.TotalCount.Should().Be(12);
    }

    [TestMethod]
    [Description("US9-AC2: SelectedIndex starts at 0")]
    public async Task CountIndicator_SelectedIndexStartsAtZero()
    {
        // Given: Menu items
        var items = CreateItems(10);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Menu opens
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: Selected index is 0
        action.MenuState.SelectedIndex.Should().Be(0);
    }

    [TestMethod]
    [Description("US9-AC3: Navigation updates SelectedIndex")]
    public async Task CountIndicator_NavigationUpdatesSelectedIndex()
    {
        // Given: Menu with items
        var items = CreateItems(10);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // When: User navigates down
        for (int i = 0; i < 5; i++)
        {
            _orchestrator.HandleDownArrow();
        }

        // Then: SelectedIndex is 5
        _orchestrator.MenuState.SelectedIndex.Should().Be(5);
    }

    [TestMethod]
    [Description("US9-AC4: Count updates when filtering reduces results")]
    public async Task CountIndicator_FilteringReducesCount()
    {
        // Given: 25 items, then filter to 8
        var items = new List<CompletionItem>();
        for (int i = 0; i < 8; i++)
            items.Add(new() { DisplayText = $"server{i}", InsertText = $"server{i}" });
        for (int i = 0; i < 17; i++)
            items.Add(new() { DisplayText = $"config{i}", InsertText = $"config{i}" });
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);
        _orchestrator.MenuState.TotalCount.Should().Be(25);

        // When: Filter to "server"
        var action = await _orchestrator.HandleCharacterAsync('s', "s", 1, CancellationToken.None);

        // Then: Count updates to 8
        action.MenuState.TotalCount.Should().Be(8);
    }

    [TestMethod]
    [Description("US9-AC5: Filtering resets SelectedIndex to 0")]
    public async Task CountIndicator_FilteringResetsSelectedIndex()
    {
        // Given: Menu with selection at index 5
        var items = CreateItems(10);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);
        for (int i = 0; i < 5; i++)
        {
            _orchestrator.HandleDownArrow();
        }
        _orchestrator.MenuState.SelectedIndex.Should().Be(5);

        // When: User types to filter
        var action = await _orchestrator.HandleCharacterAsync('i', "i", 1, CancellationToken.None);

        // Then: SelectedIndex resets to 0
        action.MenuState.SelectedIndex.Should().Be(0);
    }

    #endregion

    #region Result Limiting Indicator

    [TestMethod]
    [Description("RL-001: TotalCount reflects actual result count up to limit")]
    public async Task ResultLimiting_TotalCountReflectsActualCount()
    {
        // Given: Provider returns 50 items
        var items = CreateItems(50);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Menu opens
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: TotalCount is 50
        action.MenuState.TotalCount.Should().Be(50);
    }

    [TestMethod]
    [Description("RL-003: Exactly matching count shows in TotalCount")]
    public async Task ResultLimiting_ExactCountShown()
    {
        // Given: Provider returns 100 items
        var items = CreateItems(100);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Menu opens
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: TotalCount is 100
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
                DisplayText = $"item{i}",
                InsertText = $"item{i}"
            });
        }
        return items;
    }

    #endregion
}
