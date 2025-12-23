# Tasks: Documentation Revamp

**Input**: Design documents from `/specs/002-docs-revamp/`
**Prerequisites**: plan.md (required), spec.md (required for user stories)

**Tests**: Not applicable - documentation project (no code tests required)

**Organization**: Tasks are grouped by user story to enable independent implementation and validation of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

All documentation files are under `Docs/` directory:
- `Docs/` - Root documentation folder
- `Docs/CommandLine/` - Core library documentation
- `Docs/Remote/` - SignalR/remote CLI documentation

---

## Phase 1: Setup (Documentation Infrastructure)

**Purpose**: Establish navigation structure and audience-specific landing pages

- [ ] T001 Create main documentation index with audience paths in Docs/index.md
- [ ] T002 [P] Create implementer-focused navigation guide in Docs/ImplementerGuide.md
- [ ] T003 [P] Update readme.md to link to new index.md and add Documentation section in Docs/readme.md
- [ ] T004 [P] Add next steps section to QuickStart.md in Docs/CommandLine/QuickStart.md

**Checkpoint**: Navigation infrastructure ready - audience-specific documentation can now be created

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Fill empty/stub documentation that multiple user stories depend on

**⚠️ CRITICAL**: These files are referenced by other docs and must exist before cross-referencing

- [ ] T005 Expand CommandBase.md with protected members and console access patterns in Docs/CommandLine/CommandBase.md
- [ ] T006 [P] Expand IAnsiConsole.md with Spectre.Console integration patterns in Docs/CommandLine/IAnsiConsole.md

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Implementer Learns to Build CLI (Priority: P1) 🎯 MVP

**Goal**: A developer new to the framework can create a working CLI application within 30 minutes using the documentation

**Independent Test**: Have a developer unfamiliar with the library follow the documentation to create a working CLI with custom commands

### Implementation for User Story 1

- [ ] T007 [US1] Create DependencyInjection.md covering constructor injection and Services access in Docs/CommandLine/DependencyInjection.md
- [ ] T008 [P] [US1] Create Logging.md covering ILoggerFactory configuration in Docs/CommandLine/Logging.md
- [ ] T009 [P] [US1] Update ArgumentInfo.md to document IsRequired property in Docs/CommandLine/ArgumentInfo.md
- [ ] T010 [US1] Update CommandLineApplicationBuilder.md with all builder options table in Docs/CommandLine/CommandLineApplicationBuilder.md
- [ ] T011 [US1] Add table of contents to Commands.md in Docs/CommandLine/Commands.md
- [ ] T012 [US1] Add See Also section to Commands.md linking to related topics in Docs/CommandLine/Commands.md
- [ ] T013 [US1] Verify all cross-references in CommandLine/ folder resolve correctly

**Checkpoint**: User Story 1 complete - Implementer can learn to build CLI apps using documentation

---

## Phase 4: User Story 2 - Implementer Configures Remote CLI Server (Priority: P1)

**Goal**: A developer can set up a SignalR-based remote CLI server with JWT auth following the documentation

**Independent Test**: Have a developer set up a remote CLI server with authentication and successfully connect a client

### Implementation for User Story 2

- [ ] T014 [US2] Create SignalRClientOptions.md documenting all client configuration options in Docs/Remote/SignalRClientOptions.md
- [ ] T015 [P] [US2] Create Troubleshooting.md covering common issues and solutions in Docs/Remote/Troubleshooting.md
- [ ] T016 [US2] Add table of contents to CommandLineServer.md in Docs/Remote/CommandLineServer.md
- [ ] T017 [P] [US2] Add table of contents to Client.md in Docs/Remote/Client.md
- [ ] T018 [US2] Add link to SignalRClientOptions.md from Client.md in Docs/Remote/Client.md
- [ ] T019 [US2] Add link to Troubleshooting.md from CommandLineServer.md in Docs/Remote/CommandLineServer.md
- [ ] T020 [P] [US2] Add See Also section to JwtAuthOptions.md in Docs/Remote/JwtAuthOptions.md
- [ ] T021 [US2] Verify all cross-references in Remote/ folder resolve correctly

**Checkpoint**: User Story 2 complete - Implementer can configure remote CLI server using documentation

---

## Phase 5: User Story 3 - End User Operates CLI Application (Priority: P2)

**Goal**: An end user can understand command syntax, use autocomplete, navigate history, and use built-in commands

**Independent Test**: Have a non-developer use the CLI end-user guide to execute commands, use autocomplete, and navigate history

### Implementation for User Story 3

