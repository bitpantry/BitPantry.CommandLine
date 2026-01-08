# Quickstart: BitPantry.VirtualConsole

**Date**: 2026-01-04  
**Feature**: 011-virtual-console

## Overview

BitPantry.VirtualConsole is a virtual terminal emulator for testing CLI applications. It processes ANSI escape sequences and maintains a 2D screen buffer, enabling point-in-time visual state assertions.

## Installation

```bash
# Once published to NuGet:
dotnet add package BitPantry.VirtualConsole

# During development (project reference):
dotnet add reference ../BitPantry.VirtualConsole/BitPantry.VirtualConsole.csproj
```

## Basic Usage

### Create a Virtual Console

```csharp
using BitPantry.VirtualConsole;

// Create a virtual console with 80x25 dimensions
var console = new VirtualConsole(80, 25);
```

### Write Text

```csharp
// Simple text
console.Write("Hello, World!");

// Text with ANSI colors
console.Write("\x1b[34mBlue Text\x1b[0m");

// Cursor movement and overwrite
console.Write("AAAAA");
console.Write("\x1b[5D");  // Move cursor back 5
console.Write("BBB");      // Overwrites first 3 A's → "BBBAA"
```

### Query Screen State

```csharp
// Get a specific cell
var cell = console.GetCell(row: 0, column: 0);
Console.WriteLine($"Character: {cell.Character}");
Console.WriteLine($"Foreground: {cell.Style.ForegroundColor}");
Console.WriteLine($"Is Bold: {cell.Style.Attributes.HasFlag(CellAttributes.Bold)}");

// Get a row
var row = console.GetRow(0);
Console.WriteLine($"Row text: {row.GetText()}");

// Get entire screen as text
Console.WriteLine(console.GetScreenContent());
```

## Testing Example

### The Problem VirtualConsole Solves

Traditional console capture accumulates all output, so old correct output masks new buggy output:

```csharp
// Traditional approach - BUG NOT DETECTED
var oldConsole = new StringBuilder();
oldConsole.Append("\x1b[34mHello\x1b[0m");  // Blue text
Assert.Contains("\x1b[34m", oldConsole.ToString());  // PASS

oldConsole.Append("\x1b[H");  // Cursor home (conceptually)
oldConsole.Append("Hello");   // Overwrites with plain text (BUG!)
Assert.Contains("\x1b[34m", oldConsole.ToString());  // STILL PASSES - old output still there!
```

### The VirtualConsole Solution

```csharp
// VirtualConsole approach - BUG DETECTED
var console = new VirtualConsole(80, 25);

console.Write("\x1b[34mHello\x1b[0m");  // Blue text
Assert.AreEqual(ConsoleColor.Blue, console.GetCell(0, 0).Style.ForegroundColor);  // PASS

console.Write("\x1b[H");   // Cursor home
console.Write("Hello");    // Overwrites with plain text (BUG!)
Assert.AreEqual(ConsoleColor.Blue, console.GetCell(0, 0).Style.ForegroundColor);  // FAILS - bug caught!
```

## Common Patterns

### Testing Menu Rendering

```csharp
[TestMethod]
public void Menu_SelectedItem_HasInvertedStyle()
{
    var console = new VirtualConsole(80, 25);
    
    // Simulate menu output from your application
    var menuOutput = RenderMenuToAnsiString(selectedIndex: 1);
    console.Write(menuOutput);
    
    // Assert selected item has inverted styling
    var selectedItemRow = console.GetRow(1);
    var firstCell = selectedItemRow.GetCell(0);
    Assert.IsTrue(firstCell.Style.Attributes.HasFlag(CellAttributes.Reverse));
}
```

### Testing Color Highlighting

```csharp
[TestMethod]
public void FilterMatch_HasBlueHighlight()
{
    var console = new VirtualConsole(80, 25);
    
    // Simulate filtered menu with highlighted matches
    console.Write("conn" + "\x1b[34mect\x1b[0m" + "ion");
    
    // Assert the "ect" portion is blue
    Assert.IsNull(console.GetCell(0, 0).Style.ForegroundColor);  // 'c' - default
    Assert.IsNull(console.GetCell(0, 3).Style.ForegroundColor);  // 'n' - default
    Assert.AreEqual(ConsoleColor.Blue, console.GetCell(0, 4).Style.ForegroundColor);  // 'e' - blue
    Assert.AreEqual(ConsoleColor.Blue, console.GetCell(0, 5).Style.ForegroundColor);  // 'c' - blue
    Assert.AreEqual(ConsoleColor.Blue, console.GetCell(0, 6).Style.ForegroundColor);  // 't' - blue
    Assert.IsNull(console.GetCell(0, 7).Style.ForegroundColor);  // 'i' - default
}
```

### Testing In-Place Updates

```csharp
[TestMethod]
public void ProgressBar_UpdatesInPlace()
{
    var console = new VirtualConsole(80, 25);
    
    // Initial progress
    console.Write("[##        ] 20%");
    Assert.AreEqual("[##        ] 20%", console.GetRow(0).GetText().TrimEnd());
    
    // Update in place (cursor to start of line, overwrite)
    console.Write("\r[#####     ] 50%");
    Assert.AreEqual("[#####     ] 50%", console.GetRow(0).GetText().TrimEnd());
    
    // Only one version exists - no ghosting
    Assert.IsFalse(console.GetScreenContent().Contains("20%"));
}
```

## ANSI Sequences Supported

| Category | Sequences |
|----------|-----------|
| Cursor Movement | CUU (↑), CUD (↓), CUF (→), CUB (←), CUP (position), CHA (column) |
| Erase | ED (display), EL (line) |
| Styling | SGR (colors, bold, italic, underline, reverse, etc.) |
| Control Chars | CR, LF, TAB, BS |

## Error Handling

VirtualConsole throws on unrecognized ANSI sequences:

```csharp
var console = new VirtualConsole(80, 25);

// Unknown sequence throws
Assert.ThrowsException<UnrecognizedSequenceException>(() => 
    console.Write("\x1b[999z"));  // Not a valid sequence

// Exception includes the sequence for debugging
try { console.Write("\x1b[999z"); }
catch (UnrecognizedSequenceException ex)
{
    Console.WriteLine(ex.Message);  // "Unrecognized ANSI sequence: ESC[999z"
    Console.WriteLine(ex.Sequence); // "\x1b[999z"
}
```

## Next Steps

1. Add package reference to your test project
2. Replace cumulative output capture with VirtualConsole
3. Write assertions against screen state, not output strings
4. Catch visual bugs that were previously invisible to tests!
