using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete.Context
{
    /// <summary>
    /// Encapsulates all intermediate state computed during cursor context resolution.
    /// Eliminates parameter threading through method chains and makes dependencies explicit.
    /// </summary>
    internal class ResolutionState
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
}
