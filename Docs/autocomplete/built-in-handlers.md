# Built-in Handlers

Two autocomplete handlers are registered automatically — no configuration required.

---

## EnumAutoCompleteHandler

Suggests all values of an `enum` type, filtered by the current input prefix:

```csharp
public enum Priority { Low, Medium, High, Critical }

[Command(Name = "task")]
public class TaskCommand : CommandBase
{
    [Argument(Name = "priority")]
    public Priority Priority { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Priority: {Priority}");
    }
}
```

```
app> task --priority _
  Low
  Medium
  High
  Critical

app> task --priority h_
  High
```

The handler also supports `Nullable<T>` enum types (e.g., `Priority?`).

---

## BooleanAutoCompleteHandler

Suggests `true` and `false` for non-flag `bool` arguments, filtered by prefix:

```csharp
[Argument(Name = "debug")]
public bool Debug { get; set; }
```

```
app> build --debug _
  true
  false

app> build --debug t_
  true
```

This handler does **not** activate for `[Flag]` booleans, since flags are presence-only and never accept a value.

---

## Registration

Both handlers are registered by default in the `AutoCompleteHandlerRegistryBuilder`. They participate at the lowest priority level — attribute handlers and custom type handlers take precedence.

---

## See Also

- [Autocomplete](index.md)
- [Custom Attribute Handlers](attribute-handlers.md)
- [Custom Type Handlers](type-handlers.md)
