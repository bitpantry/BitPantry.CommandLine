# Tasks: Remote File System Commands

**Input**: Design documents from `/specs/008-remote-file-commands/`
**Prerequisites**: plan.md ✅, spec.md ✅

**Tests**: TDD approach per constitution - E2E integration tests written FIRST for each command using `TestEnvironment`.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Server commands: `BitPantry.CommandLine.Remote.SignalR.Server/Commands/File/`
Client commands: `BitPantry.CommandLine.Remote.SignalR.Client/Commands/File/`
Tests: `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/`
Docs: `Docs/Remote/`

---

## Phase 1: Setup (Project Structure)

**Purpose**: Create file/folder structure for new commands

- [x] T001 Create `Commands/File/` directory in `BitPantry.CommandLine.Remote.SignalR.Server/`
- [x] T002 [P] Create `Commands/File/` directory in `BitPantry.CommandLine.Remote.SignalR.Client/`
- [x] T003 [P] Create `FileGroup.cs` command group in `BitPantry.CommandLine.Remote.SignalR.Server/Commands/File/FileGroup.cs`
- [x] T004 [P] Create `FileGroup.cs` command group (client-side) in `BitPantry.CommandLine.Remote.SignalR.Client/Commands/File/FileGroup.cs`

---

## Phase 2: Foundational (Command Registration)

**Purpose**: Wire up default registration so commands are available when packages are used

**⚠️ CRITICAL**: Commands won't be accessible until registration is complete

- [x] T005 Register server-side FileGroup and commands in server package DI setup (update existing `ServiceCollectionExtensions` or equivalent)
- [x] T006 [P] Register client-side FileGroup and commands in client package DI setup (update existing `CommandLineApplicationBuilderExtensions`)
- [x] T007 Create base E2E test file `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_FileCommands.cs` with TestEnvironment setup

**Checkpoint**: File group registered, test infrastructure ready - command implementation can begin ✅

---

## Phase 2.5: Autocomplete Infrastructure (Priority: P2) 

**Purpose**: Enable server-side autocomplete for remote path arguments

**Context**: 005-autocomplete-redesign is complete. The infrastructure exists but `AddCommandLineHub()` doesn't register `AddCompletionServices()`, causing server-side completion to return empty results.

**Risk**: Low - single line addition to DI registration; leverages fully-tested existing infrastructure

### Infrastructure Fix

- [x] T007a [US8] Add `services.AddCompletionServices()` call in `BitPantry.CommandLine.Remote.SignalR.Server/Configuration/IServiceCollectionExtensions.cs` after IFileSystem registration
  - Server-side `FilePathProvider` already injects `IFileSystem` → automatically gets `SandboxedFileSystem`
  - Completions will be sandboxed to StorageRootPath automatically

### E2E Tests for Autocomplete (User Story 8)

- [ ] T007b [P] [US8] E2E test: Connected client, `file ls doc` + Tab shows remote path completions - in `IntegrationTests_FileCommands.cs`
- [ ] T007c [P] [US8] E2E test: Disconnected client, `file download rem` + Tab returns empty (graceful fallback) - in `IntegrationTests_FileCommands.cs`
- [ ] T007d [P] [US8] E2E test: `file upload ./local` + Tab shows local file completions - in `IntegrationTests_FileCommands.cs`

**Checkpoint**: Remote path autocomplete functional - server returns sandboxed file completions

---

## Phase 3: User Story 1 - List Remote Files (Priority: P1) 🎯 MVP

**Goal**: Users can list files and directories on the remote server

**Independent Test**: Connect to server with known files, run `file ls`, verify output contains expected entries

### E2E Tests for User Story 1 (TDD - Write FIRST, verify FAIL)

