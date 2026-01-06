# Tasks: VirtualConsole Autocomplete Tests

**Input**: Design documents from `/specs/012-virtualconsole-autocomplete-tests/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅

**Tests**: This feature IS about tests. All tasks in Phases 3-9 are test implementation tasks.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Test Infrastructure**: `BitPantry.VirtualConsole.Testing/` (NEW PROJECT)
- **New Tests**: `BitPantry.CommandLine.Tests/AutoComplete/`
- **Legacy Removal**: `BitPantry.CommandLine.Tests/` (VirtualConsole/, Snapshots/, AutoComplete/ dirs)

---

## Phase 1: Legacy Removal (US8 - Priority: P1) 🎯 PREREQUISITE

**Goal**: Remove all existing autocomplete test infrastructure to create clean slate

**Independent Test**: Solution builds with no compilation errors; remaining tests pass

### Implementation

- [ ] T001 [US8] Delete entire `BitPantry.CommandLine.Tests/AutoComplete/` directory (66 files)
- [ ] T002 [US8] Delete entire `BitPantry.CommandLine.Tests/VirtualConsole/` directory (7 files)
- [ ] T003 [US8] Delete entire `BitPantry.CommandLine.Tests/Snapshots/` directory (10 files)
- [ ] T004 [US8] Remove `Verify.MSTest` package reference from `BitPantry.CommandLine.Tests/BitPantry.CommandLine.Tests.csproj`
- [ ] T005 [US8] Remove `Spectre.Console.Testing` package reference from `BitPantry.CommandLine.Tests/BitPantry.CommandLine.Tests.csproj`
- [ ] T006 [US8] Update any remaining files that reference deleted infrastructure (grep for ConsolidatedTestConsole, StepwiseTestRunner)
- [ ] T007 [US8] Build solution and verify no compilation errors
- [ ] T008 [US8] Run remaining tests and verify they pass

**Checkpoint**: Legacy infrastructure completely removed. Solution builds and remaining tests pass.

---

## Phase 2: VirtualConsole Testing Project (Foundational)

**Goal**: Create separate testing project with harness infrastructure

**⚠️ CRITICAL**: No test implementation can begin until this phase is complete

### Implementation

- [ ] T009 Create `BitPantry.VirtualConsole.Testing/BitPantry.VirtualConsole.Testing.csproj` with:
  - Reference to BitPantry.VirtualConsole project
  - PackageReference to FluentAssertions
  - PackageReference to Spectre.Console
  - TargetFramework net8.0
- [ ] T010 Add BitPantry.VirtualConsole.Testing.csproj to solution file
- [ ] T011 [P] Implement `BitPantry.VirtualConsole.Testing/IKeyboardSimulator.cs` interface with TypeText, PressKey, PressTab, PressEnter methods
- [ ] T012 [P] Implement `BitPantry.VirtualConsole.Testing/KeyboardSimulator.cs` implementing IKeyboardSimulator
- [ ] T013 Implement `BitPantry.VirtualConsole.Testing/VirtualConsoleAnsiAdapter.cs` that:
  - Implements Spectre.Console IAnsiConsole interface
  - Routes Write operations to VirtualConsole.Write()
  - Converts Spectre renderables to ANSI strings
- [ ] T014 Implement `BitPantry.VirtualConsole.Testing/AutoCompleteTestHarness.cs` that:
  - Creates VirtualConsole instance
  - Creates VirtualConsoleAnsiAdapter wrapping VirtualConsole
  - Configures AutoCompleteController with adapter
  - Exposes TypeText(), PressKey(), PressTab(), PressEnter()
  - Exposes VirtualConsole, IsMenuVisible, SelectedItem, Buffer properties
- [ ] T015 [P] Implement `BitPantry.VirtualConsole.Testing/VirtualConsoleAssertions.cs` FluentAssertions extensions:
  - Should().ContainText(string)
  - Should().HaveCellWithStyle(row, col, CellAttributes)
  - Should().HaveTextAt(row, col, string)
- [ ] T016 [P] Implement `BitPantry.VirtualConsole.Testing/HarnessAssertions.cs` FluentAssertions extensions:
  - Should().HaveMenuVisible() / HaveMenuHidden()
  - Should().HaveSelectedItem(string)
  - Should().HaveGhostText(string)
  - Should().HaveBuffer(string)
  - Should().HaveBufferPosition(int)
- [ ] T017 [P] Create `BitPantry.VirtualConsole.Testing/TestCommandBase.cs` base class for test commands
- [ ] T018 [P] Create `BitPantry.VirtualConsole.Testing/README.md` documentation
- [ ] T019 Add project reference from BitPantry.CommandLine.Tests to BitPantry.VirtualConsole.Testing
- [ ] T020 Build solution and verify VirtualConsole.Testing compiles

**Checkpoint**: Testing project created. AutoCompleteTestHarness can create test scenarios.

---

## Phase 3: User Story 1 - Ghost Text Tests (Priority: P1) 🎯 MVP

**Goal**: Test developer validates ghost text behavior (TC-1.1 through TC-1.16)

**Independent Test**: Run `GhostTextTests.cs` - all 16 tests pass validating ghost text hypothesis

### Implementation

- [ ] T021 [US1] Create `BitPantry.CommandLine.Tests/AutoComplete/GhostTextTests.cs` with test class structure
- [ ] T022 [US1] Implement TC_1_1_SingleCharacter_ShowsGhostCompletion test
- [ ] T023 [US1] Implement TC_1_2_PartialWord_ShowsRemainder test
- [ ] T024 [US1] Implement TC_1_3_ExactMatch_HidesGhost test
- [ ] T025 [US1] Implement TC_1_4_NoMatch_ShowsNoGhost test
- [ ] T026 [US1] Implement TC_1_5_SubcommandGhost_AfterCommandSpace test
- [ ] T027 [US1] Implement TC_1_6_DeepNested_SubcommandGhost test
- [ ] T028 [US1] Implement TC_1_7 through TC_1_10 ghost behavior tests
- [ ] T029 [US1] Implement TC_1_11 through TC_1_16 ghost acceptance/rejection tests
- [ ] T030 [US1] Stabilize: Run all 16 ghost text tests, document any code bugs found

**Checkpoint**: 16 ghost text tests pass. Ghost text hypothesis validated.

---

## Phase 4: User Story 2 - Menu Navigation Tests (Priority: P1)

**Goal**: Test developer validates menu display and navigation (TC-2.1 through TC-2.18)

**Independent Test**: Run `MenuNavigationTests.cs` - all 18 tests pass validating menu behavior hypothesis

### Implementation

- [ ] T031 [US2] Create `BitPantry.CommandLine.Tests/AutoComplete/MenuNavigationTests.cs` with test class structure
- [ ] T032 [US2] Implement TC_2_1_TabOpensMenu test
- [ ] T033 [US2] Implement TC_2_2_FirstItemHighlighted test
- [ ] T034 [US2] Implement TC_2_3_DownArrowNavigates test
- [ ] T035 [US2] Implement TC_2_4_UpArrowNavigates test
- [ ] T036 [US2] Implement TC_2_5_EnterAcceptsSelection test
- [ ] T037 [US2] Implement TC_2_6_EscapeClosesMenu test
- [ ] T038 [US2] Implement TC_2_7 through TC_2_12 navigation edge cases
- [ ] T039 [US2] Implement TC_2_13 through TC_2_18 menu rendering tests
- [ ] T040 [US2] Stabilize: Run all 18 menu navigation tests, document any code bugs found

**Checkpoint**: 34 cumulative tests pass (16 + 18). Menu navigation hypothesis validated.

---

## Phase 5: User Story 3 - Menu Filtering Tests (Priority: P1)

**Goal**: Test developer validates menu filtering behavior (TC-3.1 through TC-3.15)

**Independent Test**: Run `MenuFilteringTests.cs` - all 15 tests pass validating filter hypothesis

### Implementation

- [ ] T041 [US3] Create `BitPantry.CommandLine.Tests/AutoComplete/MenuFilteringTests.cs` with test class structure
- [ ] T042 [US3] Implement TC_3_1_TypeFiltersMenu test
- [ ] T043 [US3] Implement TC_3_2_MatchHighlighted test
- [ ] T044 [US3] Implement TC_3_3_BackspaceRelaxesFilter test
- [ ] T045 [US3] Implement TC_3_4_NoMatchesMessage test
- [ ] T046 [US3] Implement TC_3_5 through TC_3_10 filter behavior tests
- [ ] T047 [US3] Implement TC_3_11 through TC_3_15 filter edge cases
- [ ] T048 [US3] Stabilize: Run all 15 menu filtering tests, document any code bugs found

**Checkpoint**: 49 cumulative tests pass (34 + 15). Menu filtering hypothesis validated.

---

## Phase 6: User Story 1-3 Input Editing (Priority: P1 - completes core)

**Goal**: Complete P1 stories with input editing tests (TC-4.1 through TC-4.10)

**Independent Test**: Run `InputEditingTests.cs` - all 10 tests pass

### Implementation

- [ ] T049 [US1] Create `BitPantry.CommandLine.Tests/AutoComplete/InputEditingTests.cs` with test class structure
- [ ] T050 [US1] Implement TC_4_1 through TC_4_5 basic editing tests
- [ ] T051 [US1] Implement TC_4_6 through TC_4_10 cursor movement tests
- [ ] T052 [US1] Stabilize: Run all 10 input editing tests, document any code bugs found

**Checkpoint**: 59 cumulative tests pass. **All P1 foundation tests complete.**

---

## Phase 7: User Story 4 - Argument Completion Tests (Priority: P2)

**Goal**: Test developer validates argument name and value completion (TC-5, TC-6, TC-7)

**Independent Test**: Run Argument*Tests.cs files - all 30 tests pass

### Implementation

- [ ] T053 [US4] Create `BitPantry.CommandLine.Tests/AutoComplete/CommandCompletionTests.cs` (TC-5.1 through TC-5.4)
- [ ] T054 [US4] Implement TC_5_1 through TC_5_4 command/group completion tests
- [ ] T055 [US4] Create `BitPantry.CommandLine.Tests/AutoComplete/ArgumentNameTests.cs` (TC-6.1 through TC-6.10)
- [ ] T056 [US4] Implement TC_6_1 through TC_6_10 argument name/alias tests
- [ ] T057 [US4] Create `BitPantry.CommandLine.Tests/AutoComplete/ArgumentValueTests.cs` (TC-7.1 through TC-7.10)
- [ ] T058 [US4] Implement TC_7_1 through TC_7_10 argument value tests (enum, static values, etc.)
- [ ] T059 [US4] Stabilize: Run all 24 argument tests, document any code bugs found

**Checkpoint**: 83 cumulative tests pass (59 + 24). Argument completion hypothesis validated.

---

## Phase 8: User Story 5 - Positional Completion Tests (Priority: P2)

**Goal**: Test developer validates positional argument completion (TC-8.1 through TC-8.11)

**Independent Test**: Run `PositionalTests.cs` - all 11 tests pass

### Implementation

- [ ] T060 [US5] Create `BitPantry.CommandLine.Tests/AutoComplete/PositionalTests.cs` with test class structure
- [ ] T061 [US5] Implement TC_8_1 through TC_8_5 positional slot tests
- [ ] T062 [US5] Implement TC_8_6 through TC_8_11 IsRest and multi-positional tests
- [ ] T063 [US5] Stabilize: Run all 11 positional tests, document any code bugs found

**Checkpoint**: 94 cumulative tests pass (83 + 11). Positional completion hypothesis validated.

---

## Phase 9: User Story 6 - File Path Completion Tests (Priority: P2)

**Goal**: Test developer validates file path completion with mock filesystem (TC-9.1 through TC-9.12)

**Independent Test**: Run `FilePathTests.cs` - all 12 tests pass using MockFileSystem

### Implementation

- [ ] T064 [US6] Create `BitPantry.CommandLine.Tests/AutoComplete/FilePathTests.cs` with MockFileSystem setup
- [ ] T065 [US6] Implement TC_9_1 through TC_9_4 basic file completion tests
- [ ] T066 [US6] Implement TC_9_5 through TC_9_8 directory navigation tests
- [ ] T067 [US6] Implement TC_9_9 through TC_9_12 quoting and edge case tests
- [ ] T068 [US6] Stabilize: Run all 12 file path tests, document any code bugs found

**Checkpoint**: 106 cumulative tests pass (94 + 12). File path completion hypothesis validated.

---

## Phase 10: User Story 7 - Edge Cases & Remaining Tests (Priority: P3)

**Goal**: Complete remaining test coverage (TC-10 through TC-35)

**Independent Test**: All 283 tests pass

### Viewport & Interaction Tests

- [ ] T069 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ViewportScrollingTests.cs` (TC-10.1 through TC-10.5)
- [ ] T070 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/GhostMenuInteractionTests.cs` (TC-11.1 through TC-11.3)
- [ ] T071 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/WorkflowTests.cs` (TC-12.1 through TC-12.4)
- [ ] T072 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/HistoryNavigationTests.cs` (TC-13.1 through TC-13.4)

### Edge Case & Visual Tests

- [ ] T073 [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/EdgeCaseTests.cs` (TC-14.1 through TC-14.27)
- [ ] T074 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/VisualRenderingTests.cs` (TC-15.1 through TC-15.5)
- [ ] T075 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/SubmissionTests.cs` (TC-16.1 through TC-16.3)

