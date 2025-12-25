using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
/// Tests for menu viewport and scrolling behavior - MC-020 to MC-023.
/// </summary>
[TestClass]
public class MenuViewportTests
{
    private Mock<ICompletionProvider> _mockProvider;
    private Mock<ICompletionCache> _mockCache;
    private CommandRegistry _registry;
    private CompletionOrchestrator _orchestrator;

    [TestInitialize]
    public void Setup()
    {
        _mockProvider = new Mock<ICompletionProvider>();
        _mockProvider.Setup(p => p.Priority).Returns(0);
        
        _mockCache = new Mock<ICompletionCache>();
        _registry = new CommandRegistry();
        
        var providers = new List<ICompletionProvider> { _mockProvider.Object };
        _orchestrator = new CompletionOrchestrator(providers, _mockCache.Object, _registry);
    }

    private List<CompletionItem> CreateItems(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new CompletionItem { InsertText = $"item{i:D2}" })
            .ToList();
    }

    #region MC-020: Viewport shows 10 rows max

    [TestMethod]
    [Description("MC-020: Menu with 25 items shows 10 visible rows")]
    public async Task HandleTabAsync_ManyItems_ViewportLimitedTo10()
    {
        // Arrange
        var items = CreateItems(25);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var action = await _orchestrator.HandleTabAsync("", 0);

        // Assert
        action.Type.Should().Be(CompletionActionType.OpenMenu);
        action.MenuState.Should().NotBeNull();
        action.MenuState.Items.Should().HaveCount(25);
        action.MenuState.MaxVisibleItems.Should().Be(10);
        action.MenuState.ViewportSize.Should().Be(10);
        action.MenuState.TotalCount.Should().Be(25);
    }

    [TestMethod]
    [Description("MC-020: Menu with exactly 10 items shows all")]
    public async Task HandleTabAsync_Exactly10Items_ShowsAll()
    {
        // Arrange
        var items = CreateItems(10);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var action = await _orchestrator.HandleTabAsync("", 0);

        // Assert
        action.MenuState.ViewportSize.Should().Be(10);
        action.MenuState.TotalCount.Should().Be(10);
    }

    [TestMethod]
    [Description("MC-020: Menu with fewer than 10 items shows all")]
    public async Task HandleTabAsync_FewerThan10Items_ShowsAll()
    {
        // Arrange
        var items = CreateItems(5);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var action = await _orchestrator.HandleTabAsync("", 0);

        // Assert
        action.MenuState.ViewportSize.Should().Be(5);
        action.MenuState.TotalCount.Should().Be(5);
    }

    #endregion

    #region MC-021: Scroll down reveals more

    [TestMethod]
    [Description("MC-021: Navigating past visible items scrolls viewport down")]
    public async Task HandleDownArrow_PastViewport_ScrollsDown()
    {
        // Arrange
        var items = CreateItems(25);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        
        // Move down 10 times to go past the viewport
        for (int i = 0; i < 10; i++)
        {
            _orchestrator.HandleDownArrow();
        }

        // Assert
        _orchestrator.MenuState.SelectedIndex.Should().Be(10);
        _orchestrator.MenuState.ViewportStart.Should().BeGreaterThan(0);
    }

    [TestMethod]
    [Description("MC-021: Viewport scrolls to keep selected item visible")]
    public async Task HandleDownArrow_KeepsSelectedItemVisible()
    {
        // Arrange
        var items = CreateItems(25);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        
        // Navigate to item 15
        for (int i = 0; i < 15; i++)
        {
            _orchestrator.HandleDownArrow();
        }

        // Assert
        var state = _orchestrator.MenuState;
        var selectedIsVisible = state.SelectedIndex >= state.ViewportStart && 
                                state.SelectedIndex < state.ViewportStart + state.ViewportSize;
        selectedIsVisible.Should().BeTrue();
    }

    #endregion

    #region MC-022: Scroll up reveals earlier

    [TestMethod]
    [Description("MC-022: Navigating up past viewport scrolls viewport up")]
    public async Task HandleUpArrow_PastViewport_ScrollsUp()
    {
        // Arrange
        var items = CreateItems(25);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        
        // Navigate to middle of list
        for (int i = 0; i < 15; i++)
        {
            _orchestrator.HandleDownArrow();
        }
        
        var initialViewportStart = _orchestrator.MenuState.ViewportStart;

        // Navigate up past current viewport
        for (int i = 0; i < 10; i++)
        {
            _orchestrator.HandleUpArrow();
        }

        // Assert
        _orchestrator.MenuState.ViewportStart.Should().BeLessThan(initialViewportStart);
    }

    [TestMethod]
    [Description("MC-022: Wrapping from top to bottom updates viewport")]
    public async Task HandleUpArrow_AtTop_WrapsToBottomWithCorrectViewport()
    {
        // Arrange
        var items = CreateItems(25);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);

        // Act - press up at top to wrap
        _orchestrator.HandleUpArrow();

        // Assert
        _orchestrator.MenuState.SelectedIndex.Should().Be(24);
        // Viewport should include the last item
        var state = _orchestrator.MenuState;
        var selectedIsVisible = state.SelectedIndex >= state.ViewportStart && 
                                state.SelectedIndex < state.ViewportStart + state.ViewportSize;
        selectedIsVisible.Should().BeTrue();
    }

    #endregion

    #region MC-023: Match count updates

    [TestMethod]
    [Description("MC-023: Filtering reduces total count indicator")]
    public async Task HandleCharacterAsync_FilteringUpdatesTotalCount()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "server" },
            new() { InsertText = "status" },
            new() { InsertText = "silent" },
            new() { InsertText = "connect" },
            new() { InsertText = "config" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        _orchestrator.MenuState.TotalCount.Should().Be(5);

        // Act - filter to only 's' items
        var action = await _orchestrator.HandleCharacterAsync('s', "s", 1);

        // Assert
        action.MenuState.TotalCount.Should().Be(3); // server, status, silent
        action.MenuState.Items.Should().HaveCount(3);
    }

    [TestMethod]
    [Description("MC-023: Selected index resets when filtering")]
    public async Task HandleCharacterAsync_FilteringResetsSelectedIndex()
    {
        // Arrange
        var items = CreateItems(10);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        
        // Move to item 5
        for (int i = 0; i < 5; i++)
        {
            _orchestrator.HandleDownArrow();
        }
        _orchestrator.MenuState.SelectedIndex.Should().Be(5);

        // Act - filter
        var action = await _orchestrator.HandleCharacterAsync('i', "i", 1);

        // Assert
        action.MenuState.SelectedIndex.Should().Be(0);
    }

    [TestMethod]
    [Description("MC-023: Viewport start resets when filtering")]
    public async Task HandleCharacterAsync_FilteringResetsViewportStart()
    {
        // Arrange
        var items = CreateItems(25);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        
        // Scroll down
        for (int i = 0; i < 15; i++)
        {
            _orchestrator.HandleDownArrow();
        }

        // Act - filter
        var action = await _orchestrator.HandleCharacterAsync('i', "i", 1);

        // Assert
        action.MenuState.ViewportStart.Should().Be(0);
    }

    #endregion

    #region Viewport Edge Cases

    [TestMethod]
    [Description("Exactly 11 items should trigger scroll capability")]
    public async Task HandleTabAsync_11Items_HasScrollCapability()
    {
        // Arrange
        var items = CreateItems(11);
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var action = await _orchestrator.HandleTabAsync("", 0);

        // Assert
        action.MenuState.ViewportSize.Should().Be(10);
        action.MenuState.TotalCount.Should().Be(11);
        action.MenuState.Items.Should().HaveCount(11);
    }

    [TestMethod]
    [Description("Empty result after filter closes menu with no matches")]
    public async Task HandleCharacterAsync_FilterToEmpty_ClosesMenu()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "abc" },
            new() { InsertText = "def" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);

        // Act - filter with character that matches nothing
        var action = await _orchestrator.HandleCharacterAsync('x', "x", 1);

        // Assert
        action.Type.Should().Be(CompletionActionType.NoMatches);
        _orchestrator.IsMenuOpen.Should().BeFalse();
    }

    #endregion
}
