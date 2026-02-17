# BitPantry.VirtualConsole

A virtual terminal emulator for testing CLI applications. Processes ANSI escape sequences and maintains a 2D screen buffer — no real terminal required.

> **Companion package** — Published as a separate NuGet package. Targets .NET Standard 2.0 with zero external dependencies.

---

## Installation

```shell
dotnet add package BitPantry.VirtualConsole
```

---

## Overview

`VirtualConsole` processes streams of text containing ANSI escape sequences and maintains an in-memory screen representation:

- **Screen buffer** — 2D grid of `ScreenCell` objects (`ScreenBuffer`, `ScreenRow`, `ScreenCell`)
- **Cursor tracking** — Current position, movement, and wrapping
- **SGR style processing** — Foreground/background colors, bold, dim, underline, etc.
- **Erase modes** — Clear line, clear screen, erase to end/beginning
- **Auto-wrap** — Text wraps at the configured column width

---

## Basic Usage

```csharp
using BitPantry.VirtualConsole;

var console = new VirtualConsole(width: 80, height: 24);

console.Write("Hello, ");
console.Write("\x1b[1mWorld\x1b[0m");  // Bold "World"
console.WriteLine("!");

// Read the screen buffer
var text = console.GetScreenText();
// → "Hello, World!"
```

---

## Key Types

| Type | Description |
|------|-------------|
| `VirtualConsole` | The main entry point — write text, read screen state |
| `ScreenBuffer` | 2D grid of cells representing the terminal display |
| `ScreenRow` | A single row of cells |
| `ScreenCell` | A single character cell with style information |

---

## Use Cases

- **Automated testing** — Assert on console output without a real terminal
- **CI/CD pipelines** — Run tests that exercise console rendering
- **Output capture** — Process ANSI output from Spectre.Console or other renderers

For testing-specific extensions (assertions, Spectre.Console adapter, keyboard simulation), see [VirtualConsole.Testing](testing-extensions.md).

---

## See Also

- [VirtualConsole.Testing](testing-extensions.md)
- [Console Configuration](../building/console-configuration.md)
- [UX Testing](../testing/ux-testing.md)
- [Remote Console I/O](../remote/remote-console-io.md)
