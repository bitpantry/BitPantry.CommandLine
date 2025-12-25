# Tasks: Autocomplete Redesign

**Input**: Design documents from `/specs/005-autocomplete-redesign/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Test tasks included - spec.md defines 94+ test scenarios requiring comprehensive test coverage.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create folder structure and delete legacy autocomplete code

- [x] T001 Delete legacy autocomplete files: `AutoCompleteController.cs`, `AutoCompleteOption.cs`, `AutoCompleteOptionSet.cs`, `AutoCompleteOptionSetBuilder.cs`, `AutoCompleteContext.cs` in `BitPantry.CommandLine/AutoComplete/`
- [x] T002 [P] Create folder structure: `AutoComplete/Providers/`, `AutoComplete/Attributes/`, `AutoComplete/Cache/` in `BitPantry.CommandLine/`
- [x] T003 [P] Create test folder structure: `AutoComplete/Orchestrator/`, `AutoComplete/Ghost/`, `AutoComplete/Providers/`, `AutoComplete/Cache/`, `AutoComplete/Integration/` in `BitPantry.CommandLine.Tests/`
- [x] T004 [P] Remove `AutoCompleteFunctionName` property from `ArgumentAttribute` in `BitPantry.CommandLine/API/ArgumentAttribute.cs`

---

## Phase 2: Foundational (Core Types & Interfaces)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Core Entities

- [x] T005 [P] Create `CompletionContext` class in `BitPantry.CommandLine/AutoComplete/CompletionContext.cs` (per data-model.md)
- [x] T006 [P] Create `CompletionElementType` enum in `BitPantry.CommandLine/AutoComplete/CompletionElementType.cs`
- [x] T007 [P] Create `CompletionItem` class in `BitPantry.CommandLine/AutoComplete/CompletionItem.cs` (per data-model.md)
- [x] T008 [P] Create `CompletionItemKind` enum in `BitPantry.CommandLine/AutoComplete/CompletionItemKind.cs`
- [x] T009 [P] Create `CompletionResult` class in `BitPantry.CommandLine/AutoComplete/CompletionResult.cs` (per data-model.md)

### Core Interfaces

- [x] T010 [P] Create `ICompletionProvider` interface in `BitPantry.CommandLine/AutoComplete/Providers/ICompletionProvider.cs` (per contracts/ICompletionProvider.md)
- [x] T011 [P] Create `ICompletionOrchestrator` interface in `BitPantry.CommandLine/AutoComplete/ICompletionOrchestrator.cs` (per contracts/ICompletionOrchestrator.md)
- [x] T012 [P] Create `CompletionAction` and `CompletionActionType` in `BitPantry.CommandLine/AutoComplete/CompletionAction.cs`

### Attribute System

- [x] T013 [P] Create `CompletionAttribute` base class in `BitPantry.CommandLine/AutoComplete/Attributes/CompletionAttribute.cs` (3 constructor overloads: single string = method, 2+ strings = values, Type = provider)
- [x] T014 [P] Create `FilePathCompletionAttribute` in `BitPantry.CommandLine/AutoComplete/Attributes/FilePathCompletionAttribute.cs`
- [x] T015 [P] Create `DirectoryPathCompletionAttribute` in `BitPantry.CommandLine/AutoComplete/Attributes/DirectoryPathCompletionAttribute.cs`

### Cache Infrastructure

- [x] T016 [P] Create `CacheKey` record in `BitPantry.CommandLine/AutoComplete/Cache/CacheKey.cs`
- [x] T017 Create `CompletionCache` class in `BitPantry.CommandLine/AutoComplete/Cache/CompletionCache.cs` (100-item LRU, 5-min TTL)

### UI State

- [x] T018 [P] Create `MenuState` class in `BitPantry.CommandLine/AutoComplete/MenuState.cs` (per data-model.md)
- [x] T019 [P] Create `GhostState` class in `BitPantry.CommandLine/AutoComplete/GhostState.cs`
- [x] T020 [P] Create `MatchResult` and `MatchMode` in `BitPantry.CommandLine/AutoComplete/MatchResult.cs`

### DI Registration

- [x] T021 Register autocomplete services in `BitPantry.CommandLine/ServiceCollectionExtensions.cs` (ICompletionOrchestrator, ICompletionProvider implementations, CompletionCache)

### Foundational Tests

- [x] T022 [P] Create `CompletionCacheTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Cache/` - test LRU eviction, TTL expiry, cache invalidation
- [x] T023 [P] Create `CompletionAttributeTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Attributes/` - test constructor disambiguation (CD-001 to CD-005)

**Checkpoint**: Foundation ready - user story implementation can now begin ✅

---

## Phase 3: User Story 1 - Tab Completion with Visible Feedback (Priority: P1) 🎯 MVP

**Goal**: Users can press Tab to see a completion menu, navigate with arrows, select with Enter, cancel with Escape

**Independent Test**: Type partial command, press Tab, observe menu, navigate, accept selection

### Tests for User Story 1

- [x] T024 [P] [US1] Create `OrchestratorTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Orchestrator/` - test MC-001 to MC-012 (basic menu behavior)
- [x] T025 [P] [US1] Create `MenuViewportTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Orchestrator/` - test MC-020 to MC-023 (scrolling)
- [x] T026 [P] [US1] Create `MenuDescriptionTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Orchestrator/` - test MC-030 to MC-032
- [x] T026a [P] [US1] Create `CommandProviderTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/` - test command name/group completion
- [x] T026b [P] [US1] Create `OrchestratorIntegrationTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Orchestrator/` - integration with real console input

### Implementation for User Story 1

- [x] T027 [US1] Create `CommandCompletionProvider` in `BitPantry.CommandLine/AutoComplete/Providers/CommandCompletionProvider.cs` - completes command names and groups
- [x] T028 [US1] Create `CompletionMatcher` in `BitPantry.CommandLine/AutoComplete/CompletionMatcher.cs` - prefix/contains/fuzzy matching with highlighting
- [x] T029 [US1] Create `CompletionOrchestrator` in `BitPantry.CommandLine/AutoComplete/CompletionOrchestrator.cs` - uses Spectre.Console SelectionPrompt, implements HandleTabAsync, HandleEscape, HandleEnter
- [x] T030 [US1] Integrate orchestrator with `InputController` in `BitPantry.CommandLine/Input/InputController.cs` - wire Tab key handler (Note: Integration is via AutoCompleteController bridging to ICompletionOrchestrator)
- [x] T031 [US1] Implement real-time filtering in `CompletionOrchestrator` - HandleCharacterAsync updates menu

**Checkpoint**: User Story 1 complete - Tab completion with visible feedback implemented ✅

---

## Phase 4: User Story 2 - Inline Ghost Suggestions While Typing (Priority: P2)

**Goal**: Ghost text appears showing best match, accepted with Right Arrow

**Independent Test**: Type characters, observe ghost text, press Right Arrow to accept

### Tests for User Story 2

- [ ] T032 [P] [US2] Create `GhostDisplayTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Ghost/` - test GS-001 to GS-008 (basic ghost behavior)
- [ ] T033 [P] [US2] Create `GhostSourceTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Ghost/` - test GS-010 to GS-012 (history priority)
- [ ] T034 [P] [US2] Create `GhostMenuInteractionTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Ghost/` - test GS-020 to GS-022
- [ ] T034a [P] [US2] Create `GhostAcceptTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Ghost/` - test GS-002, GS-003 acceptance scenarios

### Implementation for User Story 2

- [ ] T035 [US2] Create `GhostTextRenderer` in `BitPantry.CommandLine/AutoComplete/GhostTextRenderer.cs` - renders muted ANSI ghost text after cursor
- [ ] T036 [US2] Implement `UpdateGhostTextAsync` in `CompletionOrchestrator` - queries history then commands
- [ ] T037 [US2] Create `HistoryProvider` in `BitPantry.CommandLine/AutoComplete/Providers/HistoryProvider.cs` - gets suggestions from command history
- [ ] T038 [US2] Integrate ghost with `InputController` - show ghost after each keystroke, hide when menu open
- [ ] T039 [US2] Handle Right Arrow acceptance in `InputController` - accept ghost text on Right Arrow or End key

**Checkpoint**: User Story 2 complete - Ghost suggestions work independently

---

## Phase 5: User Story 3 - Argument Name and Alias Completion (Priority: P2)

**Goal**: Complete --argName and -a after command name, exclude already-used arguments

**Independent Test**: Type command, type "--", press Tab, see argument names

### Tests for User Story 3

- [ ] T040 [P] [US3] Create `ArgumentNameProviderTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/` - test AC-001 to AC-006
- [ ] T041 [P] [US3] Create `ArgumentAliasProviderTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/`

### Implementation for User Story 3

- [ ] T042 [US3] Create `ArgumentNameProvider` in `BitPantry.CommandLine/AutoComplete/Providers/ArgumentNameProvider.cs` - completes --argName, excludes used args
- [ ] T043 [US3] Create `ArgumentAliasProvider` in `BitPantry.CommandLine/AutoComplete/Providers/ArgumentAliasProvider.cs` - completes -a aliases
- [ ] T044 [US3] Update `CompletionOrchestrator` to detect "--" and "-" prefixes and route to correct provider
- [ ] T045 [US3] Create `UsedArgumentTracker` in `BitPantry.CommandLine/AutoComplete/UsedArgumentTracker.cs` - tracks which args already used in input

**Checkpoint**: User Story 3 complete - Argument name/alias completion works

---

## Phase 6: User Story 4 - Argument Value Completion (Priority: P2)

**Goal**: Complete values for arguments using providers or static values

**Independent Test**: Type command with argument, Tab on value position, see provider results

### Tests for User Story 4

- [ ] T046 [P] [US4] Create `FilePathProviderTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/` - test FP-001 to FP-007 using MockFileSystem
- [ ] T047 [P] [US4] Create `DirectoryPathProviderTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/` - test DP-001, DP-002
- [ ] T048 [P] [US4] Create `EnumProviderTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/` - test EP-001 to EP-007
- [ ] T049 [P] [US4] Create `StaticValuesProviderTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/` - test SV-001 to SV-004
- [ ] T049a [P] [US4] Create `ArgumentValueCompletionTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Orchestrator/` - test AC-010 to AC-014
- [ ] T049b [P] [US4] Create `PositionalArgCompletionTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Orchestrator/` - test AC-020 to AC-021

### Implementation for User Story 4

- [ ] T050 [US4] Create `FilePathProvider` in `BitPantry.CommandLine/AutoComplete/Providers/FilePathProvider.cs` - uses IFileSystem
- [ ] T051 [US4] Create `DirectoryPathProvider` in `BitPantry.CommandLine/AutoComplete/Providers/DirectoryPathProvider.cs`
- [ ] T052 [US4] Create `EnumProvider` in `BitPantry.CommandLine/AutoComplete/Providers/EnumProvider.cs` - auto-detects enum type from PropertyType
- [ ] T053 [US4] Create `StaticValuesProvider` in `BitPantry.CommandLine/AutoComplete/Providers/StaticValuesProvider.cs` - for [Completion("a","b")] values
- [ ] T054 [US4] Create `MethodProvider` in `BitPantry.CommandLine/AutoComplete/Providers/MethodProvider.cs` - invokes completion method with DI
- [ ] T055 [US4] Update `CompletionOrchestrator` to route value completions to correct provider based on CompletionAttribute

**Checkpoint**: User Story 4 complete - Argument value completion works

---

## Phase 7: User Story 5 - Command Implementer Adds Autocomplete (Priority: P2)

**Goal**: Developers can add autocomplete with minimal code using [Completion] attribute

**Independent Test**: Create command with [Completion] attribute, verify completions work

### Tests for User Story 5

- [ ] T056 [P] [US5] Create `MethodProviderTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/` - test MB-001 to MB-010
- [ ] T057 [P] [US5] Create `ProviderResolutionTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/` - test PR-001 to PR-006
- [ ] T058 [P] [US5] Create `ShortcutAttributeTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Attributes/` - test SA-001 to SA-005
- [ ] T058a [P] [US5] Create `CommandImplementerIntegrationTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Integration/` - test CI-001 to CI-013
- [ ] T058b [P] [US5] Create `PropertyTypeContextTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Providers/` - test PT-001 to PT-003

### Implementation for User Story 5

- [ ] T059 [US5] Implement method signature validation in `MethodProvider` - fail at startup for wrong signature (MB-003, MB-004)
- [ ] T060 [US5] Implement DI parameter injection in `MethodProvider` - inject services into completion method parameters
- [ ] T061 [US5] Add `CompletionAttribute` discovery to command registration - scan properties for [Completion] attributes
- [ ] T062 [US5] Document pattern in quickstart.md - update with final tested examples

**Checkpoint**: User Story 5 complete - Developers can easily add autocomplete

---

## Phase 8: User Story 6 - Remote Command Autocomplete with Loading (Priority: P3)

**Goal**: Remote completions show loading indicator, handle errors gracefully

**Independent Test**: Connect to remote server, Tab on remote command argument, see loading then results

### Tests for User Story 6

- [ ] T063 [P] [US6] Create `RemoteCompletionTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Integration/` - test RC-001 to RC-003
- [ ] T064 [P] [US6] Create `RemoteErrorTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Integration/` - test RC-010 to RC-015
- [ ] T065 [P] [US6] Create `RemoteAsyncTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Integration/` - test RC-020 to RC-023
- [ ] T065a [P] [US6] Create `RemoteCompletionIntegrationTests.cs` in `BitPantry.CommandLine.Tests.Remote.SignalR/AutoComplete/` - uses existing TestEnvironment for end-to-end SignalR tests

### Implementation for User Story 6

- [ ] T066 [US6] Create `RemoteCompletionProvider` in `BitPantry.CommandLine/AutoComplete/Providers/RemoteCompletionProvider.cs` - fetches from SignalR server
- [ ] T067 [US6] Implement loading indicator rendering in `CompletionOrchestrator` - show spinner/dots while fetching
- [ ] T068 [US6] Implement debouncing in `CompletionOrchestrator` - CancellationTokenSource reset pattern (100ms)
- [ ] T069 [US6] Implement error handling and "(offline)" indicator
- [ ] T070 [US6] Add SignalR method for completion requests on server side

**Checkpoint**: User Story 6 complete - Remote completion with loading/error handling works

---

## Phase 9: User Story 7 - Completion Result Caching (Priority: P3)

**Goal**: Cache results to improve performance, invalidate after command execution

**Independent Test**: Tab twice for same argument - second should be instant

### Tests for User Story 7

- [ ] T071 [P] [US7] Create `CachingIntegrationTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/Cache/` - test CA-001 to CA-006

### Implementation for User Story 7

- [ ] T072 [US7] Integrate `CompletionCache` with `CompletionOrchestrator` - check cache before invoking provider
- [ ] T073 [US7] Implement local filtering on cached results - filter locally when prefix changes
- [ ] T074 [US7] Add cache invalidation hook after command execution in `CommandLineApplication`
- [ ] T075 [US7] Add `IsCached` indicator to `CompletionResult` for debugging/logging

**Checkpoint**: User Story 7 complete - Caching improves performance

---

## Phase 10: User Stories 8 & 9 - No-Match Feedback & Match Count (Priority: P3/P4)

**Goal**: Show "(no matches)" feedback and "X of Y" count indicator

**Independent Test**: Type gibberish + Tab to see "(no matches)"; observe count in menu

### Tests

- [ ] T076 [P] [US8] Add test cases for "(no matches)" indicator in `OrchestratorTests.cs`
- [ ] T077 [P] [US9] Add test cases for match count indicator in `MenuViewportTests.cs`

### Implementation

- [ ] T078 [US8] Implement "(no matches)" indicator in `CompletionOrchestrator` - brief message, auto-dismiss
- [ ] T079 [US9] Implement match count display in menu rendering - "X of Y" format

**Checkpoint**: User Stories 8 & 9 complete - Feedback indicators work

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, performance, documentation

### Edge Case Tests

- [ ] T080 [P] Create `BoundaryTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/` - test EC-001 to EC-014

### Matching & Ranking Tests

- [ ] T081 [P] Create `MatchingRankingTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/` - test MR-001 to MR-005

### Visual Feedback Tests

- [ ] T082 [P] Create `VisualFeedbackTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/` - test VF-001 to VF-007

### Result Limiting Tests

- [ ] T083 [P] Create `ResultLimitingTests.cs` in `BitPantry.CommandLine.Tests/AutoComplete/` - test RL-001 to RL-005

### Documentation & Cleanup

- [ ] T084 Update `quickstart.md` with final tested examples
- [ ] T085 Add XML documentation to all public APIs
- [ ] T086 Run full test suite and verify all 94+ test scenarios pass
- [ ] T087 Performance validation: local <50ms (SC-001), cached <10ms (SC-004), remote <3s timeout (SC-003)
- [ ] T088 Verify success criteria SC-002 (single keystroke acceptance), SC-005 (non-blocking typing during fetch), SC-006 (no flicker per VF-004/VF-005), SC-007 (80 column readability per EC-014)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 - BLOCKS all user stories
- **Phases 3-10 (User Stories)**: All depend on Phase 2 completion
- **Phase 11 (Polish)**: Depends on all user stories being complete

### User Story Dependencies

```
Phase 2 (Foundational)
        │
        ▼
   ┌────┴────┐
   │         │
   ▼         ▼
 US1       US2 ─────┐
 (Menu)   (Ghost)   │
   │                │
   ├────────────────┤
   ▼                ▼
 US3              US4 ────► US5
 (Args)          (Values)  (Implementer API)
                    │
                    ▼
                  US6 ────► US7
                (Remote)   (Cache)
                    │
                    ▼
               US8, US9
              (Feedback)
