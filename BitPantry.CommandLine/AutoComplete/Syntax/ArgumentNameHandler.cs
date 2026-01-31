using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Parsing;

namespace BitPantry.CommandLine.AutoComplete.Syntax;

/// <summary>
/// Handler for argument name autocomplete (--argName).
/// Suggests argument names when user types "--" at an argument position.
/// </summary>
public class ArgumentNameHandler : IAutoCompleteHandler
{
    /// <inheritdoc/>
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var queryString = context.QueryString ?? "";
        var options = new List<AutoCompleteOption>();

        // Only suggest argument names if query starts with "--"
        if (!queryString.StartsWith("--"))
        {
            return Task.FromResult(options);
        }

        // Get all named arguments from the command
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
        var usedPositionalArgs = UsedArgumentHelper.GetUsedPositionalArguments(parsedCommand, commandInfo);

        // All arguments can be specified by name (positional args are also named args)
        foreach (var arg in commandInfo.Arguments)
        {
            // Skip if argument has already been used (by name or alias)
            if (usedNames.Contains(arg.Name.ToUpperInvariant()))
            {
                continue;
            }
            if (arg.Alias != default && 
                usedAliases.Contains(char.ToUpperInvariant(arg.Alias)))
            {
                continue;
            }
            // Skip if positional-capable argument was satisfied positionally
            if (arg.IsPositional && usedPositionalArgs.Contains(arg))
            {
                continue;
            }

            var prefixedName = $"--{arg.Name.ToLowerInvariant()}";
            
            // Check if the prefixed name matches the query (case-insensitive)
            if (prefixedName.StartsWith(queryString, StringComparison.OrdinalIgnoreCase))
            {
                options.Add(new AutoCompleteOption(prefixedName, arg.Description));
            }
        }

        // Sort alphabetically
        return Task.FromResult(
            options.OrderBy(o => o.Value, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
