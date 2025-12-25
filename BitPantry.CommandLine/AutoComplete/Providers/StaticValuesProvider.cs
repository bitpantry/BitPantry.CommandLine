using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions from static values defined in [Completion] attribute.
/// </summary>
/// <remarks>
/// This provider handles [Completion("value1", "value2", ...)] style completions
/// where the valid values are known at compile time.
/// </remarks>
public class StaticValuesProvider : ICompletionProvider
{
    /// <inheritdoc />
    /// <remarks>
    /// Priority 70 - for static value completion.
    /// </remarks>
    public int Priority => 70;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Only handle argument values with static values in completion attribute
        if (context.ElementType != CompletionElementType.ArgumentValue)
            return false;

        return context.CompletionAttribute?.Values?.Length > 0;
    }

    /// <inheritdoc />
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(CompletionResult.Empty);

        var staticValues = context.CompletionAttribute?.Values;
        if (staticValues == null || staticValues.Length == 0)
            return Task.FromResult(CompletionResult.Empty);

        var prefix = context.CurrentWord ?? string.Empty;
        var items = new List<CompletionItem>();

        foreach (var value in staticValues)
        {
            // Filter by prefix (case-insensitive)
            if (!string.IsNullOrEmpty(prefix) &&
                !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            items.Add(new CompletionItem
            {
                DisplayText = value,
                InsertText = value,
                Kind = CompletionItemKind.ArgumentValue,
                SortPriority = 0
            });
        }

        // Sort alphabetically
        items = items.OrderBy(i => i.DisplayText).ToList();

        return Task.FromResult(new CompletionResult(items));
    }
}
