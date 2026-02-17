# Arguments

Arguments define the inputs that a command accepts. They are declared as public properties on the command class and decorated with the `[Argument]` attribute.

---

## The `[Argument]` Attribute

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ArgumentAttribute : Attribute
{
    public string Name { get; set; }
    public bool IsRequired { get; set; } = false;
    public int Position { get; set; } = -1;
    public bool IsRest { get; set; } = false;
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `Name` | Property name | The argument's invocation name (prefixed with `--`) |
| `IsRequired` | `false` | Whether the argument must be provided |
| `Position` | `-1` | Positional index (`-1` = named argument) |
| `IsRest` | `false` | Capture remaining positional values into a collection |

---

## Named Arguments

Named arguments are specified with `--name value` syntax:

```csharp
[Command(Name = "deploy")]
public class DeployCommand : CommandBase
{
    [Argument(Name = "environment", IsRequired = true)]
    [Description("Target environment")]
    public string Environment { get; set; } = "";

    [Argument(Name = "count")]
    [Description("Number of instances")]
    public int Count { get; set; } = 1;

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Deploying {Count} instance(s) to {Environment}");
    }
}
```

```
app> deploy --environment staging --count 3
Deploying 3 instance(s) to staging
```

---

## The `[Alias]` Attribute

Provide a single-character shorthand for a named argument:

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AliasAttribute : Attribute
{
    public char Alias { get; set; }
    public AliasAttribute(char alias)
}
```

```csharp
[Argument(Name = "environment", IsRequired = true)]
[Alias('e')]
public string Environment { get; set; } = "";
```

```
app> deploy -e staging
```

Aliases are prefixed with a single `-` and are always one character.

---

## The `[Description]` Attribute

Documents an argument for the help system:

```csharp
[Argument(Name = "environment")]
[Alias('e')]
[Description("The target deployment environment")]
public string Environment { get; set; } = "";
```

The description appears in `--help` output:

```
Arguments:
  --environment, -e    The target deployment environment    [required]
  --count              Number of instances                  [default: 1]
```

---

## Required vs Optional

```csharp
// Required — command fails if not provided
[Argument(Name = "name", IsRequired = true)]
public string Name { get; set; } = "";

// Optional — uses the property's default value if not provided
[Argument(Name = "count")]
public int Count { get; set; } = 1;
```

---

## Supported Data Types

Argument values are parsed from strings using `BitPantry.Parsing.Strings`. Supported types include:

- Primitives: `string`, `int`, `long`, `float`, `double`, `decimal`, `bool`
- Enums (by name, case-insensitive)
- `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, `Uri`
- Nullable variants of all value types
- Collections: arrays, `List<T>`, `IEnumerable<T>` (for multi-value or `IsRest`)

---

## See Also

- [Positional Arguments](positional-arguments.md)
- [Flags](flags.md)
- [Autocomplete](../autocomplete/index.md)
- [Global Arguments](../running/global-arguments.md)
- [Core Attributes](../api-reference/attributes.md)
