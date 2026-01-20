using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BitPantry.CommandLine.Processing.Resolution
{
    /// <summary>
    /// Resolves a parsed input command to a registered command
    /// </summary>
    public class CommandResolver
    {
        private ICommandRegistry _registry;

        /// <summary>
        /// Creates a new command resolver using the given registry of commands
        /// </summary>
        /// <param name="registry">The command registry for commands that can be resolved</param>
        public CommandResolver(ICommandRegistry registry)
        {
            _registry = registry;
        }

        /// <summary>
        /// Attempts to resolve a valid parsed input to a registered command
        /// </summary>
        /// <param name="parsedInput">The valid parsed input</param>
        /// <returns>A resolved command</returns>
        public ResolvedInput Resolve(ParsedInput parsedInput)
        {
            // is input valid

            if (!parsedInput.IsValid)
                throw new InputResolutionException(parsedInput, "The provided input is invalid and cannot be resolved");

            // resolve individual commands

            var resCmds = new List<ResolvedCommand>();

            foreach (var parsedCmd in parsedInput.ParsedCommands)
                resCmds.Add(Resolve(parsedCmd));

            // return the resolved input

            return new ResolvedInput(resCmds);
        }

        public ResolvedCommand Resolve(ParsedCommand parsedCmd)
        {
            // Try to find the command by progressively shorter paths
            // This handles the case where positional values look like group names
            var (cmdInfo, matchedPathLength) = FindCommandWithLongestPath(parsedCmd);

            if (cmdInfo == null)
                return new ResolvedCommand(parsedCmd, CommandResolutionErrorType.CommandNotFound);

            // begin to accumulate errors

            var errors = new List<ResolveCommandError>();

            // capture argument errors - unknown arguments, options with values

            var inputMap = new Dictionary<ArgumentInfo, ParsedCommandElement>();

            // Track repeated named options for collection types
            var repeatedNamedOptions = new Dictionary<ArgumentInfo, List<ParsedCommandElement>>();

            // Process named arguments (--arg or -a)
            foreach (var node in parsedCmd.Elements.Where(n => n.ElementType == CommandElementType.ArgumentName || n.ElementType == CommandElementType.ArgumentAlias))
            {
                var argInfo = node.ElementType == CommandElementType.ArgumentName
                        ? cmdInfo.Arguments.SingleOrDefault(p => p.Name.Equals(node.Value, StringComparison.OrdinalIgnoreCase))
                        : cmdInfo.Arguments.SingleOrDefault(p => p.Alias.Equals(node.Value.Single()));

                CaptureArgumentErrors(
                    errors,
                    argInfo,
                    node);

                if (argInfo != null)
                {
                    if (!inputMap.ContainsKey(argInfo))
                    {
                        inputMap.Add(argInfo, node);
                    }
                    else if (argInfo.IsCollection)
                    {
                        // For collection types, track repeated options
                        if (!repeatedNamedOptions.ContainsKey(argInfo))
                        {
                            // Initialize with the first occurrence from inputMap
                            repeatedNamedOptions[argInfo] = new List<ParsedCommandElement> { inputMap[argInfo] };
                        }
                        repeatedNamedOptions[argInfo].Add(node);
                    }
                    else
                    {
                        // For scalar types, repeated options are an error
                        errors.Add(new ResolveCommandError
                        {
                            Type = CommandResolutionErrorType.DuplicateScalarArgument,
                            Element = node,
                            Message = $"Argument \"{node.Raw}\" was specified multiple times but the target property is not a collection type."
                        });
                    }
                }
            }

            // Process positional arguments (pass matched path length for correct positional value extraction)
            var isRestValues = new Dictionary<ArgumentInfo, IReadOnlyList<ParsedCommandElement>>();
            ResolvePositionalArguments(parsedCmd, cmdInfo, matchedPathLength, inputMap, isRestValues, errors);

            // Add repeated named options to isRestValues for array population
            foreach (var kvp in repeatedNamedOptions)
            {
                isRestValues[kvp.Key] = kvp.Value.AsReadOnly();
            }

            return new ResolvedCommand(parsedCmd, cmdInfo, new ReadOnlyDictionary<ArgumentInfo, ParsedCommandElement>(inputMap), new ReadOnlyDictionary<ArgumentInfo, IReadOnlyList<ParsedCommandElement>>(isRestValues), errors);
        }

        /// <summary>
        /// Tries to find a command by progressively shorter paths from the parsed command.
        /// This handles the case where positional values appear like group/command names.
        /// Returns the found command and the number of path elements that matched.
        /// </summary>
        private (CommandInfo Command, int PathLength) FindCommandWithLongestPath(ParsedCommand parsedCmd)
        {
            var fullPath = parsedCmd.GetFullCommandPath();
            var pathParts = fullPath.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Try progressively shorter paths from longest to shortest
            for (int length = pathParts.Length; length >= 1; length--)
            {
                var tryPath = string.Join(" ", pathParts.Take(length));
                var cmdInfo = _registry.Find(tryPath);
                if (cmdInfo != null)
                {
                    return (cmdInfo, length);
                }
            }

            return (null, 0);
        }

        /// <summary>
        /// Resolves positional values from the parsed command to positional arguments on the command info.
        /// </summary>
        private void ResolvePositionalArguments(
            ParsedCommand parsedCmd,
            CommandInfo cmdInfo,
            int matchedPathLength,
            Dictionary<ArgumentInfo, ParsedCommandElement> inputMap,
            Dictionary<ArgumentInfo, IReadOnlyList<ParsedCommandElement>> isRestValues,
            List<ResolveCommandError> errors)
        {
            // Get positional arguments from command info, sorted by position
            var positionalArgs = cmdInfo.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .ToList();

            if (positionalArgs.Count == 0)
            {
                // No positional arguments defined - any PositionalValue elements are excess
                var positionalValues = GetPositionalValueElements(parsedCmd, matchedPathLength).ToList();
                if (positionalValues.Any())
                {
                    errors.Add(new ResolveCommandError
                    {
                        Type = CommandResolutionErrorType.ExcessPositionalValues,
                        Element = positionalValues.First(),
                        Message = $"Command '{cmdInfo.Name}' does not accept positional arguments, but received {positionalValues.Count} positional value(s)."
                    });
                }
                return;
            }

            // Get positional value elements from the parsed command
            var positionalValueElements = GetPositionalValueElements(parsedCmd, matchedPathLength).ToList();

            // Find the IsRest argument if any (always the last one if valid)
            var isRestArg = positionalArgs.FirstOrDefault(a => a.IsRest);
            var nonRestArgs = positionalArgs.Where(a => !a.IsRest).ToList();

            // Match positional values to positional arguments
            int positionalIndex = 0;
            foreach (var positionalArg in nonRestArgs)
            {
                if (positionalIndex < positionalValueElements.Count)
                {
                    // Map this positional value to this positional argument
                    inputMap[positionalArg] = positionalValueElements[positionalIndex];
                    positionalIndex++;
                }
                else if (positionalArg.IsRequired)
                {
                    // Required positional argument not provided
                    errors.Add(new ResolveCommandError
                    {
                        Type = CommandResolutionErrorType.MissingRequiredPositional,
                        Element = null,
                        Message = $"Required positional argument '{positionalArg.Name}' at position {positionalArg.Position} was not provided."
                    });
                }
            }

            // Handle remaining values for IsRest or excess
            var remainingCount = positionalValueElements.Count - positionalIndex;
            if (remainingCount > 0)
            {
                if (isRestArg != null)
                {
                    // Collect ALL remaining values for the IsRest argument
                    var restValuesList = positionalValueElements.Skip(positionalIndex).ToList();
                    
                    // Map the first remaining value to InputMap for backward compatibility
                    inputMap[isRestArg] = restValuesList.First();
                    
                    // Store all remaining values in isRestValues for array population
                    isRestValues[isRestArg] = restValuesList.AsReadOnly();
                }
                else
                {
                    // Excess positional values with no IsRest to capture them
                    errors.Add(new ResolveCommandError
                    {
                        Type = CommandResolutionErrorType.ExcessPositionalValues,
                        Element = positionalValueElements[positionalIndex],
                        Message = $"Command '{cmdInfo.Name}' received {remainingCount} excess positional value(s). Expected at most {nonRestArgs.Count} positional argument(s)."
                    });
                }
            }
        }

        /// <summary>
        /// Gets positional value elements from a parsed command, excluding elements that are part of the command path.
        /// </summary>
        /// <param name="parsedCmd">The parsed command</param>
        /// <param name="matchedPathLength">Number of path elements that matched the command (from FindCommandWithLongestPath)</param>
        private IEnumerable<ParsedCommandElement> GetPositionalValueElements(ParsedCommand parsedCmd, int matchedPathLength)
        {
            // Find all PositionalValue elements that come after the command path
            int pathElementsSkipped = 0;
            bool foundCommand = false;

            foreach (var element in parsedCmd.Elements)
            {
                // Skip empty elements
                if (element.ElementType == CommandElementType.Empty)
                    continue;

                // Skip Command and PositionalValue elements that form the command path
                if (!foundCommand && (element.ElementType == CommandElementType.Command || element.ElementType == CommandElementType.PositionalValue))
                {
                    pathElementsSkipped++;
                    if (pathElementsSkipped >= matchedPathLength)
                    {
                        foundCommand = true;
                    }
                    continue;
                }

                // After the command path, collect PositionalValue elements
                if (foundCommand && element.ElementType == CommandElementType.PositionalValue)
                {
                    yield return element;
                }
            }
        }

        private void CaptureArgumentErrors(
            List<ResolveCommandError> errors,
            ArgumentInfo argInfo,
            ParsedCommandElement node)
        {
            var qualifier = node.ElementType == CommandElementType.ArgumentName ? "name" : "alias";

            if (argInfo == null) // argument not found
                errors.Add(new ResolveCommandError
                {
                    Type = CommandResolutionErrorType.ArgumentNotFound,
                    Element = node,
                    Message = $"No argument property matching {qualifier} \"{node.Raw}\" could be found."
                });
            else if (argInfo.PropertyInfo.PropertyTypeName == typeof(Option).AssemblyQualifiedName && node.IsPairedWith != null) // option found with a value
                errors.Add(new ResolveCommandError
                {
                    Type = CommandResolutionErrorType.UnexpectedValue,
                    Element = node,
                    Message = $"Argument \"{node.Raw}\" has an associated value, but resolves to a property of type {typeof(Option).FullName} (options can not have associated values)"
                });
        }
    }
}
