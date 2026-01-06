# Implementation Plan: BitPantry.VirtualConsole

**Branch**: `011-virtual-console` | **Date**: 2026-01-04 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/011-virtual-console/spec.md`

## Summary

BitPantry.VirtualConsole is a virtual terminal emulator providing a 2D screen buffer with ANSI escape sequence processing for automated CLI testing. It enables point-in-time visual state assertions—the "Selenium for CLI apps"—by maintaining what a user would actually see on screen, rather than accumulating all output ever written.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: TBD (external dependencies allowed if useful functionality exists)  
**Storage**: In-memory 2D character buffer (ScreenCell[,])  
**Testing**: MSTest with FluentAssertions (per solution standards)  
**Target Platform**: Cross-platform (.NET Standard 2.0+ for maximum compatibility)
**Project Type**: Library (single project + test project)  
**Performance Goals**: N/A (configurable buffer, no package-imposed limits)  
**Constraints**: Strict TDD  
**Scale/Scope**: Initial release supports ANSI sequences used by Spectre.Console

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-Driven Development | ✅ PASS | Spec mandates strict TDD; TR-002 requires tests before implementation |
| II. Dependency Injection | ✅ PASS | VirtualConsole will be constructor-injectable; no static methods for core functionality |
| III. Security by Design | ✅ N/A | No security concerns—in-memory processing only |
| IV. Follow Existing Patterns | ✅ PASS | Will follow existing solution patterns (project structure, naming, testing) |
| V. Integration Testing | ✅ PASS | TR-005 requires complexity milestone tests (menu, progress bar, table, multi-region) |

## Project Structure

### Documentation (this feature)

```text
specs/011-virtual-console/
├── plan.md              # This file
├── research.md          # Phase 0 output - ANSI research and implementation guidance
├── data-model.md        # Phase 1 output - Entity definitions
├── quickstart.md        # Phase 1 output - Getting started guide
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
BitPantry.VirtualConsole/
├── BitPantry.VirtualConsole.csproj
├── VirtualConsole.cs           # Main entry point
├── ScreenBuffer.cs             # 2D character grid
├── ScreenCell.cs               # Single cell (char + style)
├── CellStyle.cs                # Foreground, background, attributes
├── ScreenRow.cs                # Row access wrapper
├── CursorPosition.cs           # Row/column tracking
└── AnsiParser/
    ├── AnsiSequenceParser.cs   # CSI sequence parser
    ├── SgrProcessor.cs         # SGR (styling) processor
    └── CursorProcessor.cs      # Cursor movement processor

BitPantry.VirtualConsole.Tests/
├── BitPantry.VirtualConsole.Tests.csproj
├── VirtualConsoleTests.cs
├── ScreenBufferTests.cs
├── AnsiParserTests.cs
├── SgrProcessorTests.cs
├── CursorProcessorTests.cs
└── MilestoneTests/
    ├── MenuRenderingTests.cs
    ├── ProgressBarTests.cs
    ├── TableRenderingTests.cs
    └── MultiRegionTests.cs
```

**Structure Decision**: Two projects—VirtualConsole library (future NuGet package) and test project (internal, for unit testing VirtualConsole only).

## Complexity Tracking

> No Constitution Check violations requiring justification.
