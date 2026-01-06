# Tasks: BitPantry.VirtualConsole

**Input**: Design documents from `/specs/011-virtual-console/`  
**Prerequisites**: plan.md, spec.md, data-model.md, research.md, quickstart.md  
**Generated**: 2026-01-04

**Testing Approach**: Strict TDD required per spec - all tests MUST be written BEFORE implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md structure:
- **Core Package**: `BitPantry.VirtualConsole/`
- **Test Project**: `BitPantry.VirtualConsole.Tests/`

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create project structure and configure build system

- [x] T001 Create `BitPantry.VirtualConsole/BitPantry.VirtualConsole.csproj` targeting .NET Standard 2.0+
- [x] T002 [P] Create `BitPantry.VirtualConsole.Tests/BitPantry.VirtualConsole.Tests.csproj` with MSTest and FluentAssertions
- [x] T003 Add project references from test project to main project
- [x] T004 Verify projects build successfully with `dotnet build` (keep isolated from main solution for now)

---

## Phase 2: Foundational (Core Data Types)

**Purpose**: Core value types and enums that ALL user stories depend on. MUST complete before user story work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Architecture Design

- [x] T005 Document architecture decisions addressing AC-001 through AC-005 (input queue extension points, IAnsiConsole compatibility, separable screen buffer) in `BitPantry.VirtualConsole/ARCHITECTURE.md`

### Tests (TDD - Write First)

- [x] T006 [P] Write tests for CellAttributes enum flags in `BitPantry.VirtualConsole.Tests/CellAttributesTests.cs`
- [x] T007 [P] Write tests for ClearMode enum in `BitPantry.VirtualConsole.Tests/ClearModeTests.cs`
- [x] T008 [P] Write tests for CellStyle immutability and equality in `BitPantry.VirtualConsole.Tests/CellStyleTests.cs`
- [x] T009 [P] Write tests for ScreenCell default values and equality in `BitPantry.VirtualConsole.Tests/ScreenCellTests.cs`
- [x] T010 [P] Write tests for CursorPosition struct in `BitPantry.VirtualConsole.Tests/CursorPositionTests.cs`

### Implementation

- [x] T011 [P] Implement CellAttributes flags enum in `BitPantry.VirtualConsole/CellAttributes.cs`
- [x] T012 [P] Implement ClearMode enum in `BitPantry.VirtualConsole/ClearMode.cs`
- [x] T013 [P] Implement CellStyle record/struct with immutable With* methods in `BitPantry.VirtualConsole/CellStyle.cs`
- [x] T014 [P] Implement ScreenCell struct in `BitPantry.VirtualConsole/ScreenCell.cs`
- [x] T015 [P] Implement CursorPosition struct in `BitPantry.VirtualConsole/CursorPosition.cs`

**Checkpoint**: All core data types in place - user story implementation can now begin.

---

## Phase 3: User Story 1 - Basic Screen State Verification (Priority: P1) 🎯 MVP

**Goal**: Query current visual state of console after operations - prove content overwrites work correctly.

**Independent Test**: Write "Hello", move cursor back, overwrite with "XYZ" → screen shows "HeXYZ" not "HelloXYZ".

### Tests (TDD - Write First)

- [x] T016 [P] [US1] Write tests for ScreenBuffer initialization in `BitPantry.VirtualConsole.Tests/ScreenBufferTests.cs`
- [x] T017 [P] [US1] Write tests for WriteChar at cursor position in `BitPantry.VirtualConsole.Tests/ScreenBufferTests.cs`
- [x] T018 [P] [US1] Write tests for cursor movement (MoveCursor, MoveCursorRelative) in `BitPantry.VirtualConsole.Tests/ScreenBufferTests.cs`
- [x] T019 [P] [US1] Write tests for GetCell and GetRow queries in `BitPantry.VirtualConsole.Tests/ScreenBufferTests.cs`
- [x] T020 [P] [US1] Write tests for cursor boundary clamping in `BitPantry.VirtualConsole.Tests/ScreenBufferTests.cs`
- [x] T021 [P] [US1] Write tests for ScreenRow wrapper in `BitPantry.VirtualConsole.Tests/ScreenRowTests.cs`
- [x] T022 [P] [US1] Write tests for VirtualConsole.Write basic text (no ANSI) in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`
- [x] T023 [P] [US1] Write tests for overwrite scenarios using ScreenBuffer.MoveCursorRelative in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`
- [x] T024 [P] [US1] Write tests for GetScreenText and GetScreenContent in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`
- [x] T025 [P] [US1] Write tests for Clear method in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`

