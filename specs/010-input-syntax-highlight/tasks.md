# Tasks: Input Syntax Highlighting

**Input**: Design documents from `/specs/010-input-syntax-highlight/`  
**Prerequisites**: plan.md ✅, spec.md ✅, test-cases.md ✅, data-model.md ✅, research.md ✅

**Micro-TDD Format**: Each task is an atomic behavioral unit — ONE test case, ONE red→green cycle.

**Organization**: Tasks are grouped by user story for readability. Dependencies are explicit via `[depends:]`.

## Task Format

```text
- [ ] T### [depends:T###,T###] @test-case:010:XX-### Description with file path
```

---

## Phase 1: Testing Infrastructure (PREREQUISITE - BLOCKER)

**Goal**: Extend VirtualConsoleAssertions with color-aware assertions required for all integration tests.

**Test Cases**: TI-001 through TI-010

**Why First**: Cannot validate ANY syntax highlighting behavior without these assertions. This is a blocking prerequisite.

### Tasks

- [ ] T001 @test-case:010:TI-001 Implement HaveCellWithForegroundColor for cyan detection in BitPantry.VirtualConsole.Testing/VirtualConsoleAssertions.cs
- [ ] T002 [depends:T001] @test-case:010:TI-002 Add failure message for color mismatch in HaveCellWithForegroundColor
- [ ] T003 [depends:T001] @test-case:010:TI-003 Implement HaveRangeWithForegroundColor for multi-cell validation in VirtualConsoleAssertions.cs
- [ ] T004 [depends:T003] @test-case:010:TI-004 Add first-mismatch identification to HaveRangeWithForegroundColor failure message
- [ ] T005 [depends:T001] @test-case:010:TI-005 Implement HaveCellWithForeground256 for extended palette in VirtualConsoleAssertions.cs
- [ ] T006 [depends:T005] @test-case:010:TI-006 Implement HaveRangeWithForeground256 for extended palette ranges in VirtualConsoleAssertions.cs
- [ ] T007 [depends:T001] @test-case:010:TI-007 Implement HaveCellWithFullStyle for complete CellStyle comparison in VirtualConsoleAssertions.cs
- [ ] T008 [depends:T007] @test-case:010:TI-008 Add specific difference reporting to HaveCellWithFullStyle failure message
- [ ] T009 [depends:T001] @test-case:010:TI-009 Handle null foreground (default color) in HaveCellWithForegroundColor
- [ ] T010 [depends:T003] @test-case:010:TI-010 Add bounds validation to HaveRangeWithForegroundColor

**Checkpoint**: Testing infrastructure complete — can now write and validate color-based tests

---

## Phase 2: Foundational Components

**Goal**: Create core building blocks used by all user stories.

**Test Cases**: CV-001 through CV-007, SETUP

### Tasks

- [ ] T011 @test-case:010:SETUP-001 Create ColoredSegment record type in BitPantry.CommandLine/Input/ColoredSegment.cs
- [ ] T012 @test-case:010:CV-001 Implement SyntaxColorScheme.Group returning cyan Style in BitPantry.CommandLine/AutoComplete/SyntaxColorScheme.cs
- [ ] T013 [depends:T012] @test-case:010:CV-002 Implement SyntaxColorScheme.Command returning default Style in SyntaxColorScheme.cs
- [ ] T014 [depends:T012] @test-case:010:CV-003 Implement SyntaxColorScheme.ArgumentName returning yellow Style in SyntaxColorScheme.cs
- [ ] T015 [depends:T012] @test-case:010:CV-004 Implement SyntaxColorScheme.ArgumentAlias returning yellow Style in SyntaxColorScheme.cs
- [ ] T016 [depends:T012] @test-case:010:CV-005 Implement SyntaxColorScheme.ArgumentValue returning purple Style in SyntaxColorScheme.cs
- [ ] T017 [depends:T012] @test-case:010:CV-006 Implement SyntaxColorScheme.GhostText returning dim Style in SyntaxColorScheme.cs
- [ ] T018 [depends:T012] @test-case:010:CV-007 Implement SyntaxColorScheme.Default returning default Style in SyntaxColorScheme.cs

**Checkpoint**: Color scheme centralized — all colors defined in single location

---

## Phase 3: User Story 1 - Basic Command Syntax Colorization (P1) 🎯 MVP

