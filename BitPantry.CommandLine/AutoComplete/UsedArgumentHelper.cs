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
        /// Gets positional-capable arguments that have been satisfied by positional values or by named syntax.
        /// Positional values are counted in order and matched to positional arguments by Position.
        /// Positional arguments that were specified by name (--name value) are also included.
        /// Note: This method correctly handles grouped commands by skipping command path elements
        /// (group names and command name) that are parsed as PositionalValue before counting actual arguments.
        /// </summary>
        /// <param name="parsedCommand">The parsed command to analyze</param>
        /// <param name="commandInfo">The command info containing positional argument definitions</param>
        /// <returns>Set of positional arguments that have been satisfied by positional values or named syntax</returns>
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

            // Get used argument names and aliases for checking named syntax usage
            var usedNames = GetUsedArgumentNames(parsedCommand);
            var usedAliases = GetUsedArgumentAliases(parsedCommand);

            // First, check which positional arguments were provided via named syntax (--name or -a)
            var namedPositionalArgs = new HashSet<ArgumentInfo>();
            foreach (var arg in positionalArgs)
            {
                if (usedNames.Contains(arg.Name.ToUpperInvariant()))
                {
                    namedPositionalArgs.Add(arg);
                    result.Add(arg);
                }
                else if (arg.Alias != default && usedAliases.Contains(char.ToUpperInvariant(arg.Alias)))
                {
                    namedPositionalArgs.Add(arg);
                    result.Add(arg);
                }
            }

            // Calculate how many elements form the command path (group names + command name).
            // These look like PositionalValue elements but are not actual argument values.
            // For "server profile add value1", the command path is 3 elements (server, profile, add).
            // We use the CommandInfo to determine this accurately based on the group path + command name.
            var commandPathLength = GetCommandPathLength(commandInfo, parsedCommand);

            // Count positional values AFTER the command path and match to remaining positional arguments
            int positionalIndex = 0;
            int pathElementsSkipped = 0;
            bool pastCommandPath = false;
            
            foreach (var element in parsedCommand.Elements)
            {
                // Skip empty elements
                if (element.ElementType == CommandElementType.Empty)
                    continue;

                // Skip Command and PositionalValue elements that form the command path
                if (!pastCommandPath && 
                    (element.ElementType == CommandElementType.Command || element.ElementType == CommandElementType.PositionalValue))
                {
                    pathElementsSkipped++;
                    if (pathElementsSkipped >= commandPathLength)
                    {
                        pastCommandPath = true;
                    }
                    continue;
                }

                // After the command path, count PositionalValue elements as actual argument values
                if (pastCommandPath && element.ElementType == CommandElementType.PositionalValue)
                {
                    // Find the next positional argument that wasn't already provided by name
                    while (positionalIndex < positionalArgs.Count && 
                           namedPositionalArgs.Contains(positionalArgs[positionalIndex]))
                    {
                        positionalIndex++;
                    }

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
        /// Calculates the number of elements that form the command path.
        /// Uses CommandInfo.GroupPath (or SerializedGroupPath) when available; otherwise assumes path is 1 element.
        /// For "server profile add", GroupPath="server profile", Name="add" => 3 elements.
        /// For "mycommand value", GroupPath=null/empty => 1 element (just "mycommand").
        /// </summary>
        private static int GetCommandPathLength(CommandInfo commandInfo, ParsedCommand parsedCommand)
        {
            // If CommandInfo has group information, use it for accurate calculation
            // Check both GroupPath (from Group.FullPath) and SerializedGroupPath (set directly)
            var groupPath = commandInfo?.GroupPath ?? commandInfo?.SerializedGroupPath;
            if (!string.IsNullOrWhiteSpace(groupPath))
            {
                var groupElements = groupPath.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                return groupElements + 1; // +1 for the command name itself
            }

            // Without GroupPath info, we can't reliably detect the path length from parsing alone.
            // Conservative fallback: assume the command path is just 1 element (the command name).
            // This works correctly for non-grouped commands and avoids false positives.
            return 1;
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
