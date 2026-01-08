# Data Model: VirtualConsole Integration

**Feature**: 005-virtualconsole-integration  
**Date**: 2026-01-08  
**Status**: Reference Only (cherry-pick from master)

## Overview

This is a cherry-pick integration - no new data models are created.

**For complete VirtualConsole entity documentation, see:**
- **[references/spec-011-data-model.md](references/spec-011-data-model.md)** - Full entity definitions

---

## Quick Reference

### Core Entities (from BitPantry.VirtualConsole)

| Entity | Purpose | See spec-011 |
|--------|---------|--------------|
| VirtualConsole | Terminal emulator with ANSI parsing | §1.0 |
| ScreenBuffer | Internal 2D cell grid | §2.0 |
| ScreenCell | Character + style | §3.0 |
| CellStyle | Immutable styling (colors, attributes) | §4.0 |
| CellAttributes | Flags: Bold, Italic, Underline, etc. | §5.0 |

### Testing Entities (for BitPantry.VirtualConsole.Testing)

| Entity | Purpose |
|--------|---------|
| VirtualConsoleAnsiAdapter | IAnsiConsole backed by VirtualConsole |
| VirtualConsoleAssertions | FluentAssertions extensions |
| IKeyboardSimulator | Interface for key input simulation |

---

## Entity Relationships

```
VirtualConsole (see spec-011)
    └── ScreenBuffer → ScreenCell[,] → CellStyle + CellAttributes

VirtualConsoleAnsiAdapter
    ├── VirtualConsole (composition)
    └── IAnsiConsole (implementation)

VirtualConsoleAssertions
    └── VirtualConsole (extension methods)
```

---

## Migration Mapping

| Old (to delete) | New (from master) |
|-----------------|-------------------|
| VirtualAnsiConsole | VirtualConsoleAnsiAdapter + VirtualConsole |
| VirtualAnsiConsoleAssertions | VirtualConsoleAssertions |
| (none) | IKeyboardSimulator |
