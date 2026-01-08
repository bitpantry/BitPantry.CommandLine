# Data Model: BitPantry.VirtualConsole

**Date**: 2026-01-04  
**Feature**: 011-virtual-console

## Entity Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     VirtualConsole                          │
│  - Accepts text/ANSI input via Write()                      │
│  - Exposes query API for test assertions                    │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌──────────────────────────────────┐   │
│  │ AnsiParser  │───►│         ScreenBuffer             │   │
│  │             │    │  ┌──────────────────────────┐    │   │
│  │ Parse CSI   │    │  │ ScreenCell[rows,cols]    │    │   │
│  │ Parse SGR   │    │  │                          │    │   │
│  │ Cursor Cmds │    │  │  Row 0: [H][e][l][l][o]  │    │   │
│  └─────────────┘    │  │  Row 1: [W][o][r][l][d]  │    │   │
│                     │  │  ...                     │    │   │
│  ┌─────────────┐    │  └──────────────────────────┘    │   │
│  │ CellStyle   │    │  CursorPosition: (row, col)      │   │
│  │ (current)   │    │  CurrentStyle: CellStyle         │   │
│  └─────────────┘    └──────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## Entity Definitions

### VirtualConsole

**Purpose**: Main entry point. Accepts output text with embedded ANSI sequences, processes them, and provides query methods for test assertions.

| Property | Type | Description |
|----------|------|-------------|
| Width | int | Screen width in columns |
| Height | int | Screen height in rows |
| CursorRow | int | Current cursor row (0-based) |
| CursorColumn | int | Current cursor column (0-based) |

| Method | Return | Description |
|--------|--------|-------------|
| Write(string text) | void | Process text with embedded ANSI sequences |
| GetCell(int row, int col) | ScreenCell | Get cell at position |
| GetRow(int row) | ScreenRow | Get row wrapper |
| GetScreenText() | string | Get all text without ANSI codes |
| GetScreenContent() | string | Get text with line breaks |
| Clear() | void | Clear screen and reset cursor to home |

**Validation Rules**:
- Width and Height must be > 0
- Constructor validates dimensions

**State Transitions**:
- Initial: Empty screen, cursor at (0, 0), default style
- After Write: Screen updated, cursor moved, style may change
- After Clear: Empty screen, cursor at (0, 0), style preserved

---

### ScreenBuffer

**Purpose**: 2D grid of ScreenCell values representing the virtual screen.

| Property | Type | Description |
|----------|------|-------------|
| Width | int | Buffer width in columns |
| Height | int | Buffer height in rows |
| Cells | ScreenCell[,] | 2D array of cells |
| CursorRow | int | Current row (clamped to 0..Height-1) |
| CursorColumn | int | Current column (clamped to 0..Width-1) |
| CurrentStyle | CellStyle | Style applied to new characters |

| Method | Return | Description |
|--------|--------|-------------|
| WriteChar(char c) | void | Write character at cursor with current style |
| MoveCursor(int row, int col) | void | Move cursor (clamped to bounds) |
| MoveCursorRelative(int dRow, int dCol) | void | Relative cursor movement |
| GetCell(int row, int col) | ScreenCell | Get cell at position |
| GetRow(int row) | ScreenRow | Get row wrapper |
| ClearScreen(ClearMode mode) | void | Erase display (below/above/all) |
| ClearLine(ClearMode mode) | void | Erase line (right/left/all) |
| ApplyStyle(CellStyle style) | void | Set current style for subsequent writes |
| ResetStyle() | void | Reset to default style |

**Validation Rules**:
- All coordinates clamped to valid range (no exceptions for out-of-bounds)
- Line wrapping: when column >= Width, move to column 0 of next row
- Scroll: when cursor goes below Height, scroll up (future: scrollback buffer)

---

### ScreenCell

**Purpose**: Single character position on screen with associated styling.

| Property | Type | Description |
|----------|------|-------------|
| Character | char | The character at this position |
| Style | CellStyle | Visual styling for this character |

**Validation Rules**:
- Default Character is space (' ')
- Default Style is CellStyle.Default

**Identity**: Cells are value types; identity is by position in ScreenBuffer