### Implementation

- [x] T026 [US1] Implement ScreenRow wrapper class in `BitPantry.VirtualConsole/ScreenRow.cs`
- [x] T027 [US1] Implement ScreenBuffer with cells grid and cursor in `BitPantry.VirtualConsole/ScreenBuffer.cs`
- [x] T028 [US1] Implement VirtualConsole constructor and basic Write (plain text only) in `BitPantry.VirtualConsole/VirtualConsole.cs`
- [x] T029 [US1] Implement query methods (GetCell, GetRow, GetScreenText, GetScreenContent) in `BitPantry.VirtualConsole/VirtualConsole.cs`
- [x] T030 [US1] Implement Clear method in `BitPantry.VirtualConsole/VirtualConsole.cs`

**Checkpoint**: Can write plain text, move cursor manually, query screen state - proves overwrite detection works.

---

## Phase 4: User Story 3 - Cursor Movement Processing (Priority: P1)

**Goal**: Process ANSI cursor movement sequences so re-renders and in-place updates work correctly.

**Independent Test**: Write text, send `\x1b[5D` (cursor back 5), write more text → content correctly overwritten.

### Tests (TDD - Write First)

- [x] T031 [P] [US3] Write tests for AnsiSequenceParser state machine basics in `BitPantry.VirtualConsole.Tests/AnsiParserTests.cs`
- [x] T032 [P] [US3] Write tests for CSI sequence parameter parsing in `BitPantry.VirtualConsole.Tests/AnsiParserTests.cs`
- [x] T033 [P] [US3] Write tests for CursorProcessor handling CUU/CUD/CUF/CUB in `BitPantry.VirtualConsole.Tests/CursorProcessorTests.cs`
- [x] T034 [P] [US3] Write tests for CursorProcessor handling CUP (cursor position) in `BitPantry.VirtualConsole.Tests/CursorProcessorTests.cs`
- [x] T035 [P] [US3] Write tests for CursorProcessor handling CHA (cursor horizontal absolute) in `BitPantry.VirtualConsole.Tests/CursorProcessorTests.cs`
- [x] T036 [P] [US3] Write tests for carriage return (\r) processing in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`
- [x] T037 [P] [US3] Write tests for newline (\n) processing in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`
- [x] T038 [P] [US3] Write tests for VirtualConsole.Write with embedded cursor sequences in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`

### Implementation

- [x] T039 [US3] Implement ParserResult types in `BitPantry.VirtualConsole/AnsiParser/ParserResult.cs`
- [x] T040 [US3] Implement CsiSequence record in `BitPantry.VirtualConsole/AnsiParser/CsiSequence.cs`
- [x] T041 [US3] Implement AnsiSequenceParser state machine in `BitPantry.VirtualConsole/AnsiParser/AnsiSequenceParser.cs`
- [x] T042 [US3] Implement CursorProcessor for cursor movement sequences in `BitPantry.VirtualConsole/AnsiParser/CursorProcessor.cs`
- [x] T043 [US3] Integrate ANSI parsing into VirtualConsole.Write in `BitPantry.VirtualConsole/VirtualConsole.cs`

**Checkpoint**: Can process cursor movement ANSI codes - in-place updates work correctly.

---

## Phase 5: User Story 2 - ANSI Color and Style Tracking (Priority: P1)

**Goal**: Track text styling (colors, bold, invert) so styled output can be verified.

**Independent Test**: Write blue text, query cell → foreground is blue. Write inverted text → Reverse attribute set.

### Tests (TDD - Write First)

- [x] T044 [P] [US2] Write tests for SgrProcessor foreground colors (30-37, 90-97) in `BitPantry.VirtualConsole.Tests/SgrProcessorTests.cs`
- [x] T045 [P] [US2] Write tests for SgrProcessor background colors (40-47, 100-107) in `BitPantry.VirtualConsole.Tests/SgrProcessorTests.cs`
- [x] T046 [P] [US2] Write tests for SgrProcessor attributes (bold, italic, underline, reverse, etc.) in `BitPantry.VirtualConsole.Tests/SgrProcessorTests.cs`
- [x] T047 [P] [US2] Write tests for SgrProcessor reset (code 0) in `BitPantry.VirtualConsole.Tests/SgrProcessorTests.cs`
- [x] T048 [P] [US2] Write tests for SgrProcessor default foreground (39) and background (49) in `BitPantry.VirtualConsole.Tests/SgrProcessorTests.cs`
- [x] T049 [P] [US2] Write tests for SgrProcessor attribute removal codes (22-29) in `BitPantry.VirtualConsole.Tests/SgrProcessorTests.cs`
- [x] T050 [P] [US2] Write tests for 256-color mode (38;5;n and 48;5;n) in `BitPantry.VirtualConsole.Tests/SgrProcessorTests.cs`
- [x] T050a [P] [US2] Write tests for 24-bit TrueColor mode (38;2;r;g;b and 48;2;r;g;b) in `BitPantry.VirtualConsole.Tests/SgrProcessorTests.cs`
- [x] T051 [P] [US2] Write tests for VirtualConsole.Write with color sequences in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`
- [x] T052 [P] [US2] Write tests for querying styled cells in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`

### Implementation

- [x] T053 [US2] Implement all Color types (BasicColor, Color256, TrueColor) with full 24-bit support in `BitPantry.VirtualConsole/Color.cs`
- [x] T054 [US2] Update CellStyle to support extended colors in `BitPantry.VirtualConsole/CellStyle.cs`
- [x] T055 [US2] Implement SgrProcessor for SGR sequences in `BitPantry.VirtualConsole/AnsiParser/SgrProcessor.cs`
- [x] T056 [US2] Integrate SgrProcessor into VirtualConsole.Write in `BitPantry.VirtualConsole/VirtualConsole.cs`
- [x] T057 [US2] Implement ScreenBuffer.ApplyStyle and ResetStyle in `BitPantry.VirtualConsole/ScreenBuffer.cs`

**Checkpoint**: Can track and query text styling - color and attribute assertions work.

---

## Phase 6: User Story 4 - Row and Cell Queries with Style (Priority: P2)

**Goal**: Query individual cells or entire rows and get both content and styling.

**Independent Test**: Write row with mixed styling, query row → get text and per-character styling.

### Tests (TDD - Write First)

- [x] T058 [P] [US4] Write tests for ScreenRow.GetCells() enumeration in `BitPantry.VirtualConsole.Tests/ScreenRowTests.cs`
- [x] T059 [P] [US4] Write tests for ScreenRow with mixed styling in `BitPantry.VirtualConsole.Tests/ScreenRowTests.cs`
- [x] T060 [P] [US4] Write tests for querying empty regions (default space + style) in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`
- [x] T061 [P] [US4] Write tests for GetCell with complete style information in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`

### Implementation

- [x] T062 [US4] Enhance ScreenRow.GetCells() implementation in `BitPantry.VirtualConsole/ScreenRow.cs`
- [x] T063 [US4] Verify GetCell returns complete style info in `BitPantry.VirtualConsole/VirtualConsole.cs`

**Checkpoint**: Query API returns complete content + styling for cells and rows.

---

## Phase 7: User Story 5 - Clean API for External Consumers (Priority: P2)

**Goal**: Expose clean query API without test framework dependencies.

**Independent Test**: Verify package has no test framework references; API provides sufficient data for external assertions.

### Tests (TDD - Write First)

- [x] T064 [P] [US5] Write tests verifying public API completeness in `BitPantry.VirtualConsole.Tests/PublicApiTests.cs`
- [x] T065 [P] [US5] Write tests verifying cursor position is exposed via public API in `BitPantry.VirtualConsole.Tests/PublicApiTests.cs`
- [x] T066 [P] [US5] Write tests verifying screen dimensions are queryable in `BitPantry.VirtualConsole.Tests/PublicApiTests.cs`

### Implementation

- [x] T067 [US5] Review and finalize public API surface in `BitPantry.VirtualConsole/VirtualConsole.cs`
- [x] T068 [US5] Add CursorRow and CursorColumn public properties in `BitPantry.VirtualConsole/VirtualConsole.cs`
- [x] T069 [US5] Verify .csproj has no test framework references in `BitPantry.VirtualConsole/BitPantry.VirtualConsole.csproj`

**Checkpoint**: Clean API ready for external consumption.

---

## Phase 8: User Story 7 - Configurable Screen Dimensions (Priority: P3)

**Goal**: Configure virtual console dimensions for specific terminal size testing.

**Independent Test**: Create 40x10 console, write past column 40 → wraps to next line.

### Tests (TDD - Write First)

- [x] T070 [P] [US7] Write tests for custom dimensions in constructor in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`
- [x] T071 [P] [US7] Write tests for line wrapping at width boundary in `BitPantry.VirtualConsole.Tests/ScreenBufferTests.cs`
- [x] T072 [P] [US7] Write tests for content clipping at height boundary in `BitPantry.VirtualConsole.Tests/ScreenBufferTests.cs`
- [x] T073 [P] [US7] Write tests for dimension validation (must be > 0) in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`

### Implementation

- [x] T074 [US7] Implement line wrapping logic in ScreenBuffer.WriteChar in `BitPantry.VirtualConsole/ScreenBuffer.cs`
- [x] T075 [US7] Implement scrolling or clipping at height boundary in `BitPantry.VirtualConsole/ScreenBuffer.cs`
- [x] T076 [US7] Add dimension validation to VirtualConsole constructor in `BitPantry.VirtualConsole/VirtualConsole.cs`

**Checkpoint**: Configurable dimensions with proper wrapping behavior.

---

## Phase 9: User Story 6 - Extensible ANSI Sequence Handling (Priority: P3)

**Goal**: ANSI processing is extensible for future sequence support.

**Independent Test**: Unrecognized sequence throws exception with sequence details.

### Tests (TDD - Write First)

- [x] T077 [P] [US6] Write tests for unknown sequence throwing exception in `BitPantry.VirtualConsole.Tests/AnsiParserTests.cs`
- [x] T078 [P] [US6] Write tests for exception message containing sequence details in `BitPantry.VirtualConsole.Tests/AnsiParserTests.cs`
- [x] T079 [P] [US6] Write tests for ED (erase display) sequences in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`
- [x] T080 [P] [US6] Write tests for EL (erase line) sequences in `BitPantry.VirtualConsole.Tests/VirtualConsoleTests.cs`

