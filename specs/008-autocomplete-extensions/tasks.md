# Tasks: Extension-Based Autocomplete System

**Feature**: `008-autocomplete-extensions` | **Generated**: 2026-01-18 | **Spec**: [spec.md](spec.md) | **Test Cases**: [test-cases.md](test-cases.md)

---

## Phase 0: Setup

**Goal**: Establish project structure, create test files and stubs

### Tasks

- [ ] T001 Create `BitPantry.CommandLine/AutoComplete/Handlers/` directory structure
- [ ] T002 [depends:T001] Create `BitPantry.CommandLine/AutoComplete/Syntax/` directory structure
- [ ] T003 [depends:T001] Create `BitPantry.CommandLine.Tests/AutoComplete/Handlers/` test directory
- [ ] T004 [depends:T002] Create `BitPantry.CommandLine.Tests/AutoComplete/Syntax/` test directory
- [ ] T005 [depends:T003] Create `AutoCompleteConstants.cs` with `DefaultVisibleMenuItems = 5`

**Checkpoint**: Directory structure ready for implementation

---

## Phase 1: Core Interfaces & Registry

**Goal**: Establish foundational handler interfaces and registry

**User Stories**: US2 (Custom Extension Registration)

**Test Cases**: TC-1.1, TC-1.2, TC-1.3, TC-1.4

### Tasks

- [ ] T006 [depends:T005] Create `IAutoCompleteHandler` interface in `Handlers/IAutoCompleteHandler.cs`
- [ ] T007 [depends:T006] Create `ITypeAutoCompleteHandler` interface extending `IAutoCompleteHandler` in `Handlers/ITypeAutoCompleteHandler.cs`
- [ ] T008 [depends:T006] Create `IAutoCompleteAttribute` marker interface in `Handlers/IAutoCompleteAttribute.cs`
- [ ] T009 [depends:T008] Create `AutoCompleteAttribute<THandler>` generic attribute in `Handlers/AutoCompleteAttribute.cs`
- [ ] T010 [depends:T006] Create `AutoCompleteContext` class in `Handlers/AutoCompleteContext.cs`
- [ ] T011 [depends:T007,T010] @test-case:008:TC-1.1 Create `AutoCompleteHandlerRegistry` with `Register<T>()` in `Handlers/AutoCompleteHandlerRegistry.cs`
- [ ] T012 [depends:T011] @test-case:008:TC-1.2 Implement `GetHandler()` returning null when no handler matches
- [ ] T013 [depends:T012] @test-case:008:TC-1.3 Implement last-registered-wins ordering in `GetHandler()`
- [ ] T014 [depends:T013,T009] @test-case:008:TC-1.4 Implement attribute precedence in `GetHandler()` (attribute over type handler)

**Checkpoint**: Registry fully functional with correct resolution semantics

---

## Phase 2: User Story 1 - Built-in Type Autocomplete (Priority: P1)

**Goal**: Deliver automatic autocomplete for common types (enums, booleans)

**User Story**: US1 (Built-in Type Autocomplete)

**Test Cases**: TC-2.1 through TC-2.11

### Tasks

#### EnumAutoCompleteHandler

- [ ] T015 [depends:T014] @test-case:008:TC-2.1 Create `EnumAutoCompleteHandler` with `CanHandle` returning true for enum types
- [ ] T016 [depends:T015] @test-case:008:TC-2.2 `CanHandle` returns false for non-enum types
- [ ] T017 [depends:T016] @test-case:008:TC-2.3 `CanHandle` returns false for `typeof(Enum)` base type
- [ ] T018 [depends:T017] @test-case:008:TC-2.4 `GetOptionsAsync` returns all enum values when query empty
- [ ] T019 [depends:T018] @test-case:008:TC-2.5 `GetOptionsAsync` filters by prefix case-insensitively
- [ ] T020 [depends:T019] @test-case:008:TC-2.6 `GetOptionsAsync` returns alphabetically sorted results
- [ ] T021 [depends:T020] @test-case:008:TC-2.7 `GetOptionsAsync` unwraps nullable enum types

#### BooleanAutoCompleteHandler

- [ ] T022 [depends:T014] @test-case:008:TC-2.8 Create `BooleanAutoCompleteHandler` with `CanHandle` returning true for bool
- [ ] T023 [depends:T022] @test-case:008:TC-2.9 `CanHandle` returns false for non-bool types
- [ ] T024 [depends:T023] @test-case:008:TC-2.10 `GetOptionsAsync` returns ["false", "true"] when query empty
- [ ] T025 [depends:T024] @test-case:008:TC-2.11 `GetOptionsAsync` filters by prefix

#### Auto-Registration

- [ ] T026 [depends:T021,T025] @test-case:008:BUILTIN-001 Register `EnumAutoCompleteHandler` and `BooleanAutoCompleteHandler` by default in registry constructor

