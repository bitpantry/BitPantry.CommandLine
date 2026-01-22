using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Parsing;

namespace BitPantry.CommandLine.AutoComplete.Syntax;

/// <summary>
/// Handler for argument alias autocomplete (-alias).
/// Suggests argument aliases when user types "-" at an argument position.
/// </summary>
public class ArgumentAliasHandler : IAutoCompleteHandler
{
    /// <inheritdoc/>
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var queryString = context.QueryString ?? "";
        var options = new List<AutoCompleteOption>();

        // Only suggest argument aliases if query starts with "-" but NOT "--"
        if (!queryString.StartsWith("-") || queryString.StartsWith("--"))
        {
            return Task.FromResult(options);
        }

        // Get all arguments from the command
        var commandInfo = context.CommandInfo;
        if (commandInfo == null)
        {
            return Task.FromResult(options);
        }

        // Parse FullInput to determine which arguments are already used
        var parsedInput = new ParsedInput(context.FullInput);
        var parsedCommand = parsedInput.ParsedCommands.FirstOrDefault();
        
        // Exclude the current element (the one being typed) from used calculation
        var currentElement = parsedInput.GetElementAtCursorPosition(context.CursorPosition);
        var usedNames = UsedArgumentHelper.GetUsedArgumentNames(parsedCommand, currentElement);
        var usedAliases = UsedArgumentHelper.GetUsedArgumentAliases(parsedCommand, currentElement);

        // Filter to named (non-positional) arguments that have aliases
        foreach (var arg in commandInfo.Arguments.Where(a => !a.IsPositional && a.Alias != default(char)))
        {
            // Skip if argument has already been used (by name or alias)
            if (usedNames.Contains(arg.Name.ToUpperInvariant()))
            {
                continue;
            }
            if (usedAliases.Contains(char.ToUpperInvariant(arg.Alias)))
            {
                continue;
            }

            var prefixedAlias = $"-{arg.Alias}";
            
            // Check if the prefixed alias matches the query (case-insensitive)
            if (prefixedAlias.StartsWith(queryString, StringComparison.OrdinalIgnoreCase))
            {
                options.Add(new AutoCompleteOption(prefixedAlias, arg.Description ?? $"Alias for {arg.Name}"));
            }
        }

        // Sort alphabetically
        return Task.FromResult(
            options.OrderBy(o => o.Value, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
