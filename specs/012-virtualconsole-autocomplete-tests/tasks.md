# Tasks: VirtualConsole Autocomplete Tests

**Input**: Design documents from `/specs/012-virtualconsole-autocomplete-tests/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅

**Tests**: This feature IS about tests. All tasks in Phases 3-9 are test implementation tasks.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

---

## ⚠️ CRITICAL TESTING PRINCIPLES

These principles MUST be followed when implementing tests:

1. **Implement tests per the spec and test case scenario** - The test case document (autocomplete-test-cases.md) defines the expected behavior. Tests validate those hypotheses.

2. **Never weaken assertions to make tests pass** - If a test fails, assume you've found a software bug (or testing infrastructure bug). Investigate and fix the bug, don't soften the assertion.

3. **Hypothesis-driven testing** - Each test validates a specific hypothesis from the spec. The assertion must match the hypothesis strength:
   - "Menu opens with items" → Assert menu is visible AND has items
   - "Exact match prioritized" → Assert the specific item at index 0 is the exact match

4. **Failed tests are opportunities** - A failing test means either:
   - Production code has a bug → Fix it
   - Test infrastructure has a bug → Fix it
   - Spec was wrong → Document and update spec
   - NEVER: Just make the test pass by weakening it

---

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

- [X] T001 [US8] Delete entire `BitPantry.CommandLine.Tests/AutoComplete/` directory (66 files)
- [X] T002 [US8] Delete entire `BitPantry.CommandLine.Tests/VirtualConsole/` directory (7 files)
- [X] T003 [US8] Delete entire `BitPantry.CommandLine.Tests/Snapshots/` directory (10 files)
- [X] T004 [US8] Remove `Verify.MSTest` package reference from `BitPantry.CommandLine.Tests/BitPantry.CommandLine.Tests.csproj`
- [X] T005 [US8] Remove `Spectre.Console.Testing` package reference from `BitPantry.CommandLine.Tests/BitPantry.CommandLine.Tests.csproj`
- [X] T006 [US8] Update any remaining files that reference deleted infrastructure (grep for ConsolidatedTestConsole, StepwiseTestRunner)
- [X] T007 [US8] Build solution and verify no compilation errors
- [X] T008 [US8] Run remaining tests and verify they pass

**Checkpoint**: Legacy infrastructure completely removed. Solution builds and remaining tests pass.

---

## Phase 2: VirtualConsole Testing Project (Foundational)

**Goal**: Create separate testing project with harness infrastructure

**⚠️ CRITICAL**: No test implementation can begin until this phase is complete

### Implementation

- [X] T009 Create `BitPantry.VirtualConsole.Testing/BitPantry.VirtualConsole.Testing.csproj` with:
  - Reference to BitPantry.VirtualConsole project
  - PackageReference to FluentAssertions
  - PackageReference to Spectre.Console
  - TargetFramework net8.0
- [X] T010 Add BitPantry.VirtualConsole.Testing.csproj to solution file
- [X] T011 [P] Implement `BitPantry.VirtualConsole.Testing/IKeyboardSimulator.cs` interface with TypeText, PressKey, PressTab, PressEnter methods
- [X] T012 [P] Implement `BitPantry.VirtualConsole.Testing/KeyboardSimulator.cs` implementing IKeyboardSimulator
- [X] T013 Implement `BitPantry.VirtualConsole.Testing/VirtualConsoleAnsiAdapter.cs` that:
  - Implements Spectre.Console IAnsiConsole interface
  - Routes Write operations to VirtualConsole.Write()
  - Converts Spectre renderables to ANSI strings
- [X] T014 Implement `BitPantry.VirtualConsole.Testing/AutoCompleteTestHarness.cs` that:
  - Creates VirtualConsole instance
  - Creates VirtualConsoleAnsiAdapter wrapping VirtualConsole
  - Configures AutoCompleteController with adapter
  - Exposes TypeText(), PressKey(), PressTab(), PressEnter()
  - Exposes VirtualConsole, IsMenuVisible, SelectedItem, Buffer properties
- [X] T015 [P] Implement `BitPantry.VirtualConsole.Testing/VirtualConsoleAssertions.cs` FluentAssertions extensions:
  - Should().ContainText(string)
  - Should().HaveCellWithStyle(row, col, CellAttributes)
  - Should().HaveTextAt(row, col, string)
- [X] T016 [P] Implement `BitPantry.VirtualConsole.Testing/HarnessAssertions.cs` FluentAssertions extensions:
  - Should().HaveMenuVisible() / HaveMenuHidden()
  - Should().HaveSelectedItem(string)
  - Should().HaveGhostText(string)
  - Should().HaveBuffer(string)
  - Should().HaveBufferPosition(int)
- [X] T017 [P] Create `BitPantry.VirtualConsole.Testing/TestCommandBase.cs` base class for test commands
- [X] T018 [P] Create `BitPantry.VirtualConsole.Testing/README.md` documentation
- [X] T019 Add project reference from BitPantry.CommandLine.Tests to BitPantry.VirtualConsole.Testing
- [X] T020 Build solution and verify VirtualConsole.Testing compiles

**Checkpoint**: Testing project created. AutoCompleteTestHarness can create test scenarios.

---

## Phase 3: User Story 1 - Ghost Text Tests (Priority: P1) 🎯 MVP

**Goal**: Test developer validates ghost text behavior (TC-1.1 through TC-1.16)

**Independent Test**: Run `GhostTextTests.cs` - all 16 tests pass validating ghost text hypothesis

### Implementation

- [X] T021 [US1] Create `BitPantry.CommandLine.Tests/AutoComplete/GhostTextTests.cs` with test class structure
- [X] T022 [US1] Implement TC_1_1_SingleCharacter_ShowsGhostCompletion test
- [X] T023 [US1] Implement TC_1_2_PartialWord_ShowsRemainder test
- [X] T024 [US1] Implement TC_1_3_ExactMatch_HidesGhost test
- [X] T025 [US1] Implement TC_1_4_NoMatch_ShowsNoGhost test
- [X] T026 [US1] Implement TC_1_5_SubcommandGhost_AfterCommandSpace test
- [X] T027 [US1] Implement TC_1_6_DeepNested_SubcommandGhost test
- [X] T028 [US1] Implement TC_1_7 through TC_1_10 ghost behavior tests
- [X] T029 [US1] Implement TC_1_11 through TC_1_16 ghost acceptance/rejection tests
- [X] T030 [US1] Stabilize: Run all 16 ghost text tests, document any code bugs found

**Checkpoint**: 14/16 ghost text tests pass. 2 tests skipped (require history integration). Ghost text hypothesis validated.

---

## Phase 4: User Story 2 - Menu Navigation Tests (Priority: P1)

**Goal**: Test developer validates menu display and navigation (TC-2.1 through TC-2.18)

**Independent Test**: Run `MenuNavigationTests.cs` - all 18 tests pass validating menu behavior hypothesis

### Implementation

- [X] T031 [US2] Create `BitPantry.CommandLine.Tests/AutoComplete/MenuNavigationTests.cs` with test class structure
- [X] T032 [US2] Implement TC_2_1_TabOpensMenu test
- [X] T033 [US2] Implement TC_2_2_FirstItemHighlighted test
- [X] T034 [US2] Implement TC_2_3_DownArrowNavigates test
- [X] T035 [US2] Implement TC_2_4_UpArrowNavigates test
- [X] T036 [US2] Implement TC_2_5_EnterAcceptsSelection test
- [X] T037 [US2] Implement TC_2_6_EscapeClosesMenu test
- [X] T038 [US2] Implement TC_2_7 through TC_2_12 navigation edge cases
- [X] T039 [US2] Implement TC_2_13 through TC_2_18 menu rendering tests
- [X] T040 [US2] Stabilize: Run all 18 menu navigation tests, document any code bugs found

**Checkpoint**: 34 cumulative tests pass (16 + 18). Menu navigation hypothesis validated. ✅ COMPLETE

---

## Phase 5: User Story 3 - Menu Filtering Tests (Priority: P1)

**Goal**: Test developer validates menu filtering behavior (TC-3.1 through TC-3.15)

**Independent Test**: Run `MenuFilteringTests.cs` - all 15 tests pass validating filter hypothesis

### Implementation

- [X] T041 [US3] Create `BitPantry.CommandLine.Tests/AutoComplete/MenuFilteringTests.cs` with test class structure
- [X] T042 [US3] Implement TC_3_1_TypeFiltersMenu test
- [X] T043 [US3] Implement TC_3_2_MatchHighlighted test
- [X] T044 [US3] Implement TC_3_3_BackspaceRelaxesFilter test
- [X] T045 [US3] Implement TC_3_4_NoMatchesMessage test
- [X] T046 [US3] Implement TC_3_5 through TC_3_10 filter behavior tests
- [X] T047 [US3] Implement TC_3_11 through TC_3_15 filter edge cases
- [X] T048 [US3] Stabilize: Run all 15 menu filtering tests, document any code bugs found

**Checkpoint**: 49 cumulative tests pass (34 + 15). Menu filtering hypothesis validated. ✅ COMPLETE

---

## Phase 6: User Story 1-3 Input Editing (Priority: P1 - completes core)

**Goal**: Complete P1 stories with input editing tests (TC-4.1 through TC-4.10)

**Independent Test**: Run `InputEditingTests.cs` - all 10 tests pass

### Implementation

- [X] T049 [US1] Create `BitPantry.CommandLine.Tests/AutoComplete/InputEditingTests.cs` with test class structure
- [X] T050 [US1] Implement TC_4_1 through TC_4_5 basic editing tests
- [X] T051 [US1] Implement TC_4_6 through TC_4_10 cursor movement tests
- [X] T052 [US1] Stabilize: Run all 10 input editing tests, document any code bugs found

**Checkpoint**: 59 cumulative tests pass. **All P1 foundation tests complete.** ✅ COMPLETE

---

## Phase 7: User Story 4 - Argument Completion Tests (Priority: P2)

**Goal**: Test developer validates argument name and value completion (TC-5, TC-6, TC-7)

**Independent Test**: Run Argument*Tests.cs files - all 30 tests pass

### Implementation

- [X] T053 [US4] Create `BitPantry.CommandLine.Tests/AutoComplete/CommandCompletionTests.cs` (TC-5.1 through TC-5.4)
- [X] T054 [US4] Implement TC_5_1 through TC_5_4 command/group completion tests
- [X] T055 [US4] Create `BitPantry.CommandLine.Tests/AutoComplete/ArgumentNameTests.cs` (TC-6.1 through TC-6.10)
- [X] T056 [US4] Implement TC_6_1 through TC_6_10 argument name/alias tests
- [X] T057 [US4] Create `BitPantry.CommandLine.Tests/AutoComplete/ArgumentValueTests.cs` (TC-7.1 through TC-7.10)
- [X] T058 [US4] Implement TC_7_1 through TC_7_10 argument value tests (enum, static values, etc.)
- [X] T059 [US4] Stabilize: Run all 24 argument tests, document any code bugs found

**Checkpoint**: 83 cumulative tests pass (59 + 24). Argument completion hypothesis validated. ✅ COMPLETE

---

## Phase 8: User Story 5 - Positional Completion Tests (Priority: P2)

**Goal**: Test developer validates positional argument completion (TC-8.1 through TC-8.11)

**Independent Test**: Run `PositionalTests.cs` - all 11 tests pass

### Implementation

- [X] T060 [US5] Create `BitPantry.CommandLine.Tests/AutoComplete/PositionalTests.cs` with test class structure
- [X] T061 [US5] Implement TC_8_1 through TC_8_5 positional slot tests
- [X] T062 [US5] Implement TC_8_6 through TC_8_11 IsRest and multi-positional tests
- [X] T063 [US5] Stabilize: Run all 11 positional tests, document any code bugs found

**Checkpoint**: 94 cumulative tests pass (83 + 11). Positional completion hypothesis validated. ✅ COMPLETE

---

## Phase 9: User Story 6 - File Path Completion Tests (Priority: P2)

**Goal**: Test developer validates file path completion with mock filesystem (TC-9.1 through TC-9.12)

**Independent Test**: Run `FilePathTests.cs` - all 12 tests pass using MockFileSystem

### Implementation

- [X] T064 [US6] Create `BitPantry.CommandLine.Tests/AutoComplete/FilePathTests.cs` with MockFileSystem setup
- [X] T065 [US6] Implement TC_9_1 through TC_9_4 basic file completion tests
- [X] T066 [US6] Implement TC_9_5 through TC_9_8 directory navigation tests
- [X] T067 [US6] Implement TC_9_9 through TC_9_12 quoting and edge case tests
- [X] T068 [US6] Stabilize: Run all 12 file path tests, document any code bugs found

**Checkpoint**: 106 cumulative tests pass (94 + 12). File path completion hypothesis validated. ✅ COMPLETE

---

## Phase 9.5: Assertion Tightening (CRITICAL - Bug Discovery) 🔴

**Goal**: Review all tests written so far and remove weak/conditional assertions that were added to make failing tests pass. Fix resulting bugs.

**Rationale**: Tests were initially written with soft assertions (e.g., `if (IsMenuVisible) { ... }`) to avoid failures. This violates testing principles - failed tests should expose bugs, not be weakened.

### Implementation

- [ ] T068a Review and tighten ViewportScrollingTests.cs - remove conditional guards, assert per spec
- [ ] T068b Review and tighten GhostMenuInteractionTests.cs - remove conditional guards, assert per spec  
- [ ] T068c Review and tighten WorkflowTests.cs - remove conditional guards, assert per spec
- [ ] T068d Review and tighten HistoryNavigationTests.cs - remove conditional guards, assert per spec
- [ ] T068e Review and tighten EdgeCaseTests.cs - remove conditional guards, assert per spec
- [ ] T068f Review and tighten VisualRenderingTests.cs - remove conditional guards, assert per spec
- [ ] T068g Review and tighten SubmissionTests.cs - remove conditional guards, assert per spec
- [ ] T068h Review and tighten MatchRankingTests.cs - remove conditional guards, assert per spec
- [ ] T068i Review and tighten QuotingEscapingTests.cs - remove conditional guards, assert per spec
- [ ] T068j Review and tighten BoundaryValueTests.cs - remove conditional guards, assert per spec
- [ ] T068k Review and tighten AsyncBehaviorTests.cs - remove conditional guards, assert per spec
- [ ] T068l Run all tests, investigate failures, fix production code bugs (or test infrastructure bugs)
- [ ] T068m Document any bugs found and fixed

**Checkpoint**: All assertions match spec hypotheses. Any discovered bugs are fixed.

---

## Phase 10: User Story 7 - Edge Cases & Remaining Tests (Priority: P3)

**Goal**: Complete remaining test coverage (TC-10 through TC-35)

**Independent Test**: All 283 tests pass

### Viewport & Interaction Tests

- [X] T069 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ViewportScrollingTests.cs` (TC-10.1 through TC-10.5)
- [X] T070 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/GhostMenuInteractionTests.cs` (TC-11.1 through TC-11.3)
- [ ] T071 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/WorkflowTests.cs` (TC-12.1 through TC-12.4)
- [ ] T072 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/HistoryNavigationTests.cs` (TC-13.1 through TC-13.4)

### Edge Case & Visual Tests

- [X] T073 [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/EdgeCaseTests.cs` (TC-14.1 through TC-14.27)
- [X] T074 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/VisualRenderingTests.cs` (TC-15.1 through TC-15.5)
- [X] T075 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/SubmissionTests.cs` (TC-16.1 through TC-16.3)

