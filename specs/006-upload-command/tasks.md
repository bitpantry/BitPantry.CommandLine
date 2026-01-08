# Tasks: Upload Command

**Input**: Design documents from `/specs/006-upload-command/`  
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## Path Conventions

- **Client Library**: `BitPantry.CommandLine.Remote.SignalR.Client/`
- **Server Library**: `BitPantry.CommandLine.Remote.SignalR.Server/`
- **Tests**: `BitPantry.CommandLine.Tests.Remote.SignalR/`

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Prepare project structure and dependencies for Upload Command implementation

- [ ] T001 Add Microsoft.Extensions.FileSystemGlobbing NuGet package to BitPantry.CommandLine.Remote.SignalR.Client.csproj
- [ ] T002 [P] Create UploadConstants.cs with BatchExistsChunkSize=100 and MaxConcurrentUploads=4 in BitPantry.CommandLine.Remote.SignalR.Client/UploadConstants.cs
- [ ] T003 [P] Create empty UploadCommand.cs class skeleton with DI constructor in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T004 [P] Create empty UploadCommandTests.cs test class in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T005 [P] Create empty FilesExistEndpointTests.cs test class in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/FilesExistEndpointTests.cs
- [ ] T006 [P] Create empty IntegrationTests_UploadCommand.cs test class in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundational Components

- [ ] T007 [P] Write unit tests for FilesExistRequest/FilesExistResponse DTOs (implements CV-025, CV-026, CV-027) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/FilesExistEndpointTests.cs
- [ ] T008 [P] Write unit tests for FileTransferService.CheckFilesExist method with chunking (implements CV-022, CV-028, CV-029) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/FileTransferServiceTests.cs
- [ ] T009 [P] Write unit tests for FileUploadResponse DTO with status handling (implements CV-030, CV-031) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/FileTransferServiceTests.cs

### Implementation for Foundational Components

- [ ] T010 [P] Create FilesExistRequest record (Directory, Filenames[]) in BitPantry.CommandLine.Remote.SignalR/Envelopes/FilesExistRequest.cs
- [ ] T011 [P] Create FilesExistResponse record (Exists dictionary) in BitPantry.CommandLine.Remote.SignalR/Envelopes/FilesExistResponse.cs
- [ ] T012 [P] Create FileUploadResponse record (Status, Reason?, BytesWritten?) in BitPantry.CommandLine.Remote.SignalR/Envelopes/FileUploadResponse.cs
- [ ] T013 Implement POST /files/exists endpoint with authorization in BitPantry.CommandLine.Remote.SignalR.Server/FilesExistEndpoint.cs
- [ ] T014 Register FilesExistEndpoint in server endpoint configuration in BitPantry.CommandLine.Remote.SignalR.Server/ServerLogic.cs
- [ ] T015 Add CheckFilesExist method with chunking logic (BATCH_EXISTS_CHUNK_SIZE=100) to BitPantry.CommandLine.Remote.SignalR.Client/FileTransferService.cs
- [ ] T016 Add skipIfExists parameter to UploadFile method and update server handler in BitPantry.CommandLine.Remote.SignalR.Client/FileTransferService.cs
- [ ] T017 Update file upload endpoint to handle skipIfExists parameter with FileUploadResponse in BitPantry.CommandLine.Remote.SignalR.Server/ (upload handler)

**Checkpoint**: Foundation ready - FilesExistEndpoint operational, CheckFilesExist method available, skipIfExists parameter supported

---

## Phase 3: User Story 1 - Single File Upload (Priority: P1) 🎯 MVP

**Goal**: Upload a single file to a remote server with success message

**Independent Test**: `server upload myfile.txt /remote/` uploads file and displays "Uploaded myfile.txt to /remote/"

### Tests for User Story 1

