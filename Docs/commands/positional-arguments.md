# Positional Arguments

Positional arguments are specified by position in the input rather than by name. They provide a more natural syntax for commands with a small number of required inputs.

---

## Defining Positional Arguments

Set the `Position` property on `[Argument]` to a zero-based index:

```csharp
[Command(Name = "copy")]
[Description("Copies a file from source to destination")]
public class CopyCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("Source file path")]
    public string Source { get; set; } = "";

    [Argument(Position = 1, IsRequired = true)]
    [Description("Destination file path")]
    public string Destination { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Copying [bold]{Source}[/] → [bold]{Destination}[/]");
    }
}
```

```
app> copy /tmp/data.csv /opt/output/data.csv
Copying /tmp/data.csv → /opt/output/data.csv
```

---

## Ordering Rules

- Positions must be **zero-based** and **contiguous** — no gaps allowed
- Positional arguments are matched in order from the input
- Positional and named arguments can be mixed on the same command

```csharp
[Command(Name = "paint")]
public class PaintCommand : CommandBase
{
    [Argument(Position = 0)]
    public string Color { get; set; } = "red";

    [Argument(Position = 1)]
    public string Size { get; set; } = "medium";

    [Argument(Name = "glossy")]
    public bool Glossy { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Painting {Size} {Color}, Glossy={Glossy}");
    }
}
```

```
app> paint blue large --glossy true
Painting large blue, Glossy=True
```

---

## Variadic Arguments — `IsRest`

The `IsRest` property captures all remaining positional values into a collection. It must be the **last** positional argument:

```csharp
[Command(Name = "tag")]
public class TagCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("The item to tag")]
    public string Item { get; set; } = "";

    [Argument(Position = 1, IsRest = true)]
    [Description("Tags to apply")]
    public string[] Tags { get; set; } = Array.Empty<string>();

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Tagging [bold]{Item}[/] with: {string.Join(", ", Tags)}");
    }
}
```

```
app> tag document-1 urgent review important
Tagging document-1 with: urgent, review, important
```

**Rules for `IsRest`:**

- Must be the last positional argument
- Property type must be a collection (`string[]`, `List<T>`, `IEnumerable<T>`)
- Captures all remaining unmatched positional values

---

## See Also

- [Arguments](arguments.md)
- [Flags](flags.md)
- [Built-in Autocomplete Handlers](../autocomplete/built-in-handlers.md)
- [Core Attributes](../api-reference/attributes.md)
