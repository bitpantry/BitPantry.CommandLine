using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.Input;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions from command history.
/// </summary>
/// <remarks>
/// History suggestions are prioritized for ghost text because they reflect
/// the user's actual usage patterns. Most recent entries are preferred.
/// </remarks>
public class HistoryProvider : ICompletionProvider
{
    private readonly InputLog _inputLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryProvider"/> class.
    /// </summary>
    /// <param name="inputLog">The input log containing command history.</param>
    public HistoryProvider(InputLog inputLog)
    {
        _inputLog = inputLog ?? throw new ArgumentNullException(nameof(inputLog));
    }

    /// <inheritdoc />
    /// <remarks>
    /// History provider has high priority (100) to be checked before command providers.
    /// </remarks>
    public int Priority => 100;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // History provider can handle any context but is mainly used for ghost suggestions
        return context.ElementType == CompletionElementType.Empty ||
               context.ElementType == CompletionElementType.Command;
    }

    /// <inheritdoc />
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(CompletionResult.Empty);

        var prefix = context.InputText ?? string.Empty;
        var items = new List<CompletionItem>();

        // Get matching history entries (most recent first)
        var matchingHistory = GetMatchingHistory(prefix);

        foreach (var entry in matchingHistory)
        {
            items.Add(new CompletionItem
            {
                InsertText = entry,
                DisplayText = entry,
                Description = "From history",
                Kind = CompletionItemKind.Command,
                SortPriority = items.Count // Most recent = lowest number = first
            });
        }

        return Task.FromResult(new CompletionResult(items, items.Count));
    }

    /// <summary>
    /// Gets the best history match for ghost text suggestion.
    /// </summary>
    /// <param name="prefix">The current input prefix.</param>
    /// <returns>The best matching history entry, or null if no match.</returns>
    public string GetBestMatch(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return null;

        var matches = GetMatchingHistory(prefix);
        return matches.FirstOrDefault();
    }

    /// <summary>
    /// Gets matching history entries that start with the given prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <returns>Matching history entries, most recent first.</returns>
    private IEnumerable<string> GetMatchingHistory(string prefix)
    {
        // InputLog provides entries with most recent first
        return _inputLog.GetAll()
            .Where(entry => !string.IsNullOrWhiteSpace(entry) &&
                           entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                           !string.Equals(entry, prefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10); // Limit to reasonable number
    }
}
