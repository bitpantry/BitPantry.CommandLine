using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Configurable theme for command line input styling, including syntax highlighting,
/// ghost text, and autocomplete menu rendering. All properties default to the standard
/// color scheme if not explicitly overridden.
/// </summary>
public class Theme
{
    /// <summary>
    /// Style for group names (resolved or uniquely partial). Default: Cyan foreground.
    /// </summary>
    public Style Group { get; set; } = new Style(foreground: Color.Cyan);

    /// <summary>
    /// Style for command names. Default: no styling.
    /// </summary>
    public Style Command { get; set; } = Style.Plain;

    /// <summary>
    /// Style for argument names (e.g., --verbose). Default: Yellow foreground.
    /// </summary>
    public Style ArgumentName { get; set; } = new Style(foreground: Color.Yellow);

    /// <summary>
    /// Style for argument aliases (e.g., -v). Default: Yellow foreground.
    /// </summary>
    public Style ArgumentAlias { get; set; } = new Style(foreground: Color.Yellow);

    /// <summary>
    /// Style for argument values. Default: Purple foreground.
    /// </summary>
    public Style ArgumentValue { get; set; } = new Style(foreground: Color.Purple);

    /// <summary>
    /// Style for ghost text (autocomplete suggestions). Default: Dim decoration.
    /// </summary>
    public Style GhostText { get; set; } = new Style(decoration: Decoration.Dim);

    /// <summary>
    /// Style for unrecognized or default text. Default: no styling.
    /// </summary>
    public Style Default { get; set; } = Style.Plain;

    /// <summary>
    /// Style for the selected (highlighted) menu item. Default: Inverted colors.
    /// </summary>
    public Style MenuHighlight { get; set; } = new Style(decoration: Decoration.Invert);

    /// <summary>
    /// Style for group items in the autocomplete menu. Default: Cyan foreground.
    /// </summary>
    public Style MenuGroup { get; set; } = new Style(foreground: Color.Cyan);
}
