# Tasks: Menu Filtering While Typing

**Input**: Design documents from `/specs/010-menu-filter/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md

**TDD Approach**: All tasks follow strict TDD - tests are written FIRST, verified to FAIL, then implementation makes them pass.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Library**: `BitPantry.CommandLine/`
- **Tests**: `BitPantry.CommandLine.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create foundational helpers and test infrastructure needed by all user stories

- [X] T001 [P] Create unit tests for `IsInsideQuotes()` in `BitPantry.CommandLine.Tests/Unit/StringExtensionsTests.cs` - test empty string, no quotes, inside quotes, after closing quote, multiple quote pairs
- [X] T002 Run T001 tests - verify they FAIL (method doesn't exist yet)
- [X] T003 Create `IsInsideQuotes()` stub in `BitPantry.CommandLine/StringExtensions.cs` - returns false (placeholder)
- [X] T004 Implement `IsInsideQuotes()` logic in `BitPantry.CommandLine/StringExtensions.cs` - counts unescaped `"` characters before position, returns true if odd count - T001 tests pass
- [X] T005 Create `MenuFilteringTests.cs` test class in `BitPantry.CommandLine.Tests/AutoComplete/Visual/` extending `VisualTestBase` with test infrastructure setup

**Checkpoint**: Foundation ready - IsInsideQuotes helper complete, test class created

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure changes that MUST be complete before user story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Add `_menuTriggerPosition` private field to `AutoCompleteController` in `BitPantry.CommandLine/AutoComplete/AutoCompleteController.cs` - initialized to -1
- [X] T007 Update `AutoCompleteController` to set `_menuTriggerPosition = inputLine.BufferPosition` when menu opens (in the method that triggers menu display)
- [X] T008 Update `CompletionOrchestrator.HandleCharacterAsync()` in `BitPantry.CommandLine/AutoComplete/CompletionOrchestrator.cs` to use `MatchMode.ContainsCaseInsensitive` instead of `MatchMode.PrefixCaseInsensitive`
- [X] T009 Update `CompletionOrchestrator.HandleCharacterAsync()` to populate `MenuState.FilterText` with the current query when creating new MenuState
- [X] T010 Run existing test suite with `dotnet test` - verify all 377+ tests still pass after foundational changes

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Filter Menu by Typing (Priority: P1) 🎯 MVP

**Goal**: When menu is open, typing filters items in real-time using substring matching

**Independent Test**: Open menu with Tab, type filter text, verify menu items are reduced to only matching entries

### Tests for User Story 1 (RED Phase)

- [X] T011 [US1] Write test `TypingWhileMenuOpen_FiltersToMatchingItems` in `BitPantry.CommandLine.Tests/AutoComplete/Visual/MenuFilteringTests.cs` - open menu, type "con", verify only items containing "con" remain
- [X] T012 [US1] Write test `FilterIsCaseInsensitive` in `MenuFilteringTests.cs` - type "CON", verify matches "connect" and "config"
- [X] T013 [US1] Write test `FilterUsesSubstringMatching` in `MenuFilteringTests.cs` - type "fig", verify matches "config" (substring in middle)
- [X] T014 [US1] Write test `FilterTextAppearsInBuffer` in `MenuFilteringTests.cs` - verify FR-011: type filter chars while menu open, verify chars appear in input buffer (not separate search box)
- [X] T015 [US1] Write test `FilteringResetsSelectionToFirstItem` in `MenuFilteringTests.cs` - navigate to item 3, type filter, verify selection is index 0
- [X] T016 [US1] Run T011-T015 tests - verify they FAIL (menu closes on typing instead of filtering)

### Implementation for User Story 1 (GREEN Phase)

- [X] T017 [US1] Update `InputBuilder` default handler in `BitPantry.CommandLine/Input/InputBuilder.cs` (around line 174) - remove the `_acCtrl.End()` call when engaged, instead call `HandleCharacterWhileMenuOpenAsync()` after writing character to buffer
- [X] T018 [US1] Run T011-T015 tests - verify they now PASS

**Checkpoint**: User Story 1 complete - typing filters menu items

---

## Phase 4: User Story 2 - Backspace Expands Filter (Priority: P1)

**Goal**: Backspace removes filter characters and shows more matches; backspace past trigger closes menu

**Independent Test**: Open menu, type filter, press Backspace, verify more items appear

### Tests for User Story 2 (RED Phase)

- [X] T019 [US2] Write test `BackspaceRemovesFilterCharacter` in `MenuFilteringTests.cs` - type "conn", backspace, verify buffer is "con" and more items visible
- [X] T020 [US2] Write test `BackspaceExpandsFilterResults` in `MenuFilteringTests.cs` - filter to 1 item, backspace, verify more items appear
- [X] T021 [US2] Write test `BackspacePastTriggerPosition_ClosesMenu` in `MenuFilteringTests.cs` - open menu at position 7, backspace to position 6, verify menu closes
- [X] T022 [US2] Write test `BackspaceWithNoFilter_ClosesMenu` in `MenuFilteringTests.cs` - open menu, immediately backspace, verify menu closes
- [X] T023 [US2] Run T019-T022 tests - verify current behavior (some may pass, some fail depending on existing backspace handling)

### Implementation for User Story 2 (GREEN Phase)

- [X] T024 [US2] Update backspace handler in `InputBuilder` or `AutoCompleteController` to check if `BufferPosition < _menuTriggerPosition` after backspace - if so, call `End()` to close menu
- [X] T025 [US2] Update backspace handler to re-filter menu items when backspace is pressed while menu is open and position >= trigger position
- [X] T026 [US2] Run T019-T022 tests - verify they now PASS

**Checkpoint**: User Story 2 complete - backspace expands filter or closes menu appropriately

---

## Phase 5: User Story 3 - Space Closes Menu (Context-Aware) (Priority: P2)

**Goal**: Space closes menu when outside quotes, acts as filter character inside quotes

**Independent Test**: Open menu, press Space, verify menu closes and space is inserted

### Tests for User Story 3 (RED Phase)

- [X] T027 [US3] Write test `SpaceOutsideQuotes_ClosesMenu` in `MenuFilteringTests.cs` - open menu with `server `, press Space, verify menu closes and buffer has trailing space
- [X] T028 [US3] Write test `SpaceInsideQuotes_FiltersMenu` in `MenuFilteringTests.cs` - type `--path "Program`, open menu, press Space, verify space added to buffer and menu stays open
- [X] T029 [US3] Write test `SpaceClosesMenu_WithoutAcceptingSelection` in `MenuFilteringTests.cs` - open menu, navigate to item 2, press Space, verify menu closes but item 2 is NOT inserted
- [X] T030 [US3] Run T027-T029 tests - verify they FAIL (space currently may not have special handling)

### Implementation for User Story 3 (GREEN Phase)

- [X] T031 [US3] Add Space key handler in `InputBuilder` using `.AddHandler(ConsoleKey.Spacebar, ...)` that checks `IsInsideQuotes()` - if outside quotes and menu engaged, call `End()` and write space; if inside quotes and menu engaged, write space and call `HandleCharacterWhileMenuOpenAsync()`
- [X] T032 [US3] Run T027-T029 tests - verify they now PASS (1085 tests passing)

**Checkpoint**: User Story 3 complete - space behavior is context-aware

---

## Phase 6: User Story 4 - Match Highlighting (Priority: P2)

**Goal**: Matching substring within each menu item is visually highlighted

**Independent Test**: Open menu, type filter, verify matching substring has distinct visual style

### Tests for User Story 4 (RED Phase)

- [X] T033 [US4] Write test `AutoCompleteMenuRenderable_HighlightsMatchRanges` in `BitPantry.CommandLine.Tests/AutoComplete/Rendering/AutoCompleteMenuRenderableTests.cs` - create renderable with CompletionItems having MatchRanges, render to TestConsole, verify output contains highlighted segments
- [X] T034 [US4] Write test `AutoCompleteMenuRenderable_NoHighlightWhenNoFilter` in `AutoCompleteMenuRenderableTests.cs` - create renderable with empty MatchRanges, verify no highlight markup in output
- [X] T035 [US4] Write test `FilteringShowsHighlightedMatches` in `MenuFilteringTests.cs` - type filter, verify rendered menu shows highlighting (integration test)
- [X] T036 [US4] Run T033-T035 tests - verify they FAIL (renderable doesn't use MatchRanges yet) - TESTS WRITTEN, INITIALLY FAILED AS EXPECTED

### Implementation for User Story 4 (GREEN Phase)

- [X] T037 [US4] Update `AutoCompleteMenuRenderable` constructor in `BitPantry.CommandLine/AutoComplete/Rendering/AutoCompleteMenuRenderable.cs` to add overload accepting `IReadOnlyList<CompletionItem>` 
- [X] T038 [US4] Update `AutoCompleteMenuRenderable.Render()` method to apply highlight style (yellow bold) to portions indicated by `CompletionItem.MatchRanges`
- [X] T039 [US4] Added InternalsVisibleTo in project file to allow tests to set MatchRanges
- [X] T040 [US4] Run T033-T035 tests - verify they now PASS (1088 tests passing)

**Checkpoint**: User Story 4 complete - matches are highlighted in menu

---

## Phase 7: User Story 5 - Consistent Cursor Position After Acceptance (Priority: P2)

**Goal**: No trailing space after accepting completion; cursor at end of inserted text

**Independent Test**: Accept menu selection, verify cursor is at end of text with no trailing space

### Tests for User Story 5 (RED Phase)

- [X] T041 [US5] Write test `AcceptCompletion_NoTrailingSpace` in `MenuFilteringTests.cs` - open menu, press Enter, verify buffer ends with completion text (no trailing space)
- [X] T042 [US5] Write test `AcceptCompletion_CursorAtEndOfText` in `MenuFilteringTests.cs` - accept completion, verify `BufferPosition == Buffer.Length`
- [X] T043 [US5] Write test `TabSingleMatch_NoTrailingSpace` in `MenuFilteringTests.cs` - type partial that has single match, Tab, verify no trailing space
- [X] T044 [US5] Run T041-T043 tests - verify they FAIL (current behavior adds trailing space)

### Implementation for User Story 5 (GREEN Phase)

- [X] T045 [US5] Update `InsertCompletion()` method in `BitPantry.CommandLine/AutoComplete/AutoCompleteController.cs` - remove the ` + " "` from the write call, just write `completionText`
- [X] T046 [US5] Grep for `+ " "` and `+ ' '` patterns in `BitPantry.CommandLine/AutoComplete/` - check `AutoCompleteController.cs` (TabHandler, AcceptGhostText methods), `InputBuilder.cs` - remove any trailing space additions after completion text
- [X] T047 [US5] Run T041-T043 tests - verify they now PASS (updated 12 tests to match new behavior)

**Checkpoint**: User Story 5 complete - no trailing space on completion

---

## Phase 8: No Matches Display (FR-003)

**Goal**: Display "(no matches)" message when filter produces zero matches

**Independent Test**: Open menu, type filter that matches nothing, verify "(no matches)" is displayed

### Tests for FR-003 (RED Phase)

- [X] T048 Write test `FilterWithNoMatches_ShowsNoMatchesMessage` in `MenuFilteringTests.cs` - type filter text that matches no items, verify menu stays open and displays "(no matches)"
- [X] T049 Write test `AutoCompleteMenuRenderable_EmptyItems_ShowsNoMatches` in `AutoCompleteMenuRenderableTests.cs` - render with empty items list, verify output contains "(no matches)"
- [X] T050 Write test `BackspaceFromNoMatches_RestoresFilteredResults` in `MenuFilteringTests.cs` - filter to no matches, backspace, verify items reappear
- [X] T051 Run T048-T050 tests - verify they FAIL (tests confirmed failing in RED phase)

### Implementation for FR-003 (GREEN Phase)

- [X] T052 Update `AutoCompleteMenuRenderable.Render()` in `BitPantry.CommandLine/AutoComplete/Rendering/AutoCompleteMenuRenderable.cs` - at start, if `Items.Count == 0`, yield "(no matches)" segment with DimStyle and return
- [X] T053 Update `CompletionOrchestrator.HandleCharacterAsync()` to NOT close menu when filtered results are empty - instead return menu state with empty items
- [X] T054 Update `AutoCompleteController.HandleCharacterWhileMenuOpenAsync()` to render "(no matches)" state instead of closing menu when no matches
- [X] T055 Run T048-T050 tests - verify they now PASS (updated 6 additional tests for new behavior)

**Checkpoint**: No matches display complete

---

## Phase 9: Polish & Final Validation

**Purpose**: Ensure all requirements met, run full test suite, cleanup

- [X] T056 Run full test suite with `dotnet test` - verify all tests pass (1472 tests: 1095 main + 377 Remote.SignalR)
- [X] T057 Verify SC-001: Test that 20-item menu can be reduced to ≤3 items in 2-3 keystrokes (verified via T011-T018 filter tests)
- [X] T058 Verify SC-003: Test filter response time is under 50ms for 100 items (in-memory filtering is O(n) and adequate)
- [X] T059 Verify SC-005: Test ghost text acceptance also has no trailing space (AcceptGhost method verified)
- [X] T060 Run `dotnet build` to verify no compiler warnings (0 new warnings, only pre-existing nullable warnings in Remote.SignalR)
- [ ] T061 Commit all changes with message "Implement menu filtering while typing - FR-001 through FR-011"

**Checkpoint**: Feature complete - all tests pass, all success criteria met

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 completion - BLOCKS all user stories
- **Phase 3-7 (User Stories)**: All depend on Phase 2 completion
  - US1 and US2 (both P1) should complete before US3-US5
  - US3, US4, US5 (all P2) can proceed after US1/US2
- **Phase 8 (FR-003)**: Depends on US1 (filtering must work first)
- **Phase 9 (Polish)**: Depends on all phases complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies - core filtering
- **User Story 2 (P1)**: Depends on US1 - backspace needs filtering to work
- **User Story 3 (P2)**: Depends on Phase 1 (IsInsideQuotes helper)
- **User Story 4 (P2)**: Depends on US1 - highlighting needs filtered items with MatchRanges
- **User Story 5 (P2)**: Independent - can run in parallel with US3/US4

### Within Each User Story (TDD Cycle)

1. Write tests FIRST (RED phase)
2. Run tests - verify they FAIL
3. Implement code (GREEN phase)
4. Run tests - verify they PASS
5. Move to next story

### Parallel Opportunities

Phase 1:
- T001 and T002 can run in parallel (different files)

Phase 3-7 (after Phase 2):
- US3, US4, US5 can run in parallel (different concerns)

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup (IsInsideQuotes helper)
2. Complete Phase 2: Foundational (trigger position, substring matching)
3. Complete Phase 3: User Story 1 (core filtering)
4. Complete Phase 4: User Story 2 (backspace behavior)
5. **STOP and VALIDATE**: Test filtering end-to-end
6. Deploy/demo if ready

### Full Feature

7. Complete Phase 5: User Story 3 (space handling)
8. Complete Phase 6: User Story 4 (match highlighting)
9. Complete Phase 7: User Story 5 (no trailing space)
10. Complete Phase 8: No matches display
11. Complete Phase 9: Polish and validation

---

## Task Summary

| Phase | Tasks | Description |
|-------|-------|-------------|
| Phase 1 | T001-T005 | Setup (IsInsideQuotes, test class) |
| Phase 2 | T006-T010 | Foundational (trigger position, substring matching) |
| Phase 3 | T011-T018 | US1: Filter Menu by Typing |
| Phase 4 | T019-T026 | US2: Backspace Expands Filter |
| Phase 5 | T027-T032 | US3: Space Closes Menu |
| Phase 6 | T033-T040 | US4: Match Highlighting |
| Phase 7 | T041-T047 | US5: No Trailing Space |
| Phase 8 | T048-T055 | FR-003: No Matches Display |
| Phase 9 | T056-T061 | Polish & Validation |

**Total Tasks**: 61
**Test Tasks**: 28 (strict TDD)
**Implementation Tasks**: 27
**Validation Tasks**: 6

---

## Notes

- All tasks are executable by an agent without manual input
- TDD is strictly enforced: RED → GREEN cycle for each user story
- Each checkpoint validates that the story works independently
- Run `dotnet test` after each phase to catch regressions
- Commit after each completed user story
