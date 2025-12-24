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

- [ ] T001 [P] Create `BitPantry.CommandLine.Tests/Groups/GroupAttributeTests.cs` - test attribute properties, defaults, validation (expect compile failure initially)
- [ ] T002 [P] Create `BitPantry.CommandLine.Tests/Groups/GroupInfoTests.cs` - test FullPath computation, parent/child setup (expect compile failure initially)

### Implementation for Setup

- [ ] T003 Create `BitPantry.CommandLine/Help/` directory structure
- [ ] T004 Create `BitPantry.CommandLine/API/GroupAttribute.cs` with Name and Description properties (make T001 pass)
- [ ] T005 [P] Create `BitPantry.CommandLine/Component/GroupInfo.cs` with Name, Description, Parent, MarkerType, ChildGroups, Commands, FullPath properties (make T002 pass)
- [ ] T006 Remove `Namespace` property from `BitPantry.CommandLine/API/CommandAttribute.cs`, add `Group` property (Type)
- [ ] T007 Remove `Namespace` property from `BitPantry.CommandLine/Component/CommandInfo.cs`, add `Group` property (GroupInfo), update `FullyQualifiedName` to use space-separated format

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core registry and resolution infrastructure that ALL user stories depend on (TDD: tests before implementation)

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundational (write first, expect failures)

- [ ] T008 [P] Create `BitPantry.CommandLine.Tests/Groups/CommandRegistryGroupTests.cs` - test RegisterGroup, FindGroup, FindCommand methods (expect compile failure initially)
- [ ] T009 [P] Create `BitPantry.CommandLine.Tests/Groups/GroupParsingTests.cs` - test GroupPath extraction from space-separated input (expect compile failure initially)

### Implementation for Foundational

- [ ] T010 Add group tracking to `BitPantry.CommandLine/CommandRegistry.cs` - add `_groups` list, `Groups`, `RootGroups`, `RootCommands` properties
- [ ] T011 Implement `RegisterGroup(Type markerType)` method in `BitPantry.CommandLine/CommandRegistry.cs` (make T008 pass)
- [ ] T012 Implement `FindGroup(string name, GroupInfo parent)` method in `BitPantry.CommandLine/CommandRegistry.cs`
- [ ] T013 Implement `FindCommand(string name, GroupInfo group)` method in `BitPantry.CommandLine/CommandRegistry.cs`
- [ ] T014 Update assembly scanning in `BitPantry.CommandLine/CommandRegistryApplicationBuilder.cs` to discover and register `[Group]` classes
- [ ] T015 Add `ResolvedType` enum (Command, Group) to `BitPantry.CommandLine/Processing/Resolution/ResolvedCommand.cs`
- [ ] T016 Add `GroupInfo` property to `BitPantry.CommandLine/Processing/Resolution/ResolvedCommand.cs`
- [ ] T017 Update `CommandResolver.Resolve()` in `BitPantry.CommandLine/Processing/Resolution/CommandResolver.cs` to handle group-aware resolution
- [ ] T018 Add `GroupPath` property to `BitPantry.CommandLine/Processing/Parsing/ParsedCommand.cs` (make T009 pass)
- [ ] T019 Update parsing in `BitPantry.CommandLine/Processing/Parsing/ParsedInput.cs` to handle space-separated group paths
- [ ] T020 Change `ReplaceDuplicateCommands` default to `false` in `BitPantry.CommandLine/CommandRegistry.cs`

**Checkpoint**: Foundation ready - registry can track groups, resolver can navigate group hierarchy

---

## Phase 3: User Story 1 - Define and Invoke Grouped Commands (Priority: P1) 🎯 MVP

**Goal**: Commands can be organized into groups using `[Group]` marker classes and invoked with space-separated syntax

**Independent Test**: Create a Group class and Command class, invoke with `group command` syntax

### Tests for User Story 1

- [ ] T021 [P] [US1] Add integration tests to `BitPantry.CommandLine.Tests/Groups/GroupRegistrationTests.cs` - test group discovery and registration from assembly
- [ ] T022 [P] [US1] Add integration tests to `BitPantry.CommandLine.Tests/Groups/GroupResolutionTests.cs` - test resolving `group command` to CommandInfo
- [ ] T023 [US1] Create `BitPantry.CommandLine.Tests/Groups/GroupInvocationTests.cs` - end-to-end test of command execution via group path

### Implementation for User Story 1

- [ ] T024 [US1] Create test group classes in `BitPantry.CommandLine.Tests/TestCommands/Groups/` - Math group with Add, Subtract commands
- [ ] T025 [US1] Implement group name derivation (class name → lowercase, matching existing CommandAttribute behavior) in `GroupInfo` constructor
- [ ] T026 [US1] Wire up command-to-group association during registration in `CommandRegistry`
- [ ] T027 [US1] Verify dot-notation (`math.add`) is NOT recognized - add test case in `GroupResolutionTests.cs`

