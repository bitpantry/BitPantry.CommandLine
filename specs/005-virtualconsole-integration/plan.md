# Implementation Plan: VirtualConsole Integration

**Branch**: `005-virtualconsole-integration` | **Date**: 2026-01-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-virtualconsole-integration/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Integrate the VirtualConsole terminal emulator (from spec 011 on master) into the rework branch to enable functional UI testing. This involves cherry-picking BitPantry.VirtualConsole and BitPantry.VirtualConsole.Tests, creating a slimmed-down BitPantry.VirtualConsole.Testing with general-purpose components only, migrating existing tests from VirtualAnsiConsole to VirtualConsoleAnsiAdapter, and copying documentation.

## Technical Context

**Language/Version**: C# / .NET 8.0 (VirtualConsole targets .NET Standard 2.0)  
**Primary Dependencies**: FluentAssertions 6.12.0, Spectre.Console, MSTest 3.6.1  
**Storage**: N/A  
**Testing**: MSTest with FluentAssertions (250 existing tests in VirtualConsole.Tests)  
**Target Platform**: Cross-platform (.NET Standard 2.0 for core, .NET 8.0 for testing)
**Project Type**: Multi-project solution - adding 3 projects  
**Performance Goals**: N/A (test infrastructure, not production code)  
**Constraints**: VirtualConsole.Testing MUST NOT depend on BitPantry.CommandLine  
**Scale/Scope**: Cherry-pick ~30 source files, migrate 4 test usages, delete 3 files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Test-Driven Development** | ✅ PASS | VirtualConsole has 250 tests written TDD-style. Cherry-pick includes tests. |
| **II. Dependency Injection** | ✅ PASS | VirtualConsole is a pure library with no DI requirements. Testing adapters are simple wrappers. |
| **III. Security by Design** | ✅ N/A | Test infrastructure only - no security concerns. |
| **IV. Follow Existing Patterns** | ✅ PASS | VirtualConsoleAnsiAdapter follows IAnsiConsole pattern. FluentAssertions follow existing test patterns. |
| **V. Integration Testing** | ✅ PASS | VirtualConsole.Tests provides comprehensive integration tests for terminal emulation. |

**Gate Result**: ✅ PASS - No violations. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/005-virtualconsole-integration/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (minimal - cherry-pick)
├── quickstart.md        # Phase 1 output
├── checklists/          # Validation checklists
└── references/          # Spec 011 materials from master
```

### Source Code (repository root)

```text
# Projects to add (cherry-pick from master)
BitPantry.VirtualConsole/           # Core terminal emulator (.NET Standard 2.0)
├── VirtualConsole.cs
├── ScreenBuffer.cs
├── ScreenCell.cs
├── ScreenRow.cs
├── CellStyle.cs
├── CellAttributes.cs
├── AnsiParser/                     # ANSI sequence parsing
│   ├── AnsiSequenceParser.cs
│   ├── CsiSequence.cs
│   ├── SgrProcessor.cs
│   └── CursorProcessor.cs
└── BitPantry.VirtualConsole.csproj

BitPantry.VirtualConsole.Tests/     # 250 tests for VirtualConsole
├── VirtualConsoleTests.cs
├── ScreenBufferTests.cs
├── AnsiParserTests.cs
├── SgrProcessorTests.cs
├── MilestoneTests/
└── BitPantry.VirtualConsole.Tests.csproj

BitPantry.VirtualConsole.Testing/   # NEW - general-purpose testing utilities
├── VirtualConsoleAssertions.cs     # FluentAssertions extensions
├── VirtualConsoleAnsiAdapter.cs    # IAnsiConsole → VirtualConsole bridge
├── IKeyboardSimulator.cs           # Keyboard simulation interface
└── BitPantry.VirtualConsole.Testing.csproj  # NEW (not cherry-picked)

# Documentation to copy
Docs/VirtualConsole/
├── README.md
├── getting-started.md
└── ansi-support.md

# Files to DELETE (after migration)
BitPantry.CommandLine.Tests/VirtualConsole/  # Entire folder
```

**Structure Decision**: Adding 3 new projects at repository root following existing solution structure. VirtualConsole.Testing is a new project with cherry-picked files but a new .csproj to avoid autocomplete dependencies.

## Complexity Tracking

> No violations - table not required.
