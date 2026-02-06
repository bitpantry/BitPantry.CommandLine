using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Static mapping of semantic token types to Spectre.Console styles.
/// Used for syntax highlighting of command line input.
/// </summary>
public static class SyntaxColorScheme
{
    /// <summary>
    /// Style for group names (resolved or uniquely partial). Cyan foreground.
    /// </summary>
    public static Style Group { get; } = new Style(foreground: Color.Cyan);

    /// <summary>
    /// Style for command names. Default (no styling).
    /// </summary>
    public static Style Command { get; } = Style.Plain;

    /// <summary>
    /// Style for argument names (e.g., --verbose). Yellow foreground.
    /// </summary>
    public static Style ArgumentName { get; } = new Style(foreground: Color.Yellow);

    /// <summary>
    /// Style for argument aliases (e.g., -v). Yellow foreground.
    /// </summary>
    public static Style ArgumentAlias { get; } = new Style(foreground: Color.Yellow);

    /// <summary>
    /// Style for argument values. Purple foreground.
    /// </summary>
    public static Style ArgumentValue { get; } = new Style(foreground: Color.Purple);

    /// <summary>
    /// Style for ghost text (autocomplete suggestions). Dim decoration.
    /// </summary>
    public static Style GhostText { get; } = new Style(decoration: Decoration.Dim);

    /// <summary>
    /// Style for unrecognized or default text. No styling.
    /// </summary>
    public static Style Default { get; } = Style.Plain;
}
