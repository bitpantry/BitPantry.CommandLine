namespace BitPantry.CommandLine.Input;

/// <summary>
/// Result of attempting to match partial text against the command registry.
/// </summary>
public enum TokenMatchResult
{
    /// <summary>
    /// Text uniquely matches exactly one group.
    /// </summary>
    UniqueGroup,

    /// <summary>
    /// Text uniquely matches exactly one command.
    /// </summary>
    UniqueCommand,

    /// <summary>
    /// Text matches multiple items or mixed types.
    /// </summary>
    Ambiguous,

    /// <summary>
    /// Text doesn't match any registered group/command.
    /// </summary>
    NoMatch
}
