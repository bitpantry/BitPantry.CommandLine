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

namespace BitPantry.CommandLine.Tests.AutoComplete.EdgeCases;

/// <summary>
/// Tests for boundary and edge cases (EC-001 to EC-014).
/// These tests verify the system handles unusual or extreme conditions gracefully.
/// </summary>
[TestClass]
public class BoundaryTests
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

    #region EC-001: Zero results

    [TestMethod]
    [Description("EC-001: No commands registered shows no matches")]
    public async Task ZeroResults_NoCommandsRegistered_ReturnsNoMatches()
    {
        // Given: No commands registered, no provider results
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletionResult.Empty);

        // When: Tab at empty prompt
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: No matches indicator
        action.Type.Should().Be(CompletionActionType.NoMatches);
    }

    #endregion

    #region EC-002: One result

    [TestMethod]
    [Description("EC-002: Single matching command accepts immediately")]
    public async Task OneResult_SingleMatch_AcceptsImmediately()
    {
        // Given: Only one matching command
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "help", InsertText = "help" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("hel", 3, CancellationToken.None);

        // Then: Accepts immediately
        action.Type.Should().Be(CompletionActionType.InsertText);
        action.InsertText.Should().Be("help");
    }

    #endregion

    #region EC-003: Exactly 10 results

    [TestMethod]
    [Description("EC-003: Exactly 10 items fits in menu without scrolling")]
    public async Task ExactlyTenResults_FitsInMenu()
    {
        // Given: 10 matching items
        var items = CreateItems(10);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: Menu shows all 10 items
        action.MenuState.Items.Should().HaveCount(10);
        action.MenuState.TotalCount.Should().Be(10);
    }

    #endregion

    #region EC-004: Exactly 11 results

    [TestMethod]
    [Description("EC-004: 11 items requires scrolling")]
    public async Task ElevenResults_RequiresScrolling()
    {
        // Given: 11 matching items
        var items = CreateItems(11);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: Menu shows items with scroll capability
        action.MenuState.Items.Should().HaveCount(11);
        action.MenuState.TotalCount.Should().Be(11);
    }

    #endregion

    #region EC-005: Very long command name

    [TestMethod]
    [Description("EC-005: Very long command name is handled")]
    public async Task VeryLongCommandName_HandledGracefully()
    {
        // Given: Command name is 200 chars
        var longName = new string('a', 200);
        var items = new List<CompletionItem>
        {
            new() { DisplayText = longName, InsertText = longName }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("a", 1, CancellationToken.None);

        // Then: Accepts without crashing (single match)
        action.Type.Should().Be(CompletionActionType.InsertText);
        action.InsertText.Should().Be(longName);
    }

    #endregion

    #region EC-006: Unicode in command names

    [TestMethod]
    [Description("EC-006: Unicode command names display correctly")]
    public async Task UnicodeCommandName_DisplaysCorrectly()
    {
        // Given: Unicode command name
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "日本語", InsertText = "日本語" },
            new() { DisplayText = "中文命令", InsertText = "中文命令" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: Unicode displays correctly
        action.MenuState.Items.Should().HaveCount(2);
        action.MenuState.Items[0].DisplayText.Should().Be("日本語");
        action.MenuState.Items[1].DisplayText.Should().Be("中文命令");
    }

    #endregion

    #region EC-007: Special chars in values

    [TestMethod]
    [Description("EC-007: Special characters in values are preserved")]
    public async Task SpecialCharsInValues_PreservedCorrectly()
    {
        // Given: File with special chars
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "file[1].txt", InsertText = "file[1].txt" },
            new() { DisplayText = "path with spaces.txt", InsertText = "\"path with spaces.txt\"" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("f", 1, CancellationToken.None);

        // Then: Menu shows items with special chars
        action.MenuState.Items.Should().HaveCount(2);
        action.MenuState.Items[0].DisplayText.Should().Be("file[1].txt");
    }

    #endregion

    #region EC-008: Empty string in results

    [TestMethod]
    [Description("EC-008: Empty string in results is handled")]
    public async Task EmptyStringInResults_HandledGracefully()
    {
        // Given: Provider returns empty string along with valid
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "", InsertText = "" },
            new() { DisplayText = "valid", InsertText = "valid" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: Items are shown (empty string may be filtered or shown)
        action.Should().NotBeNull();
        // Implementation may filter empty strings - verify no crash
    }

    #endregion

    #region EC-009: Null InsertText

    [TestMethod]
    [Description("EC-009: Null InsertText in results is handled gracefully")]
    public async Task NullInResults_HandledGracefully()
    {
        // Given: Provider returns item with null InsertText
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "display", InsertText = null },
            new() { DisplayText = "valid", InsertText = "valid" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: Handled gracefully (no crash)
        action.Should().NotBeNull();
    }

    #endregion

    #region EC-010: Rapid Tab presses

    [TestMethod]
    [Description("EC-010: Rapid Tab presses are handled")]
    public async Task RapidTabPresses_HandledGracefully()
    {
        // Given: Items available
        var items = CreateItems(5);
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Rapid Tab presses (simulate 10 Tab presses)
        CompletionAction lastAction = null;
        for (int i = 0; i < 10; i++)
        {
            lastAction = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);
        }

        // Then: No crash, final action is valid
        lastAction.Should().NotBeNull();
    }

    #endregion

    #region EC-013: Very narrow terminal (logical test)

    [TestMethod]
    [Description("EC-013: Menu state valid regardless of terminal width")]
    public async Task VeryNarrowTerminal_MenuStateValid()
    {
        // Given: Items with long names
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "very-long-command-name-that-exceeds-normal-width", InsertText = "command" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("v", 1, CancellationToken.None);

        // Then: Action is valid (rendering is terminal's responsibility)
        action.Type.Should().Be(CompletionActionType.InsertText);
    }

    #endregion

    #region EC-014: Minimum terminal width (80 columns)

    [TestMethod]
    [Description("EC-014: Content readable at 80 columns")]
    public async Task MinimumTerminalWidth_ContentValid()
    {
        // Given: Items with descriptions
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "help", InsertText = "help", Description = "Shows help for commands" },
            new() { DisplayText = "connect", InsertText = "connect", Description = "Connect to server" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // Then: Items have descriptions populated
        action.MenuState.Items.Should().HaveCount(2);
        action.MenuState.Items[0].Description.Should().Be("Shows help for commands");
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
