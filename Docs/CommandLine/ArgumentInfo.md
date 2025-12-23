# ArgumentInfo

`BitPantry.CommandLine.Component.ArgumentInfo`

[← Back to Implementer Guide](../ImplementerGuide.md)

The `ArgumentInfo` object models a structured representation of a [command argument](Commands.md#arguments).

When a [command](Commands.md) type is registered with a [CommandRegistry](CommandRegistry.md), relevant type information is organized into a [CommandInfo](CommandInfo.md) object. [CommandInfo](CommandInfo.md) objects are used by the command line application to resolve, activate, and execute commands. Relevant property information for [command arguments](Commands.md#arguments) is stored in `ArgumentInfo` objects, which are available as a collection on the [CommandInfo](CommandInfo.md) object.

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | The name of the argument |
| `Alias` | `char` | The single-character alias for the argument, or `default(char)` if not defined |
| `Description` | `string` | The argument description from the `[Description]` attribute |
| `PropertyInfo` | `SerializablePropertyInfo` | Metadata about the underlying property |
| `AutoCompleteFunctionName` | `string` | Name of the function that provides auto-complete values |
| `IsAutoCompleteFunctionAsync` | `bool` | Whether the auto-complete function is asynchronous |

## Defining Arguments

Arguments are defined on command classes using the `[Argument]` attribute:

```csharp
[Command(Name = "greet")]
public class GreetCommand : CommandBase
{
    [Argument(IsRequired = true)]
    [Alias('n')]
    [Description("The name to greet")]
    public string Name { get; set; }

    [Argument]
    [Alias('c')]
    [Description("Number of times to repeat")]
    public int Count { get; set; } = 1;

    public void Execute(CommandExecutionContext ctx)
    {
        for (int i = 0; i < Count; i++)
            Console.WriteLine($"Hello, {Name}!");
    }
}
```

### [Argument] Attribute Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | Property name | Override the argument name |
| `IsRequired` | `bool` | `false` | Whether the argument must be provided |
| `AutoCompleteFunctionName` | `string` | `null` | Function name for custom auto-complete |

### [Alias] Attribute

The `[Alias]` attribute defines a single-character shorthand for an argument:

```csharp
[Argument]
[Alias('n')]  // Can use -n instead of --name
public string Name { get; set; }
```

## Usage in Command Syntax

Arguments can be specified using their full name or alias:

```bash
# Using full name
myapp greet --name "World"

# Using alias
myapp greet -n "World"

# Both arguments with full names
myapp greet --name "World" --count 3

# Both arguments with aliases
myapp greet -n "World" -c 3
```

## Required Arguments

When `IsRequired = true`, the command will fail if the argument is not provided:

```csharp
[Argument(IsRequired = true)]
[Description("Required: the file path to process")]
public string FilePath { get; set; }
```

If a required argument is missing, the framework returns a validation error and displays help for the command.

---

## See Also

- [Commands](Commands.md) - Complete command definition guide
- [Command Arguments](Commands.md#arguments) - Argument attribute details
- [CommandInfo](CommandInfo.md) - Command metadata structure
- [CommandSyntax](CommandSyntax.md) - How to invoke commands with arguments