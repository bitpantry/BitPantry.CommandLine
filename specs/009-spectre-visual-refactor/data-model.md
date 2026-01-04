# Data Model: Spectre Visual Rendering Refactor

**Branch**: `009-spectre-visual-refactor` | **Date**: January 3, 2026

## Overview

This document defines the key entities, their relationships, and state transitions for the Spectre Visual Rendering Refactor feature.

---

## Entity Definitions

### 1. AutoCompleteMenuRenderable

**Purpose**: Renders the autocomplete menu as a vertical list of items with selection highlighting.

**Inheritance**: Extends `Spectre.Console.Rendering.Renderable`

```csharp
public class AutoCompleteMenuRenderable : Renderable
{
    // State (immutable - create new instance for updates)
    public IReadOnlyList<string> Items { get; }
    public int SelectedIndex { get; }
    public int ViewportStart { get; }
    public int ViewportSize { get; }
    
    // Derived
    public int ViewportEnd => Math.Min(ViewportStart + ViewportSize, Items.Count);
    public bool HasScrollUp => ViewportStart > 0;
    public bool HasScrollDown => ViewportEnd < Items.Count;
}
```

**Fields**:

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| Items | IReadOnlyList\<string\> | Menu items to display | Not null, count >= 0 |
| SelectedIndex | int | Currently selected item index | >= 0 && < Items.Count (or -1 for no selection) |
| ViewportStart | int | First visible item index | >= 0 && < Items.Count |
| ViewportSize | int | Maximum visible items | >= 1 |

**Rendering Output**:
- Each item rendered on its own line
- Selected item uses `Style.Parse("invert")` (inverted colors)
- Non-selected items use default style
- Scroll indicators: `(↑ N more above)` and `(↓ N more below)` when applicable

---

### 2. GhostTextRenderable

**Purpose**: Renders the ghost text suggestion in dim gray style.

**Inheritance**: Extends `Spectre.Console.Rendering.Renderable`

```csharp
public class GhostTextRenderable : Renderable
{
    // State (immutable)
    public string GhostText { get; }
    public Style GhostStyle { get; }
}
```

**Fields**:

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| GhostText | string | The ghost text to display | Not null (can be empty) |
| GhostStyle | Style | Spectre style for ghost text | Not null; defaults to `new Style(Color.Grey, decoration: Decoration.Dim)` |

**Rendering Output**:
- Single segment with ghost text in configured style
- Returns empty segments if GhostText is empty or whitespace

---

### 3. MenuLiveRenderable

**Purpose**: Internal Renderable that handles cursor positioning and SegmentShape tracking for in-place updates.

**Note**: This replicates Spectre's internal `LiveRenderable` pattern. See also `MenuLiveRenderer` (section 3a) which is the public wrapper implementing `IMenuRenderer`.

**Relationship**: `MenuLiveRenderer` (public API) uses `MenuLiveRenderable` (internal cursor/shape logic) internally.

```csharp
public class MenuLiveRenderer
{
    // Dependencies (injected)
    private readonly IAnsiConsole _console;
    
    // State (mutable - tracks rendering lifecycle)
    private SegmentShape? _shape;  // Tracks max dimensions (Spectre pattern)
    private bool _isVisible;
    
    // Cursor positioning (matches Spectre's exact approach)
    public IRenderable PositionCursor(RenderOptions options)
    {
        if (_shape == null) return new ControlCode(string.Empty);
        var linesToMoveUp = _shape.Value.Height - 1;
        return new ControlCode("\r" + AnsiCodes.CursorUp(linesToMoveUp));
    }
    
    public IRenderable RestoreCursor()
    {
        if (_shape == null) return new ControlCode(string.Empty);
        var linesToClear = _shape.Value.Height - 1;
        // Pattern: CR + EL(2) + (CUU(1) + EL(2)).Repeat(linesToClear)
        var sequence = "\r" + AnsiCodes.ClearLine;
        for (int i = 0; i < linesToClear; i++)
        {
            sequence += AnsiCodes.CursorUp(1) + AnsiCodes.ClearLine;
        }
        return new ControlCode(sequence);
    }
    
    // Methods (IMenuRenderer implementation)
    public void Show(IReadOnlyList<string> items, int selectedIndex, int viewportStart, int viewportSize);
    public void Update(IReadOnlyList<string> items, int selectedIndex, int viewportStart, int viewportSize);
    public void Hide();
    
    // Internal: creates AutoCompleteMenuRenderable from parameters
}
```

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| _console | IAnsiConsole | Target console for rendering |
| _shape | SegmentShape? | Maximum dimensions ever rendered (Inflate pattern from Spectre) |
| _isVisible | bool | Whether menu is currently displayed |