- [ ] T018 [P] [US1] Write test: UploadCommand verifies connection before upload (implements CV-009, CV-010, CV-011, UX-007, EH-001) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T019 [P] [US1] Write test: UploadCommand with valid single file returns success (implements UX-001, DF-001) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T020 [P] [US1] Write test: UploadCommand with missing single file returns error (implements UX-009, CV-003, DF-002, EH-002) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T021 [P] [US1] Write test: UploadCommand without arguments returns error (implements UX-008) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T022 [P] [US1] Write test: UploadCommand destination as directory appends filename (implements UX-011, DF-010) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T023 [P] [US1] Write test: UploadCommand destination as file uses path as-is (implements UX-012, DF-011) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T024 [P] [US1] Write integration test: end-to-end single file upload (implements IT-001) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

### Implementation for User Story 1

- [ ] T025 [US1] Add UploadCommand registration with [Command] attribute under ServerGroup in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T026 [US1] Add Source and Destination positional arguments with [Argument] attributes in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T027 [US1] Implement connection state verification (return 1 if not connected) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T028 [US1] Implement ExpandSource method for literal file path detection (CV-002, CV-003) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T029 [US1] Implement UploadSingleFileAsync calling FileTransferService.UploadFile in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T030 [US1] Implement destination path resolution (directory vs file) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T031 [US1] Implement success output "Uploaded {filename} to {destination}" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T032 [US1] Implement file not found error handling with proper message in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: Single file upload operational - `server upload file.txt /remote/` works

---

## Phase 4: User Story 2 - Multi-File Upload with Glob Pattern (Priority: P2)

**Goal**: Upload multiple files using wildcard patterns with client-side glob expansion

**Independent Test**: `server upload *.txt /remote/` uploads all .txt files and shows "Uploaded 3 files to /remote/"

### Tests for User Story 2

