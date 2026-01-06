# BitPantry.VirtualConsole Architecture

**Date**: 2026-01-04  
**Spec**: [specs/011-virtual-console/spec.md](../specs/011-virtual-console/spec.md)

## Overview

BitPantry.VirtualConsole is a virtual terminal emulator that processes ANSI escape sequences and maintains a 2D screen buffer for testing CLI applications. This document addresses the key architectural decisions.

## Acceptance Criteria Architecture Notes

### AC-001: Input Queue Extension Points

The VirtualConsole accepts text input through the `Write(string text)` method. Future extensibility for input queue handling (e.g., simulating keyboard input) can be achieved by:

1. Adding an `IInputProvider` interface for custom input sources
2. Extending VirtualConsole with `ProcessInput(char key)` method
3. Maintaining an input buffer that can be injected during tests

**Current Scope**: Write-only operations (output processing). Input simulation deferred to future enhancement.

### AC-002: IAnsiConsole Compatibility

The architecture is designed to enable future `IAnsiConsole` (Spectre.Console) compatibility:

1. **VirtualConsole as TextWriter**: Can be wrapped in a TextWriter for Spectre.Console output redirection
2. **Complete ANSI Processing**: Processes all SGR (styling) and cursor movement sequences
3. **Future Adapter Pattern**: An `AnsiConsoleAdapter` can implement `IAnsiConsole` using VirtualConsole as backing store

**Current Scope**: Direct Write() API. IAnsiConsole adapter deferred to future enhancement.

### AC-003: Separable Screen Buffer

The `ScreenBuffer` class is designed as a separable component:

```
VirtualConsole (coordinator)
    ├── AnsiSequenceParser (parses input)
    ├── SgrProcessor (handles styling)
    ├── CursorProcessor (handles movement)
    └── ScreenBuffer (2D grid storage) ← Separable
```

**Key Separation Properties**:
- ScreenBuffer has no dependency on ANSI parsing
- ScreenBuffer exposes primitive operations (WriteChar, MoveCursor)
- ScreenBuffer can be used independently for custom terminal implementations

### AC-004: State Machine Parser

ANSI escape sequence parsing uses a state machine pattern:

```
States:
┌─────────┐    ESC    ┌────────┐    [     ┌──────────┐
│ Ground  │─────────►│ Escape │────────►│ CsiEntry │
└─────────┘          └────────┘          └──────────┘
     ▲                                        │
     │                                        ▼
     │                              ┌──────────────┐
     │ final char                   │   CsiParam   │
     └──────────────────────────────┴──────────────┘
```

**State Descriptions**:
- **Ground**: Normal text mode, emitting printable characters
- **Escape**: Received ESC (0x1B), waiting for sequence type
- **CsiEntry**: Received CSI (ESC [), ready for parameters
- **CsiParam**: Collecting numeric parameters and intermediate bytes

### AC-005: Unrecognized Sequence Handling

The parser throws exceptions for unrecognized sequences to ensure test reliability:

```csharp
public class UnrecognizedAnsiSequenceException : Exception
{
    public string Sequence { get; }
    public char FinalByte { get; }
    public int[] Parameters { get; }
}
```

**Rationale**: Testing library should be strict. Unknown sequences indicate:
1. A new sequence type that needs implementation
2. Corrupted/invalid input that should fail tests
3. Version mismatch between CLI output and VirtualConsole

## Component Hierarchy

```
VirtualConsole (Public API)
│
├── Properties
│   ├── Width, Height (dimensions)
│   └── CursorRow, CursorColumn (position)
│
├── Methods
│   ├── Write(string) - main input processing
│   ├── GetCell(row, col) - query single cell
│   ├── GetRow(row) - query entire row
│   ├── GetScreenText() - all text, no formatting
│   ├── GetScreenContent() - text with line breaks
│   └── Clear() - reset screen
│
└── Internal Components
    ├── ScreenBuffer - 2D cell storage
    ├── AnsiSequenceParser - CSI/escape parsing
    ├── SgrProcessor - style handling
    └── CursorProcessor - movement handling
```

## Data Flow

```
Input: "Hello\x1b[34m World\x1b[0m"
       │
       ▼
┌──────────────────────────────────────────────────┐
│                 VirtualConsole                    │
│                                                   │
│   ┌─────────────────────────────────────────┐    │
│   │         AnsiSequenceParser              │    │
│   │  "H" → Print('H')                       │    │
│   │  "e" → Print('e')                       │    │
│   │  ...                                    │    │
│   │  "\x1b[34m" → CsiSequence(34, 'm')      │    │
│   │  " " → Print(' ')                       │    │
│   │  "W" → Print('W')                       │    │
│   │  ...                                    │    │
│   │  "\x1b[0m" → CsiSequence(0, 'm')        │    │
│   └─────────────────────────────────────────┘    │
│               │                                   │
│               ▼                                   │
│   ┌─────────────────┐  ┌─────────────────┐       │
│   │  SgrProcessor   │  │ CursorProcessor │       │
│   │  (m commands)   │  │ (A/B/C/D/H/G)   │       │
│   └────────┬────────┘  └────────┬────────┘       │
│            │                    │                │
│            ▼                    ▼                │
│   ┌─────────────────────────────────────────┐    │
│   │              ScreenBuffer               │    │
│   │  ┌───┬───┬───┬───┬───┬───┬───┬───┐     │    │
│   │  │ H │ e │ l │ l │ o │   │ W │...│     │    │
│   │  │def│def│def│def│def│blu│blu│   │     │    │
│   │  └───┴───┴───┴───┴───┴───┴───┴───┘     │    │
│   └─────────────────────────────────────────┘    │
└──────────────────────────────────────────────────┘
```

## Thread Safety

**Current Design**: Not thread-safe. VirtualConsole is intended for single-threaded test execution.

**Future Enhancement**: If concurrent access is needed, synchronization can be added at the VirtualConsole level.

## Memory Management

- **Fixed Buffer Size**: Dimensions set at construction, no dynamic resizing
- **No Scrollback**: Content scrolled off-screen is lost (matches spec)
- **Struct-Based Cells**: ScreenCell is a value type to minimize allocations
- **Immutable Styles**: CellStyle uses With* pattern for immutable updates
