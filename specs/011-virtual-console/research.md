# Research: BitPantry.VirtualConsole

**Date**: 2026-01-04  
**Feature**: 011-virtual-console

## Overview

This document consolidates research on ANSI escape sequence processing, terminal emulation patterns, and implementation guidance for building the VirtualConsole package.

---

## 1. ANSI Escape Sequence Standards

### Primary Reference: ECMA-48 (ISO/IEC 6429)

**Source**: [ECMA-48 Standard (5th Edition, June 1991)](https://www.ecma-international.org/publications-and-standards/standards/ecma-48/)

**Decision**: Use ECMA-48 as the authoritative standard for control sequences.

**Rationale**: ECMA-48 is the international standard that defines:
- Control Sequence Introducer (CSI) format: `ESC [`
- Select Graphic Rendition (SGR) for colors/styles
- Cursor movement sequences (CUU, CUD, CUF, CUB)
- Erase sequences (ED, EL)

**Key Implementation Details**:
- CSI sequences: `ESC [ <params> <final-char>`
- Parameters are semicolon-separated decimal numbers
- Missing/zero parameters default to 1 for most commands
- Final character determines the command (e.g., `m` for SGR, `A` for CUU)

---

## 2. XTerm Control Sequences

### Primary Reference: XTerm ctlseqs

**Source**: [XTerm Control Sequences](https://invisible-island.net/xterm/ctlseqs/ctlseqs.html)

**Decision**: Use XTerm documentation as the practical implementation reference.

**Rationale**: 
- XTerm is the de facto standard terminal emulator
- Spectre.Console generates XTerm-compatible sequences
- Comprehensive documentation of real-world sequences

**Key Sequences for VirtualConsole (Priority Order)**:

### Cursor Movement (CSI Pn X)
| Sequence | Name | Action |
|----------|------|--------|
| `CSI Pn A` | CUU | Cursor Up Pn times (default 1) |
| `CSI Pn B` | CUD | Cursor Down Pn times (default 1) |
| `CSI Pn C` | CUF | Cursor Forward Pn times (default 1) |
| `CSI Pn D` | CUB | Cursor Backward Pn times (default 1) |
| `CSI Pn ; Pn H` | CUP | Cursor Position (row;col, default 1;1) |
| `CSI Pn G` | CHA | Cursor Horizontal Absolute (column) |

### Erase Sequences
| Sequence | Name | Action |
|----------|------|--------|
| `CSI Ps J` | ED | Erase in Display (0=below, 1=above, 2=all) |
| `CSI Ps K` | EL | Erase in Line (0=right, 1=left, 2=all) |

### Character Attributes (SGR) - `CSI Ps m`
| Code | Attribute |
|------|-----------|
| 0 | Reset all attributes |
| 1 | Bold |
| 2 | Dim (faint) |
| 3 | Italic |
| 4 | Underline |
| 5 | Blink |
| 7 | Reverse (invert) |
| 8 | Hidden |
| 9 | Strikethrough |
| 22 | Normal intensity (not bold/faint) |
| 23 | Not italic |
| 24 | Not underline |
| 25 | Not blink |
| 27 | Not reverse |
| 28 | Not hidden |
| 29 | Not strikethrough |

### Foreground Colors (SGR)
| Code | Color | Code | Bright Color |
|------|-------|------|--------------|
| 30 | Black | 90 | Bright Black (Gray) |
| 31 | Red | 91 | Bright Red |
| 32 | Green | 92 | Bright Green |
| 33 | Yellow | 93 | Bright Yellow |
| 34 | Blue | 94 | Bright Blue |
| 35 | Magenta | 95 | Bright Magenta |
| 36 | Cyan | 96 | Bright Cyan |
| 37 | White | 97 | Bright White |
| 39 | Default foreground | | |

### Background Colors (SGR)
| Code | Color | Code | Bright Color |
|------|-------|------|--------------|
| 40 | Black | 100 | Bright Black |
| 41 | Red | 101 | Bright Red |
| 42 | Green | 102 | Bright Green |
| 43 | Yellow | 103 | Bright Yellow |
| 44 | Blue | 104 | Bright Blue |
| 45 | Magenta | 105 | Bright Magenta |
| 46 | Cyan | 106 | Bright Cyan |
| 47 | White | 107 | Bright White |
| 49 | Default background | | |

### 256-Color Mode (SGR)
- Foreground: `CSI 38 ; 5 ; <n> m`
- Background: `CSI 48 ; 5 ; <n> m`
- Colors 0-15: Standard + bright colors
- Colors 16-231: 6×6×6 color cube
- Colors 232-255: Grayscale ramp

### 24-bit True Color (SGR)
- Foreground: `CSI 38 ; 2 ; <r> ; <g> ; <b> m`
- Background: `CSI 48 ; 2 ; <r> ; <g> ; <b> m`

---

## 3. VT100 Terminal Reference

### Primary Reference: VT100 User Guide Chapter 3

**Source**: [VT100 Programmer Information](https://vt100.net/docs/vt100-ug/chapter3.html)

**Decision**: Use VT100 docs for understanding original terminal behavior.

**Rationale**: VT100 established the baseline terminal behavior that all modern terminals extend.

**Key Insights**:
- Control characters (0x00-0x1F, 0x7F) have special meanings
- ESC (0x1B) introduces escape sequences
- LF (0x0A) moves down; CR (0x0D) moves to column 0
- Tab (0x09) moves to next tab stop
- Control chars can be embedded in escape sequences and are processed immediately

---

## 4. Existing .NET Terminal Libraries

### Terminal.Gui

**Source**: [github.com/gui-cs/Terminal.Gui](https://github.com/gui-cs/Terminal.Gui)

**Relevance**: Full TUI framework with internal terminal handling.

**Patterns to Study**:
- Driver abstraction for terminal I/O
- Screen buffer management
- Unicode/emoji handling
- Test infrastructure using virtual drivers

**Implementation Notes**:
- Uses a "Driver" pattern to abstract terminal operations
- Has `FakeDriver` for testing—similar concept to VirtualConsole
- Manages screen as 2D array of cells with attributes

### Spectre.Console

**Source**: [github.com/spectreconsole/spectre.console](https://github.com/spectreconsole/spectre.console)

**Relevance**: Primary consumer of VirtualConsole's output—understanding what sequences it generates.

**Key Patterns**:
- `IAnsiConsole` interface for console abstraction
- Renders to ANSI sequences (the input VirtualConsole will process)
- Uses SGR sequences for colors/styles
- Tables, panels, trees all render as ANSI text

**Implementation Notes**:
- Spectre.Console has `TestConsole` for testing—but it doesn't process ANSI, just captures output
- Our VirtualConsole fills the gap by actually processing those sequences
- Future `IAnsiConsole` implementation should target Spectre.Console compatibility

---

## 5. Wikipedia ANSI Escape Code Reference

### Primary Reference: Wikipedia ANSI Escape Code

**Source**: [en.wikipedia.org/wiki/ANSI_escape_code](https://en.wikipedia.org/wiki/ANSI_escape_code)

**Relevance**: Excellent practical reference with tables and examples.

**Key Information**:
- C0 control codes (BEL, BS, TAB, LF, CR)
- CSI command format and common sequences
- SGR parameter table with all attributes
- Color codes with RGB values for different terminals

---

## 6. Implementation Strategy

### Parser Architecture

**Decision**: State machine parser for CSI sequences.

**Rationale**: 
- CSI sequences have well-defined structure
- State machine handles partial sequences and embedded control chars
- Easy to extend for new sequences

**States**:
1. **Ground**: Normal text, watching for ESC
2. **Escape**: Saw ESC, waiting for `[` (CSI) or other
3. **CSI Entry**: In CSI, collecting parameters
4. **CSI Param**: Collecting numeric parameters
5. **CSI Execute**: Got final byte, execute command

### Screen Buffer Design

**Decision**: 2D array of ScreenCell structs.

**Rationale**:
- Direct row/column access (O(1) lookup)
- Each cell stores char + style independently
- Simple to implement and understand

**Structure**:
```csharp
public struct ScreenCell
{
    public char Character { get; set; }
    public CellStyle Style { get; set; }
}

public struct CellStyle
{
    public ConsoleColor? ForegroundColor { get; set; }
    public ConsoleColor? BackgroundColor { get; set; }
    public CellAttributes Attributes { get; set; }  // Bold, Italic, etc.
}

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

### Error Handling

**Decision**: Throw on unrecognized sequences with details.

**Rationale**:
- Testing library should be strict—unknown sequences indicate a gap
- Error message includes the sequence for easy debugging
- Matches spec requirement FR-016

---

## 7. Alternatives Considered

### Alternative 1: Process Output as Text Stream

**Rejected**: Doesn't maintain screen state; just accumulates output.

**Why Rejected**: This is exactly the problem VirtualConsole solves.

### Alternative 2: Use Existing Terminal Emulator Library

**Rejected**: No .NET library provides the specific capability needed.

**Why Rejected**: 
- Terminal.Gui's FakeDriver is internal and tied to Terminal.Gui
- Spectre.Console's TestConsole doesn't process ANSI sequences
- Building our own gives full control and clean API

### Alternative 3: Regex-Based ANSI Parsing

**Rejected**: Regex can't handle all edge cases.

**Why Rejected**:
- Control chars embedded in sequences
- Partial sequences at buffer boundaries
- State-dependent behavior (cursor position affects output location)

---

## 8. Test Strategy

### Unit Tests
- Individual ANSI sequence parsing
- SGR attribute application
- Cursor movement bounds checking
- Screen clear operations

### Integration Tests
- Multi-sequence operations
- Real Spectre.Console output processing
- Complex render scenarios

### Milestone Tests (per spec)
1. **Menu rendering**: Selection inversion + filter highlighting + scrolling
2. **Progress bar**: In-place updates with cursor movement
3. **Table rendering**: Borders, headers, styled cells
4. **Multi-region**: Independent styled regions updating

### Regression Test for Original Bug
```csharp
[TestMethod]
public void OverwriteRemovesStyling_BugDetected()
{
    var console = new VirtualConsole(80, 25);
    
    // Write blue text
    console.Write("\x1b[34mHello\x1b[0m");
    Assert.AreEqual(ConsoleColor.Blue, console.GetCell(0, 0).Style.ForegroundColor);
    
    // Overwrite same location with unstyled text (simulating bug)
    console.Write("\x1b[H");  // cursor home
    console.Write("Hello");   // no color
    
    // VirtualConsole detects the change—this is the value proposition
    Assert.IsNull(console.GetCell(0, 0).Style.ForegroundColor);
}
```

---

## References Summary

| Resource | URL | Usage |
|----------|-----|-------|
| ECMA-48 Standard | https://www.ecma-international.org/publications-and-standards/standards/ecma-48/ | Authoritative CSI/SGR definitions |
| XTerm Control Sequences | https://invisible-island.net/xterm/ctlseqs/ctlseqs.html | Practical sequence reference |
| VT100 User Guide Ch.3 | https://vt100.net/docs/vt100-ug/chapter3.html | Original terminal behavior |
| Wikipedia ANSI Codes | https://en.wikipedia.org/wiki/ANSI_escape_code | Quick reference tables |
| Terminal.Gui | https://github.com/gui-cs/Terminal.Gui | .NET TUI patterns, FakeDriver concept |
| Spectre.Console | https://github.com/spectreconsole/spectre.console | Output format to process, IAnsiConsole interface |