### Provider & Caching Tests

- [ ] T076 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/RemoteCompletionTests.cs` (TC-17.x using TestEnvironment)
- [ ] T077 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/CachingTests.cs` (TC-18.1 through TC-18.7)
- [ ] T078 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ProviderConfigTests.cs` (TC-19.1 through TC-19.20)
- [ ] T079 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/MatchRankingTests.cs` (TC-20.1 through TC-20.5)
- [ ] T080 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ResultLimitingTests.cs` (TC-21.1 through TC-21.5)

### Environment & Async Tests

- [ ] T081 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/TerminalEdgeCaseTests.cs` (TC-22.1 through TC-22.6)
- [ ] T082 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/KeyboardVariationTests.cs` (TC-23.1 through TC-23.8)
- [ ] T083 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ContextSensitivityTests.cs` (TC-24.1 through TC-24.6)
- [ ] T084 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/AsyncBehaviorTests.cs` (TC-25.1 through TC-25.5)
- [ ] T085 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/QuotingEscapingTests.cs` (TC-26.1 through TC-26.8)

### Final Tests

- [ ] T086 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/StatePersistenceTests.cs` (TC-30.1 through TC-30.5)
- [ ] T087 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ProviderInteractionTests.cs` (TC-31.1 through TC-31.5)
- [ ] T088 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/VirtualConsoleIntegrationTests.cs` (TC-32.1 through TC-32.6)
- [ ] T089 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ConfigurationTests.cs` (TC-33.1 through TC-33.5)
- [ ] T090 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ErrorFeedbackTests.cs` (TC-34.1 through TC-34.5)
- [ ] T091 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/BoundaryValueTests.cs` (TC-35.1 through TC-35.6)

