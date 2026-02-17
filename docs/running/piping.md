# Command Piping

Commands can be chained using the `|` pipe operator. The output of one command becomes the input of the next.

---

## Basic Piping

```
app> generate-data --count 10 | transform --format json | save --path output.json
```

Each command in the pipeline executes in sequence. The return value of one command is delivered as the `Input` property to the next.

---

## Producing Output

A command produces pipe output by returning a value from its `Execute` method:

```csharp
[Command(Name = "generate")]
public class GenerateCommand : CommandBase
{
    [Argument(Name = "count")]
    public int Count { get; set; } = 5;

    public object Execute(CommandExecutionContext ctx)
    {
        var items = Enumerable.Range(1, Count)
            .Select(i => $"Item {i}")
            .ToList();

        return items;
    }
}
```

---

## Consuming Input

The downstream command receives the output through `CommandExecutionContext<T>`:

```csharp
[Command(Name = "format")]
public class FormatCommand : CommandBase
{
    public object Execute(CommandExecutionContext<List<string>> ctx)
    {
        var formatted = string.Join("\n", ctx.Input.Select(i => $"  - {i}"));
        Console.MarkupLine(formatted);
        return formatted;
    }
}
```

```
app> generate --count 3 | format
  - Item 1
  - Item 2
  - Item 3
```

---

## Type Safety

The generic type parameter `T` in `CommandExecutionContext<T>` declares what type the command expects as input. The `InputType` is recorded in the command's `CommandInfo` metadata.

If the upstream command's return type doesn't match the downstream command's expected input type, a resolution error occurs.

---

## Error Propagation

If any command in the pipeline fails (throws an exception or calls `Fail()`), the entire pipeline stops and the error is reported. Subsequent commands are not executed.

---

## See Also

- [Running Commands](index.md)
- [The Processing Pipeline](processing-pipeline.md)
- [Arguments](../commands/arguments.md)
