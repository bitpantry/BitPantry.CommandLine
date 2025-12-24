# Tasks: Positional Arguments

**Input**: Design documents from `/specs/004-positional-arguments/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Per Constitution Principle I (TDD), all tests are written FIRST and must FAIL before implementation.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1-US6) this task belongs to
- Paths are relative to repository root

## User Story Mapping

| Story | Title | Priority | Spec Reference |
|-------|-------|----------|----------------|
| US1 | Define Positional Arguments on Commands | P1 | User Story 1 |
| US2 | Invoke Commands with Positional Arguments | P1 | User Story 2 |
| US3 | Variadic Positional Arguments (IsRest) | P2 | User Story 3 |
| US4 | Repeated Named Options | P2 | User Story 4 |
| US5 | Auto-Complete for Positional Arguments | P3 | User Story 5 |
| US6 | Help Display for Positional Arguments | P3 | User Story 6 |

---

## Phase 1: Setup (Test Infrastructure)

**Purpose**: Create test command classes and establish test infrastructure

- [ ] T001 Create directory BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T002 Create directory BitPantry.CommandLine.Tests/Commands/RepeatedOptionCommands/
- [ ] T003 [P] Create SinglePositionalCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T004 [P] Create MultiplePositionalCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T005 [P] Create PositionalWithNamedCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T006 [P] Create IsRestCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T007 [P] Create IsRestWithPrecedingCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T008 [P] Create RequiredPositionalCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T009 [P] Create OptionalPositionalCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T010 [P] Create PositionalWithAutoCompleteCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T011 [P] Create InvalidIsRestScalarCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T012 [P] Create InvalidIsRestNotPositionalCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T013 [P] Create InvalidMultipleIsRestCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T014 [P] Create InvalidIsRestNotLastCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T015 [P] Create InvalidGapPositionCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T016 [P] Create InvalidDuplicatePositionCommand.cs in BitPantry.CommandLine.Tests/Commands/PositionalCommands/
- [ ] T017 [P] Create RepeatedOptionArrayCommand.cs in BitPantry.CommandLine.Tests/Commands/RepeatedOptionCommands/
- [ ] T018 [P] Create RepeatedOptionListCommand.cs in BitPantry.CommandLine.Tests/Commands/RepeatedOptionCommands/
- [ ] T019 [P] Create RepeatedOptionScalarCommand.cs in BitPantry.CommandLine.Tests/Commands/RepeatedOptionCommands/

---

## Phase 2: Foundational (Core Attribute Extensions)

**Purpose**: Extend ArgumentAttribute and ArgumentInfo - BLOCKS all user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T020 Add Position property (int, default -1) to ArgumentAttribute in BitPantry.CommandLine/API/ArgumentAttribute.cs
- [ ] T021 Add IsRest property (bool, default false) to ArgumentAttribute in BitPantry.CommandLine/API/ArgumentAttribute.cs
- [ ] T022 Add Position property to ArgumentInfo in BitPantry.CommandLine/Component/ArgumentInfo.cs
- [ ] T023 Add IsRest property to ArgumentInfo in BitPantry.CommandLine/Component/ArgumentInfo.cs
- [ ] T024 Add IsPositional computed property (Position >= 0) to ArgumentInfo in BitPantry.CommandLine/Component/ArgumentInfo.cs
- [ ] T025 Add IsCollection computed property to ArgumentInfo in BitPantry.CommandLine/Component/ArgumentInfo.cs
- [ ] T026 Update CommandReflection.GetArgumentInfos to read Position and IsRest from attribute in BitPantry.CommandLine/Processing/Description/CommandReflection.cs
- [ ] T027 Add PositionalValue enum value to CommandElementType in BitPantry.CommandLine/Processing/Parsing/ParsedCommandElement.cs
- [ ] T028 Add EndOfOptions enum value to CommandElementType in BitPantry.CommandLine/Processing/Parsing/ParsedCommandElement.cs
- [ ] T029 Create ArgumentValues wrapper class in BitPantry.CommandLine/Processing/Resolution/ArgumentValues.cs
- [ ] T030 Add new error types to CommandResolutionErrorType in BitPantry.CommandLine/Processing/Resolution/CommandResolutionErrorType.cs

**Checkpoint**: Foundation ready - user story implementation can begin

---

## Phase 3: User Story 1 - Define Positional Arguments (Priority: P1) 🎯 MVP

**Goal**: CLI implementers can define positional arguments using `[Argument(Position = N)]` attribute

**Independent Test**: Create a command with positional arguments, register it, verify ArgumentInfo has correct Position values

### Tests for User Story 1

> **Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T031 [P] [US1] Write test VAL-001: Valid single positional in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T032 [P] [US1] Write test VAL-002: Valid multiple positional in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T033 [P] [US1] Write test VAL-003: Valid IsRest on array in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T034 [P] [US1] Write test VAL-004: IsRest on scalar error in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T035 [P] [US1] Write test VAL-005: IsRest without Position error in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T036 [P] [US1] Write test VAL-006: Multiple IsRest error in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T037 [P] [US1] Write test VAL-007: IsRest not last error in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T038 [P] [US1] Write test VAL-008: Gap in positions error in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T039 [P] [US1] Write test VAL-009: Duplicate positions error in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T040 [P] [US1] Write test VAL-010: Negative position error in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T041 [P] [US1] Write test VAL-011: Mixed positional and named in BitPantry.CommandLine.Tests/DescribeCommandTests.cs
- [ ] T042 [P] [US1] Write test VAL-012: Error message contains command and property name in BitPantry.CommandLine.Tests/DescribeCommandTests.cs

### Implementation for User Story 1

- [ ] T043 [US1] Add positional argument validation logic to CommandInfo constructor in BitPantry.CommandLine/Commands/CommandInfo.cs
- [ ] T044 [US1] Add ValidatePositionalArguments method to CommandInfo in BitPantry.CommandLine/Commands/CommandInfo.cs
- [ ] T045 [US1] Create PositionalArgumentValidationException in BitPantry.CommandLine/Commands/PositionalArgumentValidationException.cs
- [ ] T046 [US1] Run tests VAL-001 through VAL-012 and verify all pass

**Checkpoint**: Positional arguments can be defined and validated at registration time

---

## Phase 4: User Story 2 - Invoke Commands with Positional Arguments (Priority: P1) 🎯 MVP

**Goal**: CLI users can invoke commands with positional syntax like `copy source.txt dest.txt`

**Independent Test**: Parse `cmd val1 val2`, resolve to command with Position=0,1, activate and verify properties set

### Tests for User Story 2

> **Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T047 [P] [US2] Write test PARSE-001: Single positional value in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T048 [P] [US2] Write test PARSE-002: Multiple positional values in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T049 [P] [US2] Write test PARSE-003: Positional then named in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T050 [P] [US2] Write test PARSE-004: Named then positional-like in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T051 [P] [US2] Write test PARSE-005: End-of-options separator in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T052 [P] [US2] Write test PARSE-006: Multiple after -- in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T053 [P] [US2] Write test PARSE-007: Empty positional region in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T054 [P] [US2] Write test PARSE-008: Quoted positional value in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T055 [P] [US2] Write test PARSE-009: Mixed quotes and bare in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T056 [P] [US2] Write test PARSE-010: Bare -- with no following in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T057 [P] [US2] Write test PARSE-011: Mid-positional -- in BitPantry.CommandLine.Tests/ParsedCommandTests.cs
- [ ] T058 [P] [US2] Write test RES-001: Single positional resolved in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T059 [P] [US2] Write test RES-002: Multiple positional resolved in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T060 [P] [US2] Write test RES-003: Positional + named in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T061 [P] [US2] Write test RES-006: Missing required positional in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T062 [P] [US2] Write test RES-007: Excess positional (no IsRest) in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T063 [P] [US2] Write test RES-011: Positional after -- in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T064 [P] [US2] Write test ACT-001: String positional in BitPantry.CommandLine.Tests/CommandActivatorTests.cs
- [ ] T065 [P] [US2] Write test ACT-002: Int positional in BitPantry.CommandLine.Tests/CommandActivatorTests.cs
- [ ] T066 [P] [US2] Write test ACT-007: Positional type mismatch in BitPantry.CommandLine.Tests/CommandActivatorTests.cs
- [ ] T067 [P] [US2] Write test ACT-009: Mixed positional + named in BitPantry.CommandLine.Tests/CommandActivatorTests.cs

### Implementation for User Story 2

- [ ] T068 [US2] Update ParsedCommandElement constructor to classify PositionalValue elements in BitPantry.CommandLine/Processing/Parsing/ParsedCommandElement.cs
- [ ] T069 [US2] Add -- end-of-options separator handling in ParsedCommandElement in BitPantry.CommandLine/Processing/Parsing/ParsedCommandElement.cs
- [ ] T070 [US2] Update ResolvedCommand.InputMap to use ArgumentValues type in BitPantry.CommandLine/Processing/Resolution/ResolvedCommand.cs
- [ ] T071 [US2] Add positional element matching logic to CommandResolver.Resolve in BitPantry.CommandLine/Processing/Resolution/CommandResolver.cs
- [ ] T072 [US2] Add missing required positional error handling in CommandResolver in BitPantry.CommandLine/Processing/Resolution/CommandResolver.cs
- [ ] T073 [US2] Add excess positional values error handling in CommandResolver in BitPantry.CommandLine/Processing/Resolution/CommandResolver.cs
- [ ] T074 [US2] Update CommandActivator to handle positional values from ArgumentValues in BitPantry.CommandLine/Processing/Activation/CommandActivator.cs
- [ ] T075 [US2] Run tests PARSE-*, RES-001 through RES-003, RES-006, RES-007, RES-011, ACT-* and verify all pass
- [ ] T076 [US2] Write and run integration test INT-001: Full positional execution in BitPantry.CommandLine.Tests/
- [ ] T077 [US2] Write and run regression test INT-004: Backward compatibility in BitPantry.CommandLine.Tests/

**Checkpoint**: Basic positional arguments fully functional - MVP complete

---

## Phase 5: User Story 3 - Variadic Positional Arguments (Priority: P2)

**Goal**: CLI implementers can use `IsRest = true` to capture all remaining positional values

**Independent Test**: Parse `cmd a b c d`, resolve to command with IsRest, verify array contains [b,c,d]

### Tests for User Story 3

- [ ] T076 [P] [US3] Write test RES-004: IsRest collects remaining in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T077 [P] [US3] Write test RES-005: IsRest with zero extra in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T080 [P] [US3] Write test ACT-003: IsRest string array in BitPantry.CommandLine.Tests/CommandActivatorTests.cs
- [ ] T081 [P] [US3] Write test ACT-004: IsRest int array in BitPantry.CommandLine.Tests/CommandActivatorTests.cs
- [ ] T082 [P] [US3] Write test ACT-005: IsRest List<string> in BitPantry.CommandLine.Tests/CommandActivatorTests.cs
- [ ] T083 [P] [US3] Write test ACT-008: Empty IsRest in BitPantry.CommandLine.Tests/CommandActivatorTests.cs

### Implementation for User Story 3

- [ ] T084 [US3] Add IsRest collection logic to CommandResolver.Resolve in BitPantry.CommandLine/Processing/Resolution/CommandResolver.cs
- [ ] T085 [US3] Update CommandActivator to populate IsRest collections in BitPantry.CommandLine/Processing/Activation/CommandActivator.cs
- [ ] T086 [US3] Add GetElementType helper for collection type extraction in BitPantry.CommandLine/Processing/Activation/CommandActivator.cs
- [ ] T087 [US3] Run tests RES-004, RES-005, ACT-003 through ACT-005, ACT-008 and verify all pass
- [ ] T088 [US3] Write and run integration test INT-002: Full IsRest execution in BitPantry.CommandLine.Tests/

**Checkpoint**: Variadic positional arguments fully functional

---

## Phase 6: User Story 4 - Repeated Named Options (Priority: P2)

**Goal**: CLI users can specify `--opt a --opt b` to build collections

**Independent Test**: Parse `cmd --opt a --opt b`, resolve and activate, verify array contains [a,b]

### Tests for User Story 4

- [ ] T089 [P] [US4] Write test RES-008: Repeated option collection in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T090 [P] [US4] Write test RES-009: Repeated option scalar error in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T091 [P] [US4] Write test RES-010: Mixed delimiter + repeated in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T092 [P] [US4] Write test RES-010b: Repeated then delimiter in BitPantry.CommandLine.Tests/ResolveCommandTests.cs
- [ ] T093 [P] [US4] Write test ACT-006: Repeated option array in BitPantry.CommandLine.Tests/CommandActivatorTests.cs

### Implementation for User Story 4

- [ ] T094 [US4] Add repeated option detection logic to CommandResolver.Resolve in BitPantry.CommandLine/Processing/Resolution/CommandResolver.cs
- [ ] T095 [US4] Add ArgumentValues.Append for accumulating repeated values in BitPantry.CommandLine/Processing/Resolution/ArgumentValues.cs
- [ ] T096 [US4] Update CommandActivator to merge delimiter-parsed and repeated values in BitPantry.CommandLine/Processing/Activation/CommandActivator.cs
- [ ] T097 [US4] Add duplicate scalar argument error handling in CommandResolver in BitPantry.CommandLine/Processing/Resolution/CommandResolver.cs
- [ ] T098 [US4] Run tests RES-008 through RES-010b, ACT-006 and verify all pass
- [ ] T099 [US4] Write and run integration test INT-003: Full repeated option in BitPantry.CommandLine.Tests/

**Checkpoint**: Repeated named options fully functional

---

## Phase 7: User Story 5 - Auto-Complete for Positional Arguments (Priority: P3)

**Goal**: Auto-complete suggests values for positional argument slots

**Independent Test**: Trigger auto-complete at positional slot, verify correct argument's completion function invoked

### Tests for User Story 5

- [ ] T100 [P] [US5] Write test AC-001: First positional slot in BitPantry.CommandLine.Tests/AutoCompleteSetBuilderTests_Positional.cs
- [ ] T101 [P] [US5] Write test AC-002: Second positional slot in BitPantry.CommandLine.Tests/AutoCompleteSetBuilderTests_Positional.cs
- [ ] T102 [P] [US5] Write test AC-003: IsRest continues in BitPantry.CommandLine.Tests/AutoCompleteSetBuilderTests_Positional.cs
- [ ] T103 [P] [US5] Write test AC-004: No autocomplete function in BitPantry.CommandLine.Tests/AutoCompleteSetBuilderTests_Positional.cs
- [ ] T104 [P] [US5] Write test AC-005: Context has prior values in BitPantry.CommandLine.Tests/AutoCompleteSetBuilderTests_Positional.cs
- [ ] T105 [P] [US5] Write test AC-006: After named option in BitPantry.CommandLine.Tests/AutoCompleteSetBuilderTests_Positional.cs
- [ ] T106 [P] [US5] Write test AC-007: Partial positional in BitPantry.CommandLine.Tests/AutoCompleteSetBuilderTests_Positional.cs

### Implementation for User Story 5

- [ ] T107 [US5] Add CurrentPositionalIndex property to AutoCompleteContext in BitPantry.CommandLine/AutoComplete/AutoCompleteContext.cs
- [ ] T108 [US5] Add AllValues property to AutoCompleteContext in BitPantry.CommandLine/AutoComplete/AutoCompleteContext.cs
- [ ] T109 [US5] Add BuildOptions_PositionalValue method to AutoCompleteOptionsBuilder in BitPantry.CommandLine/AutoComplete/AutoCompleteOptionsBuilder.cs
- [ ] T110 [US5] Update AutoCompleteOptionsBuilder.Build to handle PositionalValue elements in BitPantry.CommandLine/AutoComplete/AutoCompleteOptionsBuilder.cs
- [ ] T111 [US5] Add positional slot counting logic to determine which argument to complete in BitPantry.CommandLine/AutoComplete/AutoCompleteOptionsBuilder.cs
- [ ] T112 [US5] Run tests AC-001 through AC-007 and verify all pass

**Checkpoint**: Auto-complete for positional arguments fully functional

---

## Phase 8: User Story 6 - Help Display for Positional Arguments (Priority: P3)

**Goal**: Help output shows positional arguments with proper notation

**Independent Test**: Request help for command with positional args, verify synopsis format

### Tests for User Story 6

- [ ] T113 [P] [US6] Write test HELP-001: Required positional in BitPantry.CommandLine.Tests/Help/HelpFormatterTests.cs
- [ ] T114 [P] [US6] Write test HELP-002: Optional positional in BitPantry.CommandLine.Tests/Help/HelpFormatterTests.cs
- [ ] T115 [P] [US6] Write test HELP-003: Variadic positional in BitPantry.CommandLine.Tests/Help/HelpFormatterTests.cs
- [ ] T116 [P] [US6] Write test HELP-004: Mixed pos + named in BitPantry.CommandLine.Tests/Help/HelpFormatterTests.cs
- [ ] T117 [P] [US6] Write test HELP-005: Multiple positional in BitPantry.CommandLine.Tests/Help/HelpFormatterTests.cs
- [ ] T118 [P] [US6] Write test HELP-006: Repeated option note in BitPantry.CommandLine.Tests/Help/HelpFormatterTests.cs

### Implementation for User Story 6

- [ ] T119 [US6] Update HelpFormatter.FormatCommand to generate positional synopsis in BitPantry.CommandLine/Help/HelpFormatter.cs
- [ ] T120 [US6] Add GetPositionalSynopsis helper method to HelpFormatter in BitPantry.CommandLine/Help/HelpFormatter.cs
- [ ] T121 [US6] Add angle bracket notation for required positionals in HelpFormatter in BitPantry.CommandLine/Help/HelpFormatter.cs
- [ ] T122 [US6] Add square bracket notation for optional positionals in HelpFormatter in BitPantry.CommandLine/Help/HelpFormatter.cs
- [ ] T123 [US6] Add ellipsis notation for variadic positionals in HelpFormatter in BitPantry.CommandLine/Help/HelpFormatter.cs
- [ ] T124 [US6] Add "can be repeated" note for collection options in HelpFormatter in BitPantry.CommandLine/Help/HelpFormatter.cs
- [ ] T125 [US6] Run tests HELP-001 through HELP-006 and verify all pass
- [ ] T126 [US6] Write and run integration test INT-005: Help with positional in BitPantry.CommandLine.Tests/

**Checkpoint**: Help display for positional arguments fully functional

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, integration tests, and final validation

- [ ] T127 [P] Write and run integration test INT-006: Validation error at startup in BitPantry.CommandLine.Tests/
- [ ] T128 [P] Add "Positional Arguments" section to Docs/EndUserGuide.md
- [ ] T129 [P] Add "Repeated Options" section to Docs/EndUserGuide.md
- [ ] T130 [P] Add `--` separator explanation to Docs/EndUserGuide.md
- [ ] T131 [P] Add `[Argument(Position=N)]` usage to Docs/ImplementerGuide.md
- [ ] T132 [P] Add `[Argument(Position=N, IsRest=true)]` usage to Docs/ImplementerGuide.md
- [ ] T133 [P] Add validation rules table to Docs/ImplementerGuide.md
- [ ] T134 [P] Add repeated option collection syntax to Docs/ImplementerGuide.md
- [ ] T135 Run full test suite and verify no regressions
- [ ] T136 Run quickstart.md validation scenarios manually
- [ ] T137 Code review and cleanup

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) ──► Phase 2 (Foundational) ──► Phases 3-8 (User Stories)
                                              │
                                              ├─► Phase 3 (US1) ──┐
                                              ├─► Phase 4 (US2) ──┤
                                              ├─► Phase 5 (US3) ──┤
                                              ├─► Phase 6 (US4) ──┼──► Phase 9 (Polish)
                                              ├─► Phase 7 (US5) ──┤
                                              └─► Phase 8 (US6) ──┘
```

