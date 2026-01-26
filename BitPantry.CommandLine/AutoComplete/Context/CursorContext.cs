using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete.Context
{
    /// <summary>
    /// Provides rich context about the cursor position within CLI input.
    /// Combines what has been typed (ParsedInput) with what is valid (registry schema).
    /// This is the primary input for the autocomplete system to determine available options.
    /// </summary>
    public class CursorContext
    {
        /// <summary>
        /// The determined semantic context type at the cursor position.
        /// </summary>
        public CursorContextType ContextType { get; init; }

        /// <summary>
        /// Partial text at cursor for filtering suggestions.
        /// Example: "conn" in "server conn|" where | is cursor.
        /// Empty string if cursor is at whitespace or start of new element.
        /// </summary>
        public string QueryText { get; init; } = string.Empty;

        /// <summary>
        /// The 1-based start position where replacement should begin.
        /// Used to determine where to insert/replace text when accepting a suggestion.
        /// </summary>
        public int ReplacementStart { get; init; }

        /// <summary>
        /// The resolved group if the cursor is within a group context.
        /// Null if at root level or no group has been identified.
        /// </summary>
        public GroupInfo? ResolvedGroup { get; init; }

        /// <summary>
        /// The resolved command if a command has been fully identified before the cursor.
        /// Null if still in command/group selection phase.
        /// </summary>
        public CommandInfo? ResolvedCommand { get; init; }

        /// <summary>
        /// The specific argument being completed when ContextType is ArgumentValue.
        /// Null for other context types.
        /// </summary>
        public ArgumentInfo? TargetArgument { get; init; }

        /// <summary>
        /// For positional value context: which positional parameter index (0-based).
        /// Null if not a positional context.
        /// </summary>
        public int? PositionalIndex { get; init; }

        /// <summary>
        /// Set of arguments that have already been provided in the input.
        /// Used to filter out already-used arguments from suggestions.
        /// </summary>
        public IReadOnlySet<ArgumentInfo> UsedArguments { get; init; } = new HashSet<ArgumentInfo>();

        /// <summary>
        /// The underlying parsed input representation.
        /// </summary>
        public ParsedInput ParsedInput { get; init; }

        /// <summary>
        /// The element at or immediately before the cursor position, if any.
        /// Null if cursor is in whitespace with no adjacent element.
        /// </summary>
        public ParsedCommandElement? ActiveElement { get; init; }

        /// <summary>
        /// The 1-based cursor position in the input.
        /// </summary>
        public int CursorPosition { get; init; }

        /// <summary>
        /// Creates an Empty context indicating no valid autocomplete position.
        /// </summary>
        public static CursorContext Empty(string input, int cursorPosition) => new()
        {
            ContextType = CursorContextType.Empty,
            CursorPosition = cursorPosition,
            ReplacementStart = cursorPosition,
            ParsedInput = new ParsedInput(input)
        };
    }
}
