using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Represents the result of matching an input against a completion item.
/// </summary>
public sealed class MatchResult
{
    /// <summary>
    /// Gets the matched completion item.
    /// </summary>
    public required CompletionItem Item { get; init; }

    /// <summary>
    /// Gets the match score (higher is better match).
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Gets the mode used for matching.
    /// </summary>
    public MatchMode Mode { get; init; }

    /// <summary>
    /// Gets the character ranges that matched (for highlighting).
    /// </summary>
    public IReadOnlyList<(int Start, int Length)> MatchRanges { get; init; } = [];

    /// <summary>
    /// Gets whether this was an exact match.
    /// </summary>
    public bool IsExactMatch => Mode == MatchMode.Exact && Score >= 1.0;

    /// <summary>
    /// Gets whether this was a prefix match.
    /// </summary>
    public bool IsPrefixMatch => Mode == MatchMode.Prefix;

    /// <summary>
    /// Gets whether this was a case-insensitive match.
    /// </summary>
    public bool IsCaseInsensitive => Mode is MatchMode.PrefixCaseInsensitive or MatchMode.ContainsCaseInsensitive;

    /// <summary>
    /// Creates a no-match result.
    /// </summary>
    public static MatchResult NoMatch(CompletionItem item) => new()
    {
        Item = item,
        Score = 0,
        Mode = MatchMode.None
    };

    /// <summary>
    /// Creates an exact match result.
    /// </summary>
    public static MatchResult Exact(CompletionItem item) => new()
    {
        Item = item,
        Score = 1.0,
        Mode = MatchMode.Exact,
        MatchRanges = [(0, item.InsertText.Length)]
    };

    /// <summary>
    /// Creates a prefix match result.
    /// </summary>
    public static MatchResult Prefix(CompletionItem item, int prefixLength, bool caseSensitive) => new()
    {
        Item = item,
        Score = (double)prefixLength / item.InsertText.Length,
        Mode = caseSensitive ? MatchMode.Prefix : MatchMode.PrefixCaseInsensitive,
        MatchRanges = [(0, prefixLength)]
    };

    /// <summary>
    /// Creates a contains match result.
    /// </summary>
    public static MatchResult Contains(CompletionItem item, int start, int length, bool caseSensitive) => new()
    {
        Item = item,
        Score = (double)length / item.InsertText.Length * 0.5, // Contains is scored lower than prefix
        Mode = caseSensitive ? MatchMode.Contains : MatchMode.ContainsCaseInsensitive,
        MatchRanges = [(start, length)]
    };
}

/// <summary>
/// Specifies the matching mode used.
/// </summary>
public enum MatchMode
{
    /// <summary>No match.</summary>
    None,

    /// <summary>Exact match (same string).</summary>
    Exact,

    /// <summary>Prefix match (case-sensitive).</summary>
    Prefix,

    /// <summary>Prefix match (case-insensitive).</summary>
    PrefixCaseInsensitive,

    /// <summary>Contains match (case-sensitive).</summary>
    Contains,

    /// <summary>Contains match (case-insensitive).</summary>
    ContainsCaseInsensitive
}
