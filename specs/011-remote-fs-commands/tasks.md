# Tasks: Remote File System Management Commands

**Input**: Design documents from `/specs/011-remote-fs-commands/`
**Prerequisites**: plan.md, spec.md, test-cases.md, data-model.md, contracts/, research.md, quickstart.md

**Micro-TDD Format**: Each task is an atomic behavioral unit (one test case, one red-green cycle).

## Phase 1: Setup (Infrastructure)

**Purpose**: Register server command scaffolding before behavioral tasks.

- [ ] T001 @test-case:011:SETUP-001 Add server command group scaffold in BitPantry.CommandLine.Remote.SignalR.Server/Commands/ServerGroup.cs
- [ ] T002 [depends:T001] @test-case:011:SETUP-002 Register server command types in BitPantry.CommandLine.Remote.SignalR.Server/Configuration/IServiceCollectionExtensions.cs

---

## Phase 3: User Story 1 - Browse Remote File System (Priority: P1)

**Goal**: Deliver server-side ls command with listing formats, sorting, recursion, globbing, and robust errors.

**Independent Test Criteria**: `server ls` supports short/long/recursive/sort modes and renders expected output against known server fixtures.

**Test Cases**: CV-001, CV-002, CV-003, CV-004, CV-005, CV-006, CV-007, CV-008, CV-009, CV-033, DF-001, DF-002, DF-003, DF-004, DF-005, DF-006, DF-048, DF-055, EH-001, EH-002, EH-021, EH-029, UX-001, UX-002, UX-003, UX-004, UX-005, UX-006, UX-007, UX-008, UX-009, UX-010, UX-011, UX-012, UX-026

### Tasks

