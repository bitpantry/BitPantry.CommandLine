using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Orchestrator;

/// <summary>
/// Tests for <see cref="CompletionOrchestrator"/> - MC-001 to MC-012 basic menu behavior.
/// </summary>
[TestClass]
public class OrchestratorTests
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
        _orchestrator = new CompletionOrchestrator(providers, _mockCache.Object, _registry, new ServiceCollection().BuildServiceProvider());
    }

    #region MC-001: Tab opens menu at empty prompt

    [TestMethod]
    [Description("MC-001: Tab at empty prompt opens menu with all top-level commands/groups")]
    public async Task HandleTabAsync_EmptyInput_ShowsAllCommands()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "help", Kind = CompletionItemKind.Command },
            new() { InsertText = "connect", Kind = CompletionItemKind.Command }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var action = await _orchestrator.HandleTabAsync("", 0);

        // Assert
        action.Type.Should().Be(CompletionActionType.OpenMenu);
        action.MenuState.Should().NotBeNull();
        action.MenuState.Items.Should().HaveCount(2);
    }

    #endregion

    #region MC-002: Tab opens menu with partial input

    [TestMethod]
    [Description("MC-002: Tab with partial input shows matching commands")]
    public async Task HandleTabAsync_PartialInput_ShowsFilteredCommands()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "help", Kind = CompletionItemKind.Command }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var action = await _orchestrator.HandleTabAsync("hel", 3);

        // Assert
        action.Type.Should().Be(CompletionActionType.InsertText);
        action.InsertText.Should().Be("help");
    }

    [TestMethod]
    [Description("MC-002: Tab with partial input shows multiple matches in menu")]
    public async Task HandleTabAsync_PartialInput_MultipleMatches_ShowsMenu()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "help", Kind = CompletionItemKind.Command },
            new() { InsertText = "history", Kind = CompletionItemKind.Command }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var action = await _orchestrator.HandleTabAsync("h", 1);

        // Assert
        action.Type.Should().Be(CompletionActionType.OpenMenu);
        action.MenuState.Items.Should().HaveCount(2);
    }

    #endregion

    #region MC-003/MC-004: Down/Up arrow navigates

    [TestMethod]
    [Description("MC-003: Down arrow navigates to next item")]
    public async Task HandleDownArrow_NavigatesToNextItem()
    {
        // Arrange - open menu first
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" },
            new() { InsertText = "item3" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        _orchestrator.MenuState.SelectedIndex.Should().Be(0);

        // Act
        var action = _orchestrator.HandleDownArrow();

        // Assert
        action.Type.Should().Be(CompletionActionType.SelectionChanged);
        action.MenuState.SelectedIndex.Should().Be(1);
    }

    [TestMethod]
    [Description("MC-004: Up arrow navigates to previous item")]
    public async Task HandleUpArrow_NavigatesToPreviousItem()
    {
        // Arrange - open menu and move down first
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" },
            new() { InsertText = "item3" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        _orchestrator.HandleDownArrow(); // Move to index 1
        _orchestrator.MenuState.SelectedIndex.Should().Be(1);

        // Act
        var action = _orchestrator.HandleUpArrow();

        // Assert
        action.Type.Should().Be(CompletionActionType.SelectionChanged);
        action.MenuState.SelectedIndex.Should().Be(0);
    }

    #endregion

    #region MC-005/MC-006: Arrow key wrapping

    [TestMethod]
    [Description("MC-005: Down arrow at bottom wraps to first item")]
    public async Task HandleDownArrow_AtBottom_WrapsToTop()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" },
            new() { InsertText = "item3" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        _orchestrator.HandleDownArrow(); // 1
        _orchestrator.HandleDownArrow(); // 2 (last)

        // Act
        var action = _orchestrator.HandleDownArrow();

        // Assert
        action.Type.Should().Be(CompletionActionType.SelectionChanged);
        action.MenuState.SelectedIndex.Should().Be(0);
    }

    [TestMethod]
    [Description("MC-006: Up arrow at top wraps to last item")]
    public async Task HandleUpArrow_AtTop_WrapsToBottom()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" },
            new() { InsertText = "item3" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        _orchestrator.MenuState.SelectedIndex.Should().Be(0);

        // Act
        var action = _orchestrator.HandleUpArrow();

        // Assert
        action.Type.Should().Be(CompletionActionType.SelectionChanged);
        action.MenuState.SelectedIndex.Should().Be(2);
    }

    #endregion

    #region MC-007: Enter accepts selection

    [TestMethod]
    [Description("MC-007: Enter accepts currently selected item")]
    public async Task HandleEnter_AcceptsSelectedItem()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "connect" },
            new() { InsertText = "configure" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("con", 3);

        // Act
        var action = _orchestrator.HandleEnter();

        // Assert
        action.Type.Should().Be(CompletionActionType.InsertText);
        action.InsertText.Should().Be("connect");
        _orchestrator.IsMenuOpen.Should().BeFalse();
    }

    #endregion

    #region MC-008: Escape cancels without change

    [TestMethod]
    [Description("MC-008: Escape closes menu without accepting")]
    public async Task HandleEscape_ClosesMenuWithoutAccepting()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "connect" },
            new() { InsertText = "configure" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("con", 3);
        _orchestrator.IsMenuOpen.Should().BeTrue();

        // Act
        var action = _orchestrator.HandleEscape();

        // Assert
        action.Type.Should().Be(CompletionActionType.CloseMenu);
        action.InsertText.Should().BeNull();
        _orchestrator.IsMenuOpen.Should().BeFalse();
    }

    #endregion

    #region MC-009: Typing filters menu

    [TestMethod]
    [Description("MC-009: Typing while menu open filters results")]
    public async Task HandleCharacterAsync_FiltersMenuResults()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "server" },
            new() { InsertText = "silent" },
            new() { InsertText = "status" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        _orchestrator.MenuState.Items.Should().HaveCount(3);

        // Act - type 's' to filter to 's' prefixed items, then 'e' to filter to 'se'
        await _orchestrator.HandleCharacterAsync('s', "s", 1);
        var action = await _orchestrator.HandleCharacterAsync('e', "se", 2);

        // Assert
        action.Type.Should().Be(CompletionActionType.SelectionChanged);
        action.MenuState.Items.Should().HaveCount(1);
        action.MenuState.Items[0].InsertText.Should().Be("server");
    }

    #endregion

    #region MC-011: Tab with no matches

    [TestMethod]
    [Description("MC-011: Tab with no matching commands shows no matches")]
    public async Task HandleTabAsync_NoMatches_ShowsNoMatchesIndicator()
    {
        // Arrange
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletionResult.Empty);

        // Act
        var action = await _orchestrator.HandleTabAsync("xyznonexistent", 14);

        // Assert
        action.Type.Should().Be(CompletionActionType.NoMatches);
    }

    #endregion

    #region MC-012: Tab on already complete command (single result auto-inserts)

    [TestMethod]
    [Description("MC-012: Tab with single matching result auto-inserts without showing menu")]
    public async Task HandleTabAsync_SingleResult_AutoInsertsImmediately()
    {
        // MC-012 Implementation Note: When there's only ONE matching result,
        // we auto-insert rather than showing a menu with a single item.
        // This provides a faster UX. When multiple items match (even if one is exact),
        // the menu is shown (see MR-003 test).
        
        // Arrange - Only one item matches
        var items = new List<CompletionItem>
        {
            new() { InsertText = "help" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var action = await _orchestrator.HandleTabAsync("help", 4);

        // Assert - Single result is auto-inserted
        action.Type.Should().Be(CompletionActionType.InsertText);
        action.InsertText.Should().Be("help");
    }

    #endregion

    #region Menu Not Open Edge Cases

    [TestMethod]
    [Description("Escape when menu not open returns None")]
    public void HandleEscape_MenuNotOpen_ReturnsNone()
    {
        // Act
        var action = _orchestrator.HandleEscape();

        // Assert
        action.Type.Should().Be(CompletionActionType.None);
    }

    [TestMethod]
    [Description("Enter when menu not open returns None")]
    public void HandleEnter_MenuNotOpen_ReturnsNone()
    {
        // Act
        var action = _orchestrator.HandleEnter();

        // Assert
        action.Type.Should().Be(CompletionActionType.None);
    }

    [TestMethod]
    [Description("Down arrow when menu not open returns None")]
    public void HandleDownArrow_MenuNotOpen_ReturnsNone()
    {
        // Act
        var action = _orchestrator.HandleDownArrow();

        // Assert
        action.Type.Should().Be(CompletionActionType.None);
    }

    [TestMethod]
    [Description("Up arrow when menu not open returns None")]
    public void HandleUpArrow_MenuNotOpen_ReturnsNone()
    {
        // Act
        var action = _orchestrator.HandleUpArrow();

        // Assert
        action.Type.Should().Be(CompletionActionType.None);
    }

    #endregion

    #region Cache Behavior

    [TestMethod]
    [Description("Tab uses cached results when available")]
    public async Task HandleTabAsync_UsesCachedResults()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "cached-command" }
        };
        var cachedResult = new CompletionResult(items, items.Count);
        
        _mockCache.Setup(c => c.Get(It.IsAny<CacheKey>())).Returns(cachedResult);

        // Act
        var action = await _orchestrator.HandleTabAsync("cac", 3);

        // Assert
        action.Type.Should().Be(CompletionActionType.InsertText);
        action.InsertText.Should().Be("cached-command");
        _mockProvider.Verify(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    [Description("Results are cached after fetch")]
    public async Task HandleTabAsync_CachesResults()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "new-command" },
            new() { InsertText = "new-config" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        _mockCache.Setup(c => c.Get(It.IsAny<CacheKey>())).Returns((CompletionResult)null);

        // Act
        await _orchestrator.HandleTabAsync("new", 3);

        // Assert
        _mockCache.Verify(c => c.Set(It.IsAny<CacheKey>(), It.IsAny<CompletionResult>()), Times.Once);
    }

    #endregion

    #region Tab While Menu Open

    [TestMethod]
    [Description("Tab while menu open moves to next item")]
    public async Task HandleTabAsync_MenuOpen_MovesToNextItem()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" },
            new() { InsertText = "item3" }
        };
        var result = new CompletionResult(items, items.Count);
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider.Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _orchestrator.HandleTabAsync("", 0);
        _orchestrator.MenuState.SelectedIndex.Should().Be(0);

        // Act
        var action = await _orchestrator.HandleTabAsync("", 0);

        // Assert
        action.Type.Should().Be(CompletionActionType.SelectionChanged);
        action.MenuState.SelectedIndex.Should().Be(1);
    }

    #endregion
}
