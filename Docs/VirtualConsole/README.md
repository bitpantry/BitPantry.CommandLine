# BitPantry.VirtualConsole

A virtual terminal emulator for automated testing of .NET command-line applications.

## Overview

BitPantry.VirtualConsole provides a true 2D screen buffer that processes ANSI escape sequences to represent what a user would actually see on screen at any point in time. Unlike traditional console output capture (which accumulates all output), this package enables point-in-time visual state assertions.

## Why This Package?

Traditional testing approaches capture console output as a cumulative string:

```
Write "Hello"
Cursor up
Write "World"
```

**Cumulative output**: `"Hello\e[AWorld"` - Contains everything, including cursor codes.

**What user sees**: `"World"` on line 1 (overwrote "Hello")

VirtualConsole maintains the actual screen state, letting you assert what the user sees, not what was ever written.

## Quick Start

```csharp
// Create a virtual console
var console = new VirtualConsole(width: 80, height: 25);

// Write output (including ANSI sequences)
console.Write("Hello");
console.Write("\e[3D");  // Cursor left 3
console.Write("XYZ");

// Query current screen state
var row = console.GetRow(0);
Assert.AreEqual("HeXYZ", row.Text);  // Not "HelloXYZ"!
```

## Features

- **2D Screen Buffer**: Maintains actual screen state with cursor tracking
- **ANSI Sequence Processing**: Handles cursor movement, colors, and styling
- **Style Tracking**: Query foreground/background colors and attributes per cell
- **Clean Query API**: No test framework dependencies - build your own assertions
- **Configurable Dimensions**: Set custom screen sizes for viewport testing

## Documentation

- [Getting Started](getting-started.md) - Installation and basic usage
- [Screen Buffer](screen-buffer.md) - Understanding the screen model
- [ANSI Support](ansi-support.md) - Supported escape sequences
- [Examples](examples.md) - Common usage patterns

## Package Philosophy

BitPantry.VirtualConsole provides **data, not assertions**. The package exposes a rich query API for screen state, but does not include test framework dependencies or assertion helpers.

**You build the assertions** in your test project using your preferred test framework (MSTest, xUnit, NUnit) and assertion library (FluentAssertions, Shouldly, etc.).

## Supported ANSI Sequences

| Category | Sequences |
|----------|-----------|
| Cursor Movement | CUU (up), CUD (down), CUF (forward), CUB (back) |
| Positioning | Carriage return, Newline |
| Colors | SGR foreground/background (basic + 256-color) |
| Attributes | Bold, dim, italic, underline, blink, invert, hidden, strikethrough |
| Erase | ED (erase display), EL (erase line) |

## Use Cases

- **CLI Testing**: Verify menu rendering, highlighting, selection states
- **TUI Testing**: Test text-based user interfaces with styled output
- **Progress Indicators**: Verify in-place updates work correctly
- **Visual Regression**: Catch bugs where correct output is overwritten

## Package Status

Currently built as part of the BitPantry.CommandLine solution. Will be extracted to a standalone NuGet package for broader consumption.

## License

MIT License - see LICENSE file for details.
