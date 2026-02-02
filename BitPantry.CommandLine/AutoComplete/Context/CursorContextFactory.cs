using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using System;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete.Context
{
    /// <summary>
    /// Factory for creating CursorContext instances from ResolutionState.
    /// Contains all the pure data transformation logic for building context objects.
    /// </summary>
    internal static class CursorContextFactory
    {
        /// <summary>
        /// Creates context for empty/root position.
        /// </summary>
        public static CursorContext CreateRootContext(string input, int cursorPosition)
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
        /// Creates context for an argument name element.
        /// </summary>
        public static CursorContext CreateArgumentNameContext(ResolutionState state)
        {
            return new CursorContext
            {
                ContextType = CursorContextType.ArgumentName,
                QueryText = state.Element.Value,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element.StartPosition,
                ResolvedCommand = state.ResolvedCommand,
                ProvidedValues = state.ProvidedValues,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates context for an argument alias element.
        /// </summary>
        public static CursorContext CreateArgumentAliasContext(ResolutionState state)
        {
            return new CursorContext
            {
                ContextType = CursorContextType.ArgumentAlias,
                QueryText = state.Element.Value,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element.StartPosition,
                ResolvedCommand = state.ResolvedCommand,
                ProvidedValues = state.ProvidedValues,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Checks if an Unexpected element is actually a partial prefix ("--" or "-").
        /// After EndOfOptions (--), dash-prefixed values are treated as positional values.
        /// </summary>
        public static CursorContext CheckForPartialPrefix(ResolutionState state)
        {
            var raw = state.Element.Raw.Trim();

            // Check for partial prefix patterns
            if (raw == "--" || raw == "-")
            {
                // After EndOfOptions, dashes are literal positional values, not argument prefixes
                if (state.Element.IsAfterEndOfOptions)
                {
                    return CreatePositionalValueContext(state, GetPositionalIndex, GetPositionalArgument);
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
                        ProvidedValues = state.ProvidedValues,
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
                    ProvidedValues = state.ProvidedValues,
                    ActiveElement = state.Element,
                    ParsedInput = state.ParsedInput
                };
            }

            return CursorContext.Empty(state.Input, state.CursorPosition);
        }

        /// <summary>
        /// Creates context for argument value (after --argName).
        /// </summary>
        public static CursorContext CreateArgumentValueContext(ResolutionState state)
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
                ProvidedValues = state.ProvidedValues,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates context for a positional value.
        /// </summary>
        public static CursorContext CreatePositionalValueContext(
            ResolutionState state,
            Func<ParsedCommand, ParsedCommandElement, int, int> getPositionalIndex,
            Func<CommandInfo, int, ArgumentInfo> getPositionalArgument)
        {
            var positionalIndex = getPositionalIndex(state.ParsedCommand, state.Element, state.PathEndPosition);
            var positionalArg = getPositionalArgument(state.ResolvedCommand, positionalIndex);

            return new CursorContext
            {
                ContextType = CursorContextType.PositionalValue,
                QueryText = state.Element.Value,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.Element.StartPosition,
                ResolvedCommand = state.ResolvedCommand,
                TargetArgument = positionalArg,
                PositionalIndex = positionalIndex,
                ProvidedValues = state.ProvidedValues,
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
        public static CursorContext CreateEndOfOptionsContext(ResolutionState state)
        {
            // When cursor is AFTER the -- element (space typed), we're in positional territory
            // GetElementAtCursorPosition returns the -- element when cursor is at EndPosition + 1
            if (state.CursorPosition == state.Element.EndPosition + 1)
            {
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
                ProvidedValues = state.ProvidedValues,
                ActiveElement = state.Element,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates positional value context for an empty slot (after EndOfOptions or when positional args expected).
        /// </summary>
        public static CursorContext CreatePositionalContextForEmptySlot(ResolutionState state)
        {
            // Find first unfilled positional using ProvidedValues.ContainsKey as source of truth
            var nextPositional = state.ResolvedCommand.Arguments
                .Where(a => a.IsPositional && !state.ProvidedValues.ContainsKey(a))
                .OrderBy(a => a.Position)
                .FirstOrDefault();

            // Calculate positional index (count of used positional arguments)
            var usedPositionalCount = state.ProvidedValues.Keys.Count(a => a.IsPositional);

            return new CursorContext
            {
                ContextType = CursorContextType.PositionalValue,
                QueryText = string.Empty,
                CursorPosition = state.CursorPosition,
                ReplacementStart = state.CursorPosition,
                ResolvedCommand = state.ResolvedCommand,
                TargetArgument = nextPositional,
                PositionalIndex = usedPositionalCount,
                ProvidedValues = state.ProvidedValues,
                ParsedInput = state.ParsedInput
            };
        }

        /// <summary>
        /// Creates context when cursor is within a group (awaiting command/subgroup).
        /// </summary>
        public static CursorContext CreateGroupChildContext(ResolutionState state)
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
        public static CursorContext CreateRootLevelContext(ResolutionState state)
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

        #region Private Helpers

        /// <summary>
        /// Gets the positional index for a value element.
        /// </summary>
        private static int GetPositionalIndex(ParsedCommand parsedCommand, ParsedCommandElement element, int pathEndPosition)
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
        private static ArgumentInfo GetPositionalArgument(CommandInfo command, int index)
        {
            return command.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .Skip(index)
                .FirstOrDefault();
        }

        #endregion
    }
}
