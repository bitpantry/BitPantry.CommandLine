# BitPantry.VirtualConsole.Testing

A testing extension library for BitPantry.CommandLine autocomplete testing using VirtualConsole.

## Overview

This library provides infrastructure for testing autocomplete behavior in BitPantry.CommandLine applications. It uses VirtualConsole as a virtual terminal emulator to capture and assert on ANSI output, enabling precise validation of autocomplete rendering, menu navigation, and ghost text display.

## Key Components

### AutoCompleteTestHarness

The main entry point for autocomplete testing. Creates a complete test environment with:
- VirtualConsole for screen buffer capture
- Keyboard simulation for typing and key presses
- AutoCompleteController integration
- FluentAssertions extensions for easy assertions

```csharp
using var harness = new AutoCompleteTestHarness(
    configureApp: builder => builder.RegisterCommand<ServerCommand>()
);

// Type text
await harness.TypeTextAsync("ser");

// Check ghost text appeared
harness.Should().HaveGhostText("ver");

// Press Tab to open menu
await harness.PressTabAsync();

// Verify menu is visible
harness.Should().HaveMenuVisible()
    .And.HaveSelectedItem("server");
```

### VirtualConsoleAnsiAdapter

Bridges Spectre.Console's IAnsiConsole interface to VirtualConsole, allowing code that uses IAnsiConsole to be tested with VirtualConsole's screen buffer inspection.

### KeyboardSimulator

Simulates keyboard input for testing, processing each keystroke through the input line and autocomplete controller.

## Assertions

### Harness Assertions

```csharp
// Menu state
harness.Should().HaveMenuVisible();
harness.Should().HaveMenuHidden();
harness.Should().HaveSelectedItem("server");
harness.Should().HaveSelectedIndex(0);
harness.Should().HaveMenuItemCount(5);
harness.Should().HaveMenuItemContaining("connect");
harness.Should().NotHaveMenuItemContaining("oldcmd");

// Ghost text
harness.Should().HaveGhostText("erver");
harness.Should().HaveGhostTextVisible();
harness.Should().HaveNoGhostText();

// Buffer state
harness.Should().HaveBuffer("server connect");
harness.Should().HaveBufferStartingWith("server");
harness.Should().HaveBufferPosition(6);
harness.Should().HaveCursorAtEnd();

// Screen content
harness.Should().HaveScreenContaining("server");
```

### VirtualConsole Assertions

```csharp
// Screen content
harness.Console.Should().ContainText("server");
harness.Console.Should().NotContainText("oldcmd");

// Cell styles
harness.Console.Should().HaveCellWithStyle(0, 5, CellAttributes.Dim);  // Ghost text
harness.Console.Should().HaveDimCellAt(0, 5);  // Shorthand for Dim
harness.Console.Should().HaveReverseCellAt(1, 0);  // Selection highlight

// Text at position
harness.Console.Should().HaveTextAt(0, 2, "server");
harness.Console.Should().HaveRowContaining(1, "connect");

// Cursor position
harness.Console.Should().HaveCursorAt(0, 8);

// Range styles
harness.Console.Should().HaveRangeWithStyle(0, 5, 4, CellAttributes.Dim);
```

## Test Commands

The library includes pre-built test commands for common scenarios:

- `MinimalTestCommand` - Simple command with no arguments
- `StringArgTestCommand` - Command with a string argument
- `EnumArgTestCommand` - Command with an enum argument
- `MultiArgTestCommand` - Command with multiple argument types
- `PositionalTestCommand` - Command with positional arguments
- `IsRestTestCommand` - Command with variadic positional arguments
- `ServerCommand`, `ConnectCommand`, `DisconnectCommand` - For testing command hierarchies

## Usage Example

```csharp
[TestClass]
public class GhostTextTests
{
    [TestMethod]
    public async Task TC_1_1_SingleCharacter_ShowsGhostCompletion()
    {
        // Arrange: Create harness with "server" command registered
        using var harness = new AutoCompleteTestHarness(
            configureApp: builder => builder.RegisterCommand<ServerCommand>()
        );
        
        // Act: Type "s"
        await harness.TypeTextAsync("s");
        
        // Assert: Ghost text "erver" appears
        harness.Should().HaveGhostText("erver");
        
        // Also verify the ghost text is rendered with dim style
        harness.Console.Should().HaveDimCellAt(0, harness.PromptLength + 1);
    }
}
```

## Failure Diagnostics

When assertions fail, detailed diagnostic information is included:

```
Expected ghost text "erver" but found "".

VirtualConsole Buffer:
> s                                                                              

Harness State:
  Buffer: "s"
  BufferPosition: 1
  IsMenuVisible: false
  GhostText: ""
  HasGhostText: false
```

## Dependencies

- BitPantry.VirtualConsole - Core virtual terminal emulator
- BitPantry.CommandLine - Command line framework being tested
- FluentAssertions - Assertion library
- Spectre.Console - Console rendering (IAnsiConsole interface)
