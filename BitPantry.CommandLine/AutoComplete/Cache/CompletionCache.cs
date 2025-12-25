using System;
using System.Collections.Concurrent;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete.Cache;

/// <summary>
/// Thread-safe cache for completion results.
/// </summary>
public sealed class CompletionCache : ICompletionCache
{
    /// <summary>
    /// Default cache duration of 5 minutes.
    /// </summary>
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<CacheKey, CacheEntry> _cache = new();
    private readonly int _maxEntries;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompletionCache"/> class.
    /// </summary>
    /// <param name="maxEntries">Maximum number of cache entries. Default is 100.</param>
    /// <param name="timeProvider">Time provider for testability. Default is system time.</param>
    public CompletionCache(int maxEntries = 100, TimeProvider? timeProvider = null)
    {
        _maxEntries = maxEntries;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public CompletionResult Get(CacheKey key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            // Use the time provider for expiration checking
            if (_timeProvider.GetUtcNow() < entry.ExpiresAt)
            {
                return entry.Result;
            }

            // Remove expired entry
            _cache.TryRemove(key, out _);
        }

        return null;
    }

    /// <inheritdoc />
    public bool TryGet(CacheKey key, out CompletionResult? result)
    {
        result = Get(key);
        return result != null;
    }

    /// <inheritdoc />
    public void Set(CacheKey key, CompletionResult result) => Set(key, result, DefaultDuration);

    /// <inheritdoc />
    public void Set(CacheKey key, CompletionResult result, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return;

        // Evict oldest entries if at capacity
        while (_cache.Count >= _maxEntries)
        {
            var oldest = _cache
                .OrderBy(kvp => kvp.Value.CreatedAt)
                .FirstOrDefault();

            if (oldest.Key is not null)
            {
                _cache.TryRemove(oldest.Key, out _);
            }
        }

        var now = _timeProvider.GetUtcNow();
        var entry = new CacheEntry
        {
            Result = result,
            CreatedAt = now,
            ExpiresAt = now.Add(duration)
        };

        _cache[key] = entry;
    }

    /// <inheritdoc />
    public void Invalidate(CacheKey key)
    {
        _cache.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public void InvalidateForCommand(string commandName)
    {
        var keysToRemove = _cache.Keys
            .Where(k => string.Equals(k.CommandName, commandName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _cache.Clear();
    }

    /// <inheritdoc />
    public int Count => _cache.Count;
}

/// <summary>
/// Interface for completion result caching.
/// </summary>
public interface ICompletionCache
{
    /// <summary>
    /// Gets a cached result for the given key, or null if not found.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached result, or null if not found or expired.</returns>
    CompletionResult Get(CacheKey key);

    /// <summary>
    /// Tries to get a cached result for the given key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="result">The cached result, if found.</param>
    /// <returns>True if a non-expired entry was found.</returns>
    bool TryGet(CacheKey key, out CompletionResult? result);

    /// <summary>
    /// Caches a completion result with the default duration.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="result">The result to cache.</param>
    void Set(CacheKey key, CompletionResult result);

    /// <summary>
    /// Caches a completion result.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="result">The result to cache.</param>
    /// <param name="duration">How long to cache the result.</param>
    void Set(CacheKey key, CompletionResult result, TimeSpan duration);

    /// <summary>
    /// Removes a specific entry from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    void Invalidate(CacheKey key);

    /// <summary>
    /// Removes all entries for a specific command.
    /// </summary>
    /// <param name="commandName">The command name.</param>
    void InvalidateForCommand(string commandName);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the number of entries in the cache.
    /// </summary>
    int Count { get; }
}