- [x] T008 [US1] E2E test: `file ls` lists files in storage root - in `IntegrationTests_FileCommands.cs`
- [x] T009 [P] [US1] E2E test: `file ls reports/` lists subdirectory contents - in `IntegrationTests_FileCommands.cs`
- [x] T010 [P] [US1] E2E test: `file ls --long` shows type, size, modified date - in `IntegrationTests_FileCommands.cs`
- [x] T011 [P] [US1] E2E test: `file ls --recursive` lists all files recursively - in `IntegrationTests_FileCommands.cs`
- [x] T012 [P] [US1] E2E test: `file ls nonexistent/` returns error - in `IntegrationTests_FileCommands.cs`
- [x] T013 [P] [US1] E2E test: `file ls ../outside` rejected by SandboxedFileSystem - in `IntegrationTests_FileCommands.cs`
- [x] T014 [P] [US1] E2E test: `file ls` when disconnected shows connection error - in `IntegrationTests_FileCommands.cs`

### Implementation for User Story 1

- [x] T015 [US1] Implement `FileListCommand.cs` in `BitPantry.CommandLine.Remote.SignalR.Server/Commands/File/FileListCommand.cs`
  - Inject IFileSystem, IAnsiConsole
  - path argument (optional, defaults to ".")
  - --recursive/-r flag
  - --long/-l flag
  - Use IFileSystem.Directory.EnumerateFileSystemEntries()
  - Format output per spec (short format, long format with columns)
  - Handle errors (not found, path traversal via SandboxedFileSystem)

**Checkpoint**: `file ls` fully functional - can list, filter, show details. Independent test passes. ✅

---

## Phase 4: User Story 2 - Upload Files to Server (Priority: P1)

**Goal**: Users can upload local files to the remote server with progress feedback

**Independent Test**: Create local file, run `file upload`, verify file exists on server via direct file system check

### E2E Tests for User Story 2 (TDD - Write FIRST, verify FAIL)

- [x] T016 [US2] E2E test: `file upload data.csv` uploads to storage root - in `IntegrationTests_FileCommands.cs`
- [x] T017 [P] [US2] E2E test: `file upload data.csv reports/data.csv` uploads to subdirectory - in `IntegrationTests_FileCommands.cs`
- [x] T018 [P] [US2] E2E test: `file upload nonexistent.txt` returns local file not found error - in `IntegrationTests_FileCommands.cs`
- [x] T019 [P] [US2] E2E test: Upload shows checksum verified message - in `IntegrationTests_FileCommands.cs`
- [x] T020 [P] [US2] E2E test: `file upload --force` overwrites without prompting - in `IntegrationTests_FileCommands.cs`
- [ ] T020a [P] [US2] E2E test: `file upload` with existing remote file prompts for confirmation, user confirms 'y' overwrites - in `IntegrationTests_FileCommands.cs`

### Implementation for User Story 2

- [x] T021 [US2] Implement `FileUploadCommand.cs` in `BitPantry.CommandLine.Remote.SignalR.Client/Commands/File/FileUploadCommand.cs`
  - Inject FileTransferService, IAnsiConsole, SignalRServerProxy (for connection check)
  - local-path argument (required)
  - remote-path argument (optional, defaults to filename)
  - --force/-f flag
  - Validate local file exists
  - Check connection status
  - Use FileTransferService.UploadFile() with progress callback
  - Display progress bar, transfer speed, checksum verification

**Checkpoint**: `file upload` fully functional - can upload files with progress. Independent test passes. ✅

---

## Phase 5: User Story 3 - Download Files from Server (Priority: P1)

**Goal**: Users can download files from the remote server with progress feedback

**Independent Test**: Upload file in setup, run `file download`, verify local file matches content

### E2E Tests for User Story 3 (TDD - Write FIRST, verify FAIL)

- [x] T022 [US3] E2E test: `file download config.json` downloads to current directory - in `IntegrationTests_FileCommands.cs`
- [x] T023 [P] [US3] E2E test: `file download reports/data.csv ./local/data.csv` downloads to specified path - in `IntegrationTests_FileCommands.cs`
- [x] T024 [P] [US3] E2E test: `file download nonexistent.txt` returns remote file not found error - in `IntegrationTests_FileCommands.cs`
- [x] T025 [P] [US3] E2E test: Download shows checksum verified message - in `IntegrationTests_FileCommands.cs`
- [x] T026 [P] [US3] E2E test: `file download --force` overwrites local file without prompting - in `IntegrationTests_FileCommands.cs`
- [ ] T026a [P] [US3] E2E test: `file download` with existing local file prompts for confirmation, user confirms 'y' overwrites - in `IntegrationTests_FileCommands.cs`

