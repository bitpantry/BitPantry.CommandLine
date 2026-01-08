# Tasks: VirtualConsole Integration

**Input**: Design documents from `/specs/005-virtualconsole-integration/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: Not explicitly requested in spec. Existing VirtualConsole.Tests (250 tests) are cherry-picked as part of implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Ensure branch and prerequisites are ready

- [X] T001 Verify on `005-virtualconsole-integration` branch (based on rework)
- [X] T002 Fetch latest from origin to ensure master VirtualConsole commits are available

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No foundational phase needed - this is a cherry-pick integration with no blocking infrastructure

**⚠️ NOTE**: This feature has minimal setup - user stories can proceed immediately after Phase 1.

---

## Phase 3: User Story 1 - Cherry-Pick VirtualConsole Projects (Priority: P1) 🎯 MVP

**Goal**: Add VirtualConsole and VirtualConsole.Tests projects from master branch to the rework-based solution

**Independent Test**: Build solution and run `dotnet test BitPantry.VirtualConsole.Tests` - all 250 tests pass

### Implementation for User Story 1

- [X] T003 [US1] Cherry-pick BitPantry.VirtualConsole project from master via `git checkout origin/master -- BitPantry.VirtualConsole`
- [X] T004 [US1] Cherry-pick BitPantry.VirtualConsole.Tests project from master via `git checkout origin/master -- BitPantry.VirtualConsole.Tests`
- [X] T005 [US1] Add BitPantry.VirtualConsole to solution via `dotnet sln add BitPantry.VirtualConsole/BitPantry.VirtualConsole.csproj`
- [X] T006 [US1] Add BitPantry.VirtualConsole.Tests to solution via `dotnet sln add BitPantry.VirtualConsole.Tests/BitPantry.VirtualConsole.Tests.csproj`
- [X] T007 [US1] Build solution to verify no compile errors
- [X] T008 [US1] Run VirtualConsole.Tests to verify all 250 tests pass - HALT if any fail

**Checkpoint**: VirtualConsole core is available. Solution builds. All VirtualConsole tests pass.

---

## Phase 5: User Story 2 - Migrate Existing Tests to VirtualConsoleAnsiAdapter (Priority: P2)

**Goal**: Migrate 4 usages in 3 files from `VirtualAnsiConsole` to `VirtualConsoleAnsiAdapter`, then delete the old folder

**Independent Test**: All existing CommandLine tests compile and pass. No references to `VirtualAnsiConsole` remain.

**Dependencies**: Requires User Story 3 (VirtualConsole.Testing) to be completed first, since migration uses `VirtualConsoleAnsiAdapter` from that project.

### Implementation for User Story 2

- [X] T009 [US2] Add project reference from BitPantry.CommandLine.Tests to BitPantry.VirtualConsole.Testing in BitPantry.CommandLine.Tests/BitPantry.CommandLine.Tests.csproj
- [X] T010 [US2] Add project reference from BitPantry.CommandLine.Tests.Remote.SignalR to BitPantry.VirtualConsole.Testing in BitPantry.CommandLine.Tests.Remote.SignalR/BitPantry.CommandLine.Tests.Remote.SignalR.csproj
- [X] T011 [US2] Migrate VirtualAnsiConsole usage in BitPantry.CommandLine.Tests/AutoComplete/AutoCompleteControllerTests.cs (2 instances)
- [X] T012 [US2] Migrate VirtualAnsiConsole usage in BitPantry.CommandLine.Tests/TestConsoleService.cs (1 instance)
- [X] T013 [US2] Migrate VirtualAnsiConsole usage in BitPantry.CommandLine.Tests.Remote.SignalR/TestEnvironment.cs (1 instance)
- [X] T014 [US2] Delete old VirtualConsole folder: BitPantry.CommandLine.Tests/VirtualConsole/ (3 files)
- [X] T015 [US2] Build solution and verify no compile errors
- [X] T016 [US2] Run all tests to verify migration success

**Checkpoint**: All tests pass. Old VirtualAnsiConsole is deleted. No legacy references remain.

---

## Phase 4: User Story 3 - Create VirtualConsole.Testing (Priority: P3)

**Goal**: Create a slimmed-down VirtualConsole.Testing project with general-purpose components only (no autocomplete coupling)

**Independent Test**: Create test using FluentAssertions on VirtualConsole and route Spectre.Console output through adapter

**⚠️ NOTE**: Despite being P3, this phase MUST complete before User Story 2 (Phase 5) can begin. Execution order: US1 → US3 → US2 → US4

### Implementation for User Story 3

- [X] T017 [US3] Create directory BitPantry.VirtualConsole.Testing/
- [X] [P] [US3] Extract VirtualConsoleAssertions.cs from master via `git show origin/master:BitPantry.VirtualConsole.Testing/VirtualConsoleAssertions.cs`
- [X] [P] [US3] Extract VirtualConsoleAnsiAdapter.cs from master via `git show origin/master:BitPantry.VirtualConsole.Testing/VirtualConsoleAnsiAdapter.cs`
- [X] [P] [US3] Extract IKeyboardSimulator.cs from master via `git show origin/master:BitPantry.VirtualConsole.Testing/IKeyboardSimulator.cs`
- [X] T021 [US3] Create new BitPantry.VirtualConsole.Testing.csproj (without CommandLine dependencies)
- [X] T022 [US3] Add BitPantry.VirtualConsole.Testing to solution via `dotnet sln add BitPantry.VirtualConsole.Testing/BitPantry.VirtualConsole.Testing.csproj`
- [X] T023 [US3] Build to verify no compile errors
- [X] T024 [US3] Verify project does NOT depend on BitPantry.CommandLine

**Checkpoint**: VirtualConsole.Testing compiles. FluentAssertions and Spectre adapter work. No autocomplete coupling.

---

## Phase 6: User Story 4 - Copy VirtualConsole Documentation (Priority: P4)

**Goal**: Copy VirtualConsole documentation to Docs folder for developer reference

**Independent Test**: Docs/VirtualConsole folder contains README.md, getting-started.md, ansi-support.md

### Implementation for User Story 4

- [X] T025 [US4] Cherry-pick documentation from master via `git checkout origin/master -- Docs/VirtualConsole`
- [X] T026 [US4] Verify documentation files exist and are readable

**Checkpoint**: Documentation is accessible in Docs/VirtualConsole.

---

## Phase 7: Polish & Validation

**Purpose**: Final validation and cleanup

- [X] T027 Run full solution build
- [X] T028 Run all tests across solution
- [X] T029 Grep search for any remaining `VirtualAnsiConsole` references (should be zero)
- [X] T030 Commit with message referencing spec 011 origin for traceability
- [X] T031 Run quickstart.md validation to verify all steps documented correctly

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup
    ↓
Phase 3: User Story 1 (Cherry-Pick Core) 🎯 MVP
    ↓
Phase 4: User Story 3 (Create Testing Project)
    ↓
Phase 5: User Story 2 (Migrate Tests)
    ↓
Phase 6: User Story 4 (Documentation)  ← Can run in parallel with Phase 5
    ↓
Phase 7: Polish
```