**Checkpoint**: User Story 1 complete — enums and booleans autocomplete automatically

---

## Phase 3: User Story 3 - Attribute-Based Extension Assignment (Priority: P2)

**Goal**: Enable explicit handler binding via attributes

**User Story**: US3 (Attribute-Based Extension Assignment)

**Test Cases**: TC-3.1 through TC-3.4

### Tasks

> **Note**: TC-3.1 (generic constraint) is a compile-time guarantee verified by code review, not a runtime test.

- [ ] T028 [depends:T009] @test-case:008:TC-3.2 `HandlerType` property returns correct type via `IAutoCompleteAttribute`
- [ ] T029 [depends:T028] @test-case:008:TC-3.3 Attribute works with `ITypeAutoCompleteHandler` types (compile-time test)
- [ ] T030 [depends:T029] @test-case:008:TC-3.4 Custom attributes inheriting `AutoCompleteAttribute<T>` are discoverable via marker interface

**Checkpoint**: User Story 3 complete — attribute-based handler binding functional

---

## Phase 4: User Story 2 & 4 - Integration (Priority: P1/P2)

**Goal**: End-to-end integration and context-aware suggestions

**User Stories**: US2 (Custom Extension Registration), US4 (Context-Aware Suggestions)

**Test Cases**: TC-4.1 through TC-4.6

### Tasks

- [ ] T031 [depends:T026,T030] @test-case:008:TC-4.1 End-to-end enum autocomplete works with default application
- [ ] T032 [depends:T031] @test-case:008:TC-4.2 Custom Type Handler overrides built-in when registered after
- [ ] T033 [depends:T032] @test-case:008:TC-4.3 Attribute Handler used even when matching Type Handler exists
- [ ] T034 [depends:T033] @test-case:008:TC-4.4 Handler receives `ProvidedValues` in context with already-entered values
- [ ] T035 [depends:T034] @test-case:008:TC-4.5 Boolean autocomplete works end-to-end
- [ ] T036 [depends:T035] @test-case:008:TC-4.6 Nullable enum autocomplete works end-to-end
- [ ] T036a [depends:T036] @test-case:008:TC-4.7 Handler exception gracefully degrades with logging
- [ ] T036b [depends:T036a] @test-case:008:TC-4.8 Handler returning empty is valid result (no fallback)
- [ ] T036c [depends:T036b] @test-case:008:TC-4.9 New input cancels pending autocomplete request

**Checkpoint**: Integration tests pass — handlers work in complete application context

---

## Phase 5: User Story 6 - Command Syntax Autocomplete (Priority: P1)

**Goal**: Autocomplete for command structure (groups, commands, argument names/aliases)

**User Story**: US6 (Command Syntax Autocomplete)

**Test Cases**: SYN-001 through SYN-003, SYN-005 through SYN-007

### Tasks

- [ ] T037 [depends:T014] @test-case:008:SYNTAX-001 Create `UsedArgumentHelper` in `AutoComplete/UsedArgumentHelper.cs`
- [ ] T038 [depends:T037] @test-case:008:SYN-001 Create `CommandSyntaxHandler` suggesting groups at command position
- [ ] T039 [depends:T038] @test-case:008:SYN-002 `CommandSyntaxHandler` suggests commands within typed group
- [ ] T040 [depends:T039] @test-case:008:SYN-003 `CommandSyntaxHandler` suggests root-level commands
- [ ] T042 [depends:T040] @test-case:008:SYN-005 Create `ArgumentNameHandler` suggesting `--argName` after `--`
- [ ] T043 [depends:T042] @test-case:008:SYN-006 Create `ArgumentAliasHandler` suggesting `-alias` after `-`
- [ ] T044 [depends:T043,T037] @test-case:008:SYN-007 Filter already-used arguments from suggestions

**Checkpoint**: User Story 6 complete — full command syntax autocomplete functional

---

## Phase 6: User Experience - Ghost Text & Menu (Priority: P1)

**Goal**: Ghost text auto-appearance and menu interaction

**Test Cases**: UX-001 through UX-035, UX-026b, UX-027b

### Tasks

#### Ghost Text Behavior

- [ ] T045 [depends:T036,T044] @test-case:008:UX-001 Ghost text auto-appears at autocomplete-applicable position
- [ ] T046 [depends:T045] @test-case:008:UX-002 Tab accepts single option (no menu)
- [ ] T047 [depends:T046] @test-case:008:UX-003 Tab opens menu when multiple options exist
- [ ] T048 [depends:T047] @test-case:008:UX-004 Right Arrow accepts ghost text
- [ ] T049 [depends:T048] @test-case:008:UX-008 Escape dismisses ghost text
- [ ] T050 [depends:T049] @test-case:008:UX-010 Typing updates ghost text dynamically
- [ ] T051 [depends:T050] @test-case:008:UX-012 Up Arrow dismisses ghost text and shows history
- [ ] T052 [depends:T051] @test-case:008:UX-013 No ghost text when no matches