### Implementation for User Story 3

- [x] T027 [US3] Add `Func<FileDownloadProgress, Task>` callback parameter to `FileTransferService.DownloadFile()` in `BitPantry.CommandLine.Remote.SignalR.Client/FileTransferService.cs`
  - Create `FileDownloadProgress` class (mirror of `FileUploadProgress`)
  - Refactor to use streaming read with `HttpCompletionOption.ResponseHeadersRead`
  - Report progress during byte stream read
  - Existing checksum verification remains intact

- [x] T028 [US3] Implement `FileDownloadCommand.cs` in `BitPantry.CommandLine.Remote.SignalR.Client/Commands/File/FileDownloadCommand.cs`
  - Inject FileTransferService, IAnsiConsole, SignalRServerProxy
  - remote-path argument (required)
  - local-path argument (optional, defaults to current dir + filename)
  - --force/-f flag
  - Check connection status
  - Use FileTransferService.DownloadFile() with progress callback
  - Create local directories as needed
  - Display progress bar, transfer speed, checksum verification

**Checkpoint**: `file download` fully functional. All P1 stories complete - MVP delivered! ✅

---

## Phase 6: User Story 4 - Remove Remote Files (Priority: P2)

**Goal**: Users can delete files and directories on the remote server with confirmation safeguards

**Independent Test**: Upload file, run `file rm --force`, verify file no longer exists

### E2E Tests for User Story 4 (TDD - Write FIRST, verify FAIL)

- [x] T029 [US4] E2E test: `file rm data.csv --force` deletes file - in `IntegrationTests_FileCommands.cs`
- [x] T030 [P] [US4] E2E test: `file rm emptydir/` deletes empty directory - in `IntegrationTests_FileCommands.cs`
- [x] T031 [P] [US4] E2E test: `file rm reports/` without --recursive returns error for non-empty dir - in `IntegrationTests_FileCommands.cs`
- [x] T032 [P] [US4] E2E test: `file rm reports/ --recursive --force` deletes directory tree - in `IntegrationTests_FileCommands.cs`
- [x] T033 [P] [US4] E2E test: `file rm nonexistent.txt` returns path not found error - in `IntegrationTests_FileCommands.cs`
- [x] T034 [P] [US4] E2E test: `file rm ../outside` rejected by SandboxedFileSystem - in `IntegrationTests_FileCommands.cs`
- [ ] T034a [P] [US4] E2E test: `file rm data.csv` prompts for confirmation, user confirms 'y' deletes - in `IntegrationTests_FileCommands.cs`

### Implementation for User Story 4

- [x] T035 [US4] Implement `FileRemoveCommand.cs` in `BitPantry.CommandLine.Remote.SignalR.Server/Commands/File/FileRemoveCommand.cs`
  - Inject IFileSystem, IAnsiConsole
  - path argument (required)
  - --recursive/-r flag
  - --force/-f flag
  - Determine if path is file or directory
  - Prompt for confirmation (unless --force) - requires console input on remote command
  - Use IFileSystem.File.Delete() or IFileSystem.Directory.Delete(path, recursive)
  - Show count of deleted items for recursive operations

**Checkpoint**: `file rm` fully functional with safety prompts ✅

---

## Phase 7: User Story 5 - Create Remote Directories (Priority: P2)

**Goal**: Users can create directories on the remote server

**Independent Test**: Run `file mkdir reports/2026`, verify directory appears in `file ls`

### E2E Tests for User Story 5 (TDD - Write FIRST, verify FAIL)

- [x] T036 [US5] E2E test: `file mkdir reports` creates directory - in `IntegrationTests_FileCommands.cs`
- [x] T037 [P] [US5] E2E test: `file mkdir reports/2026/q1` creates nested directories - in `IntegrationTests_FileCommands.cs`
- [x] T038 [P] [US5] E2E test: `file mkdir reports` when exists succeeds silently (idempotent) - in `IntegrationTests_FileCommands.cs`