### User Story Dependencies

| Story | Depends On | Can Parallelize With |
|-------|------------|---------------------|
| US1 (Define) | Phase 2 only | - |
| US2 (Invoke) | US1 (T045) | US1 verification (T046) |
| US3 (IsRest) | US2 (T074) | US4 |
| US4 (Repeated) | US2 (T074) | US3 |
| US5 (AutoComplete) | US2 (T074) | US6 |
| US6 (Help) | US2 (T074) | US5 |

> **Note**: Task IDs shifted after adding VAL-012 (T042), PARSE-011 (T057), and RES-010b (T092) tests per analysis review.

### Within Each User Story

1. Tests MUST be written and FAIL before implementation
2. Implementation tasks in order listed
3. Verification task confirms all tests pass
4. Integration test confirms end-to-end flow

---

## Parallel Opportunities

### Phase 1: All Setup Tasks (T001-T019)
```
T003-T019 can all run in parallel (different files)
```

### Phase 2: Attribute Extensions
```
T020-T021 in parallel (same file but different properties)
T022-T026 in parallel (same file but different properties)  
T027-T028 in parallel (same file)
```

### Phase 3-8: Tests Within Each Story
```
All test tasks marked [P] within a story can run in parallel
```

### After US2 Complete (T075)
```
US3, US4, US5, US6 can all begin in parallel if team capacity allows
```

