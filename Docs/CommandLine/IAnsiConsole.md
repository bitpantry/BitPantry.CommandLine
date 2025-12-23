# IAnsiConsole

`Spectre.Console.IAnsiConsole`

[← Back to Implementer Guide](../ImplementerGuide.md)

BitPantry.CommandLine uses [Spectre.Console](https://spectreconsole.net/) to abstract all terminal I/O. Spectre.Console is an incredible project that "makes it easier to create beautiful console applications."

## Table of Contents

- [Overview](#overview)
- [Why Use IAnsiConsole?](#why-use-iansiconsole)
- [Common Output Patterns](#common-output-patterns)
- [Remote Execution Considerations](#remote-execution-considerations)
- [Customizing Console Behavior](#customizing-console-behavior)
- [See Also](#see-also)

## Overview

`IAnsiConsole` is the primary interface for all console output in BitPantry.CommandLine. It provides:

- Rich text formatting with markup syntax
- Structured output (tables, trees, panels)
- Interactive components (prompts, progress bars)
- Consistent rendering across different terminals

**Important**: Always use the `Console` property from [CommandBase](CommandBase.md) for output. Never use `System.Console` directly, as this bypasses the framework's output handling and breaks remote command execution.

## Why Use IAnsiConsole?

1. **Remote Compatibility**: Output is correctly routed to remote clients when using SignalR
2. **Consistent Formatting**: Markup renders appropriately based on terminal capabilities
3. **Testability**: Console output can be captured and verified in tests
4. **Rich Features**: Access to Spectre.Console's full feature set

## Common Output Patterns

### Text Output

```csharp
// Simple output
Console.WriteLine("Plain text output");

// With Spectre.Console markup
Console.MarkupLine("[bold blue]Styled text[/]");

// Escaped markup (when you need literal brackets)
Console.MarkupLine("Use [[double brackets]] for literal brackets");
```

### Markup Reference

| Markup | Description |
|--------|-------------|
| `[bold]text[/]` | Bold text |
| `[italic]text[/]` | Italic text |
| `[underline]text[/]` | Underlined text |
| `[red]text[/]` | Red colored text |
| `[green]text[/]` | Green colored text |
| `[yellow]text[/]` | Yellow colored text (warnings) |
| `[blue]text[/]` | Blue colored text |
| `[bold red]text[/]` | Combined styles |

### Tables

```csharp
var table = new Table();
table.AddColumn("Name");
table.AddColumn("Status");
table.AddColumn("Last Run");

table.AddRow("Task 1", "[green]Complete[/]", "2024-01-15");
table.AddRow("Task 2", "[yellow]Pending[/]", "2024-01-14");
table.AddRow("Task 3", "[red]Failed[/]", "2024-01-13");

Console.Write(table);
```

### Progress Bars

```csharp
await Console.Progress()
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask("Processing...");
        
        while (!ctx.IsFinished)
        {
            task.Increment(10);
            await Task.Delay(100);
        }
    });
```

### Prompts (Interactive Input)

```csharp
// Text prompt
var name = Console.Ask<string>("What is your [green]name[/]?");

// Confirmation prompt
if (Console.Confirm("Do you want to continue?"))
{
    Console.MarkupLine("[green]Continuing...[/]");
}

// Selection prompt
var fruit = Console.Prompt(
    new SelectionPrompt<string>()
        .Title("What's your [green]favorite fruit[/]?")
        .AddChoices("Apple", "Banana", "Orange"));
```

### Panels and Rules

```csharp
// Panel with border
var panel = new Panel("Panel content here")
    .Header("[blue]Panel Title[/]")
    .Border(BoxBorder.Rounded);
Console.Write(panel);

// Horizontal rule
Console.Write(new Rule("[yellow]Section Divider[/]"));
```

## Remote Execution Considerations

When commands execute remotely via SignalR, the `IAnsiConsole` instance is a special implementation that:

- Serializes output and sends it to the remote client
- Handles input prompts across the network
- Maintains ANSI escape sequence compatibility

This is why you must always use `Console` from `CommandBase` rather than `System.Console`.

## Customizing Console Behavior

The console is configured during application startup. See [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md) for details on customizing console settings.

For advanced scenarios, you can inject `IAnsiConsole` directly into services:

```csharp
public class MyService
{
    private readonly IAnsiConsole _console;
    
    public MyService(IAnsiConsole console)
    {
        _console = console;
    }
    
    public void DoWork()
    {
        _console.MarkupLine("[green]Working...[/]");
    }
}
```

## See Also

- [CommandBase](CommandBase.md) - Accessing the console from commands
- [Commands](Commands.md) - Building commands with console output
- [Spectre.Console Documentation](https://spectreconsole.net/) - Complete Spectre.Console reference
- [Remote Commands](../Remote/CommandLineServer.md) - How console output works over SignalR