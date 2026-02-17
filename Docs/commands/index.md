# Defining Commands

Commands are the building blocks of a BitPantry.CommandLine application. Each command is a C# class that extends `CommandBase` and is decorated with attributes that define its name, arguments, and behavior.

---

## Overview

A command class requires:

1. **Inherit from `CommandBase`** — Provides access to `Console` (Spectre.Console `IAnsiConsole`) and `Fail()` for error signaling
2. **An `Execute` method** — The entry point for command logic
3. **Attribute decoration** — `[Command]`, `[Argument]`, `[Flag]`, `[Description]`, etc.

```csharp
[Command(Name = "deploy")]
[Description("Deploys the application to a target environment")]
public class DeployCommand : CommandBase
{
    [Argument(Name = "environment", IsRequired = true)]
    [Description("Target environment")]
    public string Environment { get; set; } = "";

    [Argument(Name = "verbose")]
    [Flag]
    public bool Verbose { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Deploying to [bold]{Environment}[/]...");

        if (Verbose)
            Console.MarkupLine("[dim]Verbose output enabled[/]");
    }
}
```

---

## The Execute Method

Commands implement an `Execute` method. Four signatures are supported:

```csharp
// Synchronous, no return
public void Execute(CommandExecutionContext ctx) { }

// Asynchronous, no return
public async Task Execute(CommandExecutionContext ctx) { }

// Synchronous with return value (for piping)
public object Execute(CommandExecutionContext ctx) { return result; }

// Asynchronous with return value
public async Task<object> Execute(CommandExecutionContext ctx) { return result; }
```

The `CommandExecutionContext` provides:

| Property | Type | Description |
|----------|------|-------------|
| `CancellationToken` | `CancellationToken` | Token for cooperative cancellation |
| `CommandRegistry` | `ICommandRegistry` | Access to all registered commands and groups |

For piped commands, use the generic `CommandExecutionContext<T>` which adds an `Input` property containing the output of the previous command.

---

## CommandBase Members

| Member | Description |
|--------|-------------|
| `Console` | `IAnsiConsole` instance for writing output (Spectre.Console) |
| `Fail(string message)` | Throws a `CommandFailedException` to signal an error |
| `Fail(string message, Exception inner)` | Throws with an inner exception |

---

## In This Section

| Page | Description |
|------|-------------|
| [Command Naming](naming.md) | The `[Command]` attribute and name derivation rules |
| [Arguments](arguments.md) | `[Argument]`, `[Alias]`, `[Description]`, required/optional, data types |
| [Positional Arguments](positional-arguments.md) | `Position` property, ordering rules, variadic `IsRest` |
| [Flags](flags.md) | `[Flag]` attribute for presence-only booleans |
| [Command Groups](groups.md) | `[Group]` and `[InGroup<T>]` for hierarchical organization |
| [Error Handling](error-handling.md) | `Fail()`, `CommandFailedException`, `IUserFacingException` |

---

## See Also

- [Command Naming](naming.md)
- [Arguments](arguments.md)
- [Command Groups](groups.md)
- [Registering Commands](../building/registering-commands.md)
- [Getting Started](../quick-start.md)
