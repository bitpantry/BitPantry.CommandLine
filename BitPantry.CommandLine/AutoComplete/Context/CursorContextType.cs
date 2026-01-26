namespace BitPantry.CommandLine.AutoComplete.Context
{
    /// <summary>
    /// Represents the semantic meaning of the cursor position within CLI syntax.
    /// This tells the autocomplete system what kind of element can be typed at this position.
    /// </summary>
    public enum CursorContextType
    {
        /// <summary>
        /// Cannot determine valid context or cursor is in an invalid position.
        /// No autocomplete suggestions should be shown.
        /// </summary>
        Empty,

        /// <summary>
        /// At root level or after whitespace at root - could be a group name or command name.
        /// Example: "|" or "ser|" at the beginning of input.
        /// </summary>
        GroupOrCommand,

        /// <summary>
        /// After a known group - must be a command or subgroup within that group.
        /// Example: "server |" where "server" is a registered group.
        /// Autocomplete should show both commands AND nested subgroups in the resolved group.
        /// </summary>
        CommandOrSubgroupInGroup,

        /// <summary>
        /// Position expects a named argument with -- prefix.
        /// Example: "myCommand --|" or "myCommand --ho|"
        /// </summary>
        ArgumentName,

        /// <summary>
        /// Position expects a named argument alias with - prefix.
        /// Example: "myCommand -|" or "myCommand -h|"
        /// </summary>
        ArgumentAlias,

        /// <summary>
        /// Position expects a value for a named argument.
        /// Example: "myCommand --host |" or "myCommand --host loc|"
        /// </summary>
        ArgumentValue,

        /// <summary>
        /// Position expects a value for a positional parameter.
        /// Example: "myCommand |" where myCommand has positional parameters defined.
        /// </summary>
        PositionalValue,

        /// <summary>
        /// Typing an incomplete prefix (just "--" or "-").
        /// Example: "myCommand --|" with no characters after the prefix yet.
        /// </summary>
        PartialPrefix
    }
}
