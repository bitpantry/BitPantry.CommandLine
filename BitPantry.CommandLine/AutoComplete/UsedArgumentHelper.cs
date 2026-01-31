using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// Provides helper methods to analyze which arguments have already been provided in the current command input.
    /// Used by autocomplete to filter out already-used arguments from suggestions.
    /// </summary>
    public static class UsedArgumentHelper
    {
        /// <summary>
        /// Gets the set of argument names that have been used in the parsed command.
        /// Names are normalized to uppercase for case-insensitive comparison.
        /// </summary>
        /// <param name="parsedCommand">The parsed command to analyze</param>
        /// <param name="excludeElement">Optional element to exclude from tracking (usually the current element being autocompleted)</param>
        /// <returns>Set of used argument names (uppercase)</returns>
        public static IReadOnlySet<string> GetUsedArgumentNames(ParsedCommand parsedCommand, ParsedCommandElement excludeElement = null)
        {
            if (parsedCommand == null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in parsedCommand.Elements)
            {
                if (element == excludeElement)
                    continue;

                if (element.ElementType == CommandElementType.ArgumentName)
                {
                    usedNames.Add(element.Value.ToUpperInvariant());
                }
            }

            return usedNames;
        }

        /// <summary>
        /// Gets the set of argument aliases that have been used in the parsed command.
        /// Aliases are normalized to uppercase for case-insensitive comparison.
        /// </summary>
        /// <param name="parsedCommand">The parsed command to analyze</param>
        /// <param name="excludeElement">Optional element to exclude from tracking (usually the current element being autocompleted)</param>
        /// <returns>Set of used argument aliases (uppercase single characters)</returns>
        public static IReadOnlySet<char> GetUsedArgumentAliases(ParsedCommand parsedCommand, ParsedCommandElement excludeElement = null)
        {
            if (parsedCommand == null)
                return new HashSet<char>();

            var usedAliases = new HashSet<char>();

            foreach (var element in parsedCommand.Elements)
            {
                if (element == excludeElement)
                    continue;

                if (element.ElementType == CommandElementType.ArgumentAlias && element.Value.Length == 1)
                {
                    usedAliases.Add(char.ToUpperInvariant(element.Value[0]));
                }
            }

            return usedAliases;
        }

        /// <summary>
        /// Checks if an argument has already been used in the parsed command.
        /// Checks both the argument name and alias.
        /// </summary>
        /// <param name="argumentInfo">The argument to check</param>
        /// <param name="parsedCommand">The parsed command to analyze</param>
        /// <param name="excludeElement">Optional element to exclude from tracking</param>
        /// <returns>True if the argument has been used (by name or alias)</returns>
        public static bool IsArgumentUsed(ArgumentInfo argumentInfo, ParsedCommand parsedCommand, ParsedCommandElement excludeElement = null)
        {
            if (argumentInfo == null || parsedCommand == null)
                return false;

            var usedNames = GetUsedArgumentNames(parsedCommand, excludeElement);
            var usedAliases = GetUsedArgumentAliases(parsedCommand, excludeElement);

            // Check if used by name
            if (usedNames.Contains(argumentInfo.Name.ToUpperInvariant()))
                return true;

            // Check if used by alias
            if (argumentInfo.Alias != default && usedAliases.Contains(char.ToUpperInvariant(argumentInfo.Alias)))
                return true;

            return false;
        }

        /// <summary>
        /// Gets positional-capable arguments that have been satisfied by positional values.
        /// Positional values are counted in order and matched to positional arguments by Position.
        /// </summary>
        /// <param name="parsedCommand">The parsed command to analyze</param>
        /// <param name="commandInfo">The command info containing positional argument definitions</param>
        /// <returns>Set of positional arguments that have been satisfied by positional values</returns>
        public static IReadOnlySet<ArgumentInfo> GetUsedPositionalArguments(
            ParsedCommand parsedCommand,
            CommandInfo commandInfo)
        {
            var result = new HashSet<ArgumentInfo>();
            
            if (parsedCommand == null || commandInfo == null)
                return result;

            var positionalArgs = commandInfo.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .ToList();

            int positionalIndex = 0;
            
            foreach (var element in parsedCommand.Elements)
            {
                if (element.ElementType == CommandElementType.PositionalValue)
                {
                    if (positionalIndex < positionalArgs.Count)
                    {
                        result.Add(positionalArgs[positionalIndex]);
                        positionalIndex++;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets arguments from the command that haven't been used yet.
        /// </summary>
        /// <param name="commandInfo">The command info containing all available arguments</param>
        /// <param name="parsedCommand">The parsed command to check for used arguments</param>
        /// <param name="excludeElement">Optional element to exclude from tracking</param>
        /// <returns>List of arguments that are still available for use</returns>
        public static IReadOnlyList<ArgumentInfo> GetAvailableArguments(
            CommandInfo commandInfo, 
            ParsedCommand parsedCommand, 
            ParsedCommandElement excludeElement = null)
        {
            if (commandInfo == null)
                return Array.Empty<ArgumentInfo>();

            var usedNames = GetUsedArgumentNames(parsedCommand, excludeElement);
            var usedAliases = GetUsedArgumentAliases(parsedCommand, excludeElement);

            return commandInfo.Arguments
                .Where(arg => !usedNames.Contains(arg.Name.ToUpperInvariant())
                              && (arg.Alias == default || !usedAliases.Contains(char.ToUpperInvariant(arg.Alias))))
                .ToList();
        }
    }
}
