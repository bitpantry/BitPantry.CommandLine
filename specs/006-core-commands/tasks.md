# Tasks: Core CLI Commands & Prompt Redesign

**Input**: Design documents from `/specs/006-core-commands/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Following Constitution principle I (TDD), tests are written FIRST before implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## User Story Mapping

| Story | Priority | Title | Dependencies |
|-------|----------|-------|--------------|
| US1 | P1 | Display Application Version | None |
| US2 | P2 | Display Full Version Information | US1 |
| US3 | P1 | Remove Obsolete ListCommands | US1 (for test migration) |
| US4 | P1 | Connect to Remote Server | US9 (prompt), US7 (profiles) |
| US5 | P1 | Disconnect from Remote Server | US9 (prompt) |
| US6 | P2 | Check Server Connection Status | US4, US5 |
| US7 | P2 | Manage Server Profiles | Foundational (ProfileManager, CredentialStore) |
| US8 | P2 | Autocomplete for Profile Names | US7 |
| US9 | P1 | Extensible Prompt System | None |
| US10 | P2 | Built-in Command Documentation | All implementation complete |

---

## Phase 1: Setup

**Purpose**: Add new dependencies and prepare project structure

- [ ] T001 Add `Sodium.Core` NuGet package to BitPantry.CommandLine.Remote.SignalR.Client.csproj
- [ ] T002 [P] Add `System.Security.Cryptography.ProtectedData` NuGet package reference to BitPantry.CommandLine.Remote.SignalR.Client.csproj
- [ ] T003 [P] Create directory structure: BitPantry.CommandLine.Remote.SignalR.Client/Profiles/
- [ ] T004 [P] Create directory structure: BitPantry.CommandLine.Remote.SignalR.Client/Prompt/
- [ ] T005 [P] Create directory structure: BitPantry.CommandLine.Remote.SignalR.Client/AutoComplete/
- [ ] T006 [P] Create directory structure: BitPantry.CommandLine.Tests/Input/ (if not exists, for core prompt tests)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before user story implementation

**⚠️ CRITICAL**: Stories US4, US5, US7, US8 depend on this phase completing

### Tests for Foundational Components

- [ ] T007 [P] Write unit tests for ICredentialStore in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/CredentialStoreTests.cs
- [ ] T008 [P] Write unit tests for ProfileManager in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/ProfileManagerTests.cs

### Foundational Implementation

- [ ] T009 [P] Create ServerProfile entity class in BitPantry.CommandLine.Remote.SignalR.Client/Profiles/ServerProfile.cs
- [ ] T010 [P] Create ProfileConfiguration entity class in BitPantry.CommandLine.Remote.SignalR.Client/Profiles/ProfileConfiguration.cs
- [ ] T011 Create ICredentialStore interface in BitPantry.CommandLine.Remote.SignalR.Client/Profiles/ICredentialStore.cs
- [ ] T012 Implement CredentialStore class in BitPantry.CommandLine.Remote.SignalR.Client/Profiles/CredentialStore.cs
- [ ] T013 Create CredentialStoreException class in BitPantry.CommandLine.Remote.SignalR.Client/Profiles/CredentialStoreException.cs
- [ ] T014 Create IProfileManager interface in BitPantry.CommandLine.Remote.SignalR.Client/Profiles/IProfileManager.cs
- [ ] T015 Implement ProfileManager class in BitPantry.CommandLine.Remote.SignalR.Client/Profiles/ProfileManager.cs
- [ ] T016 Create ProfileGroup class in BitPantry.CommandLine.Remote.SignalR.Client/ProfileGroup.cs
- [ ] T017 Run foundational tests to verify ProfileManager and CredentialStore work correctly

**Checkpoint**: ProfileManager and CredentialStore ready - profile-dependent stories can now proceed

---

## Phase 3: User Story 9 - Extensible Prompt System (Priority: P1) 🎯 MVP

**Goal**: Replace manual prompt management with composable segment-based architecture

**Independent Test**: Register segment combinations and verify prompt output renders correctly

### Tests for User Story 9

- [ ] T018 [P] [US9] Write unit tests for CompositePrompt in BitPantry.CommandLine.Tests/Input/CompositePromptTests.cs
- [ ] T019 [P] [US9] Write unit tests for AppNameSegment in BitPantry.CommandLine.Tests/Input/SegmentTests.cs

### Implementation for User Story 9

- [ ] T020 [P] [US9] Create IPromptSegment interface in BitPantry.CommandLine/Input/IPromptSegment.cs
- [ ] T021 [P] [US9] Create IPrompt interface in BitPantry.CommandLine/Input/IPrompt.cs
- [ ] T022 [US9] Implement CompositePrompt class in BitPantry.CommandLine/Input/CompositePrompt.cs
- [ ] T023 [US9] Implement AppNameSegment class in BitPantry.CommandLine/Input/AppNameSegment.cs
- [ ] T024 [US9] Register IPrompt and AppNameSegment in DI in BitPantry.CommandLine/CommandLineApplicationBuilder.cs
- [ ] T025 [US9] Update REPL to use IPrompt instead of Prompt class in BitPantry.CommandLine/CommandLineApplication.cs
- [ ] T026 [P] [US9] Create ServerConnectionSegment class in BitPantry.CommandLine.Remote.SignalR.Client/Prompt/ServerConnectionSegment.cs
- [ ] T027 [P] [US9] Create ProfileSegment class in BitPantry.CommandLine.Remote.SignalR.Client/Prompt/ProfileSegment.cs
- [ ] T028 [US9] Register SignalR Client prompt segments in DI in BitPantry.CommandLine.Remote.SignalR.Client/CommandLineApplicationBuilderExtensions.cs
- [ ] T029 [US9] Run US9 tests to verify prompt system works correctly

**Checkpoint**: Prompt system complete - commands can update prompt via segment state

---

## Phase 4: User Story 1 - Display Application Version (Priority: P1)

**Goal**: Add `version` command to display application version

**Independent Test**: Run `version` command and verify output matches assembly version

### Tests for User Story 1

- [ ] T030 [P] [US1] Write unit tests for VersionCommand in BitPantry.CommandLine.Tests/Commands/VersionCommandTests.cs

### Implementation for User Story 1

- [ ] T031 [US1] Implement VersionCommand class in BitPantry.CommandLine/Commands/VersionCommand.cs
- [ ] T032 [US1] Register VersionCommand in CommandLineApplicationBuilder in BitPantry.CommandLine/CommandLineApplicationBuilder.cs
- [ ] T033 [US1] Run US1 tests to verify version command works correctly

**Checkpoint**: `version` command available

---

## Phase 5: User Story 3 - Remove Obsolete ListCommands (Priority: P1)

**Goal**: Remove `lc` command and migrate tests to use `version`

**Independent Test**: Verify `lc` is no longer recognized and no ListCommandsCommand exists

### Implementation for User Story 3

- [ ] T034 [US3] Delete ListCommandsCommand.cs from BitPantry.CommandLine/Commands/ListCommandsCommand.cs
- [ ] T035 [US3] Remove ListCommandsCommand registration from BitPantry.CommandLine/CommandLineApplicationBuilder.cs
- [ ] T036 [US3] Search for and update any tests using `lc` command to use `version` instead
- [ ] T037 [US3] Run all tests to verify no regressions from ListCommands removal

**Checkpoint**: `lc` command removed, `version` command is the new default test command

---

## Phase 6: User Story 2 - Display Full Version Information (Priority: P2)

**Goal**: Add `--full` flag to version command for framework assembly info

**Independent Test**: Run `version --full` and verify framework assemblies are listed

### Tests for User Story 2

- [ ] T038 [P] [US2] Add tests for --full flag in BitPantry.CommandLine.Tests/Commands/VersionCommandTests.cs

### Implementation for User Story 2

- [ ] T039 [US2] Add framework assembly discovery to VersionCommand in BitPantry.CommandLine/Commands/VersionCommand.cs
- [ ] T040 [US2] Run US2 tests to verify --full flag works correctly

**Checkpoint**: `version --full` shows framework assembly versions

---

## Phase 7: User Story 7 - Manage Server Profiles (Priority: P2)

**Goal**: Implement all profile management commands

**Independent Test**: Run profile CRUD commands and verify profiles are stored/retrieved correctly

### Tests for User Story 7

- [ ] T041 [P] [US7] Write integration tests for profile commands in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Profiles.cs

### Implementation for User Story 7

- [ ] T042 [P] [US7] Implement ProfileListCommand in BitPantry.CommandLine.Remote.SignalR.Client/Commands/ProfileListCommand.cs
- [ ] T043 [P] [US7] Implement ProfileAddCommand in BitPantry.CommandLine.Remote.SignalR.Client/Commands/ProfileAddCommand.cs
- [ ] T044 [P] [US7] Implement ProfileRemoveCommand in BitPantry.CommandLine.Remote.SignalR.Client/Commands/ProfileRemoveCommand.cs
- [ ] T045 [P] [US7] Implement ProfileShowCommand in BitPantry.CommandLine.Remote.SignalR.Client/Commands/ProfileShowCommand.cs
- [ ] T046 [P] [US7] Implement ProfileSetDefaultCommand in BitPantry.CommandLine.Remote.SignalR.Client/Commands/ProfileSetDefaultCommand.cs
- [ ] T047 [P] [US7] Implement ProfileSetKeyCommand in BitPantry.CommandLine.Remote.SignalR.Client/Commands/ProfileSetKeyCommand.cs
- [ ] T048 [US7] Register all profile commands in DI in BitPantry.CommandLine.Remote.SignalR.Client/CommandLineApplicationBuilderExtensions.cs
- [ ] T049 [US7] Run US7 tests to verify profile management works correctly

**Checkpoint**: All `server profile` subcommands work

---

## Phase 8: User Story 8 - Autocomplete for Profile Names (Priority: P2)

**Goal**: Add autocomplete suggestions for profile name arguments

**Independent Test**: Trigger autocomplete in REPL for profile arguments and verify suggestions

### Tests for User Story 8

- [ ] T050 [P] [US8] Write unit tests for ProfileNameProvider in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/ProfileNameProviderTests.cs

### Implementation for User Story 8

- [ ] T051 [US8] Implement ProfileNameProvider in BitPantry.CommandLine.Remote.SignalR.Client/AutoComplete/ProfileNameProvider.cs
- [ ] T052 [US8] Add [Completion(Provider = typeof(ProfileNameProvider))] to all profile name arguments in profile commands
- [ ] T053 [US8] Register ProfileNameProvider in DI in BitPantry.CommandLine.Remote.SignalR.Client/CommandLineApplicationBuilderExtensions.cs
- [ ] T054 [US8] Run US8 tests to verify autocomplete works correctly

**Checkpoint**: Profile names autocomplete in REPL

---

## Phase 9: User Story 4 - Connect to Remote Server (Priority: P1)

**Goal**: Redesign server connect command with profile support and timeout

**Independent Test**: Run `server connect` with various arguments and verify connection state

### Tests for User Story 4

- [ ] T055 [P] [US4] Write integration tests for connect command in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Connect.cs

### Implementation for User Story 4

- [ ] T056 [US4] Delete existing ConnectCommand.cs from BitPantry.CommandLine.Remote.SignalR.Client/ConnectCommand.cs
- [ ] T057 [US4] Implement redesigned ConnectCommand in BitPantry.CommandLine.Remote.SignalR.Client/ConnectCommand.cs
- [ ] T058 [US4] Add profile support to ConnectCommand (--profile, --api-key, --timeout) with ProfileNameProvider autocomplete on --profile
- [ ] T059 [US4] Add auto-disconnect behavior when switching servers
- [ ] T060 [US4] Update ClientLogic to work with new prompt segments in BitPantry.CommandLine.Remote.SignalR.Client/ClientLogic.cs
- [ ] T061 [US4] Run US4 tests to verify connect command works correctly

**Checkpoint**: `server connect` works with profiles, timeout, and auto-disconnect

---

## Phase 10: User Story 5 - Disconnect from Remote Server (Priority: P1)

**Goal**: Redesign server disconnect command

**Independent Test**: Connect then disconnect, verify state changes

### Tests for User Story 5

- [ ] T062 [P] [US5] Write integration tests for disconnect command in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Disconnect.cs

### Implementation for User Story 5

- [ ] T063 [US5] Delete existing DisconnectCommand.cs from BitPantry.CommandLine.Remote.SignalR.Client/DisconnectCommand.cs
- [ ] T064 [US5] Implement redesigned DisconnectCommand in BitPantry.CommandLine.Remote.SignalR.Client/DisconnectCommand.cs
- [ ] T065 [US5] Ensure prompt reverts on disconnect via segment state
- [ ] T066 [US5] Run US5 tests to verify disconnect command works correctly

**Checkpoint**: `server disconnect` works with new prompt system

---

## Phase 11: User Story 6 - Check Server Connection Status (Priority: P2)

**Goal**: Add server status command with JSON output

**Independent Test**: Run `server status` in connected and disconnected states

### Tests for User Story 6

- [ ] T067 [P] [US6] Write integration tests for status command in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Status.cs

### Implementation for User Story 6

- [ ] T068 [US6] Create ConnectionStatus model in BitPantry.CommandLine.Remote.SignalR.Client/ConnectionStatus.cs
- [ ] T069 [US6] Implement StatusCommand in BitPantry.CommandLine.Remote.SignalR.Client/StatusCommand.cs
- [ ] T070 [US6] Add --json flag support with JSON serialization
- [ ] T071 [US6] Add exit code 1 when not connected for scripting
- [ ] T072 [US6] Register StatusCommand in DI in BitPantry.CommandLine.Remote.SignalR.Client/CommandLineApplicationBuilderExtensions.cs
- [ ] T073 [US6] Run US6 tests to verify status command works correctly

**Checkpoint**: `server status` shows connection info with JSON option

---

## Phase 12: User Story 10 - Built-in Command Documentation (Priority: P2)

**Goal**: Update documentation for all built-in commands organized by package

**Independent Test**: Review documentation for completeness against spec

### Implementation for User Story 10

- [ ] T074 [P] [US10] Update BuiltInCommands.md to remove `lc` documentation in Docs/CommandLine/BuiltInCommands.md (depends: T034)
- [ ] T075 [P] [US10] Add `version` command documentation to Docs/CommandLine/BuiltInCommands.md (depends: T031)
- [ ] T076 [P] [US10] Create ServerCommands.md for server connect/disconnect/status in Docs/Remote/ServerCommands.md (depends: T057, T064, T069)
- [ ] T077 [P] [US10] Create ProfileManagement.md for server profile commands in Docs/Remote/ProfileManagement.md (depends: T042-T047)
- [ ] T078 [US10] Add built-in commands index organized by package in Docs/CommandLine/BuiltInCommands.md
- [ ] T079 [US10] Verify all command documentation matches spec.md command reference

**Checkpoint**: Documentation complete for all built-in commands

---

## Phase 13: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup and validation

- [ ] T080 Run all unit tests to verify no regressions
- [ ] T081 Run all integration tests to verify end-to-end functionality
- [ ] T082 [P] Remove old Prompt class in BitPantry.CommandLine/Input/Prompt.cs
- [ ] T083 [P] Code cleanup: remove any TODO comments from existing ConnectCommand/DisconnectCommand
- [ ] T084 Verify quickstart.md scenarios work end-to-end
- [ ] T085 Update CLAUDE.md or agent context if needed
- [ ] T086 Final build and test run before PR

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup
    ↓
Phase 2: Foundational (ProfileManager, CredentialStore)
    ↓
    ├── Phase 3: US9 Prompt System (no story dependencies)
    │       ↓
    │   ├── Phase 4: US1 Version (needs US9 for builder registration pattern)
    │   │       ↓
    │   │   Phase 5: US3 Remove ListCommands (needs US1 for test migration)
    │   │       ↓
    │   │   Phase 6: US2 Version --full (extends US1)
    │   │
    │   ├── Phase 7: US7 Profile Commands (needs Foundational)
    │   │       ↓
    │   │   Phase 8: US8 Profile Autocomplete (needs US7)
    │   │       ↓
    │   │   ├── Phase 9: US4 Connect (needs US7, US8, US9)
    │   │   │       ↓
    │   │   │   Phase 10: US5 Disconnect (needs US9)
    │   │   │       ↓
    │   │   │   Phase 11: US6 Status (needs US4, US5)
    │   │   │
    │   └───────────────────────────────────────────────┘
    │
Phase 12: US10 Documentation (needs all implementation complete)
    ↓
Phase 13: Polish
```