---

## Implementation Strategy

### MVP First (US1 + US2 Only)

1. Complete Phase 1: Setup (T001-T019)
2. Complete Phase 2: Foundational (T020-T030)
3. Complete Phase 3: US1 - Define (T031-T046)
4. Complete Phase 4: US2 - Invoke (T047-T077)
5. **STOP and VALIDATE**: Test positional arguments work end-to-end
6. Deploy/demo if ready - basic positional arguments functional

### Incremental Delivery

1. MVP: US1 + US2 → Basic positional arguments ✓
2. Add US3: IsRest → Variadic support ✓
3. Add US4: Repeated → Collection options ✓
4. Add US5: AutoComplete → Enhanced UX ✓
5. Add US6: Help → Discoverability ✓
6. Polish: Docs + final tests ✓

### Task Counts

| Phase | Task Count |
|-------|------------|
| Phase 1: Setup | 19 |
| Phase 2: Foundational | 11 |
| Phase 3: US1 | 16 |
| Phase 4: US2 | 31 |
| Phase 5: US3 | 11 |
| Phase 6: US4 | 11 |
| Phase 7: US5 | 13 |
| Phase 8: US6 | 14 |
| Phase 9: Polish | 11 |
| **Total** | **137** |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- Each user story independently completable and testable
- Verify tests FAIL before implementing
- Commit after each logical group of tasks
- Stop at any checkpoint to validate independently
