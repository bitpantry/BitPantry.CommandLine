# Autocomplete

BitPantry.CommandLine provides real-time autocomplete with inline ghost text and a dropdown menu. Autocomplete covers command names, group names, argument names/aliases, and argument values.

---

## How It Works

In interactive mode (`RunInteractive()`):

1. As the user types, the `CursorContextResolver` determines what kind of token is at the cursor
2. The appropriate autocomplete handlers are queried for suggestions
3. Suggestions appear as ghost text (inline dim text) or in a dropdown menu
4. **Tab** triggers/accepts suggestions, **Arrow keys** navigate the menu

---

## What Gets Autocompleted

| Token Type | Source | Example |
|------------|--------|---------|
| Group name | `ICommandRegistry.Groups` | `ser` → `server` |
| Command name | `ICommandRegistry.Commands` | `dep` → `deploy` |
| Argument name | `CommandInfo.Arguments` | `--env` → `--environment` |
| Argument alias | `ArgumentInfo.Alias` | `-e` |
| Argument value | Autocomplete handlers | `--environment sta` → `staging` |

---

## Handler Resolution Order

When resolving value suggestions for an argument, handlers are checked in this order:

1. **Attribute handler** — `[AutoComplete<THandler>]` on the property (highest priority)
2. **Type handler** — `ITypeAutoCompleteHandler` matched via `CanHandle(Type)` (last registered wins)
3. **Built-in handler** — `EnumAutoCompleteHandler`, `BooleanAutoCompleteHandler`

---

## Registering Handlers

Custom type handlers are registered via the builder:

```csharp
builder.ConfigureAutoComplete(ac =>
{
    ac.Register<FilePathAutoCompleteHandler>();
    ac.Register<DateAutoCompleteHandler>();
});
```

Attribute handlers are resolved from the DI container and must be registered as services:

```csharp
builder.Services.AddTransient<EnvironmentHandler>();
```

---

## In This Section

| Page | Description |
|------|-------------|
| [Built-in Handlers](built-in-handlers.md) | `EnumAutoCompleteHandler`, `BooleanAutoCompleteHandler` |
| [Custom Attribute Handlers](attribute-handlers.md) | Per-argument handlers with `[AutoComplete<T>]` |
| [Custom Type Handlers](type-handlers.md) | Broad type-based handlers with `ITypeAutoCompleteHandler` |
| [Ghost Text & Menu Rendering](rendering.md) | How suggestions are displayed |
| [Remote Autocomplete](remote-autocomplete.md) | Server-side autocomplete for remote commands |

---

## See Also

- [Built-in Handlers](built-in-handlers.md)
- [Custom Attribute Handlers](attribute-handlers.md)
- [Custom Type Handlers](type-handlers.md)
- [Ghost Text & Menu Rendering](rendering.md)
- [Arguments](../commands/arguments.md)