**Goal**: Colorize typed text based on semantic meaning (groups=cyan, commands=white, args=yellow, values=purple).

**Test Cases**: CV-030 through CV-036, CV-010 through CV-017, UX-001 through UX-005

### TokenMatchResolver Tasks

- [ ] T019 [depends:T011] @test-case:010:CV-030 Implement ResolveMatch returning UniqueGroup for exact group match in BitPantry.CommandLine/Input/TokenMatchResolver.cs
- [ ] T020 [depends:T019] @test-case:010:CV-031 Implement ResolveMatch returning UniqueCommand for exact command match in TokenMatchResolver.cs
- [ ] T021 [depends:T019] @test-case:010:CV-032 Implement ResolveMatch returning UniqueGroup for unique partial group match in TokenMatchResolver.cs
- [ ] T022 [depends:T019] @test-case:010:CV-033 Implement ResolveMatch returning Ambiguous for multi-match in TokenMatchResolver.cs
- [ ] T023 [depends:T019] @test-case:010:CV-034 Implement ResolveMatch returning NoMatch for no matches in TokenMatchResolver.cs
- [ ] T024 [depends:T019] @test-case:010:CV-035 Implement ResolveMatch with group context for commands within group in TokenMatchResolver.cs
- [ ] T025 [depends:T019] @test-case:010:CV-036 Implement ResolveMatch detecting subgroups within parent group in TokenMatchResolver.cs

### SyntaxHighlighter Core Tasks

- [ ] T026 [depends:T018,T011] @test-case:010:CV-010 Implement Highlight returning empty list for empty input in BitPantry.CommandLine/Input/SyntaxHighlighter.cs
- [ ] T027 [depends:T026] @test-case:010:CV-011 Handle null input gracefully in SyntaxHighlighter.Highlight
- [ ] T028 [depends:T026,T019] @test-case:010:CV-012 Implement Highlight returning cyan segment for known group in SyntaxHighlighter.cs
- [ ] T029 [depends:T028] @test-case:010:CV-013 Implement Highlight returning default segment for root command in SyntaxHighlighter.cs
- [ ] T030 [depends:T028] @test-case:010:CV-014 Implement Highlight for "group command" returning two segments in SyntaxHighlighter.cs
- [ ] T031 [depends:T030] @test-case:010:CV-015 Implement Highlight for "group command --arg" returning three segments in SyntaxHighlighter.cs
- [ ] T032 [depends:T031] @test-case:010:CV-016 Implement Highlight for full command with arg value returning four segments in SyntaxHighlighter.cs
- [ ] T033 [depends:T031] @test-case:010:CV-017 Implement Highlight for alias -h with value returning four segments in SyntaxHighlighter.cs

### Integration Tests (require TI-xxx)

- [ ] T034 [depends:T010,T028] @test-case:010:UX-001 Verify complete group name displays cyan in integration test
- [ ] T035 [depends:T010,T029] @test-case:010:UX-002 Verify complete command name displays default/white in integration test
- [ ] T036 [depends:T010,T031] @test-case:010:UX-003 Verify --flag displays yellow in integration test
- [ ] T037 [depends:T010,T033] @test-case:010:UX-004 Verify -alias displays yellow in integration test
- [ ] T038 [depends:T010,T032] @test-case:010:UX-005 Verify argument value displays purple in integration test

**Checkpoint**: US1 complete — basic command input colorization working

---

## Phase 4: User Story 2 - Dynamic Character-by-Character Colorization (P1)

**Goal**: Colors update dynamically as each character is typed, handling partial/ambiguous matches.

**Test Cases**: CV-018, CV-019, UX-006 through UX-008

### Tasks

- [ ] T039 [depends:T021] @test-case:010:CV-018 Implement Highlight for partial uniquely matching group in SyntaxHighlighter.cs
- [ ] T040 [depends:T023] @test-case:010:CV-019 Implement Highlight for partial matching nothing in SyntaxHighlighter.cs
- [ ] T041 [depends:T010,T039] @test-case:010:UX-006 Verify partial "ser" uniquely matching group displays cyan
- [ ] T042 [depends:T010,T022] @test-case:010:UX-007 Verify ambiguous partial "c" displays default color
- [ ] T043 [depends:T010,T039] @test-case:010:UX-008 Verify partial "con" uniquely matching command displays default