### Provider & Caching Tests

- [ ] T076 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/RemoteCompletionTests.cs` (TC-17.x using TestEnvironment)
- [ ] T077 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/CachingTests.cs` (TC-18.1 through TC-18.7)
- [ ] T078 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ProviderConfigTests.cs` (TC-19.1 through TC-19.20)
- [X] T079 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/MatchRankingTests.cs` (TC-20.1 through TC-20.5)
- [ ] T080 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ResultLimitingTests.cs` (TC-21.1 through TC-21.5)

### Environment & Async Tests

- [ ] T081 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/TerminalEdgeCaseTests.cs` (TC-22.1 through TC-22.6)
- [ ] T082 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/KeyboardVariationTests.cs` (TC-23.1 through TC-23.8)
- [ ] T083 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ContextSensitivityTests.cs` (TC-24.1 through TC-24.6)
- [X] T084 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/AsyncBehaviorTests.cs` (TC-25.1 through TC-25.5)
- [X] T085 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/QuotingEscapingTests.cs` (TC-26.1 through TC-26.8)

### Final Tests

- [ ] T086 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/StatePersistenceTests.cs` (TC-30.1 through TC-30.5)
- [ ] T087 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ProviderInteractionTests.cs` (TC-31.1 through TC-31.5)
- [ ] T088 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/VirtualConsoleIntegrationTests.cs` (TC-32.1 through TC-32.6)
- [ ] T089 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ConfigurationTests.cs` (TC-33.1 through TC-33.5)
- [ ] T090 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/ErrorFeedbackTests.cs` (TC-34.1 through TC-34.5)
- [X] T091 [P] [US7] Create `BitPantry.CommandLine.Tests/AutoComplete/BoundaryValueTests.cs` (TC-35.1 through TC-35.6)

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
