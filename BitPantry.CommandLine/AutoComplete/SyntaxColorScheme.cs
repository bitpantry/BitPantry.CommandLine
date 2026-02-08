using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Static convenience accessor that delegates to a default <see cref="Theme"/> instance.
/// Retained for backward compatibility. New code should inject and use <see cref="Theme"/> directly.
/// </summary>
public static class SyntaxColorScheme
{
    private static readonly Theme _default = new Theme();

    public static Style Group => _default.Group;
    public static Style Command => _default.Command;
    public static Style ArgumentName => _default.ArgumentName;
    public static Style ArgumentAlias => _default.ArgumentAlias;
    public static Style ArgumentValue => _default.ArgumentValue;
    public static Style GhostText => _default.GhostText;
    public static Style Default => _default.Default;
    public static Style MenuHighlight => _default.MenuHighlight;
    public static Style MenuGroup => _default.MenuGroup;
}
