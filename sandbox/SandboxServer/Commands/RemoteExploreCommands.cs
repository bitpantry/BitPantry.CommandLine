using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxServer.Commands;

/// <summary>
/// Remote command group for exploring file path autocomplete and theme transmission.
/// Use 'rexplore browse' to test server-side sandboxed filesystem autocomplete.
/// Use 'rexplore theme' to verify the client's theme was transmitted to the server.
/// </summary>
[Group(Name = "rexplore")]
[Description("Explore remote file path autocomplete and theme transmission")]
public class RemoteExploreGroup { }

/// <summary>
/// Browse the server's sandboxed file system using the built-in FilePathAutoCompleteHandler.
/// When connected ('rexplore browse'), Tab autocompletes paths within the server's storage root.
/// Directories are styled using the client's Theme.MenuGroup (transmitted over SignalR).
/// </summary>
[InGroup<RemoteExploreGroup>]
[Command(Name = "browse")]
[Description("Browse server filesystem with real path autocomplete")]
public class RemoteExploreBrowseCommand : CommandBase
{
    [Argument(Name = "path", Position = 0)]
    [Description("File or directory path on server (Tab for autocomplete)")]
    [FilePathAutoComplete]
    public string Path { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
    {
        if (string.IsNullOrWhiteSpace(Path))
        {
            AnsiConsole.MarkupLine("[blue][[REMOTE]][/] [yellow]No path specified.[/] Type 'explore browse ' and press Tab.");
            return;
        }

        AnsiConsole.MarkupLine($"[blue][[REMOTE]][/] [green]Selected path:[/] {Markup.Escape(Path)}");
    }
}

/// <summary>
/// Displays the Theme as seen by the server.
/// When a client connects, its Theme is transmitted via CreateClientRequest.
/// This command proves the theme arrived intact by displaying all style properties
/// as the server sees them.
/// </summary>
[InGroup<RemoteExploreGroup>]
[Command(Name = "theme")]
[Description("Display theme received from client (proves transmission)")]
public class RemoteExploreThemeCommand : CommandBase
{
    private readonly Theme _theme;

    public RemoteExploreThemeCommand(Theme theme)
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

        Console.MarkupLine("[blue][[REMOTE]][/] [bold]Server-Side Theme (received from client)[/]");
        Console.Write(table);
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