- [ ] T022 [US3] Create EndUserGuide.md with command syntax, REPL features, and keyboard shortcuts in Docs/EndUserGuide.md
- [ ] T023 [US3] Expand REPL.md with input history, Tab autocomplete, and prompt customization in Docs/CommandLine/REPL.md
- [ ] T024 [P] [US3] Create BuiltInCommands.md documenting lc command with examples in Docs/CommandLine/BuiltInCommands.md
- [ ] T025 [P] [US3] Create Remote BuiltInCommands.md documenting server.connect and server.disconnect in Docs/Remote/BuiltInCommands.md
- [ ] T026 [US3] Add cross-references from EndUserGuide.md to REPL.md and BuiltInCommands.md
- [ ] T027 [US3] Update index.md to include End User path linking to EndUserGuide.md in Docs/index.md

**Checkpoint**: User Story 3 complete - End users can operate CLI apps using documentation

---

## Phase 6: User Story 4 - Implementer Uses Dependency Injection (Priority: P2)

**Goal**: A developer can inject services into commands using DI patterns documented in the docs

**Independent Test**: Have a developer inject a service into a command following the documentation

### Implementation for User Story 4

- [ ] T028 [US4] Add code example for registering services in DependencyInjection.md in Docs/CommandLine/DependencyInjection.md
- [ ] T029 [US4] Add code example for constructor injection in commands in Docs/CommandLine/DependencyInjection.md
- [ ] T030 [US4] Add code example for scoped services pattern in Docs/CommandLine/DependencyInjection.md
- [ ] T031 [US4] Add cross-reference from CommandLineApplicationBuilder.md to DependencyInjection.md in Docs/CommandLine/CommandLineApplicationBuilder.md
- [ ] T032 [US4] Add cross-reference from Commands.md to DependencyInjection.md in Docs/CommandLine/Commands.md

**Checkpoint**: User Story 4 complete - Implementers can use DI in commands using documentation

---

## Phase 7: User Story 5 - Implementer Adds Autocomplete (Priority: P2)

**Goal**: A developer can add autocomplete functionality to command arguments following the documentation

**Independent Test**: Have a developer add autocomplete to an argument following the documentation

### Implementation for User Story 5

- [ ] T033 [US5] Expand AutoComplete.md with overview and AutoCompleteFunctionName property in Docs/CommandLine/AutoComplete.md
- [ ] T034 [US5] Add AutoCompleteContext and AutoCompleteOption documentation in Docs/CommandLine/AutoComplete.md
- [ ] T035 [US5] Add code example for basic static autocomplete in Docs/CommandLine/AutoComplete.md
- [ ] T036 [US5] Add code example for dynamic autocomplete based on context in Docs/CommandLine/AutoComplete.md
- [ ] T037 [US5] Add code example for async autocomplete with external data in Docs/CommandLine/AutoComplete.md
- [ ] T038 [US5] Add keyboard shortcuts documentation (Tab, Shift+Tab, Escape) in Docs/CommandLine/AutoComplete.md
- [ ] T039 [US5] Add See Also section linking to Commands.md and ArgumentInfo.md in Docs/CommandLine/AutoComplete.md
- [ ] T040 [US5] Add cross-reference from Commands.md to AutoComplete.md in Docs/CommandLine/Commands.md

**Checkpoint**: User Story 5 complete - Implementers can add autocomplete using documentation

---

## Phase 8: User Story 6 - Developer Navigates Between Topics (Priority: P3)

**Goal**: Developers can efficiently navigate between related documentation topics via cross-references

**Independent Test**: Verify all cross-reference links work and any page is reachable within 3 clicks from index

### Implementation for User Story 6

- [ ] T041 [US6] Add Prerequisites and See Also sections to all documentation pages missing them in Docs/CommandLine/
- [ ] T042 [P] [US6] Add Prerequisites and See Also sections to all documentation pages missing them in Docs/Remote/
- [ ] T043 [US6] Add back-to-index navigation link to all CommandLine documentation pages
- [ ] T044 [P] [US6] Add back-to-index navigation link to all Remote documentation pages
- [ ] T045 [US6] Audit and fix all broken internal links in Docs/CommandLine/
- [ ] T046 [P] [US6] Audit and fix all broken internal links in Docs/Remote/
- [ ] T047 [US6] Validate 3-click navigation from index.md to every documentation page

**Checkpoint**: User Story 6 complete - All navigation paths verified and working

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Consistency pass across all documentation

- [ ] T048 [P] Review and reorganize Commands.md for consistency with new documentation structure in Docs/CommandLine/Commands.md
- [ ] T049 [P] Verify SignalRServerOptions fully documented with all settings and defaults in Docs/Remote/CommandLineServer.md
- [ ] T050 [P] Verify JwtAuthOptions fully documented with all options, defaults, and examples in Docs/Remote/JwtAuthOptions.md
- [ ] T051 [P] Verify FileSystemConfiguration.md covers all file transfer options and validators for consistency in Docs/Remote/FileSystemConfiguration.md
- [ ] T052 Verify heading hierarchy (H1→H2→H3) across all new and modified files
- [ ] T053 [P] Verify code block formatting (triple backticks with language identifier) across all files
- [ ] T054 [P] Verify consistent tone (technical tutorial, code-first, step-by-step) across all files
- [ ] T055 Verify table of contents added to all pages with >3 sections
- [ ] T056 Final link validation - run check for broken links across all documentation
- [ ] T057 Word count validation - verify new files meet target word counts per plan.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS user stories that reference CommandBase.md, IAnsiConsole.md
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - User stories can proceed in priority order (P1 → P2 → P3)
  - Some parallelization possible within priority levels
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational - No dependencies on other stories (can parallel with US1)
- **User Story 3 (P2)**: Can start after Foundational - References BuiltInCommands which is created in this story
- **User Story 4 (P2)**: Depends on US1 (DependencyInjection.md created in US1)
- **User Story 5 (P2)**: Can start after Foundational - AutoComplete.md is independent
- **User Story 6 (P3)**: Depends on all other stories - adds cross-references to existing pages

