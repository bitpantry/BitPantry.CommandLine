using System;

namespace BitPantry.CommandLine.AutoComplete.Cache;

/// <summary>
/// Represents a cached completion result with expiration time.
/// </summary>
public sealed class CacheEntry
{
    /// <summary>
    /// Gets the cached completion result.
    /// </summary>
    public required CompletionResult Result { get; init; }

    /// <summary>
    /// Gets when this entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets when this entry expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Gets whether this entry has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