- [ ] T003 [depends:T002] @test-case:011:CV-001 Implement CV-001 (Path argument is optional) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T004 [depends:T003] @test-case:011:CV-002 Implement CV-002 (Path argument accepted as positional) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T005 [depends:T004] @test-case:011:CV-003 Implement CV-003 (`--long` / `-l` flag activates long mode) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T006 [depends:T005] @test-case:011:CV-004 Implement CV-004 (`--recursive` flag activates recursion) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T007 [depends:T006] @test-case:011:CV-005 Implement CV-005 (`--sort` accepts `name`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T008 [depends:T007] @test-case:011:CV-006 Implement CV-006 (`--sort` accepts `size`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T009 [depends:T008] @test-case:011:CV-007 Implement CV-007 (`--sort` accepts `modified`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T010 [depends:T009] @test-case:011:CV-008 Implement CV-008 (`--reverse` alone reverses default sort) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T011 [depends:T010] @test-case:011:CV-009 Implement CV-009 (`--reverse` combined with `--sort size`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T012 [depends:T011] @test-case:011:CV-033 Implement CV-033 (`-l` alias accepted as `--long`) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T013 [depends:T012] @test-case:011:DF-001 Implement DF-001 (Lists files at specified path) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T014 [depends:T013] @test-case:011:DF-002 Implement DF-002 (Lists subdir contents when path is a dir) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T015 [depends:T014] @test-case:011:DF-003 Implement DF-003 (Glob pattern `*.txt` filters to text files) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T016 [depends:T015] @test-case:011:DF-004 Implement DF-004 (Glob `*.log` matches multiple) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T017 [depends:T016] @test-case:011:DF-005 Implement DF-005 (Traverses subdirectories) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T018 [depends:T017] @test-case:011:DF-006 Implement DF-006 (Actual sort by file size) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T019 [depends:T018] @test-case:011:DF-048 Implement DF-048 (End-to-end: files in tempDir appear after connect) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T020 [depends:T019] @test-case:011:DF-055 Implement DF-055 (Server commands appear after connect) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T021 [depends:T020] @test-case:011:EH-001 Implement EH-001 (Path not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T022 [depends:T021] @test-case:011:EH-002 Implement EH-002 (Path is a file (not a dir) and no glob) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T023 [depends:T022] @test-case:011:EH-021 Implement EH-021 (`SandboxedFileSystem` blocks path traversal attempt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T024 [depends:T023] @test-case:011:EH-029 Implement EH-029 (Path not found returns error (not exception)) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T025 [depends:T024] @test-case:011:UX-001 Implement UX-001 (Default list — files and directories shown) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T026 [depends:T025] @test-case:011:UX-002 Implement UX-002 (Directories suffixed with `/`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T027 [depends:T026] @test-case:011:UX-003 Implement UX-003 (Files have no trailing `/`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T028 [depends:T027] @test-case:011:UX-004 Implement UX-004 (Long format shows table with Type, Name, Size, Last Modified columns) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T029 [depends:T028] @test-case:011:UX-005 Implement UX-005 (File size formatted as human-readable) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T030 [depends:T029] @test-case:011:UX-006 Implement UX-006 (Directory size column shows `—`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T031 [depends:T030] @test-case:011:UX-007 Implement UX-007 (Tree view shows nested entries) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T032 [depends:T031] @test-case:011:UX-008 Implement UX-008 (Entries ordered by size (smallest first)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T033 [depends:T032] @test-case:011:UX-009 Implement UX-009 (Entries ordered by size (largest first)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T034 [depends:T033] @test-case:011:UX-010 Implement UX-010 (Entries ordered by last modified (oldest first)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T035 [depends:T034] @test-case:011:UX-011 Implement UX-011 (Entries ordered alphabetically) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T036 [depends:T035] @test-case:011:UX-012 Implement UX-012 (Reverses default (name) sort) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T037 [depends:T036] @test-case:011:UX-026 Implement UX-026 (End-to-end output visible in VirtualConsole) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs

**Checkpoint**: User Story 1 complete — all mapped tests pass

## Phase 4: User Story 2 - Create Remote Directories (Priority: P1)

**Goal**: Deliver server-side mkdir with parent creation semantics and validation.

**Independent Test Criteria**: `server mkdir` creates directories correctly with and without `--parents` and emits clear errors.

**Test Cases**: CV-010, CV-011, CV-036, DF-007, DF-008, DF-009, DF-010, DF-049, EH-003, EH-022, UX-013, UX-027

### Tasks

- [ ] T038 [depends:T002] @test-case:011:CV-010 Implement CV-010 (`path` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T039 [depends:T038] @test-case:011:CV-011 Implement CV-011 (`--parents` / `-p` flag activates deep creation) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T040 [depends:T039] @test-case:011:CV-036 Implement CV-036 (`-p` alias accepted as `--parents`) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_MkdirCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T041 [depends:T040] @test-case:011:DF-007 Implement DF-007 (Directory created at path) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T042 [depends:T041] @test-case:011:DF-008 Implement DF-008 (All intermediate dirs created) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T043 [depends:T042] @test-case:011:DF-009 Implement DF-009 (Fails if parent missing without `--parents`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T044 [depends:T043] @test-case:011:DF-010 Implement DF-010 (Idempotent when directory already exists) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T045 [depends:T044] @test-case:011:DF-049 Implement DF-049 (End-to-end: directory exists on disk after command) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_MkdirCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T046 [depends:T045] @test-case:011:EH-003 Implement EH-003 (Parent does not exist) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T047 [depends:T046] @test-case:011:EH-022 Implement EH-022 (Path traversal attempt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T048 [depends:T047] @test-case:011:UX-013 Implement UX-013 (Success message includes path) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
- [ ] T049 [depends:T048] @test-case:011:UX-027 Implement UX-027 (Created message visible in VirtualConsole) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_MkdirCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs

**Checkpoint**: User Story 2 complete — all mapped tests pass

## Phase 5: User Story 3 - Remove Remote Files and Directories (Priority: P1)

**Goal**: Deliver server-side rm for files, directories, recursive deletion, and glob confirmation.

**Independent Test Criteria**: `server rm` deletes intended targets only, confirms destructive glob operations, and blocks unsafe actions.

**Test Cases**: CV-012, CV-013, CV-014, CV-015, CV-016, CV-017, CV-034, DF-011, DF-012, DF-013, DF-014, DF-015, DF-016, DF-017, DF-018, DF-019, DF-020, DF-050, EH-004, EH-005, EH-006, EH-007, EH-008, EH-023, EH-028, UX-014, UX-015

### Tasks

- [ ] T050 [depends:T002] @test-case:011:CV-012 Implement CV-012 (`path` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T051 [depends:T050] @test-case:011:CV-013 Implement CV-013 (`--recursive` / `-r` flag allows non-empty dir deletion) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T052 [depends:T051] @test-case:011:CV-014 Implement CV-014 (`--directory` / `-d` flag allows empty dir deletion) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T053 [depends:T052] @test-case:011:CV-015 Implement CV-015 (`--force` / `-f` flag skips confirmation) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T054 [depends:T053] @test-case:011:CV-016 Implement CV-016 (Without `-r` deleting non-empty dir produces error) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T055 [depends:T054] @test-case:011:CV-017 Implement CV-017 (Without `-d` deleting empty dir produces error) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T056 [depends:T055] @test-case:011:CV-034 Implement CV-034 (`-r` alias accepted as `--recursive`) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_RmCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T057 [depends:T056] @test-case:011:DF-011 Implement DF-011 (Single file deleted) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T058 [depends:T057] @test-case:011:DF-012 Implement DF-012 (Empty directory deleted) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T059 [depends:T058] @test-case:011:DF-013 Implement DF-013 (Non-existent path with `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T060 [depends:T059] @test-case:011:DF-014 Implement DF-014 (Non-existent path without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T061 [depends:T060] @test-case:011:DF-015 Implement DF-015 (Non-empty directory deleted recursively) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T062 [depends:T061] @test-case:011:DF-016 Implement DF-016 (Glob pattern matches and deletes multiple) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T063 [depends:T062] @test-case:011:DF-017 Implement DF-017 (Glob with fewer than threshold — no prompt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T064 [depends:T063] @test-case:011:DF-018 Implement DF-018 (Glob with ≥ threshold — prompts (answered yes)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T065 [depends:T064] @test-case:011:DF-019 Implement DF-019 (Glob with ≥ threshold — prompts (answered no)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T066 [depends:T065] @test-case:011:DF-020 Implement DF-020 (Cannot delete storage root) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T067 [depends:T066] @test-case:011:DF-050 Implement DF-050 (End-to-end: file gone after command) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_RmCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T068 [depends:T067] @test-case:011:EH-004 Implement EH-004 (Path not found without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T069 [depends:T068] @test-case:011:EH-005 Implement EH-005 (Path not found with `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T070 [depends:T069] @test-case:011:EH-006 Implement EH-006 (Non-empty dir without `-r` or `-d`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T071 [depends:T070] @test-case:011:EH-007 Implement EH-007 (Empty dir without `-d` or `-r`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T072 [depends:T071] @test-case:011:EH-008 Implement EH-008 (Attempt to delete storage root) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T073 [depends:T072] @test-case:011:EH-023 Implement EH-023 (Path traversal attempt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T074 [depends:T073] @test-case:011:EH-028 Implement EH-028 (Cannot delete outside sandbox in integration) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_RmCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T075 [depends:T074] @test-case:011:UX-014 Implement UX-014 (Per-item success indicator) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T076 [depends:T075] @test-case:011:UX-015 Implement UX-015 (Multiple glob matches show item count) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs

**Checkpoint**: User Story 3 complete — all mapped tests pass

## Phase 6: User Story 4 - Move and Rename Remote Files (Priority: P2)

**Goal**: Deliver server-side mv for files/directories with overwrite and validation behavior.

**Independent Test Criteria**: `server mv` moves/renames entries atomically and respects overwrite/safety rules.

**Test Cases**: CV-018, CV-019, CV-020, CV-037, DF-021, DF-022, DF-023, DF-024, DF-025, DF-026, DF-051, EH-009, EH-010, EH-011, EH-024, UX-016

### Tasks

- [ ] T077 [depends:T002] @test-case:011:CV-018 Implement CV-018 (`source` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T078 [depends:T077] @test-case:011:CV-019 Implement CV-019 (`destination` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T079 [depends:T078] @test-case:011:CV-020 Implement CV-020 (`--force` / `-f` flag allows overwrite) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T080 [depends:T079] @test-case:011:CV-037 Implement CV-037 (`-f` alias accepted as `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_MvCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T081 [depends:T080] @test-case:011:DF-021 Implement DF-021 (File moved to new location) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T082 [depends:T081] @test-case:011:DF-022 Implement DF-022 (Directory moved) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T083 [depends:T082] @test-case:011:DF-023 Implement DF-023 (Fails if source not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T084 [depends:T083] @test-case:011:DF-024 Implement DF-024 (Fails if destination exists without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T085 [depends:T084] @test-case:011:DF-025 Implement DF-025 (Overwrites existing destination file) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T086 [depends:T085] @test-case:011:DF-026 Implement DF-026 (Fails if source same as destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T087 [depends:T086] @test-case:011:DF-051 Implement DF-051 (End-to-end: file at new location) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_MvCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T088 [depends:T087] @test-case:011:EH-009 Implement EH-009 (Source not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T089 [depends:T088] @test-case:011:EH-010 Implement EH-010 (Destination already exists without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T090 [depends:T089] @test-case:011:EH-011 Implement EH-011 (Source equals destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T091 [depends:T090] @test-case:011:EH-024 Implement EH-024 (Path traversal in source) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs
- [ ] T092 [depends:T091] @test-case:011:UX-016 Implement UX-016 (Success shows source and destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MvCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/MvCommand.cs

**Checkpoint**: User Story 4 complete — all mapped tests pass

## Phase 7: User Story 5 - Copy Remote Files and Directories (Priority: P2)

**Goal**: Deliver server-side cp for file and recursive directory copy with overwrite and progress semantics.

**Independent Test Criteria**: `server cp` duplicates content accurately and enforces recursive/overwrite rules.

**Test Cases**: CV-021, CV-022, CV-023, CV-024, CV-025, CV-035, DF-027, DF-028, DF-029, DF-030, DF-031, DF-032, DF-052, EH-012, EH-013, EH-014, EH-025, UX-017, UX-018

### Tasks

- [ ] T093 [depends:T002] @test-case:011:CV-021 Implement CV-021 (`source` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T094 [depends:T093] @test-case:011:CV-022 Implement CV-022 (`destination` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T095 [depends:T094] @test-case:011:CV-023 Implement CV-023 (`--recursive` / `-r` required for directory copy) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T096 [depends:T095] @test-case:011:CV-024 Implement CV-024 (`--recursive` / `-r` accepted for directory copy) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T097 [depends:T096] @test-case:011:CV-025 Implement CV-025 (`--force` / `-f` flag allows overwrite) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T098 [depends:T097] @test-case:011:CV-035 Implement CV-035 (`-r` alias accepted as `--recursive`) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CpCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T099 [depends:T098] @test-case:011:DF-027 Implement DF-027 (File copied, original preserved) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T100 [depends:T099] @test-case:011:DF-028 Implement DF-028 (Directory and contents copied) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T101 [depends:T100] @test-case:011:DF-029 Implement DF-029 (Nested directory structure preserved) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T102 [depends:T101] @test-case:011:DF-030 Implement DF-030 (Fails if source directory without `--recursive`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T103 [depends:T102] @test-case:011:DF-031 Implement DF-031 (Fails if dest file exists without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T104 [depends:T103] @test-case:011:DF-032 Implement DF-032 (Overwrites existing destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T105 [depends:T104] @test-case:011:DF-052 Implement DF-052 (End-to-end: both files exist) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CpCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T106 [depends:T105] @test-case:011:EH-012 Implement EH-012 (Source not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T107 [depends:T106] @test-case:011:EH-013 Implement EH-013 (Source is directory without `--recursive`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T108 [depends:T107] @test-case:011:EH-014 Implement EH-014 (Destination exists without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T109 [depends:T108] @test-case:011:EH-025 Implement EH-025 (Path traversal in destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T110 [depends:T109] @test-case:011:UX-017 Implement UX-017 (Success shows source and destination) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs
- [ ] T111 [depends:T110] @test-case:011:UX-018 Implement UX-018 (Summary shows item count) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CpCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CpCommand.cs

**Checkpoint**: User Story 5 complete — all mapped tests pass

## Phase 8: User Story 6 - View Remote File Contents (Priority: P2)

**Goal**: Deliver server-side cat with head/tail, binary detection, and large-file safeguards.

**Independent Test Criteria**: `server cat` outputs text correctly with line limits and safety prompts for binary/large files.

**Test Cases**: CV-026, CV-027, CV-028, CV-029, CV-030, CV-031, DF-033, DF-034, DF-035, DF-036, DF-037, DF-038, DF-039, DF-040, DF-041, DF-042, DF-053, EH-015, EH-016, EH-017, EH-018, EH-019, EH-026, EH-030, UX-019, UX-020, UX-021, UX-022, UX-028

### Tasks

- [ ] T112 [depends:T002] @test-case:011:CV-026 Implement CV-026 (`path` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T113 [depends:T112] @test-case:011:CV-027 Implement CV-027 (`--lines` / `-n` accepts integer) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T114 [depends:T113] @test-case:011:CV-028 Implement CV-028 (`--tail` / `-t` accepts integer) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T115 [depends:T114] @test-case:011:CV-029 Implement CV-029 (`--lines` and `--tail` mutually exclusive) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T116 [depends:T115] @test-case:011:CV-030 Implement CV-030 (`--force` / `-f` flag bypasses binary check) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T117 [depends:T116] @test-case:011:CV-031 Implement CV-031 (`--force` / `-f` flag bypasses large-file prompt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T118 [depends:T117] @test-case:011:DF-033 Implement DF-033 (Outputs all lines of text file) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T119 [depends:T118] @test-case:011:DF-034 Implement DF-034 (Outputs only first 2 lines) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T120 [depends:T119] @test-case:011:DF-035 Implement DF-035 (`--lines` > file length: all lines) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T121 [depends:T120] @test-case:011:DF-036 Implement DF-036 (Outputs only last 2 lines) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T122 [depends:T121] @test-case:011:DF-037 Implement DF-037 (`--tail` > file length: all lines) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T123 [depends:T122] @test-case:011:DF-038 Implement DF-038 (Binary file detected — aborts) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T124 [depends:T123] @test-case:011:DF-039 Implement DF-039 (Binary file with `--force` — outputs anyway) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T125 [depends:T124] @test-case:011:DF-040 Implement DF-040 (Large file without `--lines` prompts (yes)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T126 [depends:T125] @test-case:011:DF-041 Implement DF-041 (Large file without `--lines` prompts (no)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T127 [depends:T126] @test-case:011:DF-042 Implement DF-042 (Large file with `--force` — no prompt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T128 [depends:T127] @test-case:011:DF-053 Implement DF-053 (End-to-end: file content visible in VirtualConsole) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CatCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T129 [depends:T128] @test-case:011:EH-015 Implement EH-015 (File not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T130 [depends:T129] @test-case:011:EH-016 Implement EH-016 (Path is a directory) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T131 [depends:T130] @test-case:011:EH-017 Implement EH-017 (Binary content without `--force`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T132 [depends:T131] @test-case:011:EH-018 Implement EH-018 (`--lines=0`) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T133 [depends:T132] @test-case:011:EH-019 Implement EH-019 (`--lines` and `--tail` together) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T134 [depends:T133] @test-case:011:EH-026 Implement EH-026 (Path traversal attempt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T135 [depends:T134] @test-case:011:EH-030 Implement EH-030 (Binary file error shown end-to-end) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CatCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T136 [depends:T135] @test-case:011:UX-019 Implement UX-019 (Lines displayed without modification) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T137 [depends:T136] @test-case:011:UX-020 Implement UX-020 (Footer shows head indicator) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T138 [depends:T137] @test-case:011:UX-021 Implement UX-021 (Footer shows tail indicator) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T139 [depends:T138] @test-case:011:UX-022 Implement UX-022 (No footer when neither `--lines` nor `--tail` used) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/CatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs
- [ ] T140 [depends:T139] @test-case:011:UX-028 Implement UX-028 (File content visible in VirtualConsole) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CatCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/CatCommand.cs

**Checkpoint**: User Story 6 complete — all mapped tests pass

## Phase 9: User Story 7 - Inspect Remote File Information (Priority: P3)

**Goal**: Deliver server-side stat for file/directory metadata and aggregate directory stats.

**Independent Test Criteria**: `server stat` returns accurate metadata for files and directories with clear error handling.

**Test Cases**: CV-032, DF-043, DF-044, DF-045, DF-046, DF-047, DF-054, EH-020, EH-027, UX-023, UX-024, UX-025

### Tasks

- [ ] T141 [depends:T002] @test-case:011:CV-032 Implement CV-032 (`path` argument is required) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T142 [depends:T141] @test-case:011:DF-043 Implement DF-043 (Returns correct name and path for file) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T143 [depends:T142] @test-case:011:DF-044 Implement DF-044 (Returns correct size for file) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T144 [depends:T143] @test-case:011:DF-045 Implement DF-045 (Returns created and modified timestamps) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T145 [depends:T144] @test-case:011:DF-046 Implement DF-046 (Returns correct file count for directory) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T146 [depends:T145] @test-case:011:DF-047 Implement DF-047 (Directory total size is recursive sum) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T147 [depends:T146] @test-case:011:DF-054 Implement DF-054 (End-to-end: stat output visible) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_StatCommand.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T148 [depends:T147] @test-case:011:EH-020 Implement EH-020 (Path not found) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T149 [depends:T148] @test-case:011:EH-027 Implement EH-027 (Path traversal attempt) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T150 [depends:T149] @test-case:011:UX-023 Implement UX-023 (All fields rendered for a file) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T151 [depends:T150] @test-case:011:UX-024 Implement UX-024 (Size shown in human-readable and raw bytes) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs
- [ ] T152 [depends:T151] @test-case:011:UX-025 Implement UX-025 (Directory shows ItemCount, FileCount, DirectoryCount) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/StatCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/StatCommand.cs

**Checkpoint**: User Story 7 complete — all mapped tests pass

## Phase 10: User Story 8 - Documentation Updated (Priority: P1)

**Goal**: Update docs and inline help to reflect all seven commands.

**Independent Test Criteria**: README and command help text match implemented syntax and behaviors for all commands.

**Test Cases**: UX-029

### Tasks

- [ ] T153 [depends:T002] @test-case:011:SETUP-003 Document command reference updates in README.md
- [ ] T154 [depends:T153] @test-case:011:UX-029 Implement UX-029 (Command help reflects remote file system syntax) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CommandHelp.cs
- [ ] T158 [depends:T154] @test-case:011:SETUP-005 Update CLAUDE.md command guidance for remote file system commands

**Checkpoint**: User Story 8 documentation and help coverage complete

## Phase 11: Cross-Cutting Reliability and Glob Coverage (Priority: P1)

**Goal**: Close cross-command gaps for disconnected execution and glob no-match behavior.

**Independent Test Criteria**: Disconnected command invocation returns a clear error, and glob no-match behavior is explicit for `ls` and `rm`.

**Test Cases**: EH-031, EH-032, EH-033, EH-034

### Tasks

- [ ] T155 [depends:T154] @test-case:011:EH-031 Implement EH-031 (Disconnected invocation returns standard not-connected message) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LsCommand.cs
- [ ] T156 [depends:T155] @test-case:011:EH-032 Implement EH-032 (`server ls` glob no-match displays explicit message) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
- [ ] T157 [depends:T156] @test-case:011:EH-033 Implement EH-033 (`server rm` glob no-match displays explicit message) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/RmCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/RmCommand.cs
- [ ] T159 [depends:T157] @test-case:011:EH-034 Implement EH-034 (Mid-operation disconnect aborts with clear error) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_CpCommand.cs

**Checkpoint**: Cross-cutting gaps closed

---

## Dependencies & Execution Order

1. Complete setup tasks T001-T002 first.
2. Execute P1 stories in order: User Story 1 (`ls`), User Story 2 (`mkdir`), User Story 3 (`rm`).
3. Execute P2 stories: User Story 4 (`mv`), User Story 5 (`cp`), User Story 6 (`cat`).
4. Execute P3 story: User Story 7 (`stat`).
5. Finish documentation tasks in User Story 8.
6. Execute cross-cutting coverage tasks in Phase 11.

## Parallel Execution Examples

- After T002, first tasks of each command story can run in parallel: User Story 1 task, User Story 2 task, User Story 3 task, User Story 4 task, User Story 5 task, User Story 6 task, and User Story 7 task.
- Integration-level tasks for distinct commands can run in parallel once their corresponding unit-level chains are green.

## Implementation Strategy

1. MVP scope: User Stories 1-3 (browse/create/remove).
2. Increment 2: User Stories 4-6 (move/copy/cat).
3. Increment 3: User Story 7 + documentation story.
4. Increment 4: Cross-cutting reliability and glob no-match behavior.

## Validation

- [x] 155 of 155 test cases from test-cases.md mapped to exactly one task
- [x] Every non-setup task has exactly one @test-case reference
- [x] Task IDs are sequential and unique
- [x] Dependencies are explicit and acyclic (linear per story chain)
- [x] Each task description includes concrete file paths
