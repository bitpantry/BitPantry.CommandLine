# Tasks: Command Groups

**Input**: Design documents from `/specs/003-command-groups/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: TDD approach - tests written before implementation per constitution.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, etc.)
- Include exact file paths in descriptions

## Path Conventions

- **Library**: `BitPantry.CommandLine/`
- **Tests**: `BitPantry.CommandLine.Tests/`

---

## Phase 1: Setup

**Purpose**: Create new types and remove namespace infrastructure (TDD: tests before implementation)

### Tests for Setup (write first, expect failures)

- [X] T001 [P] Create `BitPantry.CommandLine.Tests/Groups/GroupAttributeTests.cs` - test attribute properties, defaults, validation (expect compile failure initially)
- [X] T002 [P] Create `BitPantry.CommandLine.Tests/Groups/GroupInfoTests.cs` - test FullPath computation, parent/child setup (expect compile failure initially)

### Implementation for Setup

- [X] T003 Create `BitPantry.CommandLine/Help/` directory structure
- [X] T004 Create `BitPantry.CommandLine/API/GroupAttribute.cs` with Name and Description properties (make T001 pass)
- [X] T005 [P] Create `BitPantry.CommandLine/Component/GroupInfo.cs` with Name, Description, Parent, MarkerType, ChildGroups, Commands, FullPath properties (make T002 pass)
- [X] T006 Remove `Namespace` property from `BitPantry.CommandLine/API/CommandAttribute.cs`, add `Group` property (Type)
- [X] T007 Remove `Namespace` property from `BitPantry.CommandLine/Component/CommandInfo.cs`, add `Group` property (GroupInfo), update `FullyQualifiedName` to use space-separated format

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core registry and resolution infrastructure that ALL user stories depend on (TDD: tests before implementation)

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundational (write first, expect failures)

- [X] T008 [P] Create `BitPantry.CommandLine.Tests/Groups/CommandRegistryGroupTests.cs` - test RegisterGroup, FindGroup, FindCommand methods (expect compile failure initially)
- [X] T009 [P] Create `BitPantry.CommandLine.Tests/Groups/GroupParsingTests.cs` - test GroupPath extraction from space-separated input (expect compile failure initially)

### Implementation for Foundational

- [X] T010 Add group tracking to `BitPantry.CommandLine/CommandRegistry.cs` - add `_groups` list, `Groups`, `RootGroups`, `RootCommands` properties
- [X] T011 Implement `RegisterGroup(Type markerType)` method in `BitPantry.CommandLine/CommandRegistry.cs` (make T008 pass)
- [X] T012 Implement `FindGroup(string name, GroupInfo parent)` method in `BitPantry.CommandLine/CommandRegistry.cs`
- [X] T013 Implement `FindCommand(string name, GroupInfo group)` method in `BitPantry.CommandLine/CommandRegistry.cs`
- [X] T014 Update assembly scanning in `BitPantry.CommandLine/CommandRegistryApplicationBuilder.cs` to discover and register `[Group]` classes
- [X] T015 Add `ResolvedType` enum (Command, Group) to `BitPantry.CommandLine/Processing/Resolution/ResolvedCommand.cs`
- [X] T016 Add `GroupInfo` property to `BitPantry.CommandLine/Processing/Resolution/ResolvedCommand.cs`
- [X] T017 Update `CommandResolver.Resolve()` in `BitPantry.CommandLine/Processing/Resolution/CommandResolver.cs` to handle group-aware resolution
- [X] T018 Add `GroupPath` property to `BitPantry.CommandLine/Processing/Parsing/ParsedCommand.cs` (make T009 pass)
- [X] T019 Update parsing in `BitPantry.CommandLine/Processing/Parsing/ParsedInput.cs` to handle space-separated group paths
- [X] T020 Change `ReplaceDuplicateCommands` default to `false` in `BitPantry.CommandLine/CommandRegistry.cs`

**Checkpoint**: Foundation ready - registry can track groups, resolver can navigate group hierarchy

---

## Phase 3: User Story 1 - Define and Invoke Grouped Commands (Priority: P1) 🎯 MVP

**Goal**: Commands can be organized into groups using `[Group]` marker classes and invoked with space-separated syntax

**Independent Test**: Create a Group class and Command class, invoke with `group command` syntax

### Tests for User Story 1

- [X] T021 [P] [US1] Add integration tests to `BitPantry.CommandLine.Tests/Groups/GroupRegistrationTests.cs` - test group discovery and registration from assembly
- [X] T022 [P] [US1] Add integration tests to `BitPantry.CommandLine.Tests/Groups/GroupResolutionTests.cs` - test resolving `group command` to CommandInfo
- [X] T023 [US1] Create `BitPantry.CommandLine.Tests/Groups/GroupInvocationTests.cs` - end-to-end test of command execution via group path
- [X] T024 [P] [US1] Add root-level command test (FR-006) - command with no Group property invoked as `myapp version`
- [X] T025 [P] [US1] Add argument parsing test (FR-014) - verify tokens after command resolution are parsed as arguments (`math add --num1 5 --num2 3`)

### Implementation for User Story 1

- [X] T026 [US1] Create test group classes in `BitPantry.CommandLine.Tests/TestCommands/Groups/` - Math group with Add, Subtract commands
- [X] T027 [US1] Implement group name derivation (class name → lowercase, matching existing CommandAttribute behavior) in `GroupInfo` constructor
- [X] T028 [US1] Wire up command-to-group association during registration in `CommandRegistry`
- [X] T029 [US1] Verify dot-notation (`math.add`) is NOT recognized - add test case in `GroupResolutionTests.cs`

**Checkpoint**: User Story 1 complete - grouped commands work with space-separated syntax

---

## Phase 4: User Story 2 - Group Discoverability (Priority: P1)

**Goal**: Typing a group name alone displays available commands and subgroups

**Independent Test**: Invoke `myapp math` and verify help output listing commands

### Tests for User Story 2

- [ ] T030 [P] [US2] Create `BitPantry.CommandLine.Tests/Help/HelpFormatterTests.cs` - test group help output format, command help format, root help format (FR-020)
- [X] T031 [P] [US2] Create `BitPantry.CommandLine.Tests/Help/HelpInterceptionTests.cs` - test: (1) `--help`/`-h` flag detection, (2) FR-018a: help must be standalone - `cmd -f val --help` returns error, (3) FR-018b: error message format "error: --help cannot be combined with other arguments\nFor usage, run: cmd --help", (4) pipeline with help returns same error
- [X] T032 [P] [US2] Add exit code tests (FR-010) - verify group help exits with code 0, verify command help exits with code 0
- [X] T033 [P] [US2] Add explicit `-h` shorthand test (US2-3) - verify `myapp math -h` produces same output as `myapp math --help`

### Implementation for User Story 2

- [X] T034 [US2] Create `BitPantry.CommandLine/Help/IHelpFormatter.cs` interface with `DisplayGroupHelp`, `DisplayCommandHelp`, `DisplayRootHelp` methods
- [X] T035 [US2] Implement `BitPantry.CommandLine/Help/HelpFormatter.cs` using Spectre.Console for rich formatting
- [X] T036 [US2] Implement `CheckHelpRequest()` in `BitPantry.CommandLine/Processing/Execution/CommandLineApplicationCore.cs` - validate help is standalone
- [X] T037 [US2] Add help interception between resolution and execution in `CommandLineApplicationCore.Run()`
- [X] T038 [US2] Implement group-only resolution (when user types `group` alone) in `CommandResolver`
- [X] T039 [US2] Register `IHelpFormatter` in DI container in `BitPantry.CommandLine/ServiceCollectionExtensions.cs`
- [X] T040 [US2] Test that `myapp group --help` produces same output as `myapp group` alone
- [X] T041 [US2] Test that `myapp` or `myapp --help` displays root help (FR-020)

**Checkpoint**: User Story 2 complete - invoking group alone shows discoverable help

---

## Phase 5: User Story 3 - Nested Groups via C# Class Nesting (Priority: P2)

**Goal**: Nested groups work via C# nested classes (`Files.Io` → `files io`)

**Independent Test**: Create nested group classes, invoke `myapp files io upload`

### Tests for User Story 3

- [X] T042 [P] [US3] Add nested group test cases to `BitPantry.CommandLine.Tests/Groups/GroupRegistrationTests.cs`
- [X] T043 [P] [US3] Add multi-level path resolution tests to `BitPantry.CommandLine.Tests/Groups/GroupResolutionTests.cs`
- [X] T044 [P] [US3] Add 3+ level deep nesting test - verify `myapp level1 level2 level3 cmd` works correctly
- [X] T045 [P] [US3] Add group-with-only-subgroups test (FR-022 edge) - group containing only subgroups (no direct commands) is valid and shows subgroups in help

### Implementation for User Story 3

- [X] T046 [US3] Create test nested group classes in `BitPantry.CommandLine.Tests/TestCommands/Groups/` - Files.Io with Upload command
- [X] T047 [US3] Update group discovery to detect nested classes with `[Group]` attribute in `CommandRegistryApplicationBuilder`
- [X] T048 [US3] Establish parent-child relationships during registration based on C# nesting
- [X] T049 [US3] Update `GroupInfo.FullPath` to handle multi-level paths (`files io`)
- [X] T050 [US3] Test `myapp files` shows subgroup `io`, test `myapp files io` shows `upload` command

**Checkpoint**: User Story 3 complete - nested groups work with multi-word paths

---

## Phase 6: User Story 4 - Startup Validation (Priority: P2)

**Goal**: Configuration errors caught at startup with clear messages

**Independent Test**: Register invalid configuration, verify startup fails with descriptive error

### Tests for User Story 4

- [X] T051 [P] [US4] Create `BitPantry.CommandLine.Tests/Groups/GroupValidationTests.cs` - test: (1) empty group error (FR-022: no commands AND no subgroups), (2) name collision error, (3) duplicate command error, (4) FR-027: argument named `help` or alias `h` causes startup failure
- [X] T052 [P] [US4] Add non-group class reference test - command with `Group = typeof(NonGroupClass)` causes startup validation error
- [X] T053 [P] [US4] Add valid configuration positive test (US4-4) - verify valid group/command structure starts successfully with no errors

### Error Message Tests for User Story 4

- [X] T054 [P] [US4] Create `BitPantry.CommandLine.Tests/Groups/GroupErrorTests.cs` - test: (1) invalid subcommand in valid group shows error + list of available commands, (2) non-existent group name shows error + suggestions, (3) verify exit code is non-zero for errors

### Implementation for User Story 4

- [X] T055 [US4] Implement empty group validation in `CommandRegistry.Build()` - error if group has no commands AND no subgroups (FR-022)
- [X] T056 [US4] Implement name collision detection (command/group same name at same level) in `CommandRegistry.Build()`
- [X] T057 [US4] Implement reserved name validation - arguments named `help` or with alias `h` must cause startup failure (FR-027)
- [X] T058 [US4] Verify `ReplaceDuplicateCommands = false` default is applied correctly (set in T020)
- [X] T059 [US4] Ensure error messages identify built-in commands when collision occurs
- [X] T060 [US4] Implement error handling for invalid subcommand and non-existent group with helpful messages

**Checkpoint**: User Story 4 complete - invalid configurations fail fast with clear errors

---

## Phase 7: User Story 5 - Case Sensitivity Configuration (Priority: P3)

**Goal**: Configurable case sensitivity for command/group matching

**Independent Test**: Configure case-insensitive, verify `Math Add` resolves correctly

### Tests for User Story 5

- [X] T061 [P] [US5] Add case sensitivity test cases to `BitPantry.CommandLine.Tests/Groups/GroupResolutionTests.cs`

### Implementation for User Story 5

- [X] T062 [US5] Add `CaseSensitive` option to configuration (default `false` - case-insensitive by default per user choice)
- [X] T063 [US5] Update `FindGroup` and `FindCommand` to respect case sensitivity setting
- [X] T064 [US5] Test case-insensitive (default): `Math Add` → resolves to `math add`
- [X] T065 [US5] Test case-sensitive (opt-in): `Math Add` → not found

**Checkpoint**: User Story 5 complete - case sensitivity is configurable

---

## Phase 8: User Story 6 - Built-in Command Override Control (Priority: P3)

**Goal**: Control whether custom commands can override built-ins

**Independent Test**: Register command named `help` with different settings, verify behavior

### Tests for User Story 6

- [X] T066 [P] [US6] Add built-in override test cases to `BitPantry.CommandLine.Tests/Groups/GroupValidationTests.cs`

### Implementation for User Story 6

- [X] T067 [US6] Update duplicate detection to identify built-in commands in error messages
- [X] T068 [US6] Test `ReplaceDuplicateCommands = false` (default) with built-in conflict → startup error
- [X] T069 [US6] Test `ReplaceDuplicateCommands = true` with built-in conflict → custom replaces built-in

**Checkpoint**: User Story 6 complete - built-in override behavior is controllable

---

## Phase 9: Migration & Cleanup

**Purpose**: Update existing code, tests, and built-in commands

### Code Updates

- [ ] T070 Update `BitPantry.CommandLine/Commands/ListCommandsCommand.cs` to display groups instead of namespaces
- [ ] T071 [P] Remove all namespace-related helper methods from `BitPantry.CommandLine/Component/CommandInfo.cs`
- [ ] T072 [P] Remove `ValidateNamespace()` method if present
- [ ] T073 Rewrite `BitPantry.CommandLine/AutoComplete/AutoCompleteOptionSetBuilder.cs` - replace dot-notation namespace parsing with space-separated group resolution
- [ ] T074 Update `BitPantry.CommandLine/Processing/Description/CommandReflection.cs` - remove `Namespace` assignment, add `Group` resolution
- [ ] T075 [P] Update `BitPantry.CommandLine/Client/IServerProxy.cs` - change `cmdNamespace` parameter to `groupPath` in `AutoComplete` method
- [ ] T076 [P] Update `BitPantry.CommandLine/Client/NoopServerProxy.cs` - change `cmdNamespace` parameter to `groupPath` in `AutoComplete` method

### Test Command Cleanup

- [ ] T077 [P] Delete or convert `BitPantry.CommandLine.Tests/Commands/ResolveCommands/DupNameDifferentNamespace.cs` to group-based
- [ ] T078 [P] Delete or convert `BitPantry.CommandLine.Tests/Commands/ResolveCommands/CommandWithNamespace.cs` to group-based
- [ ] T079 [P] Delete or convert `BitPantry.CommandLine.Tests/Commands/AutoCompleteCommands/CommandWithNamespace.cs` to group-based
- [ ] T080 [P] Delete or convert `BitPantry.CommandLine.Tests/Commands/AutoCompleteCommands/DupNameDifferentNamespace.cs` to group-based
- [ ] T081 [P] Delete `BitPantry.CommandLine.Tests/Commands/DescribeCommands/BadNamespace_*.cs` files (3 files - validation no longer applies)
- [ ] T082 [P] Delete or convert `BitPantry.CommandLine.Tests/Commands/DescribeCommands/CommandWithNamespace.cs` to group-based
- [ ] T083 [P] Delete or convert `BitPantry.CommandLine.Tests/Commands/DescribeCommands/CommandWithNamespaceNoName.cs` to group-based
- [ ] T084 [P] Update `BitPantry.CommandLine.Tests/Commands/ApplicationCommands/VirtualCommand.cs` - change `Namespace = "test"` to group-based
- [ ] T085 Search for and remove any remaining `Namespace` references in codebase

### Existing Test File Updates

- [ ] T086 [P] Update `BitPantry.CommandLine.Tests/DescribeCommandTests.cs` to use group-based test commands
- [ ] T087 [P] Update `BitPantry.CommandLine.Tests/ResolveCommandTests.cs` to use group-based test commands
- [ ] T088 [P] Update `BitPantry.CommandLine.Tests/ResolveInputTests.cs` to use group-based resolution
- [ ] T089 [P] Update `BitPantry.CommandLine.Tests/ParsedInputTests.cs` for space-separated syntax (remove dot-notation tests)
- [ ] T090 [P] Update `BitPantry.CommandLine.Tests/ParsedCommandTests.cs` for GroupPath property
- [ ] T091 [P] Update `BitPantry.CommandLine.Tests/CommandActivatorTests.cs` to use group-based test commands
- [ ] T092 [P] Update `BitPantry.CommandLine.Tests/CommandActivatorWithDITests.cs` to use group-based test commands
- [ ] T093 [P] Update `BitPantry.CommandLine.Tests/CommandLineApplicationTests.cs` for group-based invocation
- [ ] T094 [P] Update `BitPantry.CommandLine.Tests/AutoCompleteControllerTests.cs` for space-separated syntax
- [ ] T095 [P] Update `BitPantry.CommandLine.Tests/AssemblyRegistrationTests.cs` for group-based registration
- [ ] T096 Run full test suite and fix any remaining failures

**Checkpoint**: All existing tests pass with group-based model

---

## Phase 10: Polish & Documentation

**Purpose**: Documentation updates and final validation

### Root Documentation

- [ ] T097 [P] Update `README.md` with group-based examples, remove namespace references

### Docs/ Directory Updates

- [ ] T098 [P] Update `Docs/index.md` with group-based overview
- [ ] T099 [P] Update `Docs/readme.md` with group-based examples
- [ ] T100 [P] Update `Docs/EndUserGuide.md` with space-separated invocation syntax
- [ ] T101 [P] Update `Docs/ImplementerGuide.md` with [Group] attribute usage, nested groups pattern
- [ ] T102 [P] Review and update `Docs/CommandLine/` subdirectory files for namespace→group changes
- [ ] T103 [P] Review and update `Docs/Remote/` subdirectory files if affected by group changes

### Code Documentation

- [ ] T104 Update all XML documentation in source files for new properties/methods
- [ ] T105 Validate no documentation references to namespaces remain (grep search)
- [ ] T106 Run quickstart.md scenarios as validation tests

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational) ← BLOCKS ALL USER STORIES
    ↓
Phase 3 (US1) ──┐
Phase 4 (US2) ──┼── Can run in parallel after Phase 2
Phase 5 (US3) ──┤
Phase 6 (US4) ──┘
    ↓
Phase 7 (US5) ──┬── Can run in parallel (lower priority)
Phase 8 (US6) ──┘
    ↓
Phase 9 (Migration)
    ↓
Phase 10 (Documentation)
```

