# Theme Configuration

Customize the visual appearance of syntax highlighting, ghost text suggestions, and the autocomplete menu.

---

## Configuring the Theme

Use `ConfigureTheme()` on the builder:

```csharp
var app = new CommandLineApplicationBuilder()
    .ConfigureTheme(t =>
    {
        t.Command = new Style(foreground: Color.Green);
        t.ArgumentName = new Style(foreground: Color.Orange1);
        t.GhostText = new Style(foreground: Color.Grey, decoration: Decoration.Italic);
    })
    .Build();
```

---

## Theme Properties

All properties use Spectre.Console's `Style` type:

| Property | Default | Used For |
|----------|---------|----------|
| `Group` | `Cyan` | Group names in input |
| `Command` | `Plain` | Command names in input |
| `ArgumentName` | `Yellow` | Argument names (`--name`) |
| `ArgumentAlias` | `Yellow` | Argument aliases (`-n`) |
| `ArgumentValue` | `Purple` | Argument values |
| `GhostText` | `Dim` | Inline autocomplete suggestions |
| `Default` | `Plain` | Unrecognized or default text |
| `MenuHighlight` | `Invert` | Selected item in autocomplete menu |
| `MenuGroup` | `Cyan` | Group names in autocomplete menu |

---

## Example

A dark-theme-friendly configuration:

```csharp
builder.ConfigureTheme(t =>
{
    t.Group = new Style(foreground: Color.Teal);
    t.Command = new Style(foreground: Color.White, decoration: Decoration.Bold);
    t.ArgumentName = new Style(foreground: Color.Yellow);
    t.ArgumentAlias = new Style(foreground: Color.Yellow, decoration: Decoration.Dim);
    t.ArgumentValue = new Style(foreground: Color.Aqua);
    t.GhostText = new Style(foreground: Color.Grey, decoration: Decoration.Italic);
    t.MenuHighlight = new Style(foreground: Color.Black, background: Color.Yellow);
    t.MenuGroup = new Style(foreground: Color.Teal, decoration: Decoration.Bold);
});
```

---

## See Also

- [Building the Application](index.md)
- [Syntax Highlighting](../syntax-highlighting.md)
- [Ghost Text & Menu Rendering](../autocomplete/rendering.md)