### Implementation

- [x] T081 [US6] Implement exception throwing for unrecognized sequences in `BitPantry.VirtualConsole/AnsiParser/AnsiSequenceParser.cs`
- [x] T082 [US6] Implement ED (erase display) handler in `BitPantry.VirtualConsole/AnsiParser/EraseProcessor.cs`
- [x] T083 [US6] Implement EL (erase line) handler in `BitPantry.VirtualConsole/AnsiParser/EraseProcessor.cs`
- [x] T084 [US6] Integrate ED/EL into VirtualConsole.Write in `BitPantry.VirtualConsole/VirtualConsole.cs`

**Checkpoint**: Unknown sequences throw helpful exceptions; erase sequences work.

---

## Phase 10: User Story 8 - Real-World CLI Complexity (Priority: P2) 🏆 Milestone

**Goal**: Prove VirtualConsole handles production CLI output (progress bars, tables, multi-region layouts).

**Independent Test**: Capture Spectre.Console table output → VirtualConsole produces correct screen state.

### Tests (TDD - Write First)

- [x] T085 [P] [US8] Write menu rendering milestone test in `BitPantry.VirtualConsole.Tests/MilestoneTests/MenuRenderingTests.cs`
- [x] T086 [P] [US8] Write progress bar milestone test in `BitPantry.VirtualConsole.Tests/MilestoneTests/ProgressBarTests.cs`
- [x] T087 [P] [US8] Write table rendering milestone test in `BitPantry.VirtualConsole.Tests/MilestoneTests/TableRenderingTests.cs`
- [x] T088 [P] [US8] Write multi-region milestone test in `BitPantry.VirtualConsole.Tests/MilestoneTests/MultiRegionTests.cs`
- [x] T089 [P] [US8] Write filter highlighting regression test (the original bug scenario) in `BitPantry.VirtualConsole.Tests/MilestoneTests/MenuRenderingTests.cs`