### Within Each User Story

- Create new files before modifying existing files
- Add content before adding cross-references
- Verify cross-references after all content is in place

### Parallel Opportunities

- T002, T003, T004 can run in parallel (different files)
- T005, T006 can run in parallel (different files)
- US1 and US2 can run in parallel (different focus areas)
- Within US3: T024, T025 can run in parallel
- Within US6: CommandLine/ and Remote/ tasks can run in parallel
- Within Polish: T048, T049, T050 can run in parallel

---

## Parallel Example: User Story 1 & 2

```bash
# US1 and US2 can start simultaneously after Phase 2:

# Team Member A - User Story 1 (Core CLI):
Task T007: Create DependencyInjection.md
Task T008: Create Logging.md (parallel with T007)
Task T009: Update ArgumentInfo.md (parallel with T007)
Task T010: Update CommandLineApplicationBuilder.md
...

# Team Member B - User Story 2 (Remote CLI):
Task T014: Create SignalRClientOptions.md
Task T015: Create Troubleshooting.md (parallel with T014)
Task T016: Add ToC to CommandLineServer.md
...
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup (navigation infrastructure)
2. Complete Phase 2: Foundational (CommandBase.md, IAnsiConsole.md)
3. Complete Phase 3: User Story 1 (Core CLI documentation)
4. Complete Phase 4: User Story 2 (Remote CLI documentation)
5. **STOP and VALIDATE**: Both P1 stories complete - core implementer documentation ready
6. Deploy/publish documentation

### Incremental Delivery

1. Complete Setup + Foundational → Navigation ready
2. Add User Story 1 → Core CLI docs complete → Publish
3. Add User Story 2 → Remote CLI docs complete → Publish
4. Add User Story 3 → End-user docs complete → Publish
5. Add User Stories 4 & 5 → Advanced topics complete → Publish
6. Add User Story 6 → All cross-references complete → Publish
7. Polish pass → Final quality check → Publish

---

## Task Summary

| Phase | Tasks | Priority |
|-------|-------|----------|
| Phase 1: Setup | T001-T004 (4 tasks) | - |
| Phase 2: Foundational | T005-T006 (2 tasks) | - |
| Phase 3: User Story 1 | T007-T013 (7 tasks) | P1 |
| Phase 4: User Story 2 | T014-T021 (8 tasks) | P1 |
| Phase 5: User Story 3 | T022-T027 (6 tasks) | P2 |
| Phase 6: User Story 4 | T028-T032 (5 tasks) | P2 |
| Phase 7: User Story 5 | T033-T040 (8 tasks) | P2 |
| Phase 8: User Story 6 | T041-T047 (7 tasks) | P3 |
| Phase 9: Polish | T048-T057 (10 tasks) | - |
| **Total** | **57 tasks** | |

### Parallel Opportunities

- **Setup**: 3 of 4 tasks parallelizable
- **Foundational**: 2 of 2 tasks parallelizable
- **US1 + US2**: Can run entirely in parallel (15 tasks)
- **US3**: 2 of 6 tasks parallelizable
- **US4**: Sequential (depends on US1 output)
- **US5**: 8 tasks, mostly sequential (same file)
- **US6**: 4 of 7 tasks parallelizable
- **Polish**: 6 of 10 tasks parallelizable

### Independent Test Criteria

| Story | Test Criteria |
|-------|---------------|
| US1 | Developer creates working CLI app with commands in 30 min using docs |
| US2 | Developer sets up remote CLI server with JWT auth using docs |
| US3 | Non-developer operates CLI using end-user guide |
| US4 | Developer injects service into command using docs |
| US5 | Developer adds autocomplete to argument using docs |
| US6 | Any page reachable in 3 clicks; all links work |

### Suggested MVP Scope

**MVP = Phase 1 + Phase 2 + Phase 3 (US1) + Phase 4 (US2)**

This delivers:
- ✅ Navigation infrastructure
- ✅ Core CLI implementation documentation  
- ✅ Remote CLI configuration documentation
- ✅ Both P1 user stories complete

Total MVP tasks: 21 tasks (T001-T021)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Documentation is consumed directly from GitHub - no build step required