### User Story Dependencies

| Story | Depends On | Can Parallel With |
|-------|------------|-------------------|
| US1 (P1) | Phase 2 | - |
| US2 (P1) | Phase 2 | US1 (different files) |
| US3 (P2) | Phase 2 | US1, US2 |
| US4 (P2) | Phase 2 | US1, US2, US3 |
| US5 (P3) | Phase 2 | US1-US4 |
| US6 (P3) | Phase 2 | US1-US5 |

### Parallel Opportunities

**Within Phase 1:**
- T001 and T002 can run in parallel

**Within Phase 2:**
- T008 and T009 can run in parallel (tests)

**Phase 3 (US1):**
- T021, T022, T024, T025 can run in parallel (tests)

**Phase 4 (US2):**
- T030, T031, T032, T033 can run in parallel (tests)

**Phase 5 (US3):**
- T042, T043, T044, T045 can run in parallel (tests)

**Phase 6 (US4):**
- T051, T052, T053, T054 can run in parallel (tests)

**Phase 9 Migration:**
- T071, T072, T075, T076 can run in parallel (namespace removal)
- T077-T084 can run in parallel (test command cleanup)
- T086-T095 can run in parallel (test file updates)

**Phase 10 Documentation:**
- T097-T103 can run in parallel

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (grouped commands work)
4. Complete Phase 4: User Story 2 (discoverability works)
5. **STOP and VALIDATE**: Core functionality complete
6. Deploy/demo if ready

### Incremental Delivery

| Increment | Stories | Value Delivered |
|-----------|---------|-----------------|
| MVP | US1 + US2 | Commands organized in groups, discoverable |
| +Nesting | US3 | Multi-level group hierarchies |
| +Validation | US4 | Fail-fast configuration errors |
| +Config | US5 + US6 | Platform customization |
| Complete | Migration + Docs | Full namespace replacement |

---

## Summary

| Phase | Task Count | Parallel Tasks |
|-------|------------|----------------|
| Setup | 7 | 2 |
| Foundational | 13 | 2 |
| US1 (P1) | 9 | 4 |
| US2 (P1) | 12 | 4 |
| US3 (P2) | 9 | 4 |
| US4 (P2) | 10 | 4 |
| US5 (P3) | 5 | 1 |
| US6 (P3) | 4 | 1 |
| Migration | 27 | 19 |
| Documentation | 10 | 7 |
| **Total** | **106** | **48** |

**MVP Scope**: Phases 1-4 (41 tasks) - delivers grouped commands with discoverability

**TDD Compliance**: All phases have tests written BEFORE implementation per constitution.

**Test Coverage**: Includes happy paths, edge cases, error scenarios, and exit code verification for all FRs and user stories.
