using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxServer.Commands;

/// <summary>
/// Tests interactive prompts over SignalR.
/// Exercises the ConfirmationPrompt and TextPrompt paths that require
/// ReadKey RPC round-trips between server and client.
///
/// Usage (after 'server connect'):
///   remoteprompt                  → ConfirmationPrompt (y/n)
///   remoteprompt --style text     → TextPrompt (free text input)
///   remoteprompt --style select   → SelectionPrompt (arrow keys)
/// </summary>
[Command(Name = "remoteprompt")]
[Description("Test interactive Spectre.Console prompts over SignalR")]
public class RemotePromptCommand : CommandBase
{
    [Argument(Name = "style")]
    [Description("Prompt style: confirm (default), text, or select")]
    public string Style { get; set; } = "confirm";

    public Task Execute(CommandExecutionContext ctx)
    {
        switch (Style.ToLowerInvariant())
        {
            case "confirm":
                RunConfirmPrompt();
                break;
            case "text":
                RunTextPrompt();
                break;
            case "select":
                RunSelectionPrompt();
                break;
            default:
                Console.MarkupLine($"[red]Unknown style: {Style}. Use confirm, text, or select.[/]");
                break;
        }

        return Task.CompletedTask;
    }

    private void RunConfirmPrompt()
    {
        Console.MarkupLine("[blue][[REMOTE]][/] Testing ConfirmationPrompt...");
        var confirmed = Console.Prompt(new ConfirmationPrompt("Do you want to proceed?"));
        Console.MarkupLine(confirmed
            ? "[green]You confirmed![/]"
            : "[yellow]You declined.[/]");
    }

    private void RunTextPrompt()
    {
        Console.MarkupLine("[blue][[REMOTE]][/] Testing TextPrompt...");
        var name = Console.Prompt(new TextPrompt<string>("What is your [green]name[/]?"));
        Console.MarkupLine($"[green]Hello, {name.EscapeMarkup()}![/]");
    }

    private void RunSelectionPrompt()
    {
        Console.MarkupLine("[blue][[REMOTE]][/] Testing SelectionPrompt...");
        var choice = Console.Prompt(
            new SelectionPrompt<string>()
                .Title("Pick a [green]color[/]:")
                .AddChoices("Red", "Green", "Blue", "Yellow"));
        Console.MarkupLine($"[green]You picked: {choice}[/]");
    }
}
