using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Commands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BitPantry.CommandLine.Tests.AutoComplete.Cache;

/// <summary>
/// Integration tests for completion caching (CA-001 to CA-006)
/// </summary>
[TestClass]
public class CachingIntegrationTests
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
        _registry = new CommandRegistry();
        
        // Register a test command
        _registry.RegisterCommand<TestCacheCommand>();
        
        _orchestrator = new CompletionOrchestrator(
            new[] { _mockProvider.Object },
            _mockCache.Object,
            _registry);
    }

    #region CA-001: Cache hit - instant results

    [TestMethod]
    public async Task CA001_SecondTab_SameArgument_UsesCachedResults()
    {
        // Given: First Tab populates cache, second Tab uses cached results
        var expectedItems = new List<CompletionItem>
        {
            new() { DisplayText = "dev", InsertText = "dev" },
            new() { DisplayText = "prod", InsertText = "prod" }
        };
        var cachedResult = new CompletionResult(expectedItems);

        // Mock cache to return result on second call
        var callCount = 0;
        _mockCache.Setup(c => c.Get(It.IsAny<CacheKey>()))
            .Returns(() => {
                callCount++;
                return callCount > 1 ? cachedResult : null;
            });
        
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // First Tab - populates cache
        await _orchestrator.HandleTabAsync("test", 4, CancellationToken.None);
        _orchestrator.HandleEscape(); // Close menu

        // When: User presses Tab again
        var secondAction = await _orchestrator.HandleTabAsync("test", 4, CancellationToken.None);

        // Then: Results appear (from cache - provider not called for second Tab)
        _mockProvider.Verify(
            p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()),
            Times.Once, "Provider should only be called once; second call uses cache");
    }

    [TestMethod]
    public async Task CA001_CachedResults_ProviderNotCalled()
    {
        // Given: Cache is pre-populated
        var cachedItems = new List<CompletionItem>
        {
            new() { DisplayText = "cached1", InsertText = "cached1" },
            new() { DisplayText = "cached2", InsertText = "cached2" }
        };
        _mockCache.Setup(c => c.Get(It.IsAny<CacheKey>()))
            .Returns(new CompletionResult(cachedItems));

        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);

        // When: Tab pressed
        var action = await _orchestrator.HandleTabAsync("test", 4, CancellationToken.None);

        // Then: Provider was not called (cache hit)
        _mockProvider.Verify(
            p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
        action.MenuState.Items.Should().HaveCount(2);
    }

    #endregion

    #region CA-002: Different arg = cache miss

    [TestMethod]
    public async Task CA002_DifferentInput_TriggersFetch()
    {
        // Given: Cache returns null for "test" input (cache miss)
        _mockCache.Setup(c => c.Get(It.Is<CacheKey>(k => k.PartialValue == "test")))
            .Returns((CompletionResult)null);

        var items = new List<CompletionItem>
        {
            new() { DisplayText = "testcommand", InsertText = "testcommand" }
        };
        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
        _mockProvider
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(items));

        // When: User presses Tab on "test"
        var action = await _orchestrator.HandleTabAsync("test", 4, CancellationToken.None);

        // Then: New fetch triggered (provider called)
        _mockProvider.Verify(
            p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region CA-003: Cache invalidated on execute

    [TestMethod]
    public void CA003_InvalidateCacheForCommand_ClearsCommandCache()
    {
        // Given: Cache has entries for "testcache" command
        // When: User executes "testcache" command (simulated by invalidation call)
        _orchestrator.InvalidateCacheForCommand("testcache");

        // Then: Cache.InvalidateForCommand was called
        _mockCache.Verify(c => c.InvalidateForCommand("testcache"), Times.Once);
    }

    [TestMethod]
    public void CA003_InvalidateCacheForCommand_CalledWithCorrectCommandName()
    {
        // Given: Different command names
        var commandNames = new[] { "cmd1", "cmd2", "testcache" };

        // When: Invalidating each command
        foreach (var cmd in commandNames)
        {
            _orchestrator.InvalidateCacheForCommand(cmd);
        }

        // Then: Each invalidation call is correct
        foreach (var cmd in commandNames)
        {
            _mockCache.Verify(c => c.InvalidateForCommand(cmd), Times.Once);
        }
    }

    #endregion

    #region CA-004: Cache TTL expiry

    [TestMethod]
    public void CA004_CacheEntry_ExpiredByTTL_ReturnsMiss()
    {
        // Given: Cache entry is old (simulated via TimeProvider)
        var mockTimeProvider = new Mock<TimeProvider>();
        var startTime = DateTimeOffset.UtcNow;
        mockTimeProvider.Setup(t => t.GetUtcNow()).Returns(startTime);
        
        var cache = new CompletionCache(100, mockTimeProvider.Object);
        var cacheKey = new CacheKey { CommandName = null, PartialValue = "test" };
        
        cache.Set(cacheKey, new CompletionResult(new List<CompletionItem>
        {
            new() { DisplayText = "dev", InsertText = "dev" }
        }));

        // Entry should exist
        cache.Get(cacheKey).Should().NotBeNull();

        // When: 6 minutes pass (cache default TTL is 5 minutes)
        mockTimeProvider.Setup(t => t.GetUtcNow()).Returns(startTime.AddMinutes(6));

        // Then: Entry expired - returns null
        cache.Get(cacheKey).Should().BeNull("entry should be expired after 6 minutes");
    }

    [TestMethod]
    public void CA004_CacheEntry_NotExpired_ReturnsResult()
    {
        // Given: Recent cache entry
        var mockTimeProvider = new Mock<TimeProvider>();
        var startTime = DateTimeOffset.UtcNow;
        mockTimeProvider.Setup(t => t.GetUtcNow()).Returns(startTime);
        
        var cache = new CompletionCache(100, mockTimeProvider.Object);
        var cacheKey = new CacheKey { CommandName = null, PartialValue = "test" };
        var expectedResult = new CompletionResult(new List<CompletionItem>
        {
            new() { DisplayText = "dev", InsertText = "dev" }
        });
        
        cache.Set(cacheKey, expectedResult);

        // When: 4 minutes pass (cache default TTL is 5 minutes)
        mockTimeProvider.Setup(t => t.GetUtcNow()).Returns(startTime.AddMinutes(4));

        // Then: Entry still valid
        var result = cache.Get(cacheKey);
        result.Should().NotBeNull("entry should still be valid after 4 minutes");
        result.Items.Should().HaveCount(1);
    }

    #endregion

    #region CA-005: Cache respects context (key includes partial value)

    [TestMethod]
    public void CA005_DifferentPartialValue_HasDifferentCacheKey()
    {
        // Note: Current cache key uses (CommandName, ArgumentName, PartialValue)
        // This test verifies that different partial values create different keys
        
        var cacheKey1 = new CacheKey { PartialValue = "" };
        var cacheKey2 = new CacheKey { PartialValue = "d" };
        
        // Keys should not be equal
        cacheKey1.Equals(cacheKey2).Should().BeFalse("different partial values should have different cache keys");
        cacheKey1.GetHashCode().Should().NotBe(cacheKey2.GetHashCode());
    }

    [TestMethod]
    public void CA005_SamePartialValue_SameCacheKey()
    {
        var cacheKey1 = new CacheKey { CommandName = "test", PartialValue = "val" };
        var cacheKey2 = new CacheKey { CommandName = "test", PartialValue = "val" };
        
        // Keys should be equal
        cacheKey1.Equals(cacheKey2).Should().BeTrue("same command and partial value should have same cache key");
        cacheKey1.GetHashCode().Should().Be(cacheKey2.GetHashCode());
    }

    #endregion

    #region CA-006: Prefix reuses cache (local filtering)

    [TestMethod]
    public async Task CA006_CachedResult_UsedForLocalFiltering()
    {
        // Given: Cached results [item1, item2, item3]
        var allItems = new List<CompletionItem>
        {
            new() { DisplayText = "item1", InsertText = "item1" },
            new() { DisplayText = "item2", InsertText = "item2" },
            new() { DisplayText = "item3", InsertText = "item3" }
        };
        _mockCache.Setup(c => c.Get(It.IsAny<CacheKey>()))
            .Returns(new CompletionResult(allItems));

        _mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);

        // When: Tab with prefix
        var action = await _orchestrator.HandleTabAsync("item", 4, CancellationToken.None);

        // Then: All items shown from cache
        action.MenuState.Items.Should().HaveCount(3);

        // Provider should NOT have been called (cache hit)
        _mockProvider.Verify(
            p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CA006_CharacterTyped_FiltersLocally()
    {
        // Given: Menu open with [dev, staging, prod]
        var allItems = new List<CompletionItem>
        {
            new() { DisplayText = "dev", InsertText = "dev" },
            new() { DisplayText = "staging", InsertText = "staging" },
            new() { DisplayText = "prod", InsertText = "prod" }
        };
        _mockCache.Setup(c => c.Get(It.IsAny<CacheKey>()))
            .Returns(new CompletionResult(allItems));

        // Open menu first
        await _orchestrator.HandleTabAsync("", 0, CancellationToken.None);

        // When: User types "d" 
        var filterAction = await _orchestrator.HandleCharacterAsync('d', "d", 1, CancellationToken.None);

        // Then: Filters locally to items starting with "d"
        filterAction.MenuState.Items.Should().HaveCount(1);
        filterAction.MenuState.Items[0].DisplayText.Should().Be("dev");
    }

    #endregion

    #region Test Command

    [Command(Name = "testcache")]
    public class TestCacheCommand : CommandBase
    {
        [Argument(Name = "env")]
        public string Environment { get; set; }

        [Argument(Name = "region")]
        public string Region { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion
}