- [ ] T033 [P] [US2] Write test: ExpandSource with glob pattern *.txt returns matching files (implements CV-004, DF-009) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T034 [P] [US2] Write test: ExpandSource with glob pattern matching zero files (implements CV-005, UX-010, EH-003) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T035 [P] [US2] Write test: ExpandSource with ? wildcard pattern (implements CV-006) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T036 [P] [US2] Write test: ExpandSource with relative vs absolute paths (implements CV-007, CV-008) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T037 [P] [US2] Write test: ExpandSource with recursive **/*.txt pattern (implements CV-020, CV-021, UX-013, DF-012) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T038 [P] [US2] Write test: UploadMultipleFilesAsync uploads all files with summary (implements UX-002) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T039 [P] [US2] Write test: UploadMultipleFilesAsync with max concurrency 4 (implements CV-015) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T040 [P] [US2] Write test: Multi-file with partial failure continues and summarizes (implements CV-017, DF-005, EH-006, EH-007, EH-012) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T041 [P] [US2] Write test: Multi-file with some files not found (implements DF-006, EH-008, EH-013) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T042 [P] [US2] Write integration test: end-to-end multi-file upload (implements IT-002) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [ ] T043 [P] [US2] Write integration test: recursive glob upload (implements IT-006) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

### Implementation for User Story 2

- [ ] T044 [US2] Extend ExpandSource to use Microsoft.Extensions.FileSystemGlobbing Matcher in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T045 [US2] Implement ParseGlobPattern to extract base directory and pattern in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T046 [US2] Implement UploadMultipleFilesAsync with SemaphoreSlim concurrency control in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T047 [US2] Add thread-safe success/failure counting with Interlocked in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T048 [US2] Implement partial failure handling - continue on error, collect failures in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T049 [US2] Implement multi-file summary output "Uploaded X of Y files to {destination}" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T050 [US2] Implement zero-match warning "No files matched pattern: {pattern}" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T051 [US2] Implement not-found files tracking and summary in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: Glob upload operational - `server upload *.txt /remote/` works with proper concurrency

---

## Phase 5: User Story 3 - Progress Display (Priority: P3)

**Goal**: Show upload progress for large files and concurrent multi-file uploads

**Independent Test**: Upload file >= 1MB shows progress bar; multi-file shows progress table

### Tests for User Story 3

- [ ] T052 [P] [US3] Write test: Single file >= 1MB displays progress bar (implements UX-003, CV-012, CV-018) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T053 [P] [US3] Write test: Single file < 1MB uploads without progress display (implements UX-004, CV-013) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T054 [P] [US3] Write test: Multi-file upload shows all progress tasks upfront (implements UX-005, CV-014) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T055 [P] [US3] Write test: Progress task updates to Completed on success (implements CV-016, DF-001) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T056 [P] [US3] Write test: Progress task updates to Failed on exception (implements CV-017, DF-003) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T057 [P] [US3] Write test: Progress callback calculates percentage from TotalRead/FileSize (implements DF-007) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T058 [P] [US3] Write test: Progress callback handles error in FileUploadProgress (implements DF-008) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T059 [P] [US3] Write integration test: upload with progress callback (implements IT-003) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

### Implementation for User Story 3

- [ ] T060 [US3] Implement file size check (>= 1MB threshold) for progress display decision in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T061 [US3] Implement single-file Spectre.Console progress bar with TaskDescriptionColumn, ProgressBarColumn, PercentageColumn, SpinnerColumn in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T062 [US3] Implement progress callback calculating percentage from TotalRead/FileSize*100 in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T063 [US3] Implement multi-file progress table with all tasks created upfront as "Pending" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T064 [US3] Implement task description updates: "[green]Completed[/]", "[red]Failed[/]", "[yellow]Skipped[/]" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T065 [US3] Handle missing files in progress display (skip from table, include in summary) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: Progress display operational - large files show progress, multi-file shows concurrent table

---

## Phase 6: User Story 4 - Upload from External Shell (Priority: P4)

**Goal**: Enable upload command invocation from external shells with proper glob handling

**Independent Test**: From PowerShell, `dotnet run -- server upload "*.txt" /remote/` uploads matching files

### Tests for User Story 4

- [ ] T066 [P] [US4] Write test: Quoted glob pattern from external shell preserved (implements UX-006) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T067 [P] [US4] Write test: Unquoted literal file path works from external shell (implements US-004) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs

### Implementation for User Story 4

- [ ] T068 [US4] Add help text documenting quoting requirement for glob patterns in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T069 [US4] Verify ExpandSource handles shell-quoted patterns correctly in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: External shell invocation operational - quoted globs work correctly

---

## Phase 7: User Story 5 - Skip Existing Files (Priority: P5)

**Goal**: Allow non-destructive batch uploads with --skip-existing flag

**Independent Test**: `server upload *.txt /remote/ --skip-existing` skips files that exist on server

### Tests for User Story 5

- [ ] T070 [P] [US5] Write test: --skip-existing flag calls CheckFilesExist (implements CV-023, DF-013) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T071 [P] [US5] Write test: No flag does not call CheckFilesExist (implements CV-024) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T072 [P] [US5] Write test: Files existing on server are skipped with warning (implements UX-014, DF-014) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T073 [P] [US5] Write test: Short flag -s works same as --skip-existing (implements UX-015) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T074 [P] [US5] Write test: All files existing shows "0 files uploaded. X skipped." (implements UX-016, DF-015) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T075 [P] [US5] Write test: CheckFilesExist fails falls back to upload all (implements EH-014, EH-015) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T076 [P] [US5] Write test: Server returns "skipped" for TOCTOU race (implements CV-031, DF-017) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T077 [P] [US5] Write test: Large batch (250 files) triggers chunked requests (implements DF-016) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T078 [P] [US5] Write integration test: skip existing integration (implements IT-007) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [ ] T079 [P] [US5] Write integration test: batch exists check (implements IT-008) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [ ] T080 [P] [US5] Write integration test: overwrite existing (default) (implements IT-009, DF-018) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [ ] T081 [P] [US5] Write integration test: server-side skip TOCTOU (implements IT-010) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [ ] T082 [P] [US5] Write integration test: large batch exists check (250 files) (implements IT-011) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

### Implementation for User Story 5

- [ ] T083 [US5] Add --skip-existing / -s option with [Option] attribute in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T084 [US5] Implement pre-upload existence check calling FileTransferService.CheckFilesExist in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T085 [US5] Implement file filtering based on existence check results in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T086 [US5] Implement skipped file warning "Skipped (exists): {filename}" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T087 [US5] Pass skipIfExists=true to UploadFile when --skip-existing is set in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T088 [US5] Handle server "skipped" response (TOCTOU case) with "Skipped (server)" message in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T089 [US5] Implement skipped count in summary "Uploaded X files. Y skipped (already exist)." in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T090 [US5] Implement fallback to upload all if CheckFilesExist fails (log warning) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: Skip existing operational - `server upload *.txt /remote/ --skip-existing` works

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T091 [P] Write test: Cancellation token cancels upload (implements CV-019, EH-009) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T092 [P] Write test: Permission denied errors (implements EH-004, EH-005) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T093 [P] Write test: Connection lost during upload (implements EH-010) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T094 [P] Write test: Invalid destination path (implements EH-011) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T095 [P] Write test: Recursive glob in inaccessible directory (implements EH-016) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T096 [P] Write test: Server returns unknown status (implements EH-017) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [ ] T097 [P] Write integration test: upload when disconnected (implements IT-004) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [ ] T098 [P] Write integration test: upload with cancellation (implements IT-005) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [ ] T099 Implement CancellationToken support throughout UploadCommand in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T100 Add permission error handling with proper messaging in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T101 Add connection lost error handling in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [ ] T102 [P] Run quickstart.md validation scenarios in sandbox environment
- [ ] T103 Code cleanup and refactoring pass for UploadCommand.cs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) - MVP
- **User Story 2 (Phase 4)**: Depends on Phase 3 (extends ExpandSource, adds multi-file)
- **User Story 3 (Phase 5)**: Depends on Phase 4 (adds progress to existing upload logic)
- **User Story 4 (Phase 6)**: Depends on Phase 4 (documents shell behavior)
- **User Story 5 (Phase 7)**: Depends on Foundational (CheckFilesExist), Phase 4 (multi-file support)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1**: MVP - basic single file upload (no dependencies on other stories)
- **US2**: Extends US1 with glob expansion and multi-file support
- **US3**: Extends US1/US2 with progress display
- **US4**: Documentation/verification of US2 shell behavior
- **US5**: Uses Foundational CheckFilesExist + US2 multi-file infrastructure

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Models/DTOs before services
- Services before command logic
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

**Phase 1 (Setup)**: T002, T003, T004, T005, T006 can run in parallel

**Phase 2 (Foundational)**: 
- T007, T008, T009 (tests) can run in parallel
- T010, T011, T012 (DTOs) can run in parallel

**Each User Story Phase**:
- All tests marked [P] can run in parallel
- Tests must fail before implementation begins

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
T018: Test connection verification
T019: Test valid single file upload
T020: Test missing file error
T021: Test missing arguments error
T022: Test destination as directory
T023: Test destination as file
T024: Integration test for single file
```

## Parallel Example: User Story 5

```bash
# Launch all tests for User Story 5 together:
T070-T082: All skip-existing tests can run in parallel
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Single File Upload)
4. **STOP and VALIDATE**: Test `server upload file.txt /remote/`
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 → Single file upload works → **MVP!**
3. US2 → Glob patterns work → Multi-file capability
4. US3 → Progress display → User-friendly experience
5. US4 → Shell documentation → External script support
6. US5 → Skip existing → Non-destructive batch uploads
7. Polish → Edge cases, error handling hardened

---

## Test Case Coverage Summary

| Category | Test Case IDs | Task Coverage |
|----------|---------------|---------------|
| UX (User Experience) | UX-001 to UX-016 | T019-T024, T033-T043, T052-T059, T066-T067, T070-T082 |
| CV (Component/Unit) | CV-001 to CV-031 | T007-T009, T018-T024, T033-T041, T052-T058, T070-T077 |
| DF (Data Flow) | DF-001 to DF-018 | T019-T024, T033-T041, T055-T058, T070-T082 |
| EH (Error Handling) | EH-001 to EH-017 | T018, T020, T040-T041, T075-T076, T091-T098 |
| IT (Integration) | IT-001 to IT-011 | T024, T042-T043, T059, T078-T082, T097-T098 |

**Total Test Cases**: 72 (UX-16, CV-31, DF-18, EH-17, IT-11 with overlap)
**Total Test Tasks**: 52

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All test tasks include "(implements X-XXX)" referencing test case IDs