**Checkpoint**: US2 complete — dynamic character-by-character colorization working

---

## Phase 5: User Story 3 - Real-time Recoloring on Edit (P2)

**Goal**: Colors update when user edits (backspace, cursor movement, insert).

**Test Cases**: CV-020, CV-040 through CV-044, UX-009, UX-010

### ConsoleLineMirror Tasks

- [ ] T044 [depends:T011] @test-case:010:CV-040 Implement RenderWithStyles for single segment in BitPantry.CommandLine/Input/ConsoleLineMirror.cs
- [ ] T045 [depends:T044] @test-case:010:CV-041 Implement RenderWithStyles for multiple segments in ConsoleLineMirror.cs
- [ ] T046 [depends:T044] @test-case:010:CV-042 Implement cursor positioning at end after RenderWithStyles in ConsoleLineMirror.cs
- [ ] T047 [depends:T044] @test-case:010:CV-043 Implement cursor positioning mid-line after RenderWithStyles in ConsoleLineMirror.cs
- [ ] T048 [depends:T044] @test-case:010:CV-044 Implement line clearing before RenderWithStyles in ConsoleLineMirror.cs

### SyntaxHighlighter Whitespace

- [ ] T049 [depends:T028] @test-case:010:CV-020 Implement whitespace handling with default style in SyntaxHighlighter.cs

### Integration Tests

- [ ] T050 [depends:T010,T048] @test-case:010:UX-009 Verify backspace re-renders with updated colors
- [ ] T051 [depends:T010,T047] @test-case:010:UX-010 Verify cursor returns to position after mid-line edit

**Checkpoint**: US3 complete — editing maintains accurate highlighting

---

## Phase 6: User Story 4 - Autocomplete Integration (P2)

**Goal**: Syntax highlighting works seamlessly with ghost text and menu.

**Test Cases**: UX-011, UX-012, DF-001 through DF-007

### InputBuilder Integration Tasks

- [ ] T052 [depends:T048,T033] @test-case:010:DF-001 Integrate SyntaxHighlighter.Highlight call in InputBuilder.OnKeyPressed
- [ ] T053 [depends:T052] @test-case:010:DF-002 Trigger re-highlight on backspace in InputBuilder
- [ ] T054 [depends:T052] @test-case:010:DF-003 Coordinate ghost text clearing and re-highlighting in InputBuilder
- [ ] T055 [depends:T052] @test-case:010:DF-004 Maintain highlighting when Tab opens menu in InputBuilder
- [ ] T056 [depends:T052] @test-case:010:DF-005 Preserve highlighting during arrow key menu navigation
- [ ] T057 [depends:T052] @test-case:010:DF-006 Re-highlight after menu selection accepted in InputBuilder
- [ ] T058 [depends:T052] @test-case:010:DF-007 Handle paste with final state highlighting in InputBuilder

### Visual Integration Tests

- [ ] T059 [depends:T010,T054] @test-case:010:UX-011 Verify colored input + dim ghost text render without conflict
- [ ] T060 [depends:T010,T055] @test-case:010:UX-012 Verify input remains colored while menu displays

**Checkpoint**: US4 complete — highlighting + autocomplete work together

---

## Phase 7: User Story 5 - Nested Group Hierarchy Colorization (P2)

**Goal**: Each group level in nested hierarchies colorizes appropriately.

**Test Cases**: CV-021, UX-013, UX-014

### Tasks

- [ ] T061 [depends:T025] @test-case:010:CV-021 Implement Highlight for nested groups "server files download" in SyntaxHighlighter.cs
- [ ] T062 [depends:T010,T061] @test-case:010:UX-013 Verify "server profile add" shows two cyan + one white
- [ ] T063 [depends:T010,T061] @test-case:010:UX-014 Verify 3-level nested "admin users roles assign" shows three cyan + one white

**Checkpoint**: US5 complete — nested group hierarchies colorize correctly

---

## Phase 8: User Story 6 - Invalid/Unrecognized Input Styling (P3)

**Goal**: Unrecognized input appears in default style; error cases handled gracefully.

**Test Cases**: UX-015, UX-016, EH-001 through EH-007

### Tasks