### Parallel Opportunities

- **Phase 1**: T002, T003, T004, T005, T006 can all run in parallel
- **Phase 2**: T007, T008 (tests), T009, T010 can run in parallel
- **Phase 3**: T018, T019 (tests), T020, T021 can run in parallel
- **Phase 7**: T042-T047 (profile commands) can all run in parallel
- **Phase 12**: T074, T075, T076, T077 (docs) can all run in parallel

---

## Parallel Example: Phase 7 Profile Commands

```bash
# All profile command implementations can run in parallel:
Task T042: ProfileListCommand
Task T043: ProfileAddCommand
Task T044: ProfileRemoveCommand
Task T045: ProfileShowCommand
Task T046: ProfileSetDefaultCommand
Task T047: ProfileSetKeyCommand
```

---

## Implementation Strategy

### MVP First (US1 + US9 + US3)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (optional for MVP, but needed for profiles)
3. Complete Phase 3: US9 Prompt System
4. Complete Phase 4: US1 Version Command
5. Complete Phase 5: US3 Remove ListCommands
6. **STOP and VALIDATE**: Test basic version command works
7. Deploy/demo if ready

### Incremental Delivery

1. **MVP**: Setup + Foundational + US9 + US1 + US3 → Version command works
2. **+US2**: Add --full flag → Full version info
3. **+US7**: Add profile management → Profiles can be saved
4. **+US8**: Add autocomplete → Better UX for profiles
5. **+US4+US5**: Redesigned connect/disconnect → Full connection flow
6. **+US6**: Add status command → Connection visibility
7. **+US10**: Documentation → Developer adoption

---

## Summary

| Metric | Count |
|--------|-------|
| Total Tasks | 86 |
| Setup Tasks | 6 |
| Foundational Tasks | 11 |
| US1 Tasks | 4 |
| US2 Tasks | 3 |
| US3 Tasks | 4 |
| US4 Tasks | 7 |
| US5 Tasks | 5 |
| US6 Tasks | 7 |
| US7 Tasks | 9 |
| US8 Tasks | 5 |
| US9 Tasks | 12 |
| US10 Tasks | 6 |
| Polish Tasks | 7 |
| Parallel Opportunities | 35 tasks marked [P] |

**Suggested MVP Scope**: US9 (Prompt) + US1 (Version) + US3 (Remove lc) = 20 tasks
