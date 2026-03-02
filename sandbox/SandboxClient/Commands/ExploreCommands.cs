using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

/// <summary>
/// Command group for exploring file path autocomplete and theme features.
/// Use 'explore browse' to test real filesystem autocomplete locally.
/// Use 'explore theme' to inspect the current client-side theme.
/// </summary>
[Group(Name = "explore")]
[Description("Explore file path autocomplete and theme features")]
public class ExploreGroup { }

/// <summary>
/// Browse the local file system using the built-in FilePathAutoCompleteHandler.
/// Type 'explore browse ' and press Tab to autocomplete real filesystem paths.
/// Directories are styled using Theme.MenuGroup (cyan by default).
/// </summary>
[InGroup<ExploreGroup>]
[Command(Name = "browse")]
[Description("Browse local filesystem with real path autocomplete")]
public class ExploreBrowseCommand : CommandBase
{
    [Argument(Name = "path", Position = 0)]
    [Description("File or directory path (Tab for autocomplete)")]
    [FilePathAutoComplete]
    public string Path { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
    {
        if (string.IsNullOrWhiteSpace(Path))
        {
            AnsiConsole.MarkupLine("[yellow]No path specified.[/] Type 'explore browse ' and press Tab to see autocomplete.");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Selected path:[/] {Markup.Escape(Path)}");
    }
}

/// <summary>
/// Displays the current Theme's style properties.
/// Shows the client-side theme used for syntax highlighting and autocomplete menu rendering.
/// </summary>
[InGroup<ExploreGroup>]
[Command(Name = "theme")]
[Description("Display current theme style properties")]
public class ExploreThemeCommand : CommandBase
{
    private readonly Theme _theme;

    public ExploreThemeCommand(Theme theme)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    public void Execute(CommandExecutionContext ctx)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Foreground[/]")
            .AddColumn("[bold]Background[/]")
            .AddColumn("[bold]Decoration[/]")
            .AddColumn("[bold]Preview[/]");

        AddRow(table, "Group", _theme.Group);
        AddRow(table, "Command", _theme.Command);
        AddRow(table, "ArgumentName", _theme.ArgumentName);
        AddRow(table, "ArgumentAlias", _theme.ArgumentAlias);
        AddRow(table, "ArgumentValue", _theme.ArgumentValue);
        AddRow(table, "GhostText", _theme.GhostText);
        AddRow(table, "Default", _theme.Default);
        AddRow(table, "MenuHighlight", _theme.MenuHighlight);
        AddRow(table, "MenuGroup", _theme.MenuGroup);

        AnsiConsole.MarkupLine("[bold]Client Theme[/]");
        AnsiConsole.Write(table);
    }

    private static void AddRow(Table table, string name, Style style)
    {
        var fg = style.Foreground.ToString();
        var bg = style.Background.ToString();
        var dec = style.Decoration.ToString();

        table.AddRow(
            new Markup(Markup.Escape(name)),
            new Markup(Markup.Escape(fg)),
            new Markup(Markup.Escape(bg)),
            new Markup(Markup.Escape(dec)),
            new Markup("sample", style));
    }
}