- [ ] T064 [depends:T040] @test-case:010:UX-015 Verify unrecognized "nonexistent" displays default style
- [ ] T065 [depends:T032] @test-case:010:UX-016 Verify quoted "hello world" displays as single purple segment
- [ ] T066 [depends:T027] @test-case:010:EH-001 Handle malformed input gracefully returning default segments
- [ ] T067 [depends:T026] @test-case:010:EH-002 Verify graceful degradation when console doesn't support colors
- [ ] T068 [depends:T032] @test-case:010:EH-003 Verify highlighting handles 1000+ char input without hanging
- [ ] T069 [depends:T028] @test-case:010:EH-004 Handle input with escape sequences correctly
- [ ] T070 [depends:T049] @test-case:010:EH-005 Handle whitespace-only input returning default segments
- [ ] T071 [depends:T065] @test-case:010:EH-006 Handle unclosed quote highlighting appropriately
- [ ] T072 [depends:T026] @test-case:010:EH-007 Handle empty registry returning all default style
- [ ] T073 [depends:T031] @test-case:010:EH-008 Handle argument for non-accepting command returning default style
- [ ] T074 [depends:T052] @test-case:010:EH-009 Handle rapid typing with batch final-state highlighting

**Checkpoint**: US6 complete — all edge cases and error conditions handled

---

## Phase 9: Polish & Refactor

**Goal**: Centralize colors in existing components for consistency.

**Test Cases**: REFACTOR (existing tests should pass)

### Tasks

- [ ] T075 [depends:T017] @test-case:010:REFACTOR-001 Refactor GhostTextController to use SyntaxColorScheme.GhostText in GhostTextController.cs
- [ ] T076 [depends:T012] @test-case:010:REFACTOR-002 Refactor AutoCompleteMenuRenderer to use SyntaxColorScheme.MenuHighlight and MenuGroup in AutoCompleteMenuRenderer.cs

**Checkpoint**: All colors centralized — consistent theming possible

---

## Summary

| Metric | Count |
|--------|-------|
| Total Tasks | 76 |
| Testing Infrastructure | 10 |
| Foundational | 8 |
| US1 (P1 MVP) | 20 |
| US2 (P1) | 5 |
| US3 (P2) | 8 |
| US4 (P2) | 9 |
| US5 (P2) | 3 |
| US6 (P3) | 11 |
| Polish | 2 |

### Test Case Coverage

| Category | Test Cases | Tasks |
|----------|------------|-------|
| TI (Infrastructure) | TI-001 to TI-010 | T001-T010 |
| CV (Component) | CV-001 to CV-044 | T012-T018, T019-T033, T039-T040, T044-T049, T061 |
| UX (User Experience) | UX-001 to UX-016 | T034-T043, T050-T051, T059-T060, T062-T065 |
| DF (Data Flow) | DF-001 to DF-007 | T052-T058 |
| EH (Error Handling) | EH-001 to EH-009 | T066-T074 |
| SETUP/REFACTOR | 3 | T011, T075-T076 |

**Coverage**: 73/73 test cases mapped (100%)

### MVP Scope

**Minimum Viable Product**: Complete Phases 1-4 (T001-T043)
- Testing infrastructure 
- SyntaxColorScheme
- TokenMatchResolver
- SyntaxHighlighter core
- US1 + US2 integration tests

This delivers the core value: real-time syntax highlighting with dynamic partial-match colorization.

### Dependency Graph (Story Completion Order)

```
Phase 1: Testing Infrastructure (BLOCKING)
    ↓
Phase 2: Foundational Components
    ↓
Phase 3: US1 - Basic Colorization (MVP) ────┐
    ↓                                       │
Phase 4: US2 - Dynamic Partial Match ───────┤
    ↓                                       │
Phase 5: US3 - Edit Recoloring ─────────────┤ (can parallelize after US1)
    ↓                                       │
Phase 6: US4 - Autocomplete Integration ────┤
    ↓                                       │
Phase 7: US5 - Nested Groups ───────────────┤
    ↓                                       │
Phase 8: US6 - Error Handling ──────────────┘
    ↓
Phase 9: Polish & Refactor
```

### Parallel Execution Opportunities

After Phase 2 (Foundational) completes:
- T019-T025 (TokenMatchResolver) can run parallel with T044-T048 (ConsoleLineMirror.RenderWithStyles)

After Phase 3 (US1) completes:
- US3, US5, US6 implementation tasks can run in parallel (all depend on US1 core)

Integration tests (UX-xxx) must wait for TI-xxx infrastructure.