### Stabilization

- [ ] T092 [US7] Run full test suite, count total tests, document bugs found
- [ ] T093 [US7] Verify test count matches 283 documented test cases

**Checkpoint**: All 283 tests implemented and passing.

---

## Phase 11: Documentation Update (Polish)

**Goal**: Update CLAUDE.md and testing documentation to reflect new approach

### Implementation

- [ ] T094 [P] Update CLAUDE.md testing section to remove StepwiseTestRunner, ConsolidatedTestConsole, Verify.MSTest references
- [ ] T095 [P] Update CLAUDE.md testing section to document VirtualConsole testing approach
- [ ] T096 [P] Add examples of writing VirtualConsole-based tests to CLAUDE.md
- [ ] T097 Review and update any other documentation referencing old test infrastructure
- [ ] T098 Final validation: All tests pass, documentation complete, no legacy references remain

**Checkpoint**: Documentation complete. Feature ready for merge.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Legacy Removal) → BLOCKS ALL
        ↓
Phase 2 (Testing Infrastructure) → BLOCKS ALL TEST PHASES
        ↓
Phases 3-6 (P1 Foundation) → Execute sequentially (US1 → US2 → US3)
        ↓
Phases 7-9 (P2 Features) → Can execute in parallel after Phase 6
        ↓
