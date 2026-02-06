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
}
