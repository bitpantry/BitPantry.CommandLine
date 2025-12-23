# CommandBase

`BitPantry.CommandLine.API.CommandBase`

[← Back to Implementer Guide](../ImplementerGuide.md)

All command types must extend `CommandBase`, which provides access to console output through Spectre.Console's `IAnsiConsole` interface.

## Table of Contents

- [Overview](#overview)
- [Protected Members](#protected-members)
- [Command Lifecycle](#command-lifecycle)
- [Console Output Patterns](#console-output-patterns)
- [Example Command](#example-command)
- [See Also](#see-also)

## Overview

`CommandBase` is the abstract base class that all commands must inherit from. It provides:

- Access to the configured console for output
- Integration with Spectre.Console for rich terminal formatting
- Lifecycle management handled by the framework

## Protected Members

### Console Property

```csharp
protected IAnsiConsole Console { get; }
```

The `Console` property provides access to the Spectre.Console `IAnsiConsole` interface. Use this for all terminal output in your commands.

```csharp
public class MyCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        // Simple text output
        Console.WriteLine("Hello, World!");
        
        // Rich markup
        Console.MarkupLine("[bold green]Success![/]");
        
        // Tables, progress bars, and more via Spectre.Console
        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Value");
        table.AddRow("Status", "[green]Active[/]");
        Console.Write(table);
    }
}
```

## Command Lifecycle

Commands are instantiated as **transient** instances by default:

1. **Resolution**: The framework resolves the command type from the registry
2. **Activation**: A new instance is created via dependency injection
3. **Console Setup**: The `Console` property is set by the framework
4. **Execution**: Your `Execute` method is called
5. **Disposal**: The instance is disposed after execution completes

Because commands are transient, you should not store state between invocations. Use injected services for any persistent state.

## Console Output Patterns

### Standard Output

```csharp
Console.WriteLine("Standard output");
Console.Write("Without newline");
```

### Styled Output with Markup

```csharp
Console.MarkupLine("[bold]Bold text[/]");
Console.MarkupLine("[red]Error message[/]");
Console.MarkupLine("[yellow]Warning[/]");
Console.MarkupLine("[green]Success[/]");
```

### Structured Output

```csharp
// Tables
var table = new Table();
table.AddColumn("Column 1");
table.AddRow("Value 1");
Console.Write(table);

// Rules (horizontal lines)
Console.Write(new Rule("[yellow]Section Title[/]"));
```

## Example Command

```csharp
using BitPantry.CommandLine.API;
using Spectre.Console;

[Command(Name = "greet")]
[Description("Greets the user")]
public class GreetCommand : CommandBase
{
    [Argument]
    public string Name { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        if (string.IsNullOrEmpty(Name))
        {
            Console.MarkupLine("[yellow]No name provided, using 'World'[/]");
            Name = "World";
        }
        
        Console.MarkupLine($"[green]Hello, {Name}![/]");
    }
}
```

## See Also

- [IAnsiConsole](IAnsiConsole.md) - Spectre.Console integration details
- [Commands](Commands.md) - Complete command definition guide
- [Dependency Injection](DependencyInjection.md) - Injecting services into commands
- [Spectre.Console Documentation](https://spectreconsole.net/) - Full Spectre.Console reference