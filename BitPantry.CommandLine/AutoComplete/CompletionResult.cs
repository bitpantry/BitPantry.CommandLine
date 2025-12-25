using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// The result of a completion request.
/// </summary>
public sealed class CompletionResult
{
    /// <summary>
    /// The completion items to display.
    /// </summary>
    public IReadOnlyList<CompletionItem> Items { get; init; } = Array.Empty<CompletionItem>();

    /// <summary>
    /// Whether the result was retrieved from cache.
    /// </summary>
    public bool IsCached { get; init; }

    /// <summary>
    /// Whether the request timed out (remote completions).
    /// </summary>
    public bool IsTimedOut { get; init; }

    /// <summary>
    /// Whether an error occurred fetching completions.
    /// </summary>
    public bool IsError { get; init; }

    /// <summary>
    /// Error message if IsError is true.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The total number of items (before truncation/viewport).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public CompletionResult() { }

    /// <summary>
    /// Creates a result with the specified items.
    /// </summary>
    /// <param name="items">The completion items.</param>
    /// <param name="totalCount">The total count (defaults to items count).</param>
    public CompletionResult(IReadOnlyList<CompletionItem> items, int? totalCount = null)
    {
        Items = items ?? Array.Empty<CompletionItem>();
        TotalCount = totalCount ?? Items.Count;
    }

    /// <summary>
    /// Empty result singleton.
    /// </summary>
    public static CompletionResult Empty { get; } = new() { Items = Array.Empty<CompletionItem>() };

    /// <summary>
    /// Timed out result singleton.
    /// </summary>
    public static CompletionResult TimedOut { get; } = new() { IsTimedOut = true };

    /// <summary>
    /// Creates an error result with the specified message.
    /// </summary>
    public static CompletionResult Error(string message) => new() { IsError = true, ErrorMessage = message };
}