**State Machine**:

```
┌─────────────┐    Show()    ┌─────────────┐
│   Hidden    │─────────────>│   Visible   │
│ _shape=null │              │ _shape!=null│
└─────────────┘              └─────────────┘
      ^                            │ │
      │          Hide()            │ │
      └────────────────────────────┘ │
                                     │ Update()
                                     └────┐
                                          │ (_shape = _shape.Inflate(newShape)
                                          │  dimensions only grow)
                                          └──>
```

**Key Behaviors**:
- `Show()`: Initial render, sets `_maxHeight` to content height
- `Update()`: Re-renders in place, `_maxHeight = Max(_maxHeight, newHeight)`
- `Hide()`: Clears all lines up to `_maxHeight`, resets state

---

### 4. ConsolidatedTestConsole

**Purpose**: Test infrastructure combining Spectre's TestConsole with cursor position tracking.

```csharp
public class ConsolidatedTestConsole : IAnsiConsole, IDisposable
{
    // Wrapped Spectre console
    private readonly TestConsole _inner;
    
    // Cursor tracking (from VirtualAnsiConsole)
    private readonly CursorTracker _cursor;
    
    // Properties
    public string Output => _inner.Output;
    public IReadOnlyList<string> Lines => _inner.Lines;
    public TestConsoleInput Input => _inner.Input;
    public (int Column, int Line) CursorPosition => _cursor.Position;
}
```

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| _inner | TestConsole | Spectre's test console (delegation target) |
| _cursor | CursorTracker | Tracks cursor position through ANSI sequences |

**Key Behaviors**:
- Delegates all `IAnsiConsole` methods to Spectre's `TestConsole`
- Intercepts `Write()` calls to update cursor tracking
- Parses ANSI cursor movement sequences (CUU, CUD, CR, etc.)
- Provides `CursorPosition` for StepwiseTestRunner assertions

---

### 5. AnsiCodes

**Purpose**: Static helper class with ANSI escape sequence constants and builders.

```csharp
public static class AnsiCodes
{
    // Cursor visibility
    public const string HideCursor = "\u001b[?25l";
    public const string ShowCursor = "\u001b[?25h";
    
    // Line clearing
    public const string ClearLine = "\u001b[2K";
    public const string ClearToEndOfLine = "\u001b[K";
    
    // Cursor movement (builders)
    public static string CursorUp(int n) => $"\u001b[{n}A";
    public static string CursorDown(int n) => $"\u001b[{n}B";
    public static string CursorForward(int n) => $"\u001b[{n}C";
    public static string CursorBack(int n) => $"\u001b[{n}D";
    
    // Carriage return
    public const string CarriageReturn = "\r";
}
```

---

### 6. SegmentShape (NEW - From Spectre Pattern)

**Purpose**: Tracks maximum rendered dimensions to support the "inflate and pad" pattern that prevents phantom lines.

**Note**: This replicates Spectre's internal `SegmentShape` struct which is not publicly accessible.

