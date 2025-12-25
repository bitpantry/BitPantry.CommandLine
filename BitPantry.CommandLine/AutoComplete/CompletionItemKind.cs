namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// The kind of completion item (for visual differentiation).
/// </summary>
public enum CompletionItemKind
{
    /// <summary>A command (e.g., "list", "connect").</summary>
    Command,

    /// <summary>A command group/namespace (e.g., "file", "config").</summary>
    CommandGroup,

    /// <summary>An argument name (--name, --output).</summary>
    ArgumentName,

    /// <summary>An argument alias (-n, -o).</summary>
    ArgumentAlias,

    /// <summary>A value for an argument.</summary>
    ArgumentValue,

    /// <summary>A file path.</summary>
    File,

    /// <summary>A directory path.</summary>
    Directory
}