### Implementation

- [x] T090 [US8] Fix any issues discovered by milestone tests
- [x] T091 [US8] Document known limitations for complex scenarios in README

**Checkpoint**: Milestone tests pass - production-ready for real CLI testing.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, cleanup, and final validation

- [x] T092 Create README.md with usage examples in `BitPantry.VirtualConsole/README.md`
- [x] T093 [P] Add XML documentation comments to all public API members
- [x] T094 [P] Verify all tests pass with `dotnet test`
- [x] T095 Review architecture against AC-001 through AC-005 (headless terminal extension points)
- [x] T096 Verify package has no test framework dependencies (SC-004)

---

## Dependencies & Execution Order

```
Phase 1 (Setup)
    │
    ▼
Phase 2 (Foundational) ─── GATE: Must complete before user stories
    │
    ├─────────┬─────────┬─────────┐
    ▼         ▼         ▼         │
Phase 3    Phase 4    Phase 5    │  ← P1 User Stories (can partially parallelize)
(US1)      (US3)      (US2)      │     US1 first (core capability)
    │         │         │         │     US3 depends on parser from US1
    │         └────┬────┘         │     US2 depends on parser from US3
    │              │              │
    ├──────────────┴──────────────┤
    ▼                             │
Phase 6 (US4) ◄───────────────────┤  ← P2 (depends on P1 completion)
Phase 7 (US5)                     │
Phase 10 (US8)                    │
    │                             │
    ▼                             │
Phase 8 (US7)                     │  ← P3 (depends on core)
Phase 9 (US6)                     │
    │                             │
    ▼                             │
Phase 11 (Polish)                 │
```

