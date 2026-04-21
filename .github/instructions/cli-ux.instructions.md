---
description: "CLI command output and UX conventions. Use when: writing CLI commands, formatting command output, styling console text, creating tables, building command responses, using Theme or Spectre.Console markup, adding arguments or flags to commands."
applyTo: "**/Commands/**/*.cs"
---

# CLI User Experience Conventions

Command output should feel like a Linux / POSIX terminal — concise, minimal decoration, information-dense.

## General Output Philosophy

- **Terse by default.** Output only what was asked for. No banners, decorative frames, or chatty preambles.
- **One line per logical item.** Prefer line-per-record output (like `ls`, `ps`, `grep`) over decorated panels.
- **Silence is success.** Operations that succeed can say nothing or emit a single confirmation line. Never wrap a one-line result in a box.
- **Errors to the point.** Error messages state what went wrong and what to do, nothing more.
- **Machine-friendly when possible.** Favor parseable, columnar output. Avoid decorative characters that impede piping.

## Command Structure

Commands inherit from `CommandBase` and use attribute-based declaration:

```csharp
[Command(Name = "ls")]
[InGroup<ServerGroup>]
[Description("Lists directory contents on the remote server")]
public class LsCommand : CommandBase
```

### Required Patterns

- `[Command(Name = "...")]` — short, lowercase name
- `[InGroup<T>]` — associates command with its parent group
- `[Description("...")]` — concise one-line description
- Constructor injection for all dependencies
- `public async Task Execute(CommandExecutionContext context)` — entry point

### Groups

Groups are marker classes that define the command hierarchy:

```csharp
[Group(Name = "server")]
[Description("Remote server file system commands")]
public class ServerGroup { }
```

## Arguments and Flags

### Argument Declaration

```csharp
[Argument(Position = 0, Name = "path", IsRequired = true)]
[Description("Directory path to list")]
public string Path { get; set; }

[Argument(Name = "uri", IsRequired = true)]
[Alias('u')]
[Description("Server URI (e.g., https://api.example.com)")]
public string Uri { get; set; } = string.Empty;

[Argument(Name = "lines"), Alias('n')]
[Description("Display only the first N lines")]
public int? Lines { get; set; }
```

### Flag Declaration

Boolean flags use `[Flag]` — no `=true/false` syntax:

```csharp
[Argument(Name = "recursive"), Flag, Alias('r')]
[Description("Recursively remove directories and their contents")]
public bool Recursive { get; set; }

[Argument(Name = "force"), Flag, Alias('f')]
[Description("Ignore nonexistent files and do not prompt")]
public bool Force { get; set; }
```

### Naming Conventions

- Commands named after POSIX equivalents where a natural mapping exists (`ls`, `cat`, `rm`, `stat`)
- Use short single-word names: `list`, `create`, `add`, `show`, `info`
- Flags use POSIX conventions: `--long` / `-l`, `--all` / `-a`, `--force` / `-f`
- Argument names are lowercase, hyphen-separated: `--api-key`, `--consent-mode`, `--allow-path`
- Single-char aliases for frequently used arguments

### Autocomplete

**Every argument with a constrained or discoverable value set MUST have autocomplete configured.** When adding a new argument, evaluate whether a provider exists or needs to be created. Common cases that require autocomplete:

- Enum-like choices (e.g., modes, sort orders, status values) — create a provider that returns the valid values
- Entity names (e.g., profile names, server names) — create a provider that queries the available entities
- File/directory paths — use the appropriate path autocomplete attribute
- Any argument where the user would otherwise have to guess or consult help

#### Using Existing Providers

Apply `[AutoComplete<T>]` where `T` implements `IAutoCompleteHandler`:

```csharp
[Argument(Name = "consent-mode")]
[AutoComplete<ConsentModeProvider>]
[Description("Consent mode: Prompt (default), AllowAll, or DenyAll")]
public string ConsentModeArg { get; set; }
```

For path-based autocomplete, use the appropriate path autocomplete attribute (e.g., `[ServerFilePathAutoComplete]`, `[ServerDirectoryPathAutoComplete]`).

#### Creating New Providers

When no existing provider covers the argument's value set, create a new `IAutoCompleteHandler` implementation:

```csharp
public class ConsentModeProvider : IAutoCompleteHandler
{
    public Task<IEnumerable<AutoCompleteItem>> GetSuggestionsAsync(string query, CancellationToken ct)
    {
        var values = Enum.GetNames<ConsentMode>()
            .Where(n => n.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(n => new AutoCompleteItem(n));
        return Task.FromResult(values);
    }
}
```

