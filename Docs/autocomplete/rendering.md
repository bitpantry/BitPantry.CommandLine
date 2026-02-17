# Ghost Text & Menu Rendering

How autocomplete suggestions are displayed to the user during interactive input.

---

## Ghost Text

The `GhostTextController` displays a single inline suggestion as dim text after the cursor. As the user types, the ghost text updates to show the best match:

```
app> deploy --environ_ment
                     ↑ ghost text (dim)
```

Pressing **Tab** or **Right Arrow** accepts the ghost text suggestion and the full value is inserted.

Ghost text styling is controlled by the `GhostText` property of the [theme](../building/theme-configuration.md):

```csharp
builder.ConfigureTheme(t =>
{
    t.GhostText = new Style(foreground: Color.Grey, decoration: Decoration.Italic);
});
```

---

## Autocomplete Menu

The `AutoCompleteMenuController` renders a dropdown menu below the cursor when multiple suggestions are available:

```
app> task --priority _
  ┌──────────┐
  │ Low      │
  │ Medium   │  ← navigate with ↑/↓
  │ High     │
  │ Critical │
  └──────────┘
```

- **↑ / ↓** — Navigate through options
- **Tab** — Accept the highlighted option
- **Escape** — Dismiss the menu
- **Continue typing** — Filters the menu in real time

---

## Cursor Context

The `CursorContext` and `CursorContextResolver` determine what kind of suggestion to show based on the cursor position:

| Context | Suggestions |
|---------|-------------|
| After group name | Sub-groups and commands in the group |
| After command name | Argument names and aliases |
| After `--name` or `-a` | Argument values (via handlers) |
| After positional slot | Positional argument values (via handlers) |

---

## Menu Styling

Menu appearance is controlled by the theme:

| Property | Description |
|----------|-------------|
| `MenuHighlight` | Style for the currently selected option |
| `MenuGroup` | Style for group names shown in the menu |

```csharp
builder.ConfigureTheme(t =>
{
    t.MenuHighlight = new Style(foreground: Color.Black, background: Color.Yellow);
    t.MenuGroup = new Style(foreground: Color.Teal, decoration: Decoration.Bold);
});
```

---

## See Also

- [Autocomplete](index.md)
- [Theme Configuration](../building/theme-configuration.md)
- [Syntax Highlighting](../syntax-highlighting.md)
