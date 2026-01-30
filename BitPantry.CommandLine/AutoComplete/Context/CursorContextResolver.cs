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

        /// <summary>
        /// Creates a new CursorContextResolver.
        /// </summary>
        /// <param name="registry">The command registry for schema lookups.</param>
        public CursorContextResolver(ICommandRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Resolves the cursor context for the given input and buffer position.
        /// Converts the 0-based buffer position to 1-based cursor position internally.
        /// </summary>
        /// <param name="input">The current input text.</param>
        /// <param name="bufferPosition">The 0-based cursor position in the buffer.</param>
        /// <returns>The resolved cursor context.</returns>
        public CursorContext ResolveContext(string input, int bufferPosition)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            var cursorPosition = ComputeCursorPosition(input, bufferPosition);
            return Resolve(input, cursorPosition);
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
                return CursorContextFactory.CreateRootContext(input, cursorPosition);
            }

            var parsedInput = new ParsedInput(input);
            
            // Get the first command segment (we work with single commands for now)
            var parsedCommand = parsedInput.ParsedCommands.FirstOrDefault();
            if (parsedCommand == null)
            {
                return CursorContextFactory.CreateRootContext(input, cursorPosition);
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
                return CursorContextFactory.CreateGroupChildContext(state);
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
            return CursorContextFactory.CreateRootLevelContext(state);
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
                CommandElementType.ArgumentName => CursorContextFactory.CreateArgumentNameContext(state),
                CommandElementType.ArgumentAlias => CursorContextFactory.CreateArgumentAliasContext(state),
                CommandElementType.ArgumentValue => CursorContextFactory.CreateArgumentValueContext(state),
                CommandElementType.PositionalValue => CursorContextFactory.CreatePositionalValueContext(state, GetPositionalIndex, GetPositionalArgument),
                CommandElementType.Unexpected => CursorContextFactory.CheckForPartialPrefix(state),
                CommandElementType.EndOfOptions => CursorContextFactory.CreateEndOfOptionsContext(state, CountConsumedPositionals),
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
                return CursorContextFactory.CreatePositionalContextForEmptySlot(state);
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

        #region Helper Methods

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
        /// Gets the positional argument at the specified index.
        /// </summary>
        private ArgumentInfo GetPositionalArgument(CommandInfo command, int index)
        {
            return command.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .Skip(index)
                .FirstOrDefault();
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

        /// <summary>
        /// Converts a 0-based buffer position to a 1-based cursor position
        /// with special handling for end-of-input and beginning-of-input cases.
        /// </summary>
        private int ComputeCursorPosition(string input, int bufferPosition)
        {
            if (string.IsNullOrEmpty(input))
            {
                return 1;
            }
            else if (input.EndsWith(" ") || bufferPosition == 0)
            {
                return bufferPosition + 1;
            }
            else
            {
                return bufferPosition;
            }
        }

        #endregion
    }
}
