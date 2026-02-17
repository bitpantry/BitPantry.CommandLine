# Flags

Flags are boolean arguments that are toggled by their presence in the input. They do not accept a value — the argument's existence means `true`, and its absence means `false`.

---

## The `[Flag]` Attribute

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FlagAttribute : Attribute { }
```

Apply `[Flag]` to a `bool` property alongside `[Argument]`:

```csharp
[Command(Name = "build")]
public class BuildCommand : CommandBase
{
    [Argument(Name = "verbose")]
    [Alias('v')]
    [Flag]
    public bool Verbose { get; set; }

    [Argument(Name = "clean")]
    [Flag]
    public bool Clean { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        if (Clean)
            Console.MarkupLine("[yellow]Cleaning build output...[/]");

        Console.MarkupLine("Building...");

        if (Verbose)
            Console.MarkupLine("[dim]Build completed with verbose output[/]");
    }
}
```

```
app> build --verbose --clean
Cleaning build output...
Building...
Build completed with verbose output

app> build
Building...
```

---

## Flags vs Boolean Arguments

Without `[Flag]`, a `bool` argument requires an explicit value:

| Declaration | Input | Result |
|-------------|-------|--------|
| `[Argument][Flag] bool Verbose` | `--verbose` | `true` |
| `[Argument][Flag] bool Verbose` | _(absent)_ | `false` |
| `[Argument] bool Debug` | `--debug true` | `true` |
| `[Argument] bool Debug` | `--debug false` | `false` |

```csharp
// Flag — presence-only, no value accepted
[Argument(Name = "verbose")]
[Flag]
public bool Verbose { get; set; }

// Non-flag boolean — requires explicit true/false
[Argument(Name = "debug")]
public bool Debug { get; set; }
```

```
app> build --verbose                   ✓  (flag toggled on)
app> build --verbose true              ✗  (flags cannot accept values)
app> build --debug true                ✓  (explicit value required)
app> build --debug                     ✗  (missing value)
```

---

## Flags with Aliases

Flags work naturally with single-character aliases for concise input:

```csharp
[Argument(Name = "verbose")]
[Alias('v')]
[Flag]
public bool Verbose { get; set; }
```

```
app> build -v
```

---

## See Also

- [Arguments](arguments.md)
- [Positional Arguments](positional-arguments.md)
- [Core Attributes](../api-reference/attributes.md)
