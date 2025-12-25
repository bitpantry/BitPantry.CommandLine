using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Provides matching and ranking functionality for completion items.
/// </summary>
public static class CompletionMatcher
{
    /// <summary>
    /// Matches and ranks completion items against a query string.
    /// </summary>
    /// <param name="items">The completion items to match.</param>
    /// <param name="query">The query string to match against.</param>
    /// <param name="mode">The matching mode to use.</param>
    /// <returns>Matched items with scores and highlighting, sorted by relevance.</returns>
    public static IEnumerable<CompletionItem> Match(
        IEnumerable<CompletionItem> items,
        string query,
        MatchMode mode = MatchMode.Prefix)
    {
        if (items == null)
            return Enumerable.Empty<CompletionItem>();

        if (string.IsNullOrEmpty(query))
        {
            // No query - return all items with default score
            return items.Select(item =>
            {
                item.MatchScore = 0;
                item.MatchRanges = Array.Empty<Range>();
                return item;
            });
        }

        var results = new List<CompletionItem>();

        foreach (var item in items)
        {
            var matchResult = MatchItem(item, query, mode);
            if (matchResult != null && matchResult.Score > 0)
            {
                item.MatchScore = (int)(matchResult.Score * 100);
                item.MatchRanges = matchResult.MatchRanges
                    .Select(r => new Range(r.Start, r.Start + r.Length))
                    .ToArray();
                results.Add(item);
            }
        }

        // Sort by score (descending), then by display text (ascending)
        return results
            .OrderByDescending(i => i.MatchScore)
            .ThenBy(i => i.DisplayText, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Matches a single item against a query.
    /// </summary>
    /// <param name="item">The completion item to match.</param>
    /// <param name="query">The query string.</param>
    /// <param name="mode">The matching mode.</param>
    /// <returns>Match result if matched, null otherwise.</returns>
    public static MatchResult MatchItem(CompletionItem item, string query, MatchMode mode = MatchMode.Prefix)
    {
        if (item == null || string.IsNullOrEmpty(item.DisplayText))
            return MatchResult.NoMatch(item);

        var text = item.DisplayText;

        if (string.IsNullOrEmpty(query))
            return new MatchResult { Item = item, Score = 0, Mode = MatchMode.None };

        switch (mode)
        {
            case MatchMode.Prefix:
            case MatchMode.PrefixCaseInsensitive:
                return MatchPrefix(item, query, mode == MatchMode.Prefix);

            case MatchMode.Contains:
            case MatchMode.ContainsCaseInsensitive:
                return MatchContains(item, query, mode == MatchMode.Contains);

            case MatchMode.Exact:
                if (text.Equals(query, StringComparison.Ordinal))
                    return MatchResult.Exact(item);
                if (text.Equals(query, StringComparison.OrdinalIgnoreCase))
                    return new MatchResult { Item = item, Score = 0.9, Mode = MatchMode.PrefixCaseInsensitive };
                return MatchResult.NoMatch(item);

            default:
                return MatchPrefix(item, query, false);
        }
    }

    /// <summary>
    /// Matches if text starts with query.
    /// </summary>
    private static MatchResult MatchPrefix(CompletionItem item, string query, bool caseSensitive)
    {
        var text = item.DisplayText;
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (text.StartsWith(query, comparison))
        {
            return MatchResult.Prefix(item, query.Length, caseSensitive);
        }

        return MatchResult.NoMatch(item);
    }

    /// <summary>
    /// Matches if text contains query anywhere.
    /// </summary>
    private static MatchResult MatchContains(CompletionItem item, string query, bool caseSensitive)
    {
        var text = item.DisplayText;
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var index = text.IndexOf(query, comparison);
        if (index >= 0)
        {
            return MatchResult.Contains(item, index, query.Length, caseSensitive);
        }

        return MatchResult.NoMatch(item);
    }
}