**Checkpoint**: User Story 1 complete - grouped commands work with space-separated syntax

---

## Phase 4: User Story 2 - Group Discoverability (Priority: P1)

**Goal**: Typing a group name alone displays available commands and subgroups

**Independent Test**: Invoke `myapp math` and verify help output listing commands

### Tests for User Story 2

- [ ] T028 [P] [US2] Create `BitPantry.CommandLine.Tests/Help/HelpFormatterTests.cs` - test group help output format, command help format, root help format (FR-020)
- [ ] T029 [P] [US2] Create `BitPantry.CommandLine.Tests/Help/HelpInterceptionTests.cs` - test: (1) `--help`/`-h` flag detection, (2) FR-018a: help must be standalone - `cmd -f val --help` returns error, (3) FR-018b: error message format "error: --help cannot be combined with other arguments\nFor usage, run: cmd --help", (4) pipeline with help returns same error

### Implementation for User Story 2

- [ ] T030 [US2] Create `BitPantry.CommandLine/Help/IHelpFormatter.cs` interface with `DisplayGroupHelp`, `DisplayCommandHelp`, `DisplayRootHelp` methods
- [ ] T031 [US2] Implement `BitPantry.CommandLine/Help/HelpFormatter.cs` using Spectre.Console for rich formatting
- [ ] T032 [US2] Implement `CheckHelpRequest()` in `BitPantry.CommandLine/Processing/Execution/CommandLineApplicationCore.cs` - validate help is standalone
- [ ] T033 [US2] Add help interception between resolution and execution in `CommandLineApplicationCore.Run()`
- [ ] T034 [US2] Implement group-only resolution (when user types `group` alone) in `CommandResolver`
- [ ] T035 [US2] Register `IHelpFormatter` in DI container in `BitPantry.CommandLine/ServiceCollectionExtensions.cs`
- [ ] T036 [US2] Test that `myapp group --help` produces same output as `myapp group` alone
- [ ] T037 [US2] Test that `myapp` or `myapp --help` displays root help (FR-020)

**Checkpoint**: User Story 2 complete - invoking group alone shows discoverable help

---

## Phase 5: User Story 3 - Nested Groups via C# Class Nesting (Priority: P2)

**Goal**: Nested groups work via C# nested classes (`Files.Io` → `files io`)

**Independent Test**: Create nested group classes, invoke `myapp files io upload`

### Tests for User Story 3

- [ ] T038 [P] [US3] Add nested group test cases to `BitPantry.CommandLine.Tests/Groups/GroupRegistrationTests.cs`
- [ ] T039 [P] [US3] Add multi-level path resolution tests to `BitPantry.CommandLine.Tests/Groups/GroupResolutionTests.cs`

### Implementation for User Story 3

- [ ] T040 [US3] Create test nested group classes in `BitPantry.CommandLine.Tests/TestCommands/Groups/` - Files.Io with Upload command
- [ ] T041 [US3] Update group discovery to detect nested classes with `[Group]` attribute in `CommandRegistryApplicationBuilder`
- [ ] T042 [US3] Establish parent-child relationships during registration based on C# nesting
- [ ] T043 [US3] Update `GroupInfo.FullPath` to handle multi-level paths (`files io`)
- [ ] T044 [US3] Test `myapp files` shows subgroup `io`, test `myapp files io` shows `upload` command

**Checkpoint**: User Story 3 complete - nested groups work with multi-word paths

---

## Phase 6: User Story 4 - Startup Validation (Priority: P2)

**Goal**: Configuration errors caught at startup with clear messages

**Independent Test**: Register invalid configuration, verify startup fails with descriptive error

### Tests for User Story 4

- [ ] T045 [P] [US4] Create `BitPantry.CommandLine.Tests/Groups/GroupValidationTests.cs` - test: (1) empty group error (FR-022: no commands AND no subgroups), (2) name collision error, (3) duplicate command error, (4) FR-027: argument named `help` or alias `h` causes startup failure

### Implementation for User Story 4

- [ ] T046 [US4] Implement empty group validation in `CommandRegistry.Build()` - error if group has no commands AND no subgroups (FR-022)
- [ ] T047 [US4] Implement name collision detection (command/group same name at same level) in `CommandRegistry.Build()`
- [ ] T048 [US4] Implement reserved name validation - arguments named `help` or with alias `h` must cause startup failure (FR-027)
- [ ] T049 [US4] Verify `ReplaceDuplicateCommands = false` default is applied correctly (set in T020)
- [ ] T050 [US4] Ensure error messages identify built-in commands when collision occurs