### Parallel Execution Opportunities

**Within Phase 2**: T006-T010 (tests) can run in parallel; T011-T015 (implementation) can run in parallel after tests pass.

**Within Phase 3 (US1)**: T016-T025 (tests) can all run in parallel; implementation tasks sequential.

**Across P1 Stories**: After US1 is complete, US3 and US2 implementation can overlap since they touch different files (parser vs processor).

**Within Milestone Phase**: T085-T089 (milestone tests) can all run in parallel.

---

## Implementation Strategy

### MVP Scope (Recommended First Delivery)

**Phases 1-3**: Setup + Foundational + User Story 1

Delivers: Basic screen buffer with text writing, cursor tracking, and query API. Proves the core value proposition (overwrite detection).

### Increment 2

**Phases 4-5**: User Stories 3 + 2 (Cursor Movement + Styling)

Delivers: Full ANSI cursor and color support. Enables testing of styled, dynamic CLI output.

### Increment 3

**Phases 6-7 + 10**: User Stories 4 + 5 + 8

Delivers: Complete query API + milestone tests proving production readiness.

### Increment 4

**Phases 8-9 + 11**: User Stories 7 + 6 + Polish

Delivers: Full configurability, extensibility, and documentation.

---

## Summary

| Metric | Value |
|--------|-------|
| Total Tasks | 97 |
| Setup Tasks | 4 |
| Foundational Tasks | 11 |
| User Story Tasks | 77 |
| Polish Tasks | 5 |
| Parallelizable Tasks | 65 (marked with [P]) |

| User Story | Task Count | Priority |
|------------|------------|----------|
| US1 - Basic Screen State | 15 | P1 🎯 MVP |
| US2 - Color and Style | 15 | P1 |
| US3 - Cursor Movement | 13 | P1 |
| US4 - Row/Cell Queries | 6 | P2 |
| US5 - Clean API | 6 | P2 |
| US6 - Extensibility | 8 | P3 |
| US7 - Dimensions | 7 | P3 |
| US8 - Real-World Complexity | 7 | P2 🏆 |

**MVP Recommendation**: Complete Phases 1-5 (Setup + Foundational + US1 + US3 + US2) for a functional VirtualConsole with full ANSI support. This enables the original use case (detecting menu filter highlighting bugs).
