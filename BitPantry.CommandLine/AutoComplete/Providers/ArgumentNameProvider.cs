using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.Processing.Description;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions for argument names (--argName).
/// </summary>
/// <remarks>
/// This provider handles completion when the user types -- followed by
/// an optional partial argument name. It suggests all valid argument names
/// for the current command, excluding arguments that are already used.
/// </remarks>
public class ArgumentNameProvider : ICompletionProvider
{
    private readonly CommandRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentNameProvider"/> class.
    /// </summary>
    /// <param name="registry">The command registry for resolving command definitions.</param>
    public ArgumentNameProvider(CommandRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Priority 50 - after commands (0), before values.
    /// </remarks>
    public int Priority => 50;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        return context.ElementType == CompletionElementType.ArgumentName;
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

        // Get all argument properties from the command
        foreach (var arg in commandInfo.Arguments)
        {
            var argName = arg.Name;
            
            // Skip if already used
            if (usedArgs.Contains(argName, StringComparer.OrdinalIgnoreCase))
                continue;

            var displayName = $"--{argName}";
            
            // Filter by prefix (case-insensitive)
            if (!string.IsNullOrEmpty(prefix) && 
                !argName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            items.Add(new CompletionItem
            {
                DisplayText = displayName,
                InsertText = displayName,
                Description = arg.Description,
                Kind = CompletionItemKind.ArgumentName,
                SortPriority = 0
            });
        }

        // Sort alphabetically
        items = items.OrderBy(i => i.DisplayText).ToList();

        return Task.FromResult(new CompletionResult(items));
    }
}