**Checkpoint**: User Story 4 complete - invalid configurations fail fast with clear errors

---

## Phase 7: User Story 5 - Case Sensitivity Configuration (Priority: P3)

**Goal**: Configurable case sensitivity for command/group matching

**Independent Test**: Configure case-insensitive, verify `Math Add` resolves correctly

### Tests for User Story 5

- [ ] T051 [P] [US5] Add case sensitivity test cases to `BitPantry.CommandLine.Tests/Groups/GroupResolutionTests.cs`

### Implementation for User Story 5

- [ ] T052 [US5] Add `CaseSensitive` option to configuration (default `true`)
- [ ] T053 [US5] Update `FindGroup` and `FindCommand` to respect case sensitivity setting
- [ ] T054 [US5] Test case-sensitive (default): `Math Add` → not found
- [ ] T055 [US5] Test case-insensitive: `Math Add` → resolves to `math add`

**Checkpoint**: User Story 5 complete - case sensitivity is configurable

---

## Phase 8: User Story 6 - Built-in Command Override Control (Priority: P3)

**Goal**: Control whether custom commands can override built-ins

**Independent Test**: Register command named `help` with different settings, verify behavior

### Tests for User Story 6

- [ ] T056 [P] [US6] Add built-in override test cases to `BitPantry.CommandLine.Tests/Groups/GroupValidationTests.cs`

### Implementation for User Story 6

- [ ] T057 [US6] Update duplicate detection to identify built-in commands in error messages
- [ ] T058 [US6] Test `ReplaceDuplicateCommands = false` (default) with built-in conflict → startup error
- [ ] T059 [US6] Test `ReplaceDuplicateCommands = true` with built-in conflict → custom replaces built-in

**Checkpoint**: User Story 6 complete - built-in override behavior is controllable

---

## Phase 9: Migration & Cleanup

**Purpose**: Update existing code, tests, and built-in commands

- [ ] T060 Update `BitPantry.CommandLine/Commands/ListCommandsCommand.cs` to display groups instead of namespaces
- [ ] T061 [P] Remove all namespace-related helper methods from `BitPantry.CommandLine/Component/CommandInfo.cs`
- [ ] T062 [P] Remove `ValidateNamespace()` method if present
- [ ] T063 Update `BitPantry.CommandLine.Tests/DescribeCommandsTests.cs` to use group-based test commands
- [ ] T064 [P] Update `BitPantry.CommandLine.Tests/ResolveCommandsTests.cs` to use group-based test commands
- [ ] T065 [P] Update `BitPantry.CommandLine.Tests/ActivateCommandsTests.cs` to use group-based test commands
- [ ] T066 [P] Update `BitPantry.CommandLine.Tests/AutoCompleteTests.cs` for space-separated syntax
- [ ] T067 Search for and remove any remaining `Namespace` references in codebase
- [ ] T068 Run full test suite and fix any remaining failures

**Checkpoint**: All existing tests pass with group-based model

---

## Phase 10: Polish & Documentation

**Purpose**: Documentation updates and final validation

- [ ] T069 [P] Update `README.md` with group-based examples
- [ ] T070 [P] Update `Docs/getting-started.md` with group quick start
- [ ] T071 [P] Update `Docs/syntax.md` with space-separated syntax
- [ ] T072 [P] Update `Docs/advanced-topics.md` with nested group patterns
- [ ] T073 Update all XML documentation in source files for new properties/methods
- [ ] T074 Validate no documentation references to namespaces remain
- [ ] T075 Run quickstart.md scenarios as validation tests

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
- T002 and T003 can run parallel with T001

**Within Phase 2:**
- Limited parallelism - tasks are sequential due to dependencies

**User Story Tests (all [P] marked):**
- T017, T018, T019, T020 can run in parallel
- T026, T027, T028 can run in parallel

**Phase 9 Migration:**
- T059, T060, T062, T063, T064 can run in parallel

**Phase 10 Documentation:**
- T067, T068, T069, T070 can run in parallel

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
| US1 (P1) | 7 | 2 |
| US2 (P1) | 10 | 2 |
| US3 (P2) | 7 | 2 |
| US4 (P2) | 6 | 1 |
| US5 (P3) | 5 | 1 |
| US6 (P3) | 4 | 1 |
| Migration | 9 | 4 |
| Documentation | 7 | 4 |
| **Total** | **75** | **21** |

**MVP Scope**: Phases 1-4 (37 tasks) - delivers grouped commands with discoverability

**TDD Compliance**: All phases now have tests written BEFORE implementation per constitution.