Phase 10 (P3 Edge Cases) → Can start after Phase 6, many tasks [P]
        ↓
Phase 11 (Documentation) → After Phase 10
```

### User Story Dependencies

| User Story | Phase | Can Start After | Parallel With |
|------------|-------|-----------------|---------------|
| US8 (Legacy Removal) | 1 | Immediately | None |
| US1 (Ghost Text) | 3 | Phase 2 | None |
| US2 (Menu Navigation) | 4 | Phase 3 | None |
| US3 (Menu Filtering) | 5 | Phase 4 | None |
| US4 (Arguments) | 7 | Phase 6 | US5, US6 |
| US5 (Positional) | 8 | Phase 6 | US4, US6 |
| US6 (File Path) | 9 | Phase 6 | US4, US5 |
| US7 (Edge Cases) | 10 | Phase 6 | US4, US5, US6 |

### Parallel Opportunities

**Phase 2 (Infrastructure):**
- T010, T011, T014, T015, T016, T017 can run in parallel (different files)

**Phase 10 (Edge Cases):**
- T068-T090 (22 tasks marked [P]) can all run in parallel

---

## Parallel Example: Phase 10

```bash
# Launch all edge case test files together:
Task: "Create ViewportScrollingTests.cs"
Task: "Create GhostMenuInteractionTests.cs"
Task: "Create WorkflowTests.cs"
Task: "Create HistoryNavigationTests.cs"
Task: "Create VisualRenderingTests.cs"
Task: "Create SubmissionTests.cs"
# ... (all marked [P] can run simultaneously)
```

---

## Implementation Strategy

### MVP First (Phases 1-6)

1. **Phase 1**: Remove legacy (US8) - Clean slate
2. **Phase 2**: Build infrastructure - Testing harness ready
3. **Phases 3-6**: Core P1 tests - 59 tests covering ghost, menu, filter, input
4. **STOP and VALIDATE**: Core autocomplete behavior validated
5. Document bugs found (tests validate hypothesis, not code)

### Incremental Delivery

| Milestone | Tests | What's Validated |
|-----------|-------|------------------|
| Phase 6 complete | 59 | Core autocomplete UX |
| Phase 9 complete | 106 | Arguments, positionals, file paths |
| Phase 10 complete | 283 | Full coverage |
| Phase 11 complete | 283 | Documentation updated |

### Bugs Expected

Per spec SC-008: Tests should find **at least 5 bugs** in existing autocomplete code. Tests validate the documented hypothesis, not the current implementation. Failed tests indicate code bugs, not test bugs.

---

## Notes

- [P] tasks = different files, no dependencies
- Each test file implements tests from autocomplete-test-cases.md
- Test method naming: `TC_X_Y_ShortDescription` pattern
- Stabilization steps document bugs found for later fixing
- Tests use VirtualConsole assertions, not Spectre.Console.Testing
- FluentAssertions already in project (reuse existing patterns)
- MockFileSystem already in project (reuse for TC-9.x)