```

- **US1 (Tab Menu)**: Can start after Phase 2 - No dependencies
- **US2 (Ghost)**: Can start after Phase 2 - No dependencies on US1
- **US3 (Arg Names)**: Can start after Phase 2 - Reuses orchestrator from US1
- **US4 (Values)**: Can start after Phase 2 - Reuses orchestrator from US1
- **US5 (Implementer)**: Depends on US4 providers being complete
- **US6 (Remote)**: Depends on US4 provider pattern being established
- **US7 (Cache)**: Depends on US6 remote pattern being established
- **US8, US9 (Feedback)**: Can start after US1 menu works

### Parallel Opportunities

**Within Phase 2 (Foundational):**
- T005-T009 (entities) can run in parallel
- T010-T012 (interfaces) can run in parallel
- T013-T015 (attributes) can run in parallel
- T016-T020 (cache/UI state) can run in parallel

**Across User Stories (with multiple developers):**
- US1 and US2 can run in parallel
- US3 and US4 can run in parallel (after US1)
- All test tasks marked [P] within a phase can run in parallel

---

## Implementation Strategy

### MVP Scope (User Story 1 Only)

For fastest time-to-value, implement only Phase 1, Phase 2, and Phase 3 (User Story 1):
- **Deliverable**: Tab completion with menu for commands
- **Task Count**: ~31 tasks (T001-T031)
- **Estimated Effort**: Core autocomplete working

### Incremental Delivery

1. **MVP**: Phase 1-3 (Tab menu for commands)
2. **Iteration 1**: Add US2 (ghost suggestions), US3 (argument names)
3. **Iteration 2**: Add US4, US5 (argument values, developer API)
4. **Iteration 3**: Add US6, US7 (remote, caching)
5. **Iteration 4**: Add US8, US9, Polish

---

## Summary

| Phase | Description | Task Range | Count |
|-------|-------------|------------|-------|
| 1 | Setup | T001-T004 | 4 |
| 2 | Foundational | T005-T023 | 19 |
| 3 | US1: Tab Menu | T024-T031 | 10 |
| 4 | US2: Ghost | T032-T039 | 9 |
| 5 | US3: Arg Names | T040-T045 | 6 |
| 6 | US4: Values | T046-T055 | 12 |
| 7 | US5: Implementer | T056-T062 | 9 |
| 8 | US6: Remote | T063-T070 | 9 |
| 9 | US7: Cache | T071-T075 | 5 |
| 10 | US8-9: Feedback | T076-T079 | 4 |
| 11 | Polish | T080-T088 | 9 |
| **Total** | | | **96** |

**Parallel Opportunities**: 53 tasks marked [P]  
**Test Tasks**: 35 tasks (explicit test file creation)  
**MVP Scope**: 33 tasks (Phases 1-3)