#### Menu Behavior

- [ ] T053 [depends:T052] @test-case:008:UX-005 Down Arrow navigates menu (wraps bottom to top)
- [ ] T054 [depends:T053] @test-case:008:UX-006 Up Arrow navigates menu (wraps top to bottom)
- [ ] T055 [depends:T054] @test-case:008:UX-007 Enter accepts menu selection
- [ ] T056 [depends:T055] @test-case:008:UX-009 Escape closes menu preserving original text
- [ ] T057 [depends:T056] @test-case:008:UX-011 Type-to-filter in menu
- [ ] T058 [depends:T057] @test-case:008:UX-025 Backspace re-filters menu
- [ ] T059 [depends:T058] @test-case:008:UX-026 Space accepts selection (unquoted context)
- [ ] T060 [depends:T059] @test-case:008:UX-026b Space filters within quoted context
- [ ] T061 [depends:T060] @test-case:008:UX-027 Filter removes all matches closes menu
- [ ] T062 [depends:T061] @test-case:008:UX-027b Backspace restores options after filter-out

#### Menu Scrolling

- [ ] T063 [depends:T062] @test-case:008:UX-020 Scroll indicator at bottom (`▼ N more...`)
- [ ] T064 [depends:T063] @test-case:008:UX-021 Scroll indicators at both ends when scrolled to middle
- [ ] T065 [depends:T064] @test-case:008:UX-022 Scroll indicator at top when scrolled to bottom
- [ ] T066 [depends:T065] @test-case:008:UX-023 Wrap navigation bottom to top
- [ ] T067 [depends:T066] @test-case:008:UX-024 Wrap navigation top to bottom

#### Handler Integration

- [ ] T068 [depends:T067] @test-case:008:UX-014 Enum autocomplete works via implicit handler
- [ ] T069 [depends:T068] @test-case:008:UX-015 Boolean autocomplete works via implicit handler
- [ ] T070 [depends:T069] @test-case:008:UX-016 Attribute Handler overrides Type Handler

#### Positional Parameter Handling

- [ ] T071 [depends:T070] @test-case:008:UX-017 Positional enum shows ghost text
- [ ] T072 [depends:T071] @test-case:008:UX-018 Multiple positionals track independently
- [ ] T073 [depends:T072] @test-case:008:UX-019 Positional without handler shows no ghost text
- [ ] T074 [depends:T073] @test-case:008:UX-031 Positional set positionally excluded from `--` suggestions
- [ ] T075 [depends:T074] @test-case:008:UX-032 Positional set by name has no positional autocomplete
- [ ] T076 [depends:T075] @test-case:008:UX-033 Named arg set but positional unsatisfied still autocompletes
- [ ] T077 [depends:T076] @test-case:008:UX-034 After named arg only named args available
- [ ] T078 [depends:T077] @test-case:008:UX-035 Unsatisfied positional appears in `--` suggestions

#### Value Formatting

- [ ] T079 [depends:T078] @test-case:008:UX-028 Values with spaces auto-quoted
- [ ] T080 [depends:T079] @test-case:008:UX-029 Values without spaces not quoted
- [ ] T081 [depends:T080] @test-case:008:UX-030 Completion within existing quotes continues quote context

**Checkpoint**: All UX requirements satisfied — ghost text and menu fully functional

---

## Phase 7: User Story 5 - Remote Command Support (Priority: P3)

**Goal**: Autocomplete works for remote commands via SignalR

**User Story**: US5 (Remote Command Support)

**Test Cases**: RMT-001 through RMT-009, RMT-UX-001 through RMT-UX-005

### Tasks

#### Serialization

- [ ] T082 [depends:T081] @test-case:008:RMT-001 Create `AutoCompleteRequest` envelope with JSON serialization
- [ ] T083 [depends:T082] @test-case:008:RMT-002 Create `AutoCompleteResponse` envelope with JSON serialization
- [ ] T084 [depends:T083] @test-case:008:RMT-003 Verify `AutoCompleteOption` round-trip serialization

#### Integration

- [ ] T085 [depends:T084] @test-case:008:RMT-004 Remote autocomplete invocation via SignalR
- [ ] T086 [depends:T085] @test-case:008:RMT-005 Remote autocomplete has full parity with local
- [ ] T087 [depends:T086] @test-case:008:RMT-006 Remote handler resolution uses server's handlers
- [ ] T088 [depends:T087] @test-case:008:RMT-007 Remote attribute handler works
- [ ] T089 [depends:T088] @test-case:008:RMT-008 Remote CursorPosition accurately identifies context
- [ ] T090 [depends:T089] @test-case:008:RMT-009 Remote returns empty list (not null) when no matches

