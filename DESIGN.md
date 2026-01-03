# CLI Design Guidelines

This document defines the look-and-feel standards for the BitPantry.CommandLine framework. All commands, formatters, and UI components should follow these guidelines.

## Spectre Console Standard

**All console output MUST go through Spectre Console's `IAnsiConsole` interface.**

- Spectre Console automatically handles graceful degradation based on terminal capabilities
- Cross-platform Unicode/ASCII fallback is handled automatically
- Color support detection is automatic
- See official documentation: https://spectreconsole.net/

```csharp
// ✓ Correct - use injected IAnsiConsole (via CommandBase.Console)
public class MyCommand : CommandBase
{
    public override Task<int> ExecuteAsync()
    {
        Console.WriteLine("Output via Spectre Console");
        return Task.FromResult(0);
    }
}

// ✗ Incorrect - bypass Spectre Console
System.Console.WriteLine("Direct console access");
```

## Prompt Segment Ordering

Prompt segments implement `IPromptSegment` with an `Order` property. Use these ranges:

| Order Range | Category | Examples |
|-------------|----------|----------|
| 0-99 | Core/Identity | Application name, user identity |
| 100-199 | Connection State | Server host (`@hostname`), profile name |
| 200-299 | Session State | Working directory, active context |
| 300+ | Custom/User | Project-specific segments |

```csharp
// ✓ Correct - connection segment in 100-199 range
public class ServerConnectionSegment : IPromptSegment
{
    public int Order => 100;
    public string Render() => $"@{_proxy.ConnectionUri?.Host}";
}

// ✓ Correct - custom segment in 300+ range
public class MyCustomSegment : IPromptSegment
{
    public int Order => 300;
    public string Render() => "[custom]";
}
```

### Prompt Formatting
- **No space before terminator**: `sandbox @localhost$` not `sandbox @localhost $`
- **Space after terminator**: Included in suffix for user input separation
- **Segments own their decorators**: `@hostname`, `[branch]`, `(context)`

## Colors

Use color semantically and sparingly. Spectre Console handles `NO_COLOR` and terminal capability detection.

| Color | Semantic Use |
|-------|--------------|
| `red` | Errors |
| `yellow` | Warnings |
| `green` | Success confirmations |
| `dim` / `grey` | Secondary info, hints |

```csharp
// ✓ Correct - semantic color use
Console.MarkupLine("[green]Connected to server[/]");
Console.MarkupLine("[red]Profile not found[/]");
Console.MarkupLine("[yellow]Token expires soon[/]");
Console.MarkupLine("[dim]Use --help for options[/]");

// ✗ Incorrect - decorative/excessive color
Console.MarkupLine("[bold cyan]Group:[/] [yellow]sample[/]");
Console.MarkupLine("[green]echo[/]  [dim]Echoes the message[/]");
Console.MarkupLine("[bold]Header Text[/]");
```

### Color Guidelines
- Don't rely on color alone - include text context for accessibility
- Default terminal color for normal output
- Reserve color for actionable states (errors, warnings, success)
- No `[bold]` for emphasis - use plain text
- No `[cyan]` for decorative highlighting

## Symbols and Icons

**Default: No symbols.** Colors and text provide sufficient distinction.

### When Symbols ARE Appropriate
- **Progress indicators**: Spinners, progress bars (use Spectre Console components)
- **Dense tabular data**: Status columns where color alone is insufficient

### When NOT to Use Symbols
- Success/error messages (use color + descriptive text)
- Bullets in lists (use `-`)
- Navigation or flow indicators

```csharp
// ✓ Correct - no symbol, color + text
Console.MarkupLine("[green]Connected to localhost[/]");
Console.MarkupLine("[red]Profile not found[/]");

// ✗ Incorrect - unnecessary symbol
Console.MarkupLine("[green]✓ Connected to localhost[/]");
Console.MarkupLine("[red]✗ Profile not found[/]");

// ✓ Correct - simple dash for lists
Console.WriteLine("  - Item one");
Console.WriteLine("  - Item two");

// ✗ Incorrect - decorative bullets
Console.WriteLine("  • Item one");
Console.WriteLine("  → Item two");
```

### If Symbols Are Needed
Use Spectre Console's `Emoji` class for cross-platform compatibility:

```csharp
// ✓ Correct - Spectre handles fallback
Console.MarkupLine($"{Emoji.Known.CheckMark} Task complete");

// ✗ Incorrect - raw Unicode may not render
Console.MarkupLine("✓ Task complete");
```

## Spacing and Layout

### Command Spacing
- **Commands own their spacing** - the core command loop does not add spacing
- Add one blank line at the start and end of significant output blocks
- Help formatter adds its own spacing

```csharp
// ✓ Correct - command manages its own spacing
public override Task<int> ExecuteAsync()
{
    Console.WriteLine();  // Space before output
    Console.WriteLine("Command output here");
    Console.WriteLine();  // Space after output
    return Task.FromResult(0);
}
```

