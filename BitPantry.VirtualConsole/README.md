# BitPantry.VirtualConsole

A lightweight, headless virtual terminal emulator for testing CLI applications. Think "Selenium for CLI apps" - it processes ANSI escape sequences and maintains a 2D screen buffer for visual state assertions.

## Features

- **Zero dependencies** - Pure .NET Standard 2.0 library with no external dependencies
- **Full ANSI support** - CSI sequences for cursor movement, SGR for colors/styles, ED/EL for screen clearing
- **Color support** - ConsoleColor, 256-color palette, and 24-bit TrueColor (RGB)
- **Style attributes** - Bold, italic, underline, strikethrough, dim, blink, reverse, hidden
- **Line wrapping** - Automatic wrapping at configurable width boundary
- **Query API** - Get individual cells, rows, or full screen content with style information

## Installation

```bash
dotnet add package BitPantry.VirtualConsole
```

## Quick Start

```csharp
using BitPantry.VirtualConsole;

// Create a virtual console (80 columns, 24 rows)
var console = new VirtualConsole(80, 24);

// Write text with ANSI colors
console.Write("\x1b[31mRed text\x1b[0m and normal text");

// Query the screen
var cell = console.GetCell(0, 0);
Console.WriteLine($"Character: {cell.Character}");
Console.WriteLine($"Foreground: {cell.Style.ForegroundColor}");

// Get a row of text
var row = console.GetRow(0);
Console.WriteLine($"Row text: {row.GetText()}");

// Get full screen content
Console.WriteLine(console.GetScreenText());
```

## API Reference

### VirtualConsole

Main entry point for the virtual terminal.

```csharp
// Constructor
var console = new VirtualConsole(int width = 80, int height = 24);

// Write output (processes ANSI escape sequences)
console.Write(string text);

// Query individual cell
ScreenCell cell = console.GetCell(int row, int column);

// Query entire row
ScreenRow row = console.GetRow(int rowIndex);

// Get plain text content
string text = console.GetScreenText();

// Get structured content
IReadOnlyList<ScreenRow> content = console.GetScreenContent();

// Clear the screen
console.Clear();

// Get current cursor position
CursorPosition cursor = console.CursorPosition;
```

### ScreenCell

Represents a single character cell with its style.

```csharp
char Character { get; }
CellStyle Style { get; }
```

### ScreenRow

Represents a row of cells with query helpers.

```csharp
IReadOnlyList<ScreenCell> Cells { get; }
string GetText();
IReadOnlyList<ScreenCell> GetCells(int startColumn, int length);
```

### CellStyle

Immutable style information for a cell.

```csharp
// Basic colors
ConsoleColor? ForegroundColor { get; }
ConsoleColor? BackgroundColor { get; }

// 256-color mode
byte? Foreground256 { get; }
byte? Background256 { get; }

// TrueColor (RGB)
(byte R, byte G, byte B)? ForegroundRgb { get; }
(byte R, byte G, byte B)? BackgroundRgb { get; }

// Text attributes
CellAttributes Attributes { get; }

// Builder methods
CellStyle WithForeground(ConsoleColor color);
CellStyle WithBackground(ConsoleColor color);
CellStyle WithForeground256(byte colorIndex);
CellStyle WithBackground256(byte colorIndex);
CellStyle WithForegroundRgb(byte r, byte g, byte b);
CellStyle WithBackgroundRgb(byte r, byte g, byte b);
CellStyle WithAttribute(CellAttributes attribute);
CellStyle WithoutAttribute(CellAttributes attribute);
```

### CellAttributes

Text styling flags (combinable with bitwise OR).

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
    Reverse = 32,
    Hidden = 64,
    Strikethrough = 128
}
```

## Supported ANSI Sequences

### Cursor Movement (CSI)

| Sequence | Description |
|----------|-------------|
| `\x1b[nA` | Cursor Up n rows |
| `\x1b[nB` | Cursor Down n rows |
| `\x1b[nC` | Cursor Forward n columns |
| `\x1b[nD` | Cursor Back n columns |
| `\x1b[n;mH` | Move cursor to row n, column m |
| `\x1b[n;mf` | Move cursor to row n, column m |

### Colors and Styles (SGR)

| Sequence | Description |
|----------|-------------|
| `\x1b[0m` | Reset all attributes |
| `\x1b[1m` | Bold |
| `\x1b[2m` | Dim |
| `\x1b[3m` | Italic |
| `\x1b[4m` | Underline |
| `\x1b[5m` | Blink |
| `\x1b[7m` | Reverse |
| `\x1b[8m` | Hidden |
| `\x1b[9m` | Strikethrough |
| `\x1b[30-37m` | Foreground colors (black to white) |
| `\x1b[40-47m` | Background colors (black to white) |
| `\x1b[90-97m` | Bright foreground colors |
| `\x1b[100-107m` | Bright background colors |
| `\x1b[38;5;nm` | 256-color foreground |
| `\x1b[48;5;nm` | 256-color background |
| `\x1b[38;2;r;g;bm` | TrueColor foreground |
| `\x1b[48;2;r;g;bm` | TrueColor background |

### Screen Clearing (ED/EL)

| Sequence | Description |
|----------|-------------|
| `\x1b[0J` | Clear from cursor to end of screen |
| `\x1b[1J` | Clear from start of screen to cursor |
| `\x1b[2J` | Clear entire screen |
| `\x1b[0K` | Clear from cursor to end of line |
| `\x1b[1K` | Clear from start of line to cursor |
| `\x1b[2K` | Clear entire line |

### Control Characters

| Character | Description |
|-----------|-------------|
| `\r` | Carriage return (move to column 0) |
| `\n` | Line feed (move down one row) |
| `\b` | Backspace (move left one column) |
| `\t` | Tab (move to next tab stop, 8 columns) |

## Testing Examples

### Testing Menu Rendering

```csharp
[TestMethod]
public void Menu_ShouldHighlightSelectedOption()
{
    var console = new VirtualConsole(40, 10);
    
    console.Write("  Option 1\n");
    console.Write("\x1b[7m> Option 2\x1b[0m\n");  // Reverse for highlight
    console.Write("  Option 3\n");
    
    var selectedCell = console.GetCell(1, 0);
    selectedCell.Style.Attributes.Should().HaveFlag(CellAttributes.Reverse);
}
```

### Testing Progress Bar

```csharp
[TestMethod]
public void ProgressBar_ShouldUpdateInPlace()
{
    var console = new VirtualConsole(50, 5);
    
    console.Write("Progress: [          ] 0%");
    console.Write("\rProgress: [█████     ] 50%");
    
    var row = console.GetRow(0);
    row.GetText().Should().Contain("50%");
    row.GetText().Should().NotContain(" 0%");
}
```

### Testing Colored Output

```csharp
[TestMethod]
public void ColoredOutput_ShouldHaveCorrectStyles()
{
    var console = new VirtualConsole(80, 24);
    
    console.Write("\x1b[32mSuccess\x1b[0m \x1b[31mError\x1b[0m");
    
    console.GetCell(0, 0).Style.ForegroundColor.Should().Be(ConsoleColor.Green);
    console.GetCell(0, 8).Style.ForegroundColor.Should().Be(ConsoleColor.Red);
}
```

## License

MIT License - See LICENSE file for details.