```csharp
public readonly struct SegmentShape
{
    public int Width { get; }
    public int Height { get; }
    
    public SegmentShape(int width, int height)
    {
        Width = width;
        Height = height;
    }
    
    /// <summary>
    /// Returns a new shape with max dimensions of this and other.
    /// This is the "inflate" pattern - dimensions only grow, never shrink.
    /// </summary>
    public SegmentShape Inflate(SegmentShape other)
    {
        return new SegmentShape(
            Math.Max(Width, other.Width),
            Math.Max(Height, other.Height));
    }
    
    /// <summary>
    /// Pads segment lines to match this shape's dimensions.
    /// Adds trailing spaces to Width, blank lines to Height.
    /// </summary>
    public void Apply(RenderOptions options, ref List<SegmentLine> lines);
}
```

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| Width | int | Maximum width in cells |
| Height | int | Maximum height in lines |

**Key Behaviors**:
- `Inflate()`: Returns new shape with `Max(this.dim, other.dim)` - shape only grows
- `Apply()`: Pads content to match shape dimensions, preventing leftover content

---

## Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                        Production Code                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────┐     uses      ┌──────────────────┐  │
│  │ AutoCompleteController │──────────────>│ MenuLiveRenderer │  │
│  └────────────────────────┘               └────────┬─────────┘  │
│            │                                       │             │
│            │ uses                                  │ renders     │
│            v                                       v             │
│  ┌─────────────────────┐             ┌─────────────────────────┐│
│  │  GhostTextRenderer  │             │AutoCompleteMenuRenderable││
│  └──────────┬──────────┘             └─────────────────────────┘│
│             │ renders                                            │
│             v                                                    │
│  ┌─────────────────────┐                                        │
│  │ GhostTextRenderable │                                        │
│  └─────────────────────┘                                        │
│                                                                  │
│  Both renderables extend: Spectre.Console.Rendering.Renderable  │
│  Both use: AnsiCodes (static helpers)                           │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         Test Code                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────┐    uses    ┌────────────────────────┐ │
│  │  StepwiseTestRunner  │───────────>│ConsolidatedTestConsole │ │
│  └──────────────────────┘            └───────────┬────────────┘ │
│                                                  │               │
│                                      wraps       │ implements    │
│                                                  v               │
│                             ┌─────────────────────────────────┐ │
│                             │  Spectre.Console.Testing        │ │
│                             │  └── TestConsole                │ │
│                             │  └── TestConsoleInput           │ │
│                             └─────────────────────────────────┘ │
│                                                                  │
│  Snapshot tests use: Verify.MSTest + ConsolidatedTestConsole    │
└─────────────────────────────────────────────────────────────────┘
```

---

## State Transitions

### Menu Visibility States

```
User Input          Controller State          Visual Output
───────────────────────────────────────────────────────────
Type "con"   →      Menu hidden               Input: con|
                    Ghost: "nect"             Ghost: nect (dim)
                    
Press Tab    →      Menu visible              Input: con|
                    Items: [continue,         > continue
                            config,             config
                            connect]            connect
                    Selected: 0               
                    
Press ↓      →      Menu visible              Input: con|
                    Selected: 1                 continue
                                              > config
                                                connect
                                                
Press Enter  →      Menu hidden               Input: config|
                    Ghost: " --help"          Ghost:  --help (dim)
```

### LiveRenderer Height Tracking

```
Render #    Content Height    _maxHeight    Padding Lines
─────────────────────────────────────────────────────────
1           3 items           3             0
2           5 items           5             0
3           2 items           5             3 (blank lines)
4           6 items           6             0
5           1 item            6             5 (blank lines)
```

The `_maxHeight` only increases (Inflate pattern), ensuring old content is always cleared.

---

## Validation Rules

| Entity | Rule | Error Behavior |
|--------|------|----------------|
| AutoCompleteMenuRenderable | Items not null | ArgumentNullException |
| AutoCompleteMenuRenderable | ViewportSize >= 1 | ArgumentOutOfRangeException |
| AutoCompleteMenuRenderable | SelectedIndex in valid range | Clamp to valid range |
| GhostTextRenderable | GhostText not null | Default to empty string |
| MenuLiveRenderer | _console not null | ArgumentNullException (DI) |
| ConsolidatedTestConsole | Fluent config before use | InvalidOperationException |