### Indentation
- Use **2-space indentation** for nested content
- Align columns in lists using `PadRight()`

```csharp
// ✓ Correct - 2-space indent, aligned columns
Console.WriteLine("Commands:");
Console.WriteLine("  echo    Echoes the message");
Console.WriteLine("  info    Shows server info");

// ✗ Incorrect - inconsistent indentation
Console.WriteLine("Commands:");
Console.WriteLine("    echo - Echoes the message");
Console.WriteLine("  info - Shows server info");
```

### Section Separation
- One blank line between major sections
- No excessive blank lines
- No decorative line separators (`───────`)

```csharp
// ✓ Correct - blank line separation
Console.WriteLine("Connection Status");
Console.WriteLine();
Console.WriteLine("  Status:  Connected");

// ✗ Incorrect - decorative line
Console.MarkupLine("[bold]Connection Status[/]");
Console.MarkupLine("[dim]─────────────────[/]");
```

## Lists and Bullets

- Use `-` for unordered lists (ASCII, universally supported)
- Use numbered lists when order matters
- No decorative bullets (`•`, `→`, `▸`)

```csharp
// ✓ Correct
Console.WriteLine("Options:");
Console.WriteLine("  - Option A");
Console.WriteLine("  - Option B");

// ✓ Correct - numbered when order matters
Console.WriteLine("Steps:");
Console.WriteLine("  1. First step");
Console.WriteLine("  2. Second step");

// ✗ Incorrect - decorative bullets
Console.WriteLine("  • Option A");
Console.WriteLine("  → Option B");
```

## Error Messages

Format errors with context but without symbols:

```csharp
// ✓ Correct - clear error with context
Console.MarkupLine("[red]Failed to connect to server[/]");
Console.MarkupLine("[dim]Check that the server is running and accessible[/]");

// ✗ Incorrect - symbol prefix
Console.MarkupLine("[red]✗ Failed to connect[/]");
```

## Success Messages

Confirm completed actions with green color, no symbols:

```csharp
// ✓ Correct
Console.MarkupLine("[green]Connected to localhost[/]");
Console.MarkupLine("[green]Profile saved[/]");

// ✗ Incorrect - unnecessary symbol
Console.MarkupLine("[green]✓ Connected to localhost[/]");
```

## Help Output

Help text uses **default terminal colors** for readability:

- No color markup in help formatter
- Clear section headers (Description, Usage, Arguments, Options)
- Consistent column alignment
- `<required>` and `[optional]` notation in usage synopsis

See `HelpFormatter.cs` for the reference implementation.

## User Input

Use Spectre Console prompts for all user input:

```csharp
// ✓ Correct - Spectre prompts
var name = Console.Prompt(new TextPrompt<string>("Enter name:"));
var password = Console.Prompt(new TextPrompt<string>("Password:").Secret());
var confirmed = Console.Prompt(new ConfirmationPrompt("Continue?"));

// ✗ Incorrect - direct console access
Console.Write("Enter name: ");
var name = System.Console.ReadLine();
```

## Autocomplete Rendering Architecture

The autocomplete system uses Spectre Console's `Renderable` pattern for consistent, testable visual output.

### Components

| Component | Purpose |
|-----------|---------|
| `AutoCompleteMenuRenderable` | Renders vertical menu with inverted selection |
| `GhostTextRenderable` | Renders dim gray suggestion text |
| `MenuLiveRenderer` | High-level menu lifecycle (Show/Update/Hide) |
| `GhostLiveRenderer` | High-level ghost text lifecycle |
| `SegmentShape` | Tracks rendered dimensions for clean updates |

### The Inflate Pattern

Menu rendering uses the "Inflate" pattern to prevent phantom lines:

```csharp
// SegmentShape only grows, never shrinks
_shape = _shape.Inflate(newShape);
```

When menu content shrinks (e.g., 5 items → 2 items), the shape maintains the maximum dimensions. This ensures the clear operation erases all previously rendered lines, preventing visual artifacts.

### Menu Layout

The menu uses **vertical layout** (one item per line):

```
  > connect     ← selected (inverted colors)
    disconnect
    status
```

Each menu item is rendered on its own line with leading padding. The selected item uses inverted style (background/foreground swap).

### Cursor Management

Menu operations preserve cursor position:

1. Before showing menu: Save cursor column position
2. After each update: Cursor returns to start of menu area via ANSI codes
3. On hide: Clear all lines up to max height, restore cursor

```csharp
// Position cursor at start of previous render
console.Write(liveRenderable.PositionCursor());

// Clear content with proper height tracking
console.Write(liveRenderable.RestoreCursor());
```

### Testing

Use `ConsolidatedTestConsole` or Spectre's `TestConsole` for rendering tests:

```csharp
var console = new TestConsole().EmitAnsiSequences();
var menu = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 10);
console.Write(menu);
// Assert on console.Output
```