---

### CellStyle

**Purpose**: Visual styling attributes for a character.

| Property | Type | Description |
|----------|------|-------------|
| ForegroundColor | ConsoleColor? | Foreground color (null = default) |
| BackgroundColor | ConsoleColor? | Background color (null = default) |
| Attributes | CellAttributes | Bitflag attributes |

| Method | Return | Description |
|--------|--------|-------------|
| WithForeground(ConsoleColor?) | CellStyle | Return new style with foreground set |
| WithBackground(ConsoleColor?) | CellStyle | Return new style with background set |
| WithAttribute(CellAttributes) | CellStyle | Return new style with attribute added |
| WithoutAttribute(CellAttributes) | CellStyle | Return new style with attribute removed |

**Static Members**:
- `CellStyle.Default`: No colors, no attributes

**Validation Rules**:
- Immutable value type
- Equality by value comparison

---

### CellAttributes

**Purpose**: Bitflag enum for text attributes.

```csharp
[Flags]
public enum CellAttributes
{
    None = 0,
    Bold = 1,
    Dim = 2,
    Italic = 4,
    Underline = 8,
    Blink = 16,
    Reverse = 32,      // Inverted colors
    Hidden = 64,
    Strikethrough = 128
}
```

---

### ScreenRow

**Purpose**: Wrapper providing convenient access to a single row.

| Property | Type | Description |
|----------|------|-------------|
| RowIndex | int | The row index (0-based) |
| Length | int | Number of columns |

| Method | Return | Description |
|--------|--------|-------------|
| GetCell(int column) | ScreenCell | Get cell at column |
| GetText() | string | Get row text without styling |
| GetCells() | IEnumerable\<ScreenCell\> | Enumerate all cells |

---

### CursorPosition

**Purpose**: Simple struct for cursor location.

| Property | Type | Description |
|----------|------|-------------|
| Row | int | Row (0-based) |
| Column | int | Column (0-based) |

---

### ClearMode

**Purpose**: Enum for erase operations.

```csharp
public enum ClearMode
{
    ToEnd = 0,      // Cursor to end (default)
    ToBeginning = 1, // Beginning to cursor
    All = 2          // Entire screen/line
}
```

---

## Internal Entities (Not Public API)

### AnsiSequenceParser

**Purpose**: Parse ANSI escape sequences from input stream.

| State | Description |
|-------|-------------|
| Ground | Normal text mode |
| Escape | Saw ESC, waiting for next char |
| CsiEntry | Saw CSI (`ESC [`), collecting params |
| CsiParam | Collecting numeric parameters |

| Method | Return | Description |
|--------|--------|-------------|
| Process(char c) | ParserResult | Process one character |
| Reset() | void | Return to Ground state |

### ParserResult

**Purpose**: Result of parsing one character.

```csharp
public abstract class ParserResult { }
public class PrintResult : ParserResult { public char Character; }
public class SequenceResult : ParserResult { public CsiSequence Sequence; }
public class ControlResult : ParserResult { public ControlCode Code; }
public class NoActionResult : ParserResult { }  // Sequence still building
```

### CsiSequence

**Purpose**: Parsed CSI sequence.

| Property | Type | Description |
|----------|------|-------------|
| Parameters | int[] | Numeric parameters (empty if none) |
| FinalByte | char | The command character (e.g., 'm', 'A') |
| IsPrivate | bool | True if starts with `?` |

---

## Relationships

```
VirtualConsole 1──────1 ScreenBuffer
                       │
                       └───────────* ScreenCell 1──────1 CellStyle

VirtualConsole 1──────1 AnsiSequenceParser (internal)

ScreenRow ─ ─ ─ ─ ─► ScreenBuffer (reference, not composition)
```

---

## Color Mapping

For 256-color and 24-bit color support:

### Extended Color Types

```csharp
public abstract class Color { }

public class BasicColor : Color
{
    public ConsoleColor Value { get; }
}

public class Color256 : Color
{
    public byte Index { get; }  // 0-255
}

public class TrueColor : Color
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
}
```

**Note**: Initial implementation may use `ConsoleColor?` and add extended colors later per requirements.