#### User Experience

- [ ] T091 [depends:T090] @test-case:008:RMT-UX-001 Ghost text appears over remote connection
- [ ] T092 [depends:T091] @test-case:008:RMT-UX-002 Menu opens with server-provided options
- [ ] T093 [depends:T092] @test-case:008:RMT-UX-003 Type-to-filter applied locally (no round-trip)
- [ ] T094 [depends:T093] @test-case:008:RMT-UX-004 Connection failure degrades gracefully (silent)
- [ ] T095 [depends:T094] @test-case:008:RMT-UX-005 Slow connection shows ghost text when response arrives

**Checkpoint**: User Story 5 complete — remote autocomplete fully functional

---

## Phase 8: Legacy Removal & Cleanup

**Goal**: Remove legacy autocomplete code and verify no regressions

**Test Cases**: (FR-024, FR-025, FR-026)

### Tasks

- [ ] T096 [depends:T095] Remove `AutoCompleteFunctionName` from `ArgumentAttribute`
- [ ] T097 [depends:T096] Remove `AutoCompleteFunctionName` from `ArgumentInfo`
- [ ] T098 [depends:T097] Remove `IsAutoCompleteFunctionAsync` from `ArgumentInfo`
- [ ] T099 [depends:T098] Remove legacy invocation code from `AutoCompleteOptionSetBuilder`
- [ ] T100 [depends:T099] Delete `AutoComplete/AutoCompleteContext.cs` (replaced)
- [ ] T101 [depends:T100] Delete legacy test commands from `Commands/AutoCompleteCommands/`

**Checkpoint**: All legacy code removed — feature complete, ready for merge

---

## Dependencies & Execution Order

### Dependency Resolution

Dependencies are explicit in each task via `[depends:T###]`. The workflow engine:

1. Parses all tasks and builds a dependency graph (DAG)
2. Topologically sorts tasks to determine execution order
3. A task is only eligible when all its dependencies are completed
4. Batches group eligible tasks for bounded execution (10-15 per batch)

### Batch Execution Model

- **Batch 1**: T001-T005 (Setup tasks with no dependencies)
- **Batch 2**: T006-T010 (Core interfaces)
- **Batch 3**: T011-T014 (Registry implementation)
- **Batch 4+**: Continues through user story phases

Each batch is:
- Created by `/speckit.batch`
- Executed via `/speckit.execute` (one task at a time)
- Verified via `/speckit.verify` (mandatory gate)

### Within Each Task (Micro-TDD Cycle)

1. **Write Test**: Create test for the @test-case behavior
2. **Red Phase**: Run test, capture failure output as evidence
3. **Implement**: Write minimal code to make test pass
4. **Green Phase**: Run test, capture success output as evidence
5. **Verify**: Script validates evidence before task completes

---

## Test Case Validation

**Total Test Cases**: 86 (from test-cases.md)
**Total Tasks**: 104 (88 with test cases, 16 infrastructure tasks)

**ID Convention**: All test case IDs use `008:` prefix for global uniqueness across specs.

| Section | Test Cases | Tasks |
|---------|------------|-------|
| Setup | *(no tests - infrastructure)* | T001-T005 |
| Core Interfaces | *(no tests - infrastructure)* | T006-T010 |
| Registry | 008:TC-1.1 to 008:TC-1.4 | T011-T014 |
| Built-in Handlers | 008:TC-2.1 to 008:TC-2.11, 008:BUILTIN-001 | T015-T026 |
| Attribute | 008:TC-3.1 to 008:TC-3.4 | T027-T030 |
| Integration | 008:TC-4.1 to 008:TC-4.9 | T031-T036c |
| Command Syntax | 008:SYNTAX-001, 008:SYN-001 to 008:SYN-007 | T037-T044 |
| User Experience | 008:UX-001 to 008:UX-035, 008:UX-026b, 008:UX-027b | T045-T081 |
| Remote | 008:RMT-001 to 008:RMT-009, 008:RMT-UX-001 to 008:RMT-UX-005 | T082-T095 |
| Legacy Removal | *(no tests - cleanup)* | T096-T101 |

**Infrastructure tasks** (Setup, Interface creation, Legacy removal) do not have test cases — they establish project structure or remove obsolete code.

---

## Notes

- `[depends:]` specifies explicit task dependencies
- Tasks with `@test-case:` follow Micro-TDD cycle (write test → red → implement → green)
- Infrastructure tasks (no `@test-case:`) are verified by successful execution
- Evidence files capture red and green outputs for verification
- Commit after each verified task or batch completion
- Priority order: P1 (US1, US2, US6) → P2 (US3, US4) → P3 (US5)
