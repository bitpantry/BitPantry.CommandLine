using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete.Context
{
    /// <summary>
    /// Resolves the semantic context of a cursor position within CLI input.
    /// Combines parsing information with registry schema knowledge to determine
    /// what kind of element can be typed at the current position.
    /// </summary>
    public class CursorContextResolver
    {
        private readonly ICommandRegistry _registry;

        #region Resolution State

        /// <summary>
        /// Encapsulates all intermediate state computed during resolution.
        /// Eliminates parameter threading through method chains and makes
        /// dependencies explicit.
        /// </summary>
        private class ResolutionState
        {
            /// <summary>The original input string.</summary>
            public string Input { get; init; }

            /// <summary>The 1-based cursor position in the input.</summary>
            public int CursorPosition { get; init; }

            /// <summary>The fully parsed input representation.</summary>
            public ParsedInput ParsedInput { get; init; }

            /// <summary>The parsed command segment being analyzed.</summary>
            public ParsedCommand ParsedCommand { get; init; }

            /// <summary>The element at or near the cursor position.</summary>
            public ParsedCommandElement Element { get; init; }

            /// <summary>The resolved group from path resolution (may be null).</summary>
            public GroupInfo ResolvedGroup { get; init; }

            /// <summary>The resolved command from path resolution (may be null).</summary>
            public CommandInfo ResolvedCommand { get; init; }

            /// <summary>The position where the command path (groups + command) ends.</summary>
            public int PathEndPosition { get; init; }

            /// <summary>Set of arguments already used in the input (computed lazily).</summary>
            public HashSet<ArgumentInfo> UsedArguments { get; set; }

            /// <summary>Count of positional values consumed before cursor (computed lazily).</summary>
            public int ConsumedPositionalCount { get; set; }
        }

        #endregion

        /// <summary>
        /// Creates a new CursorContextResolver.
        /// </summary>
        /// <param name="registry">The command registry for schema lookups.</param>
        public CursorContextResolver(ICommandRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Resolves the cursor context for the given input and position.
        /// </summary>
        /// <param name="input">The complete input string.</param>
        /// <param name="cursorPosition">The 1-based cursor position.</param>
        /// <returns>A CursorContext describing what can be typed at this position.</returns>
        public CursorContext Resolve(string input, int cursorPosition)
        {
            if (string.IsNullOrEmpty(input))
            {
                return CreateRootContext(input, cursorPosition);
            }

            var parsedInput = new ParsedInput(input);
            
            // Get the first command segment (we work with single commands for now)
            var parsedCommand = parsedInput.ParsedCommands.FirstOrDefault();
            if (parsedCommand == null)
            {
                return CreateRootContext(input, cursorPosition);
            }

            // Get element at cursor - use cursor-aware method to handle edge cases
            var element = parsedCommand.GetElementAtCursorPosition(cursorPosition);

            // Resolve the command path (groups and command) from the elements before cursor
            var (resolvedGroup, resolvedCommand, pathEndPosition) = ResolvePath(parsedCommand, cursorPosition);

            // Build resolution state to pass through the resolution pipeline
            var state = new ResolutionState
            {
                Input = input,
                CursorPosition = cursorPosition,
                ParsedInput = parsedInput,
                ParsedCommand = parsedCommand,
                Element = element,
                ResolvedGroup = resolvedGroup,
                ResolvedCommand = resolvedCommand,
                PathEndPosition = pathEndPosition
            };

            // Determine context based on what we've resolved and cursor position
            return DetermineContext(state);
        }

        /// <summary>
        /// Creates context for empty/root position.
        /// </summary>
        private CursorContext CreateRootContext(string input, int cursorPosition)
        {
            return new CursorContext
            {
                ContextType = CursorContextType.GroupOrCommand,
                QueryText = input?.Trim() ?? string.Empty,
                CursorPosition = cursorPosition,
                ReplacementStart = 1,
                ParsedInput = new ParsedInput(input ?? string.Empty)
            };
        }

        /// <summary>
        /// Resolves the group/command path from parsed elements.
        /// Returns the resolved group, command, and the position where the path ends.
        /// </summary>
        private (GroupInfo resolvedGroup, CommandInfo resolvedCommand, int pathEndPosition) ResolvePath(
            ParsedCommand parsedCommand, 
            int cursorPosition)
        {
            GroupInfo currentGroup = null;
            CommandInfo resolvedCommand = null;
            int pathEndPosition = 0;

            foreach (var element in parsedCommand.Elements)
            {
                // Skip empty elements
                if (element.ElementType == CommandElementType.Empty)
                    continue;

                // Stop if we've passed the cursor
                if (element.StartPosition > cursorPosition)
                    break;

                // Only consider elements fully before cursor for path resolution
                // (elements the cursor is ON might be partial)
                bool isFullyBeforeCursor = element.EndPosition < cursorPosition;
                bool isCursorOnElement = element.StartPosition <= cursorPosition && cursorPosition <= element.EndPosition;

                // Command and PositionalValue can both represent group/command path elements
                if (element.ElementType == CommandElementType.Command ||
                    element.ElementType == CommandElementType.PositionalValue)
                {
                    // Try to resolve as group first
                    var groupPath = currentGroup != null 
                        ? $"{currentGroup.FullPath} {element.Value}"
                        : element.Value;
                    var group = _registry.FindGroup(groupPath);

                    if (group != null && isFullyBeforeCursor)
                    {
                        currentGroup = group;
                        pathEndPosition = element.EndPosition;
                        continue;
                    }

                    // Try to resolve as command
                    var cmd = _registry.FindCommand(element.Value, currentGroup);
                    if (cmd != null && isFullyBeforeCursor)
                    {
                        resolvedCommand = cmd;
                        pathEndPosition = element.EndPosition;
                        break; // Command found, path resolution complete
                    }

                    // If cursor is on this element, it's a partial match
                    if (isCursorOnElement)
                    {
                        pathEndPosition = element.StartPosition - 1;
                        break;
                    }
                }
                else if (element.ElementType == CommandElementType.ArgumentName ||
                         element.ElementType == CommandElementType.ArgumentAlias ||
                         element.ElementType == CommandElementType.ArgumentValue)
                {
                    // Non-command/group element (argument, etc.) - path resolution stops
                    break;
                }
            }

            return (currentGroup, resolvedCommand, pathEndPosition);
        }

        /// <summary>
        /// Determines the appropriate context type based on resolved path and cursor position.
        /// </summary>
        private CursorContext DetermineContext(ResolutionState state)
        {
            // If we have a resolved command, context depends on what's at/after cursor
            if (state.ResolvedCommand != null)
            {
                return DetermineCommandArgumentContext(state);
            }

            // If we have a resolved group but no command, cursor can be command/subgroup
            if (state.ResolvedGroup != null)
            {
                return CreateGroupChildContext(state);
            }

            // No group or command resolved - check if there are committed but unresolved tokens
            // A committed token is one that's fully typed (followed by space or at end)
            if (HasUnresolvedCommittedToken(state.ParsedCommand, state.CursorPosition))
            {
                // User typed something that doesn't match any group/command and committed it with space
                // No valid autocomplete context exists
                return CursorContext.Empty(state.Input, state.CursorPosition);
            }

            // At root level with partial or no input
            return CreateRootLevelContext(state);
        }

        /// <summary>
        /// Determines context when cursor is after a resolved command (arguments).
        /// </summary>
        private CursorContext DetermineCommandArgumentContext(ResolutionState state)
        {
            // Compute and cache derived state values
            state.UsedArguments = CollectUsedArguments(state.ParsedCommand, state.ResolvedCommand);
            state.ConsumedPositionalCount = CountConsumedPositionals(
                state.ParsedCommand, state.CursorPosition, state.PathEndPosition);

            // Check if cursor is in a gap immediately before an existing token
            // GetElementAtCursorPosition returns the previous element when cursor is at start of Empty element,
            // so we need to check if there's a non-empty element that starts right after cursor position
            if (HasImmediatelyFollowingToken(state.ParsedCommand, state.CursorPosition))
            {
                return CursorContext.Empty(state.Input, state.CursorPosition);
            }

            // No element at cursor or cursor is on empty space
            if (state.Element == null || state.Element.ElementType == CommandElementType.Empty)
            {
                return DetermineEmptySlotContext(state);
            }

            // Determine context based on element type
            return state.Element.ElementType switch
            {
                CommandElementType.ArgumentName => CreateArgumentNameContext(state),
                CommandElementType.ArgumentAlias => CreateArgumentAliasContext(state),
                CommandElementType.ArgumentValue => CreateArgumentValueContext(state),
                CommandElementType.PositionalValue => CreatePositionalValueContext(state),
                CommandElementType.Unexpected => CheckForPartialPrefix(state),
                CommandElementType.EndOfOptions => CreateEndOfOptionsContext(state),
                _ => CursorContext.Empty(state.Input, state.CursorPosition)
            };
        }

        /// <summary>
        /// Determines context when cursor is on empty space after a command.
        /// </summary>
        private CursorContext DetermineEmptySlotContext(ResolutionState state)
        {
            // After EndOfOptions (--), everything is positional
            if (state.Element != null && state.Element.IsAfterEndOfOptions)
            {
                return CreatePositionalContextForEmptySlot(state);
            }

            // Check if the previous element is an argument name/alias waiting for a value
            var pendingArg = FindPendingArgumentForValue(
                state.ParsedCommand, state.CursorPosition, state.ResolvedCommand);
            if (pendingArg != null)
            {
                return new CursorContext
                {
                    ContextType = CursorContextType.ArgumentValue,
                    QueryText = string.Empty,
                    CursorPosition = state.CursorPosition,
                    ReplacementStart = state.CursorPosition,
                    ResolvedCommand = state.ResolvedCommand,
                    TargetArgument = pendingArg,
                    UsedArguments = state.UsedArguments,
                    ParsedInput = state.ParsedInput
                };
            }

            // Check if we have unfilled positional parameters
            var positionalArgs = state.ResolvedCommand.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .ToList();
            
            if (state.ConsumedPositionalCount < positionalArgs.Count)
            {
                var nextPositional = positionalArgs[state.ConsumedPositionalCount];
                return new CursorContext
                {
                    ContextType = CursorContextType.PositionalValue,
                    QueryText = string.Empty,
                    CursorPosition = state.CursorPosition,
                    ReplacementStart = state.CursorPosition,
                    ResolvedCommand = state.ResolvedCommand,
                    TargetArgument = nextPositional,
                    PositionalIndex = state.ConsumedPositionalCount,
                    UsedArguments = state.UsedArguments,
                    ParsedInput = state.ParsedInput
                };
            }

            // No unfilled positionals - user must explicitly type -- or - to get suggestions
            // This follows the POSIX positional-first modal pattern used by git, kubectl, etc.
            return new CursorContext
            {
                ContextType = CursorContextType.Empty,
                QueryText = string.Empty,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.CursorPosition,
                ResolvedCommand = state.ResolvedCommand,
                UsedArguments = state.UsedArguments,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates context for an argument name element.
        /// </summary>
        private CursorContext CreateArgumentNameContext(ResolutionState state)
        {
            return new CursorContext
            {
                ContextType = CursorContextType.ArgumentName,
                QueryText = state.Element.Value,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element.StartPosition,
                ResolvedCommand = state.ResolvedCommand,
                UsedArguments = state.UsedArguments,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates context for an argument alias element.
        /// </summary>
        private CursorContext CreateArgumentAliasContext(ResolutionState state)
        {
            return new CursorContext
            {
                ContextType = CursorContextType.ArgumentAlias,
                QueryText = state.Element.Value,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element.StartPosition,
                ResolvedCommand = state.ResolvedCommand,
                UsedArguments = state.UsedArguments,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Checks if an Unexpected element is actually a partial prefix ("--" or "-").
        /// After EndOfOptions (--), dash-prefixed values are treated as positional values.
        /// </summary>
        private CursorContext CheckForPartialPrefix(ResolutionState state)
        {
            var raw = state.Element.Raw.Trim();
            
            // Check for partial prefix patterns
            if (raw == "--" || raw == "-")
            {
                // After EndOfOptions, dashes are literal positional values, not argument prefixes
                if (state.Element.IsAfterEndOfOptions)
                {
                    return CreatePositionalValueContext(state);
                }
                
                // "--" prefix means user explicitly wants to see argument names
                if (raw == "--")
                {
                    return new CursorContext
                    {
                        ContextType = CursorContextType.ArgumentName,
                        QueryText = string.Empty,
                        CursorPosition = state.CursorPosition,
                        ReplacementStart = state.Element.StartPosition,
                        ResolvedCommand = state.ResolvedCommand,
                        UsedArguments = state.UsedArguments,
                        ActiveElement = state.Element,
                        ParsedInput = state.ParsedInput
                    };
                }
                
                // "-" prefix means user explicitly wants to see argument aliases
                return new CursorContext
                {
                    ContextType = CursorContextType.ArgumentAlias,
                    QueryText = string.Empty,
                    CursorPosition = state.CursorPosition,
                    ReplacementStart = state.Element.StartPosition,
                    ResolvedCommand = state.ResolvedCommand,
                    UsedArguments = state.UsedArguments,
                    ActiveElement = state.Element,
                    ParsedInput = state.ParsedInput
                };
            }

            return CursorContext.Empty(state.Input, state.CursorPosition);
        }

        /// <summary>
        /// Creates context for argument value (after --argName).
        /// </summary>
        private CursorContext CreateArgumentValueContext(ResolutionState state)
        {
            // Find the argument this value belongs to
            var argElement = state.Element.IsPairedWith;
            ArgumentInfo targetArg = null;

            if (argElement != null)
            {
                if (argElement.ElementType == CommandElementType.ArgumentName)
                {
                    targetArg = state.ResolvedCommand.Arguments.FirstOrDefault(a =>
                        string.Equals(a.Name, argElement.Value, StringComparison.OrdinalIgnoreCase));
                }
                else if (argElement.ElementType == CommandElementType.ArgumentAlias)
                {
                    var aliasChar = argElement.Value.Length > 0 ? argElement.Value[0] : '\0';
                    targetArg = state.ResolvedCommand.Arguments.FirstOrDefault(a => a.Alias == aliasChar);
                }
            }

            return new CursorContext
            {
                ContextType = CursorContextType.ArgumentValue,
                QueryText = state.Element.Value,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element.StartPosition,
                ResolvedCommand = state.ResolvedCommand,
                TargetArgument = targetArg,
                UsedArguments = state.UsedArguments,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates context for a positional value.
        /// </summary>
        private CursorContext CreatePositionalValueContext(ResolutionState state)
        {
            var positionalIndex = GetPositionalIndex(state.ParsedCommand, state.Element, state.PathEndPosition);
            var positionalArg = state.ResolvedCommand.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .Skip(positionalIndex)
                .FirstOrDefault();

            return new CursorContext
            {
                ContextType = CursorContextType.PositionalValue,
                QueryText = state.Element.Value,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element.StartPosition,
                ResolvedCommand = state.ResolvedCommand,
                TargetArgument = positionalArg,
                PositionalIndex = positionalIndex,
                UsedArguments = state.UsedArguments,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates context when cursor is on the EndOfOptions (--) marker itself.
        /// When cursor is ON -- (no trailing space), user might be typing --ArgumentName,
        /// so we return PartialPrefix context for argument name completion.
        /// The actual "end of options" behavior only applies when cursor moves AFTER --.
        /// </summary>
        private CursorContext CreateEndOfOptionsContext(ResolutionState state)
        {
            // When cursor is AFTER the -- element (space typed), we're in positional territory
            // GetElementAtCursorPosition returns the -- element when cursor is at EndPosition + 1
            if (state.CursorPosition == state.Element.EndPosition + 1)
            {
                // Recompute consumed positionals for this specific position
                var consumedCount = CountConsumedPositionals(
                    state.ParsedCommand, state.CursorPosition, state.PathEndPosition);
                state.ConsumedPositionalCount = consumedCount;
                return CreatePositionalContextForEmptySlot(state);
            }

            // When cursor is ON the -- element (no space after), this is an explicit request 
            // for argument name completions (Option A: -- prefix = argument names)
            return new CursorContext
            {
                ContextType = CursorContextType.ArgumentName,
                QueryText = string.Empty,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element.StartPosition,
                ResolvedCommand = state.ResolvedCommand,
                UsedArguments = state.UsedArguments,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates positional value context for an empty slot (after EndOfOptions or when positional args expected).
        /// </summary>
        private CursorContext CreatePositionalContextForEmptySlot(ResolutionState state)
        {
            var positionalArgs = state.ResolvedCommand.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .ToList();

            ArgumentInfo targetArg = null;
            if (state.ConsumedPositionalCount < positionalArgs.Count)
            {
                targetArg = positionalArgs[state.ConsumedPositionalCount];
            }

            return new CursorContext
            {
                ContextType = CursorContextType.PositionalValue,
                QueryText = string.Empty,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.CursorPosition,
                ResolvedCommand = state.ResolvedCommand,
                TargetArgument = targetArg,
                PositionalIndex = state.ConsumedPositionalCount,
                UsedArguments = state.UsedArguments,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates context when cursor is within a group (awaiting command/subgroup).
        /// </summary>
        private CursorContext CreateGroupChildContext(ResolutionState state)
        {
            return new CursorContext
            {
                ContextType = CursorContextType.CommandOrSubgroupInGroup,
                QueryText = state.Element?.Value ?? string.Empty,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element?.StartPosition ?? state.CursorPosition,
                ResolvedGroup = state.ResolvedGroup,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates context at root level (group or command).
        /// </summary>
        private CursorContext CreateRootLevelContext(ResolutionState state)
        {
            return new CursorContext
            {
                ContextType = CursorContextType.GroupOrCommand,
                QueryText = state.Element?.Value ?? string.Empty,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element?.StartPosition ?? state.CursorPosition,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Checks if there's a non-empty token that starts immediately at the next position after cursor.
        /// This indicates cursor is in the minimal whitespace gap between tokens (single space),
        /// which is not a genuine insertion point for autocomplete.
        /// </summary>
        private bool HasImmediatelyFollowingToken(ParsedCommand parsedCommand, int cursorPosition)
        {
            // A token "immediately follows" if it starts at cursorPosition + 1
            // This means cursor is on the single space between two tokens
            foreach (var elem in parsedCommand.Elements)
            {
                if (elem.ElementType != CommandElementType.Empty && elem.StartPosition == cursorPosition + 1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if an Empty element is immediately followed by a non-Empty element.
        /// This indicates the cursor is in a single-space gap right before an existing token,
        /// which is not a genuine insertion point for autocomplete.
        /// </summary>
        private bool HasFollowingNonEmptyElement(ParsedCommand parsedCommand, ParsedCommandElement emptyElement)
        {
            var elements = parsedCommand.Elements.ToList();
            var index = elements.IndexOf(emptyElement);
            
            // Check if there's a following element and it's not empty
            if (index >= 0 && index < elements.Count - 1)
            {
                var nextElement = elements[index + 1];
                return nextElement.ElementType != CommandElementType.Empty;
            }
            
            return false;
        }

        /// <summary>
        /// Checks if there are any committed tokens (followed by space) that didn't resolve
        /// to a known group or command. A committed token indicates the user finished typing
        /// that word, so if it doesn't match anything, there's no valid autocomplete context.
        /// </summary>
        private bool HasUnresolvedCommittedToken(ParsedCommand parsedCommand, int cursorPosition)
        {
            foreach (var element in parsedCommand.Elements)
            {
                // Skip empty elements
                if (element.ElementType == CommandElementType.Empty)
                    continue;
                    
                // Only check elements fully before cursor (committed)
                if (element.EndPosition >= cursorPosition)
                    continue;
                    
                // Check if this element looks like a command/group path element
                if (element.ElementType == CommandElementType.Command ||
                    element.ElementType == CommandElementType.PositionalValue)
                {
                    // Try to resolve as group or command
                    var group = _registry.FindGroup(element.Value);
                    var command = _registry.FindCommand(element.Value, null);
                    
                    // If neither resolves, we have an unresolved committed token
                    if (group == null && command == null)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Collects arguments that have already been provided in the input.
        /// </summary>
        private HashSet<ArgumentInfo> CollectUsedArguments(ParsedCommand parsedCommand, CommandInfo commandInfo)
        {
            var usedArgs = new HashSet<ArgumentInfo>();

            foreach (var element in parsedCommand.Elements)
            {
                if (element.ElementType == CommandElementType.ArgumentName)
                {
                    var arg = commandInfo.Arguments.FirstOrDefault(a =>
                        string.Equals(a.Name, element.Value, StringComparison.OrdinalIgnoreCase));
                    if (arg != null)
                        usedArgs.Add(arg);
                }
                else if (element.ElementType == CommandElementType.ArgumentAlias)
                {
                    var aliasChar = element.Value.Length > 0 ? element.Value[0] : '\0';
                    var arg = commandInfo.Arguments.FirstOrDefault(a => a.Alias == aliasChar);
                    if (arg != null)
                        usedArgs.Add(arg);
                }
            }

            return usedArgs;
        }

        /// <summary>
        /// Gets the next unfilled positional argument.
        /// </summary>
        private ArgumentInfo GetNextPositionalArgument(CommandInfo command, HashSet<ArgumentInfo> usedArgs)
        {
            return command.Arguments
                .Where(a => a.IsPositional && !usedArgs.Contains(a))
                .OrderBy(a => a.Position)
                .FirstOrDefault();
        }

        /// <summary>
        /// Counts the number of positional values consumed before the cursor position.
        /// </summary>
        private int CountConsumedPositionals(ParsedCommand parsedCommand, int cursorPosition, int pathEndPosition)
        {
            int count = 0;
            
            foreach (var elem in parsedCommand.Elements)
            {
                // Only count elements before cursor
                if (elem.StartPosition >= cursorPosition)
                    break;
                    
                // Skip empty elements
                if (elem.ElementType == CommandElementType.Empty)
                    continue;
                
                // Skip elements that are part of the command path (before/at pathEndPosition)
                if (elem.EndPosition <= pathEndPosition)
                    continue;
                
                // PositionalValue after the command path is a consumed positional
                if (elem.ElementType == CommandElementType.PositionalValue)
                {
                    count++;
                }
            }
            
            return count;
        }

        /// <summary>
        /// Gets the positional index for a value element.
        /// </summary>
        private int GetPositionalIndex(ParsedCommand parsedCommand, ParsedCommandElement element, int pathEndPosition)
        {
            int index = 0;
            foreach (var elem in parsedCommand.Elements)
            {
                if (elem == element)
                    break;
                
                // Skip empty elements
                if (elem.ElementType == CommandElementType.Empty)
                    continue;
                    
                // Skip elements that are part of the command path
                if (elem.EndPosition <= pathEndPosition)
                    continue;
                    
                // Count positional values that come after the command path
                if (elem.ElementType == CommandElementType.PositionalValue)
                {
                    index++;
                }
            }
            return index;
        }

        /// <summary>
        /// Finds an argument name/alias that is waiting for its value (no value provided yet).
        /// Returns null if no such pending argument exists.
        /// </summary>
        private ArgumentInfo FindPendingArgumentForValue(
            ParsedCommand parsedCommand, 
            int cursorPosition,
            CommandInfo resolvedCommand)
        {
            // Look for the most recent argument name/alias before cursor that hasn't been given a value
            ParsedCommandElement lastArgElement = null;
            
            foreach (var elem in parsedCommand.Elements)
            {
                // Stop if we've passed the cursor
                if (elem.StartPosition >= cursorPosition)
                    break;
                    
                if (elem.ElementType == CommandElementType.ArgumentName ||
                    elem.ElementType == CommandElementType.ArgumentAlias)
                {
                    // Check if this argument already has a value following it
                    // If IsPairedWith is null, this arg is waiting for a value
                    lastArgElement = elem;
                }
                else if (elem.ElementType == CommandElementType.ArgumentValue ||
                         elem.ElementType == CommandElementType.PositionalValue)
                {
                    // A value was provided, so clear the pending argument
                    lastArgElement = null;
                }
            }
            
            if (lastArgElement == null)
                return null;
                
            // Find the ArgumentInfo for this element
            if (lastArgElement.ElementType == CommandElementType.ArgumentName)
            {
                return resolvedCommand.Arguments.FirstOrDefault(a =>
                    string.Equals(a.Name, lastArgElement.Value, StringComparison.OrdinalIgnoreCase));
            }
            else // ArgumentAlias
            {
                var aliasChar = lastArgElement.Value.Length > 0 ? lastArgElement.Value[0] : '\0';
                return resolvedCommand.Arguments.FirstOrDefault(a => a.Alias == aliasChar);
            }
        }
    }
}