### Implementation for User Story 5

- [x] T039 [US5] Implement `FileMkdirCommand.cs` in `BitPantry.CommandLine.Remote.SignalR.Server/Commands/File/FileMkdirCommand.cs`
  - Inject IFileSystem, IAnsiConsole
  - path argument (required)
  - Use IFileSystem.Directory.CreateDirectory() (creates parents, idempotent)
  - Show confirmation message

**Checkpoint**: `file mkdir` fully functional ✅

---

## Phase 8: User Story 6 - View Remote File Contents (Priority: P3)

**Goal**: Users can view text file contents without downloading

**Independent Test**: Upload text file, run `file cat`, verify console output matches content

### E2E Tests for User Story 6 (TDD - Write FIRST, verify FAIL)

- [x] T040 [US6] E2E test: `file cat config.json` displays file contents - in `IntegrationTests_FileCommands.cs`
- [x] T041 [P] [US6] E2E test: `file cat nonexistent.txt` returns file not found error - in `IntegrationTests_FileCommands.cs`
- [x] T042 [P] [US6] E2E test: `file cat largefile.log` truncates output with warning - in `IntegrationTests_FileCommands.cs`
- [ ] T042a [P] [US6] E2E test: `file cat image.png` (binary) shows warning prompt, user confirms 'y' displays content - in `IntegrationTests_FileCommands.cs`

### Implementation for User Story 6

- [x] T043 [US6] Implement `FileCatCommand.cs` in `BitPantry.CommandLine.Remote.SignalR.Server/Commands/File/FileCatCommand.cs`
  - Inject IFileSystem, IAnsiConsole
  - path argument (required)
  - Read file content (streaming for large files)
  - Detect binary files (null bytes in first 8KB), warn before display
  - Truncate output for files > 1 MB with warning

**Checkpoint**: `file cat` fully functional ✅

---

## Phase 9: User Story 7 - View File/Directory Metadata (Priority: P3)

**Goal**: Users can see detailed metadata for files and directories

**Independent Test**: Upload file with known properties, run `file info`, verify output shows correct metadata

### E2E Tests for User Story 7 (TDD - Write FIRST, verify FAIL)

- [x] T044 [US7] E2E test: `file info data.csv` shows file type, size, dates - in `IntegrationTests_FileCommands.cs`
- [x] T045 [P] [US7] E2E test: `file info reports/` shows directory type and dates - in `IntegrationTests_FileCommands.cs`
- [x] T046 [P] [US7] E2E test: `file info nonexistent` returns path not found error - in `IntegrationTests_FileCommands.cs`

### Implementation for User Story 7

- [x] T047 [US7] Implement `FileInfoCommand.cs` in `BitPantry.CommandLine.Remote.SignalR.Server/Commands/File/FileInfoCommand.cs`
  - Inject IFileSystem, IAnsiConsole
  - path argument (required)
  - Use IFileSystem.FileInfo or IFileSystem.DirectoryInfo
  - Display: path, type, size, created date, modified date
  - Format size with human-readable units

**Checkpoint**: `file info` fully functional. All user stories complete! ✅

---

## Phase 10: Polish & Documentation

**Purpose**: Documentation updates and cross-cutting improvements

- [x] T048 Update `Docs/Remote/BuiltInCommands.md` - Add file group to command overview table
- [x] T049 [P] Add `file ls` documentation section to `Docs/Remote/BuiltInCommands.md` (syntax, arguments, behavior, examples)
- [x] T050 [P] Add `file upload` documentation section to `Docs/Remote/BuiltInCommands.md`
- [x] T051 [P] Add `file download` documentation section to `Docs/Remote/BuiltInCommands.md`
- [x] T052 [P] Add `file rm` documentation section to `Docs/Remote/BuiltInCommands.md`
- [x] T053 [P] Add `file mkdir` documentation section to `Docs/Remote/BuiltInCommands.md`
- [x] T054 [P] Add `file cat` documentation section to `Docs/Remote/BuiltInCommands.md`
- [x] T055 [P] Add `file info` documentation section to `Docs/Remote/BuiltInCommands.md`
- [x] T056 Add "See Also" cross-references to FileSystem.md and FileSystemConfiguration.md in `Docs/Remote/BuiltInCommands.md`
- [x] T057 Run full test suite to validate all commands work together
- [ ] T058 Code review and cleanup

