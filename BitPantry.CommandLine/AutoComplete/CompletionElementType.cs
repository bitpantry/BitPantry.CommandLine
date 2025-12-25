namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// The type of element being completed.
/// </summary>
public enum CompletionElementType
{
    /// <summary>Empty input - suggest commands.</summary>
    Empty,

    /// <summary>Command or command group name.</summary>
    Command,

    /// <summary>Argument name (--name).</summary>
    ArgumentName,

    /// <summary>Argument alias (-n).</summary>
    ArgumentAlias,

    /// <summary>Argument value (named argument).</summary>
    ArgumentValue,

    /// <summary>Positional argument value.</summary>
    Positional
}
