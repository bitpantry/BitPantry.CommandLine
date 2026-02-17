# Custom Type Handlers

Type handlers provide autocomplete for all arguments of a matching type, without requiring per-property attributes.

---

## The `ITypeAutoCompleteHandler` Interface

```csharp
public interface ITypeAutoCompleteHandler : IAutoCompleteHandler
{
    bool CanHandle(Type argumentType);
}
```

A type handler extends `IAutoCompleteHandler` with a `CanHandle` method. The framework calls `CanHandle` with the argument's property type — if it returns `true`, the handler is used for that argument.

---

## Example — File Path Handler

```csharp
public class FilePathAutoCompleteHandler : ITypeAutoCompleteHandler
{
    public bool CanHandle(Type argumentType) => argumentType == typeof(string);

    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        // Only handle arguments named "path" or "file"
        if (!context.ArgumentInfo.Name.Contains("path", StringComparison.OrdinalIgnoreCase)
            && !context.ArgumentInfo.Name.Contains("file", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new List<AutoCompleteOption>());

        var directory = Path.GetDirectoryName(context.QueryString) ?? ".";
        var prefix = Path.GetFileName(context.QueryString);

        var options = Directory.GetFiles(directory, $"{prefix}*")
            .Select(f => new AutoCompleteOption(f))
            .ToList();

        return Task.FromResult(options);
    }
}
```

---

## Registration

Register type handlers via the builder:

```csharp
builder.ConfigureAutoComplete(ac =>
{
    ac.Register<FilePathAutoCompleteHandler>();
});
```

---

## Resolution Priority

When multiple type handlers match an argument, the **last registered** handler wins. The overall priority order is:

1. **Attribute handler** (`[AutoComplete<T>]`) — always wins
2. **Type handler** (`ITypeAutoCompleteHandler`) — last registered matching handler
3. **Built-in handler** — `EnumAutoCompleteHandler`, `BooleanAutoCompleteHandler`

This means custom type handlers override built-in handlers, and attribute handlers override everything.

---

## See Also

- [Autocomplete](index.md)
- [Built-in Handlers](built-in-handlers.md)
- [Interfaces](../api-reference/interfaces.md)