**Checkpoint**: Documentation complete ✅

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **Autocomplete (Phase 2.5)**: Depends on Foundational - can run in parallel with user stories
- **User Stories (Phase 3-9)**: All depend on Foundational phase completion
  - Can proceed sequentially in priority order (P1 → P2 → P3)
  - Or in parallel if multiple developers available
- **Polish (Phase 10)**: Depends on all user stories being complete

### User Story Dependencies

| Story | Priority | Depends On | Can Start After |
|-------|----------|------------|-----------------|
| US1 (file ls) | P1 | Foundational | Phase 2 |
| US2 (file upload) | P1 | Foundational | Phase 2 |
| US3 (file download) | P1 | Foundational | Phase 2 |
| US4 (file rm) | P2 | Foundational | Phase 2 |
| US5 (file mkdir) | P2 | Foundational | Phase 2 |
| US6 (file cat) | P3 | Foundational | Phase 2 |
| US7 (file info) | P3 | Foundational | Phase 2 |
| US8 (autocomplete) | P2 | Foundational | Phase 2 |

**Note**: All stories are independent - no cross-story dependencies

### Within Each User Story

1. Write E2E tests FIRST
2. Verify tests FAIL (proves tests are valid)
3. Implement command
4. Verify tests PASS
5. Story complete

### Parallel Opportunities

**Phase 1-2**: 
- T001-T004 can run in parallel (different directories)
- T005-T007 mostly parallel (different packages)

**Within Each Story**:
- All E2E tests marked [P] can be written in parallel
- Implementation is sequential (one command per story)

**Cross-Story Parallel** (if team capacity):
- After Phase 2, all 7 user stories can be worked on in parallel by different developers
- Each story is independently testable

---

## Parallel Example: User Story 1

```bash
# Write all E2E tests in parallel:
T008: E2E test file ls lists files in storage root
T009: [P] E2E test file ls reports/ lists subdirectory  
T010: [P] E2E test file ls --long shows details
T011: [P] E2E test file ls --recursive
T012: [P] E2E test file ls nonexistent/ error
T013: [P] E2E test path traversal rejection
T014: [P] E2E test disconnected error

# Then implement (sequential):
T015: Implement FileListCommand.cs
```

---

## Implementation Strategy

### MVP First (User Stories 1-3 = P1 priority)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (`file ls`)
4. Complete Phase 4: User Story 2 (`file upload`)
5. Complete Phase 5: User Story 3 (`file download`)
6. **STOP and VALIDATE**: All P1 stories complete - MVP ready!
7. Deploy/demo if ready

### Incremental Delivery

| Increment | Stories Included | Value Delivered |
|-----------|------------------|-----------------|
| MVP | US1-3 (ls, upload, download) | Core file operations |
| +P2 | +US4-5 (rm, mkdir) | File management |
| +P3 | +US6-7 (cat, info) | Quick inspection |
| Complete | All + docs | Full feature with documentation |

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 66 |
| **Phase 1 (Setup)** | 4 |
| **Phase 2 (Foundational)** | 3 |
| **Phase 2.5 (Autocomplete)** | 4 |
| **User Story 1 (file ls)** | 8 |
| **User Story 2 (file upload)** | 7 |
| **User Story 3 (file download)** | 8 |
| **User Story 4 (file rm)** | 8 |
| **User Story 5 (file mkdir)** | 4 |
| **User Story 6 (file cat)** | 5 |
| **User Story 7 (file info)** | 4 |
| **User Story 8 (autocomplete)** | 4 |
| **Phase 10 (Polish/Docs)** | 11 |
| **Parallelizable [P]** | 43 |
| **MVP Scope (P1 only)** | T001-T028 (30 tasks) |

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- TDD strictly enforced: Write tests → Verify FAIL → Implement → Verify PASS
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
