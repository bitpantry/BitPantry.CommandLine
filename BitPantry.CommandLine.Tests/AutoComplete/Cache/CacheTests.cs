using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Cache;
using System;

namespace BitPantry.CommandLine.Tests.AutoComplete.Cache;

[TestClass]
public class CacheKeyTests
{
    [TestMethod]
    public void CacheKey_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var key = new CacheKey();

        // Assert
        key.CommandName.Should().BeNull();
        key.ArgumentName.Should().BeNull();
        key.PartialValue.Should().BeEmpty();
        key.ElementType.Should().Be(CompletionElementType.Empty);
        key.ProviderType.Should().BeNull();
    }

    [TestMethod]
    public void CacheKey_Equals_SameValuesShouldBeEqual()
    {
        // Arrange
        var key1 = new CacheKey
        {
            CommandName = "test",
            ArgumentName = "--file",
            PartialValue = "val",
            ElementType = CompletionElementType.ArgumentValue
        };
        var key2 = new CacheKey
        {
            CommandName = "test",
            ArgumentName = "--file",
            PartialValue = "val",
            ElementType = CompletionElementType.ArgumentValue
        };

        // Assert
        key1.Should().BeEquivalentTo(key2);
        key1.Equals(key2).Should().BeTrue();
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }

    [TestMethod]
    public void CacheKey_Equals_DifferentValuesShouldNotBeEqual()
    {
        // Arrange
        var key1 = new CacheKey { CommandName = "test1" };
        var key2 = new CacheKey { CommandName = "test2" };

        // Assert
        key1.Equals(key2).Should().BeFalse();
    }

    [TestMethod]
    public void CacheKey_Equals_NullShouldReturnFalse()
    {
        // Arrange
        var key = new CacheKey { CommandName = "test" };

        // Assert
        key.Equals(null).Should().BeFalse();
    }

    [TestMethod]
    public void CacheKey_FromContext_ShouldExtractRelevantFields()
    {
        // Arrange
        var context = new CompletionContext
        {
            CommandName = "mycommand",
            ArgumentName = "--output",
            PartialValue = "test",
            ElementType = CompletionElementType.ArgumentValue
        };

        // Act
        var key = CacheKey.FromContext(context);

        // Assert
        key.CommandName.Should().Be("mycommand");
        key.ArgumentName.Should().Be("--output");
        key.PartialValue.Should().Be("test");
        key.ElementType.Should().Be(CompletionElementType.ArgumentValue);
    }
}

[TestClass]
public class CacheEntryTests
{
    [TestMethod]
    public void CacheEntry_NotExpired_ShouldReturnFalse()
    {
        // Arrange
        var entry = new CacheEntry
        {
            Result = CompletionResult.Empty,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        // Assert
        entry.IsExpired.Should().BeFalse();
    }

    [TestMethod]
    public void CacheEntry_Expired_ShouldReturnTrue()
    {
        // Arrange
        var entry = new CacheEntry
        {
            Result = CompletionResult.Empty,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        // Assert
        entry.IsExpired.Should().BeTrue();
    }
}

[TestClass]
public class CompletionCacheTests
{
    [TestMethod]
    public void CompletionCache_Set_ShouldStoreEntry()
    {
        // Arrange
        var cache = new CompletionCache();
        var key = new CacheKey { CommandName = "test" };
        var result = new CompletionResult
        {
            Items = [new CompletionItem { InsertText = "item" }]
        };

        // Act
        cache.Set(key, result, TimeSpan.FromMinutes(5));
        var found = cache.TryGet(key, out var cached);

        // Assert
        found.Should().BeTrue();
        cached.Should().NotBeNull();
        cached!.Items.Should().HaveCount(1);
    }

    [TestMethod]
    public void CompletionCache_TryGet_NotFound_ShouldReturnFalse()
    {
        // Arrange
        var cache = new CompletionCache();
        var key = new CacheKey { CommandName = "nonexistent" };

        // Act
        var found = cache.TryGet(key, out var result);

        // Assert
        found.Should().BeFalse();
        result.Should().BeNull();
    }

    [TestMethod]
    public void CompletionCache_Set_ZeroDuration_ShouldNotCache()
    {
        // Arrange
        var cache = new CompletionCache();
        var key = new CacheKey { CommandName = "test" };
        var result = CompletionResult.Empty;

        // Act
        cache.Set(key, result, TimeSpan.Zero);
        var found = cache.TryGet(key, out _);

        // Assert
        found.Should().BeFalse();
        cache.Count.Should().Be(0);
    }

    [TestMethod]
    public void CompletionCache_Invalidate_ShouldRemoveEntry()
    {
        // Arrange
        var cache = new CompletionCache();
        var key = new CacheKey { CommandName = "test" };
        cache.Set(key, CompletionResult.Empty, TimeSpan.FromMinutes(5));

        // Act
        cache.Invalidate(key);
        var found = cache.TryGet(key, out _);

        // Assert
        found.Should().BeFalse();
    }

    [TestMethod]
    public void CompletionCache_InvalidateForCommand_ShouldRemoveAllCommandEntries()
    {
        // Arrange
        var cache = new CompletionCache();
        cache.Set(new CacheKey { CommandName = "cmd1", ArgumentName = "arg1" },
            CompletionResult.Empty, TimeSpan.FromMinutes(5));
        cache.Set(new CacheKey { CommandName = "cmd1", ArgumentName = "arg2" },
            CompletionResult.Empty, TimeSpan.FromMinutes(5));
        cache.Set(new CacheKey { CommandName = "cmd2", ArgumentName = "arg1" },
            CompletionResult.Empty, TimeSpan.FromMinutes(5));

        // Act
        cache.InvalidateForCommand("cmd1");

        // Assert
        cache.Count.Should().Be(1);
        cache.TryGet(new CacheKey { CommandName = "cmd2", ArgumentName = "arg1" }, out _)
            .Should().BeTrue();
    }

    [TestMethod]
    public void CompletionCache_Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        var cache = new CompletionCache();
        cache.Set(new CacheKey { CommandName = "cmd1" },
            CompletionResult.Empty, TimeSpan.FromMinutes(5));
        cache.Set(new CacheKey { CommandName = "cmd2" },
            CompletionResult.Empty, TimeSpan.FromMinutes(5));

        // Act
        cache.Clear();

        // Assert
        cache.Count.Should().Be(0);
    }

    [TestMethod]
    public void CompletionCache_EvictsOldest_WhenAtCapacity()
    {
        // Arrange
        var cache = new CompletionCache(maxEntries: 2);
        var key1 = new CacheKey { CommandName = "cmd1" };
        var key2 = new CacheKey { CommandName = "cmd2" };
        var key3 = new CacheKey { CommandName = "cmd3" };

        // Act
        cache.Set(key1, CompletionResult.Empty, TimeSpan.FromMinutes(5));
        cache.Set(key2, CompletionResult.Empty, TimeSpan.FromMinutes(5));
        cache.Set(key3, CompletionResult.Empty, TimeSpan.FromMinutes(5));

        // Assert
        cache.Count.Should().Be(2);
        cache.TryGet(key1, out _).Should().BeFalse(); // Oldest evicted
        cache.TryGet(key3, out _).Should().BeTrue();
    }
}
