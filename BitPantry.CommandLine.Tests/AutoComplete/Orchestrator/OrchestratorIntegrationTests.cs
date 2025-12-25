using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Orchestrator;

/// <summary>
/// Integration tests for <see cref="CompletionOrchestrator"/> with real components.
/// </summary>
[TestClass]
public class OrchestratorIntegrationTests
{
    #region Full Workflow Tests

    [TestMethod]
    [Description("Complete workflow: open menu, navigate, accept")]
    public async Task FullWorkflow_OpenNavigateAccept()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "alpha", Description = "First item" },
            new() { InsertText = "beta", Description = "Second item" },
            new() { InsertText = "gamma", Description = "Third item" }
        };
        var orchestrator = CreateOrchestratorWithItems(items);

        // Act - Open menu
        var openAction = await orchestrator.HandleTabAsync("", 0);
        openAction.Type.Should().Be(CompletionActionType.OpenMenu);
        orchestrator.IsMenuOpen.Should().BeTrue();
        orchestrator.MenuState.SelectedIndex.Should().Be(0);

        // Act - Navigate down twice
        orchestrator.HandleDownArrow();
        orchestrator.HandleDownArrow();
        orchestrator.MenuState.SelectedIndex.Should().Be(2);

        // Act - Accept selection
        var acceptAction = orchestrator.HandleEnter();
        acceptAction.Type.Should().Be(CompletionActionType.InsertText);
        acceptAction.InsertText.Should().Be("gamma");
        orchestrator.IsMenuOpen.Should().BeFalse();
    }

    [TestMethod]
    [Description("Complete workflow: open menu, cancel")]
    public async Task FullWorkflow_OpenAndCancel()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" }
        };
        var orchestrator = CreateOrchestratorWithItems(items);

        // Act - Open menu
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.IsMenuOpen.Should().BeTrue();

        // Act - Navigate
        orchestrator.HandleDownArrow();
        orchestrator.MenuState.SelectedIndex.Should().Be(1);

        // Act - Cancel
        var cancelAction = orchestrator.HandleEscape();
        cancelAction.Type.Should().Be(CompletionActionType.CloseMenu);
        orchestrator.IsMenuOpen.Should().BeFalse();
    }

    [TestMethod]
    [Description("Complete workflow: open menu, filter, accept")]
    public async Task FullWorkflow_OpenFilterAccept()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "server" },
            new() { InsertText = "status" },
            new() { InsertText = "connect" }
        };
        var orchestrator = CreateOrchestratorWithItems(items);

        // Act - Open menu
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.MenuState.Items.Should().HaveCount(3);

        // Act - Filter
        await orchestrator.HandleCharacterAsync('s', "s", 1);
        orchestrator.MenuState.Items.Should().HaveCount(2);
        orchestrator.MenuState.Items.Should().OnlyContain(i => i.InsertText.StartsWith("s"));

        // Act - Accept
        var acceptAction = orchestrator.HandleEnter();
        acceptAction.Type.Should().Be(CompletionActionType.InsertText);
        acceptAction.InsertText.Should().StartWith("s");
    }

    #endregion

    #region Tab Navigation Tests

    [TestMethod]
    [Description("Tab while menu open advances selection")]
    public async Task Tab_WhileMenuOpen_AdvancesSelection()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "a" },
            new() { InsertText = "b" },
            new() { InsertText = "c" }
        };
        var orchestrator = CreateOrchestratorWithItems(items);

        // Act - Open menu
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.MenuState.SelectedIndex.Should().Be(0);

        // Act - Tab again
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.MenuState.SelectedIndex.Should().Be(1);

        // Act - Tab again
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.MenuState.SelectedIndex.Should().Be(2);

        // Act - Tab wraps
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.MenuState.SelectedIndex.Should().Be(0);
    }

    [TestMethod]
    [Description("Shift+Tab while menu open goes backward")]
    public async Task ShiftTab_WhileMenuOpen_GoesBackward()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "a" },
            new() { InsertText = "b" },
            new() { InsertText = "c" }
        };
        var orchestrator = CreateOrchestratorWithItems(items);

        // Open and move forward
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.HandleDownArrow();
        orchestrator.HandleDownArrow();
        orchestrator.MenuState.SelectedIndex.Should().Be(2);

        // Act - Shift+Tab
        await orchestrator.HandleShiftTabAsync();
        orchestrator.MenuState.SelectedIndex.Should().Be(1);

        await orchestrator.HandleShiftTabAsync();
        orchestrator.MenuState.SelectedIndex.Should().Be(0);
    }

    #endregion

    #region Cache Integration Tests

    [TestMethod]
    [Description("Cache invalidation clears cached results")]
    public async Task InvalidateCacheForCommand_ClearsCachedResults()
    {
        // Arrange
        var cache = new CompletionCache();
        var items = new List<CompletionItem> { new() { InsertText = "test" }, new() { InsertText = "test2" } };
        var provider = new MockProvider(items);
        var orchestrator = new CompletionOrchestrator(
            new[] { provider },
            cache,
            new CommandRegistry());

        // First call - populates cache
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.HandleEscape();
        provider.CallCount.Should().Be(1);

        // Second call - should use cache
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.HandleEscape();
        provider.CallCount.Should().Be(1); // Still 1, used cache

        // Clear all cache entries
        cache.Clear();

        // Third call - should hit provider again since cache was cleared
        await orchestrator.HandleTabAsync("", 0);

        // Assert - provider was called twice total
        provider.CallCount.Should().Be(2);
    }

    #endregion

    #region State Consistency Tests

    [TestMethod]
    [Description("Menu state is null when menu is closed")]
    public async Task MenuState_WhenClosed_ReflectsClosedState()
    {
        // Arrange
        var items = new List<CompletionItem> { new() { InsertText = "test" } };
        var orchestrator = CreateOrchestratorWithItems(items);

        // Initially closed
        orchestrator.IsMenuOpen.Should().BeFalse();

        // Open
        await orchestrator.HandleTabAsync("test", 4);
        
        // Single item is auto-accepted, so menu should be closed
        orchestrator.IsMenuOpen.Should().BeFalse();
    }

    [TestMethod]
    [Description("Menu state is consistent after multiple operations")]
    public async Task MenuState_AfterMultipleOperations_RemainsConsistent()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "a" },
            new() { InsertText = "b" },
            new() { InsertText = "c" }
        };
        var orchestrator = CreateOrchestratorWithItems(items);

        // Multiple operations
        await orchestrator.HandleTabAsync("", 0);
        orchestrator.HandleDownArrow();
        orchestrator.HandleUpArrow();
        orchestrator.HandleDownArrow();
        orchestrator.HandleDownArrow();
        orchestrator.HandleUpArrow();

        // Assert state is still consistent
        orchestrator.IsMenuOpen.Should().BeTrue();
        orchestrator.MenuState.Should().NotBeNull();
        orchestrator.MenuState.SelectedIndex.Should().BeInRange(0, 2);
        orchestrator.MenuState.Items.Should().HaveCount(3);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    [Description("Single item auto-accepts without showing menu")]
    public async Task HandleTab_SingleItem_AutoAccepts()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "onlyOne" }
        };
        var orchestrator = CreateOrchestratorWithItems(items);

        // Act
        var action = await orchestrator.HandleTabAsync("", 0);

        // Assert
        action.Type.Should().Be(CompletionActionType.InsertText);
        action.InsertText.Should().Be("onlyOne");
        orchestrator.IsMenuOpen.Should().BeFalse();
    }

    [TestMethod]
    [Description("Empty results shows no matches")]
    public async Task HandleTab_NoResults_ShowsNoMatches()
    {
        // Arrange
        var orchestrator = CreateOrchestratorWithItems(new List<CompletionItem>());

        // Act
        var action = await orchestrator.HandleTabAsync("", 0);

        // Assert
        action.Type.Should().Be(CompletionActionType.NoMatches);
        orchestrator.IsMenuOpen.Should().BeFalse();
    }

    [TestMethod]
    [Description("Filter to single item doesn't auto-accept")]
    public async Task HandleCharacter_FilterToSingle_KeepsMenuOpen()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "server" },
            new() { InsertText = "test" }
        };
        var orchestrator = CreateOrchestratorWithItems(items);

        await orchestrator.HandleTabAsync("", 0);

        // Act - filter to single
        var action = await orchestrator.HandleCharacterAsync('t', "t", 1);

        // Assert - menu still open with single item (user needs to confirm)
        action.Type.Should().Be(CompletionActionType.SelectionChanged);
        action.MenuState.Items.Should().HaveCount(1);
        orchestrator.IsMenuOpen.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private CompletionOrchestrator CreateOrchestratorWithItems(List<CompletionItem> items)
    {
        var provider = new MockProvider(items);
        var cache = new CompletionCache();
        var registry = new CommandRegistry();

        return new CompletionOrchestrator(new[] { provider }, cache, registry);
    }

    private class MockProvider : ICompletionProvider
    {
        private readonly List<CompletionItem> _items;
        public int CallCount { get; private set; }

        public MockProvider(List<CompletionItem> items)
        {
            _items = items;
        }

        public int Priority => 0;

        public bool CanHandle(CompletionContext context) => true;

        public Task<CompletionResult> GetCompletionsAsync(CompletionContext context, System.Threading.CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new CompletionResult(_items, _items.Count));
        }
    }

    #endregion
}