### Execution Order Rationale

User Story priorities from spec vs actual execution order:

| Story | Spec Priority | Execution Order | Reason |
|-------|---------------|-----------------|--------|
| US1 | P1 | 1st | Foundation - core projects |
| US3 | P3 | 2nd | US2 migration requires adapter from US3 |
| US2 | P2 | 3rd | Migration uses VirtualConsoleAnsiAdapter |
| US4 | P4 | 4th (parallel) | Documentation, no dependencies |

### Within Each User Story

- Git operations before solution operations
- Add to solution before build verification
- Build verification before test verification

### Parallel Opportunities

**Within Phase 5 (US3)**:
```bash
# Extract all 3 source files in parallel
T018: VirtualConsoleAssertions.cs
T019: VirtualConsoleAnsiAdapter.cs  
T020: IKeyboardSimulator.cs
```

**Between Phases**:
```bash
# US4 can run in parallel with US2 after US3 completes
T025-T026 (US4 Documentation) || T009-T016 (US2 Migration)
```

---

## Parallel Example: User Story 3

```bash
# After T017 creates directory, extract all files together:
Task: T018 Extract VirtualConsoleAssertions.cs
Task: T019 Extract VirtualConsoleAnsiAdapter.cs
Task: T020 Extract IKeyboardSimulator.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 3: User Story 1 (Cherry-Pick)
3. **STOP and VALIDATE**: VirtualConsole tests pass (250 tests)
4. Core terminal emulator is available for use

### Full Implementation

1. Complete Setup → Phase 1 complete
2. Complete User Story 1 → VirtualConsole available (MVP!)
3. Complete User Story 3 → Testing utilities available
4. Complete User Story 2 → Migration complete, old code deleted
5. Complete User Story 4 → Documentation available
6. Complete Polish → Feature complete, committed

### Recommended Single-Developer Sequence

Execute tasks in this order:
```
T001 → T002 → T003 → T004 → T005 → T006 → T007 → T008
         ↓ (US1 checkpoint)
T017 → T018/T019/T020 (parallel) → T021 → T022 → T023 → T024
         ↓ (US3 checkpoint)
T009 → T010 → T011 → T012 → T013 → T014 → T015 → T016
         ↓ (US2 checkpoint)  
T025 → T026
         ↓ (US4 checkpoint)
T027 → T028 → T029 → T030 → T031
         ↓ (Feature complete)
```

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Execution order differs from priority order due to US3→US2 dependency
- All tests (250) are cherry-picked with VirtualConsole.Tests, not written new
- Commit after each checkpoint for incremental progress
- Stop at any checkpoint to validate story independently
