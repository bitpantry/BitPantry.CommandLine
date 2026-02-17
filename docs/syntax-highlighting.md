# Syntax Highlighting

BitPantry.CommandLine provides real-time syntax highlighting as the user types in interactive mode. Input tokens are colored based on their role — group names, command names, argument names, argument values, and more.

---

## How It Works

The `SyntaxHighlighter` processes input on every keystroke:

1. Tokenizes the current input string
2. Resolves each token against the `ICommandRegistry`
3. Assigns a style from the [theme](building/theme-configuration.md) based on the token type
4. Re-renders the input line with colored tokens

---

## Token Types and Styles

| Token Type | Theme Property | Default Color | Example |
|------------|----------------|---------------|---------|
| Group name | `Group` | Cyan | `server` |
| Command name | `Command` | Plain | `deploy` |
| Argument name | `ArgumentName` | Yellow | `--environment` |
| Argument alias | `ArgumentAlias` | Yellow | `-e` |
| Argument value | `ArgumentValue` | Purple | `staging` |
| Unrecognized | `Default` | Plain | `unknown-text` |

---

## Example

With the default theme, a command like:

```
app> server deploy --environment staging --verbose
```

Renders as:

- `server` — Cyan (group)
- `deploy` — Plain (command)
- `--environment` — Yellow (argument name)
- `staging` — Purple (argument value)
- `--verbose` — Yellow (argument name / flag)

---

## Theme Integration

Customize syntax highlighting colors via `ConfigureTheme()`:

```csharp
builder.ConfigureTheme(t =>
{
    t.Group = new Style(foreground: Color.Teal);
    t.Command = new Style(foreground: Color.White, decoration: Decoration.Bold);
    t.ArgumentName = new Style(foreground: Color.Orange1);
    t.ArgumentValue = new Style(foreground: Color.Aqua);
});
```

See [Theme Configuration](building/theme-configuration.md) for all available properties.

---

## Scope

Syntax highlighting is active only in interactive mode (`RunInteractive()`). It has no effect in `RunOnce()` mode, which does not render input.

---

## See Also

- [Theme Configuration](building/theme-configuration.md)
- [Console Configuration](building/console-configuration.md)
- [Ghost Text & Menu Rendering](autocomplete/rendering.md)
- [Prompt Configuration](building/prompt-configuration.md)
