using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions for argument aliases (-a).
/// </summary>
/// <remarks>
/// This provider handles completion when the user types a single dash
/// followed by an optional partial alias character. It suggests all valid
/// argument aliases for the current command, excluding arguments already used.
/// </remarks>
public class ArgumentAliasProvider : ICompletionProvider
{
    private readonly CommandRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentAliasProvider"/> class.
    /// </summary>
    /// <param name="registry">The command registry for resolving command definitions.</param>
    public ArgumentAliasProvider(CommandRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Priority 51 - just after argument names.
    /// </remarks>
    public int Priority => 51;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        return context.ElementType == CompletionElementType.ArgumentAlias;
    }

    /// <inheritdoc />
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(CompletionResult.Empty);

        if (string.IsNullOrEmpty(context.CommandName))
            return Task.FromResult(CompletionResult.Empty);

        // Get the command definition
        var commandInfo = _registry.Find(context.CommandName);
        if (commandInfo == null)
            return Task.FromResult(CompletionResult.Empty);

        var prefix = context.CurrentWord?.ToLowerInvariant() ?? string.Empty;
        var usedArgs = context.UsedArguments ?? new HashSet<string>();

        var items = new List<CompletionItem>();

        // Get all argument properties with aliases from the command
        foreach (var arg in commandInfo.Arguments)
        {
            var alias = arg.Alias;
            if (alias == default(char))
                continue;

            var aliasStr = alias.ToString();

            // Skip if the argument or its alias is already used
            if (usedArgs.Contains(arg.Name, StringComparer.OrdinalIgnoreCase) ||
                usedArgs.Contains(aliasStr, StringComparer.OrdinalIgnoreCase))
                continue;

            var displayAlias = $"-{aliasStr}";

            // Filter by prefix (case-insensitive)
            if (!string.IsNullOrEmpty(prefix) &&
                !aliasStr.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            items.Add(new CompletionItem
            {
                DisplayText = displayAlias,
                InsertText = displayAlias,
                Description = $"{arg.Name}: {arg.Description}",
                Kind = CompletionItemKind.ArgumentAlias,
                SortPriority = 0
            });
        }

        // Sort alphabetically
        items = items.OrderBy(i => i.DisplayText).ToList();

        return Task.FromResult(new CompletionResult(items));
    }
}
