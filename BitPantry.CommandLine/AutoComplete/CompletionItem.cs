using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Represents a single completion suggestion.
/// </summary>
public sealed class CompletionItem
{
    private string? _displayText;

    /// <summary>
    /// The text to insert when this item is selected.
    /// </summary>
    public required string InsertText { get; init; }

    /// <summary>
    /// The display text shown in the completion menu.
    /// May differ from InsertText (e.g., display without quotes).
    /// Defaults to InsertText if not set.
    /// </summary>
    public string DisplayText
    {
        get => _displayText ?? InsertText;
        init => _displayText = value;
    }

    /// <summary>
    /// Optional description shown to the right of the item.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The type of completion item (for icons/grouping).
    /// </summary>
    public CompletionItemKind Kind { get; init; }

    /// <summary>
    /// Sort priority (higher = appears first). Default 0.
    /// </summary>
    public int SortPriority { get; init; }

    /// <summary>
    /// Match score for ranking (set by matcher).
    /// </summary>
    public int MatchScore { get; internal set; }

    /// <summary>
    /// Ranges within DisplayText that matched the query (for highlighting).
    /// </summary>
    public IReadOnlyList<Range> MatchRanges { get; internal set; } = Array.Empty<Range>();
}
