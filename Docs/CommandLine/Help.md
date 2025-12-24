# Help System

[← Back to Implementer Guide](../ImplementerGuide.md)

BitPantry.CommandLine includes a built-in help system that provides automatic documentation for commands and groups.

## Table of Contents

- [Overview](#overview)
- [Invoking Help](#invoking-help)
  - [Root Help](#root-help)
  - [Group Help](#group-help)
  - [Command Help](#command-help)
- [Help Flag Rules](#help-flag-rules)
- [Reserved Names](#reserved-names)
- [Customizing Help Output](#customizing-help-output)
- [See Also](#see-also)

## Overview

The help system automatically generates documentation from your command and group metadata, including:

- Command and group names
- Descriptions (from `[Description("...")]` attributes on commands, groups, and arguments)
- Arguments with their names, aliases, types, and descriptions
- Usage syntax

Users can discover available functionality without external documentation.

## Invoking Help

### Root Help

When users invoke your application with no arguments or with `--help`/`-h`, the root help is displayed:

```
> myapp
```

or

```
> myapp --help
```

Output:
```
Available commands and groups:

Groups:
  math  Mathematical operations
  files  File management commands

Commands:
  version  Display application version
  lc  Filters and lists registered commands

Run '<command> --help' for more information on a command.
Run '<group>' to see commands in a group.
```

### Group Help

Invoking a group name alone (or with `--help`/`-h`) displays the group's contents:

```
> myapp math
```

or

```
> myapp math --help
```

Output:
```
Group: math
  Mathematical operations

Subgroups:
  advanced  Advanced mathematical functions

Commands:
  add  Adds two numbers
  subtract  Subtracts two numbers

Usage: math <command> [options]

Run 'math <command> --help' for more information on a command.
```

This allows users to explore nested group hierarchies:

```
> myapp math advanced
```

### Command Help

Invoking a command with `--help` or `-h` displays detailed command usage:

```
> myapp math add --help
```

Output:
```
Command: math add
  Adds two numbers

Usage: math add [--num1 <value>] [--num2 <value>]

Arguments:
  --num1, -n  The first number
  --num2, -m  The second number
```

## Help Flag Rules

The help flags (`--help` and `-h`) have specific rules:

### Standalone Requirement

Help flags must be the **only** element after the command or group path. Combining help with other arguments or options is not allowed:

```
> myapp math add --num1 5 --help
error: --help cannot be combined with other arguments
For usage, run: math add --help
```

This applies to both long (`--help`) and short (`-h`) forms.

### Pipeline Restriction

Help flags cannot be used in pipelines:

```
> myapp math add --help | myapp other-cmd
error: --help cannot be combined with other arguments
For usage, run: math add --help
```

### Exit Codes

| Scenario | Exit Code |
|----------|-----------|
| Help displayed successfully | 0 (Success) |
| Help combined with other arguments | Non-zero (HelpValidationError) |

## Reserved Names

The `--help` flag and `-h` alias are **reserved** by the framework. Commands cannot define:

- An argument named `help` (case-insensitive)
- An argument with alias `h`

Attempting to do so causes a startup validation error:

```csharp
// This will fail at startup!
[Command(Name = "bad")]
class BadCommand : CommandBase
{
    [Argument(Name = "help")]  // ERROR: Reserved name
    public string MyArg { get; set; }

    [Argument]
    [Alias('h')]  // ERROR: Reserved alias
    public string OtherArg { get; set; }
}
```

Error message:
```
Command registry validation failed:
  - Reserved name: command 'bad' has argument named 'help'. This is reserved for the help system.
  - Reserved alias: command 'bad' argument 'OtherArg' uses alias 'h'. This is reserved for help.
```

## Customizing Help Output

The help system uses the `IHelpFormatter` interface for rendering. You can provide a custom implementation:

```csharp
public class MyHelpFormatter : IHelpFormatter
{
    public void DisplayGroupHelp(TextWriter writer, GroupInfo group, CommandRegistry registry)
    {
        // Custom group help formatting
    }

    public void DisplayCommandHelp(TextWriter writer, CommandInfo command)
    {
        // Custom command help formatting
    }

    public void DisplayRootHelp(TextWriter writer, CommandRegistry registry)
    {
        // Custom root help formatting
    }
}
```

Register your custom formatter using the fluent `UseHelpFormatter` method:

```csharp
var builder = new CommandLineApplicationBuilder();
builder.UseHelpFormatter<MyHelpFormatter>();
builder.RegisterCommands(typeof(Program));
var app = builder.Build();
```

Or provide an instance directly:

```csharp
var builder = new CommandLineApplicationBuilder();
builder.UseHelpFormatter(new MyHelpFormatter());
builder.RegisterCommands(typeof(Program));
var app = builder.Build();
```

---

## See Also

- [Commands](Commands.md) - Defining commands and groups
- [Command Syntax](CommandSyntax.md) - How commands are invoked
- [Built-in Commands](BuiltInCommands.md) - The `lc` command for listing all commands
- [Description Attribute](Commands.md#description-attribute) - Adding descriptions to commands
