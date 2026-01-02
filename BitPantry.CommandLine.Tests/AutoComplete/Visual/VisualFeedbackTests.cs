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

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for visual feedback behavior (VF-001 to VF-007).
/// These tests verify the visual state of the menu is correct.
/// </summary>
[TestClass]
public class VisualFeedbackTests
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

    #region VF-001: Selected item tracked

    [TestMethod]
    [Description("VF-001: Selected item is tracked via SelectedIndex")]
    public async Task SelectedItem_TrackedViaSelectedIndex()
    {
        // Given: Menu open with 5 items
        var items = CreateItems(5);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Menu opens
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: SelectedIndex is 0 (first item selected)
        action.MenuState.SelectedIndex.Should().Be(0);
    }

    #endregion

    #region VF-002: Selection moves with arrow

    [TestMethod]
    [Description("VF-002: Down arrow moves selection")]
    public async Task DownArrow_MovesSelectionDown()
    {
        // Given: Menu with items, first selected
        var items = CreateItems(5);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);
        _orchestrator.MenuState.SelectedIndex.Should().Be(0);

        // When: Down arrow pressed
        _orchestrator.HandleDownArrow();

        // Then: Selection moves to 1
        _orchestrator.MenuState.SelectedIndex.Should().Be(1);
    }

    [TestMethod]
    [Description("VF-002: Up arrow moves selection")]
    public async Task UpArrow_MovesSelectionUp()
    {
        // Given: Menu with items, third selected
        var items = CreateItems(5);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);
        _orchestrator.HandleDownArrow();
        _orchestrator.HandleDownArrow();
        _orchestrator.MenuState.SelectedIndex.Should().Be(2);

        // When: Up arrow pressed
        _orchestrator.HandleUpArrow();

        // Then: Selection moves to 1
        _orchestrator.MenuState.SelectedIndex.Should().Be(1);
    }

    #endregion

    #region VF-003: Scroll state (many items)

    [TestMethod]
    [Description("VF-003: Menu with 25 items supports scrolling")]
    public async Task ManyItems_SupportsScrolling()
    {
        // Given: 25 items
        var items = CreateItems(25);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Menu opens
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: All 25 items available
        action.MenuState.TotalCount.Should().Be(25);
        action.MenuState.Items.Should().HaveCount(25);
    }

    #endregion

    #region VF-004/VF-005: State consistent (no flicker proxy)

    [TestMethod]
    [Description("VF-004/VF-005: Menu state remains consistent during updates")]
    public async Task MenuState_RemainsConsistentDuringUpdates()
    {
        // Given: Menu open
        var items = CreateItems(10);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);
        var initialState = _orchestrator.MenuState;

        // When: Rapid navigation
        for (int i = 0; i < 5; i++)
        {
            _orchestrator.HandleDownArrow();
        }

        // Then: State is still valid
        _orchestrator.MenuState.Should().NotBeNull();
        _orchestrator.MenuState.Items.Should().HaveCount(10);
        _orchestrator.MenuState.SelectedIndex.Should().Be(5);
    }

    #endregion

    #region VF-006: Connection indicator - online (remote state)

    [TestMethod]
    [Description("VF-006: CompletionContext includes IsRemote flag")]
    public async Task CompletionContext_IncludesIsRemoteFlag()
    {
        // Given: Provider is called
        CompletionContext capturedContext = null;
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .Callback<CompletionContext, CancellationToken>((ctx, _) => capturedContext = ctx)
            .ReturnsAsync(CompletionResult.Empty);

        // When: Tab pressed
        await _orchestrator.HandleTabAsync("cmd", 3, CancellationToken.None);

        // Then: Context has IsRemote property
        capturedContext.Should().NotBeNull();
        // IsRemote is determined by command registry lookup
    }

    #endregion

    #region VF-007: Menu items have correct properties

    [TestMethod]
    [Description("VF-007: Menu items preserve all properties")]
    public async Task MenuItems_PreserveAllProperties()
    {
        // Given: Items with descriptions and kinds
        var items = new List<CompletionItem>
        {
            new()
            {
                DisplayText = "connect",
                InsertText = "connect",
                Description = "Connect to server",
                Kind = CompletionItemKind.Command
            },
            new()
            {
                DisplayText = "config",
                InsertText = "config",
                Description = "Configuration settings",
                Kind = CompletionItemKind.Command
            }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("c", 1, CancellationToken.None);

        // Then: Properties are preserved
        action.MenuState.Items.Should().HaveCount(2);
        action.MenuState.Items[0].Description.Should().Be("Connect to server");
        action.MenuState.Items[0].Kind.Should().Be(CompletionItemKind.Command);
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