Key principles for new providers:
- Filter results by the current query prefix (case-insensitive)
- Register via DI if the provider needs injected dependencies
- Place providers alongside the feature they support (e.g., same project/namespace as the command)
- Reuse the same provider across all arguments that share the same value set

## Theme-Based Styling

Inject `Theme` (from `BitPantry.CommandLine`) via constructor DI and use its style properties for consistent coloring:

```csharp
public class LsCommand : CommandBase
{
    private readonly Theme _theme;

    public LsCommand(Theme theme)
    {
        _theme = theme;
    }
}
```

### Theme Properties

| Property | Default | Semantic Meaning |
|----------|---------|------------------|
| `Group` | cyan | Containers — groups, directories, categories |
| `Command` | plain | Command names |
| `ArgumentName` | yellow | Argument/option names |
| `ArgumentAlias` | yellow | Argument aliases |
| `ArgumentValue` | purple | Argument values, user-supplied data |
| `GhostText` | dim | Placeholder / hint text |
| `Default` | plain | Normal text |
| `MenuHighlight` | invert | Selected/highlighted menu items |
| `MenuGroup` | cyan | Group items in autocomplete menus |
| `TableHeader` | grey bold | Table column headers |

### Using Theme Styles in Markup

Convert a `Style` to a Spectre.Console markup tag with `.ToMarkup()`:

```csharp
// Directories use Theme.Group — they are containers
Console.MarkupLine($"[{_theme.Group.ToMarkup()}]{name}/[/]");

// Highlighted values use Theme.ArgumentValue
Console.MarkupLine($"[{_theme.ArgumentValue.ToMarkup()}]{value}[/]");
```

### Style Consistency Rules

- **Use Theme styles first.** Only fall back to inline color markup (`[red]`, `[green]`) when no Theme property fits the semantic purpose.
- **Reuse semantic mappings across commands.** If `Theme.Group` means "container" in directory listings, it means "container" everywhere.
- **Custom inline styles are reserved for status/severity:**
  - `[red]` — errors, destructive warnings, fatal messages
  - `[green]` — success confirmations, active status
  - `[yellow]` — warnings, empty-state messages, cancellation
  - `[bold]` — labels in key-value output
- Do not introduce new arbitrary colors. If no Theme property fits the semantic purpose, ask the user whether to add a new Theme property.

## Tables

### No Borders by Default

Tables are layout tools, not decoration. Match `ls -l` style — columns separated by whitespace, no borders, no headers unless they add clarity to ambiguous data:

```csharp
var table = new Table();
table.Border(TableBorder.None);
table.HideHeaders();
table.AddColumn(new TableColumn("Name") { Padding = new Padding(0, 0, 3, 0) });
table.AddColumn(new TableColumn("Size") { Padding = new Padding(0, 0, 3, 0) });
table.AddColumn(new TableColumn("Modified"));
```

### When Headers Are Acceptable

Show column headers only when the data would be ambiguous without them. Even then, prefer `HideHeaders()` if column meaning is obvious from context.

### Column Padding

Use `Padding(left, top, right, bottom)` for column gutters instead of borders. Typical: `new Padding(0, 0, 3, 0)` for 3-space right margin.

## Command Output Patterns

### Listings (like `ls`)

```
documents/          ← directory in Theme.Group style with trailing /
notes.txt           ← plain file, no styling
README.md
```

- Directories get `Theme.Group` style and a trailing `/`
- Files get no special styling
- Empty results: single `[yellow]` message, e.g., `No files found.`

### Key-Value Detail (like `stat`)

```
Name:      report.txt
Type:      File
Size:      1.2 KB (1,234 bytes)
Created:   2026-03-15 10:30
```

- Labels in `[bold]`, values plain or with semantic Theme styles
- Align values with consistent padding

### Success Confirmation

```
Profile created: production
```

One line. No decoration. Include the relevant identifier.

### Error Messages

```csharp
Console.MarkupLine($"[red]Error:[/] '{Markup.Escape(input)}' is an invalid URI.");
Console.MarkupLine($"[red]File not found: {path}[/]");
```

- Escape user input with `Markup.Escape()` to prevent markup injection
- State what went wrong and what to do, nothing more

### Destructive Operation Warnings

Use `[red]` for irrecoverable warnings. Use confirmation prompts for destructive actions:

```csharp
if (!Force && matchCount >= ConfirmationThreshold)
{
    if (!Console.Confirm($"Delete {matchCount} items?", false))
        return;
}
```
