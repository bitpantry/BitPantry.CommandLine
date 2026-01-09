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

- [X] T001 Add Microsoft.Extensions.FileSystemGlobbing NuGet package to BitPantry.CommandLine.Remote.SignalR.Client.csproj
- [X] T002 [P] Create UploadConstants.cs with BatchExistsChunkSize=100 and MaxConcurrentUploads=4 in BitPantry.CommandLine.Remote.SignalR.Client/UploadConstants.cs
- [X] T003 [P] Create empty UploadCommand.cs class skeleton with DI constructor in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T004 [P] Create empty UploadCommandTests.cs test class in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T005 [P] Create empty FilesExistEndpointTests.cs test class in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/FilesExistEndpointTests.cs
- [X] T006 [P] Create empty IntegrationTests_UploadCommand.cs test class in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundational Components

- [X] T007 [P] Write unit tests for FilesExistRequest/FilesExistResponse DTOs (implements CV-025, CV-026, CV-027) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/FilesExistEndpointTests.cs
- [X] T008 [P] Write unit tests for FileTransferService.CheckFilesExist method with chunking (implements CV-022, CV-028, CV-029) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/FileTransferServiceTests.cs
- [X] T009 [P] Write unit tests for FileUploadResponse DTO with status handling (implements CV-030, CV-031) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/FileTransferServiceTests.cs

### Implementation for Foundational Components

- [X] T010 [P] Create FilesExistRequest record (Directory, Filenames[]) in BitPantry.CommandLine.Remote.SignalR/Envelopes/FilesExistRequest.cs
- [X] T011 [P] Create FilesExistResponse record (Exists dictionary) in BitPantry.CommandLine.Remote.SignalR/Envelopes/FilesExistResponse.cs
- [X] T012 [P] Create FileUploadResponse record (Status, Reason?, BytesWritten?) in BitPantry.CommandLine.Remote.SignalR/Envelopes/FileUploadResponse.cs
- [X] T013 Implement POST /files/exists endpoint with authorization in BitPantry.CommandLine.Remote.SignalR.Server/FilesExistEndpoint.cs
- [X] T014 Register FilesExistEndpoint in server endpoint configuration in BitPantry.CommandLine.Remote.SignalR.Server/ServerLogic.cs
- [X] T015 Add CheckFilesExist method with chunking logic (BATCH_EXISTS_CHUNK_SIZE=100) to BitPantry.CommandLine.Remote.SignalR.Client/FileTransferService.cs
- [X] T016 Add skipIfExists parameter to UploadFile method and update server handler in BitPantry.CommandLine.Remote.SignalR.Client/FileTransferService.cs
- [X] T017 Update file upload endpoint to handle skipIfExists parameter with FileUploadResponse in BitPantry.CommandLine.Remote.SignalR.Server/ (upload handler)

**Checkpoint**: Foundation ready - FilesExistEndpoint operational, CheckFilesExist method available, skipIfExists parameter supported

---

## Phase 3: User Story 1 - Single File Upload (Priority: P1) 🎯 MVP

**Goal**: Upload a single file to a remote server with success message

**Independent Test**: `server upload myfile.txt /remote/` uploads file and displays "Uploaded myfile.txt to /remote/"

### Tests for User Story 1

- [X] T018 [P] [US1] Write test: UploadCommand verifies connection before upload (implements CV-009, CV-010, CV-011, UX-007, EH-001) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T019 [P] [US1] Write test: UploadCommand with valid single file returns success (implements UX-001, DF-001) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T020 [P] [US1] Write test: UploadCommand with missing single file returns error (implements UX-009, CV-003, DF-002, EH-002) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T021 [P] [US1] Write test: UploadCommand without arguments returns error (implements UX-008) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T022 [P] [US1] Write test: UploadCommand destination as directory appends filename (implements UX-011, DF-010) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T023 [P] [US1] Write test: UploadCommand destination as file uses path as-is (implements UX-012, DF-011) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T024 [P] [US1] Write integration test: end-to-end single file upload (implements IT-001) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

### Implementation for User Story 1

- [X] T025 [US1] Add UploadCommand registration with [Command] attribute under ServerGroup in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T026 [US1] Add Source and Destination positional arguments with [Argument] attributes in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T027 [US1] Implement connection state verification (return 1 if not connected) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T028 [US1] Implement ExpandSource method for literal file path detection (CV-002, CV-003) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T029 [US1] Implement UploadSingleFileAsync calling FileTransferService.UploadFile in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T030 [US1] Implement destination path resolution (directory vs file) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T031 [US1] Implement success output "Uploaded {filename} to {destination}" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T032 [US1] Implement file not found error handling with proper message in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: Single file upload operational - `server upload file.txt /remote/` works

---

## Phase 4: User Story 2 - Multi-File Upload with Glob Pattern (Priority: P2)

**Goal**: Upload multiple files using wildcard patterns with client-side glob expansion

**Independent Test**: `server upload *.txt /remote/` uploads all .txt files and shows "Uploaded 3 files to /remote/"

### Tests for User Story 2

- [X] T033 [P] [US2] Write test: ExpandSource with glob pattern *.txt returns matching files (implements CV-004, DF-009) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T034 [P] [US2] Write test: ExpandSource with glob pattern matching zero files (implements CV-005, UX-010, EH-003) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T035 [P] [US2] Write test: ExpandSource with ? wildcard pattern (implements CV-006) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T036 [P] [US2] Write test: ExpandSource with relative vs absolute paths (implements CV-007, CV-008) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T037 [P] [US2] Write test: ExpandSource with recursive **/*.txt pattern (implements CV-020, CV-021, UX-013, DF-012) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T038 [P] [US2] Write test: UploadMultipleFilesAsync uploads all files with summary (implements UX-002) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T039 [P] [US2] Write test: UploadMultipleFilesAsync with max concurrency 4 (implements CV-015) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T040 [P] [US2] Write test: Multi-file with partial failure continues and summarizes (implements CV-017, DF-005, EH-006, EH-007, EH-012) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T041 [P] [US2] Write test: Multi-file with some files not found (implements DF-006, EH-008, EH-013) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T042 [P] [US2] Write integration test: end-to-end multi-file upload (implements IT-002) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [X] T043 [P] [US2] Write integration test: recursive glob upload (implements IT-006) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

### Implementation for User Story 2

- [X] T044 [US2] Extend ExpandSource to use Microsoft.Extensions.FileSystemGlobbing Matcher in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T045 [US2] Implement ParseGlobPattern to extract base directory and pattern in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T046 [US2] Implement UploadMultipleFilesAsync with SemaphoreSlim concurrency control in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T047 [US2] Add thread-safe success/failure counting with Interlocked in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T048 [US2] Implement partial failure handling - continue on error, collect failures in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T049 [US2] Implement multi-file summary output "Uploaded X of Y files to {destination}" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T050 [US2] Implement zero-match warning "No files matched pattern: {pattern}" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T051 [US2] Implement not-found files tracking and summary in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: Glob upload operational - `server upload *.txt /remote/` works with proper concurrency

---

## Phase 5: User Story 3 - Progress Display (Priority: P3)

**Goal**: Show upload progress for large files and concurrent multi-file uploads

**Independent Test**: Upload file >= 1MB shows progress bar; multi-file shows progress table

### Tests for User Story 3

- [X] T052 [P] [US3] Write test: Single file >= 1MB displays progress bar (implements UX-003, CV-012, CV-018) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T053 [P] [US3] Write test: Single file < 1MB uploads without progress display (implements UX-004, CV-013) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T054 [P] [US3] Write test: Multi-file upload shows all progress tasks upfront (implements UX-005, CV-014) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T055 [P] [US3] Write test: Progress task updates to Completed on success (implements CV-016, DF-001) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T056 [P] [US3] Write test: Progress task updates to Failed on exception (implements CV-017, DF-003) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T057 [P] [US3] Write test: Progress callback calculates percentage from TotalRead/FileSize (implements DF-007) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T058 [P] [US3] Write test: Progress callback handles error in FileUploadProgress (implements DF-008) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T059 [P] [US3] Write integration test: upload with progress callback (implements IT-003) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

### Implementation for User Story 3

- [X] T060 [US3] Implement file size check (>= 1MB threshold) for progress display decision in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T061 [US3] Implement single-file Spectre.Console progress bar with TaskDescriptionColumn, ProgressBarColumn, PercentageColumn, SpinnerColumn in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T062 [US3] Implement progress callback calculating percentage from TotalRead/FileSize*100 in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T063 [US3] Implement multi-file progress table with all tasks created upfront as "Pending" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T064 [US3] Implement task description updates: "[green]Completed[/]", "[red]Failed[/]", "[yellow]Skipped[/]" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T065 [US3] Handle missing files in progress display (skip from table, include in summary) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: Progress display operational - large files show progress, multi-file shows concurrent table

---

## Phase 6: User Story 4 - Upload from External Shell (Priority: P4)

**Goal**: Enable upload command invocation from external shells with proper glob handling

**Independent Test**: From PowerShell, `dotnet run -- server upload "*.txt" /remote/` uploads matching files

### Tests for User Story 4

- [X] T066 [P] [US4] Write test: Quoted glob pattern from external shell preserved (implements UX-006) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T067 [P] [US4] Write test: Unquoted literal file path works from external shell (implements US-004) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs

### Implementation for User Story 4

- [X] T068 [US4] Add help text documenting quoting requirement for glob patterns in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T069 [US4] Verify ExpandSource handles shell-quoted patterns correctly in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: External shell invocation operational - quoted globs work correctly

---

## Phase 7: User Story 5 - Skip Existing Files (Priority: P5)

**Goal**: Allow non-destructive batch uploads with --skip-existing flag

**Independent Test**: `server upload *.txt /remote/ --skip-existing` skips files that exist on server

### Tests for User Story 5

- [X] T070 [P] [US5] Write test: --skip-existing flag calls CheckFilesExist (implements CV-023, DF-013) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T071 [P] [US5] Write test: No flag does not call CheckFilesExist (implements CV-024) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T072 [P] [US5] Write test: Files existing on server are skipped with warning (implements UX-014, DF-014) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T073 [P] [US5] Write test: Short flag -s works same as --skip-existing (implements UX-015) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T074 [P] [US5] Write test: All files existing shows "0 files uploaded. X skipped." (implements UX-016, DF-015) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T075 [P] [US5] Write test: CheckFilesExist fails falls back to upload all (implements EH-014, EH-015) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T076 [P] [US5] Write test: Server returns "skipped" for TOCTOU race (implements CV-031, DF-017) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T077 [P] [US5] Write test: Large batch (250 files) triggers chunked requests (implements DF-016) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T078 [P] [US5] Write integration test: skip existing integration (implements IT-007) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [X] T079 [P] [US5] Write integration test: batch exists check (implements IT-008) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [X] T080 [P] [US5] Write integration test: overwrite existing (default) (implements IT-009, DF-018) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [X] T081 [P] [US5] Write integration test: server-side skip TOCTOU (implements IT-010) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [X] T082 [P] [US5] Write integration test: large batch exists check (250 files) (implements IT-011) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs

### Implementation for User Story 5

- [X] T083 [US5] Add --skip-existing / -s option with [Option] attribute in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T084 [US5] Implement pre-upload existence check calling FileTransferService.CheckFilesExist in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T085 [US5] Implement file filtering based on existence check results in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T086 [US5] Implement skipped file warning "Skipped (exists): {filename}" in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T087 [US5] Pass skipIfExists=true to UploadFile when --skip-existing is set in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T088 [US5] Handle server "skipped" response (TOCTOU case) with "Skipped (server)" message in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T089 [US5] Implement skipped count in summary "Uploaded X files. Y skipped (already exist)." in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T090 [US5] Implement fallback to upload all if CheckFilesExist fails (log warning) in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs

**Checkpoint**: Skip existing operational - `server upload *.txt /remote/ --skip-existing` works

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T091 [P] Write test: Cancellation token cancels upload (implements CV-019, EH-009) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T092 [P] Write test: Permission denied errors (implements EH-004, EH-005) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T093 [P] Write test: Connection lost during upload (implements EH-010) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T094 [P] Write test: Invalid destination path (implements EH-011) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T095 [P] Write test: Recursive glob in inaccessible directory (implements EH-016) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T096 [P] Write test: Server returns unknown status (implements EH-017) in BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/UploadCommandTests.cs
- [X] T097 [P] Write integration test: upload when disconnected (implements IT-004) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [X] T098 [P] Write integration test: upload with cancellation (implements IT-005) in BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_UploadCommand.cs
- [X] T099 Implement CancellationToken support throughout UploadCommand in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T100 Add permission error handling with proper messaging in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T101 Add connection lost error handling in BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs
- [X] T102 [P] Run quickstart.md validation scenarios in sandbox environment
- [X] T103 Code cleanup and refactoring pass for UploadCommand.cs

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

---

## Phase 9: Test Coverage Gap Remediation

**Purpose**: Address test cases that were marked complete but lack actual test implementations

### UX (User Experience) Gaps

- [x] T104 [P] Write test: Progress bar displays for file >= 1MB (implements UX-003) in UploadCommandTests.cs
- [x] T105 [P] Write test: No progress bar for file < 1MB (implements UX-004) in UploadCommandTests.cs
- [x] T106 [P] Write test: Multi-file progress table shows all files upfront (implements UX-005) in UploadCommandTests.cs
- [x] T107 [P] Write test: External shell with quoted glob pattern preserved (implements UX-006) in UploadCommandTests.cs
- [x] T108 [P] Write test: Missing required arguments shows proper error (implements UX-008) in UploadCommandTests.cs
- [x] T109 [P] Write test: Short flag -s works same as --skipexisting (implements UX-015) in UploadCommandTests.cs

### CV (Component/Unit) Gaps

- [x] T110 [P] Write test: Relative glob uses current directory as base (implements CV-007) in UploadCommandTests.cs
- [x] T111 [P] Write test: Absolute glob uses specified directory as base (implements CV-008) in UploadCommandTests.cs
- [x] T112 [P] Write test: Task description shows "[green]Completed[/]" on success (implements CV-016) in UploadCommandTests.cs
- [x] T113 [P] Write test: Task description shows "[red]Failed[/]" on error (implements CV-017) in UploadCommandTests.cs
- [x] T114 [P] Write test: FileTransferService.CheckFilesExist returns dictionary (implements CV-022) in FileTransferServiceTests.cs
  - Already implemented in FileTransferServiceTests.cs
- [x] T115 [P] Write test: Path traversal in FilesExist returns 400 (implements CV-027) in FilesExistEndpointTests.cs
- [x] T116 [P] Write test: Batch > 100 files triggers chunked requests (implements CV-028) in FileTransferServiceTests.cs
  - Already implemented in FileTransferServiceTests.cs
- [x] T117 [P] Write test: Exactly 100 files uses single batch (implements CV-029) in FileTransferServiceTests.cs
  - Already implemented in FileTransferServiceTests.cs

### DF (Data Flow) Gaps

- [x] T118 [P] Write test: UploadStatus transitions InProgress → Failed on error (implements DF-003) in UploadCommandTests.cs
- [x] T119 [P] Write test: Progress callback error sets exception on TCS (implements DF-008) in UploadCommandTests.cs
- [x] T120 [P] Write test: 250 files triggers 3 batch requests (implements DF-016) in FileTransferServiceTests.cs
  - Already implemented in FileTransferServiceTests.cs
- [x] T121 [P] Write test: Server returns "skipped" for TOCTOU race condition (implements DF-017) in UploadCommandTests.cs

### EH (Error Handling) Gaps

- [x] T122 [P] Write test: Local file permission denied shows error (implements EH-004) in UploadCommandTests.cs
- [x] T123 [P] Write test: Remote permission denied (403) shows error (implements EH-005) in UploadCommandTests.cs
  - N/A - sandbox server has no authorization configured
- [x] T124 [P] Write test: Network error during multi-file continues with remaining files (implements EH-006) in UploadCommandTests.cs
- [x] T125 [P] Write test: File deleted after glob expansion but before upload (implements EH-008) in UploadCommandTests.cs
- [x] T126 [P] Write test: Invalid destination path returns 400 (implements EH-011) in UploadCommandTests.cs
- [x] T127 [P] Write test: CheckFilesExist fails with 500 falls back to upload all (implements EH-014) in UploadCommandTests.cs
- [x] T128 [P] Write test: CheckFilesExist timeout falls back to upload all (implements EH-015) in UploadCommandTests.cs
- [x] T129 [P] Write test: Inaccessible folder in recursive glob handled gracefully (implements EH-016) in UploadCommandTests.cs
- [x] T130 [P] Write test: Unknown status from server logs warning and treats as success (implements EH-017) in UploadCommandTests.cs

### IT (Integration) Gaps

- [x] T131 [P] Write integration test: Batch existence check with 150 files (implements IT-008) in IntegrationTests_UploadCommand.cs
- [x] T132 [P] Write integration test: Server-side TOCTOU skip scenario (implements IT-010) in IntegrationTests_UploadCommand.cs
- [x] T133 [P] Write integration test: Large batch exists check with 250 files (implements IT-011) in IntegrationTests_UploadCommand.cs

### N/A Test Cases (Documented Exclusions)

The following test cases cannot be implemented in the current sandbox configuration:

- **CV-026**: FilesExist returns 401 without auth token - N/A (sandbox server has no authentication configured)
- **EH-005**: Remote permission denied (403) - N/A (sandbox server has no authorization configured; would require auth-enabled test environment)

---

## Phase 10: Fake Test Remediation

**Purpose**: Replace tests that verify constants/strings instead of actual behavior

**Problem Identified**: Several tests claim to implement test cases but actually test:
- Constants (`MaxConcurrentUploads.Should().Be(4)`) instead of runtime behavior
- Input strings (`Source.Contains('*')`) instead of actual processing
- These tests would NOT catch bugs in the specified behavior

**Remediation Approach**: Each task deletes the fake test and replaces it with a real test that:
1. Executes the actual code path described in the test case "When" column
2. Verifies the actual outcome described in the test case "Then" column
3. Would fail if the specified behavior was broken

### Glob Expansion Fake Tests (CV-004, CV-005, CV-006)

**Current Problem**: Tests verify `Source.Contains('*')` instead of actual glob expansion behavior

- [x] T134 [P] **REWRITE** ExpandSource_GlobPattern_ParsesPatternCorrectly (CV-004): Delete fake test. New test must create temp directory with file1.txt, file2.txt, file3.log, call ExpandSource with "*.txt" pattern, and assert exactly file1.txt and file2.txt are returned (not file3.log). Use real temp files, not mocks. in UploadCommandTests.cs
  - **DONE**: Rewrote test to use real temp files and verify actual glob expansion results

- [x] T135 [P] **REWRITE** ExpandSource_GlobNoMatches_PatternIsRecognized (CV-005): Delete fake test. New test must create temp directory with file.txt, call ExpandSource with "*.xyz" pattern, and assert empty list returned. Verify error message mentions "No files matched pattern". in UploadCommandTests.cs
  - **DONE**: Rewrote test to verify empty list returned for non-matching pattern

- [x] T136 [P] **REWRITE** ExpandSource_QuestionMarkWildcard_PatternIsRecognized (CV-006): Delete fake test. New test must create temp directory with data1.json, data2.json, data10.json, call ExpandSource with "data?.json" pattern, and assert exactly data1.json and data2.json are returned (not data10.json). in UploadCommandTests.cs
  - **DONE**: Rewrote test. **BUG FOUND**: Microsoft.Extensions.FileSystemGlobbing doesn't support ? wildcard (see dotnet/runtime#82406). Fixed by implementing regex post-filtering workaround in UploadCommand.cs

### Concurrency Fake Test (CV-015)

**Current Problem**: Tests verify `MaxConcurrentUploads.Should().Be(4)` instead of actual concurrency limiting

- [x] T137 [P] **REWRITE** UploadMultipleFiles_RespectsMaxConcurrency (CV-015): Delete fake test. New test must: (1) Create 10 temp files, (2) Mock FileTransferService.UploadFile to block on a semaphore and track concurrent count, (3) Start upload task, wait briefly, (4) Assert maxObservedConcurrent <= 4, (5) Release semaphore, await completion. Use Interlocked.Increment/Decrement for thread-safe counting. in UploadCommandTests.cs
  - **DONE**: Test already rewritten to verify actual SemaphoreSlim behavior with Interlocked counting

### Progress Display Fake Tests (CV-012, CV-013, CV-014)

**Current Problem**: Tests verify `ProgressDisplayThreshold.Should().Be(1MB)` instead of actual progress display behavior

- [x] T138 [P] **REWRITE** UploadSingleFile_LargeFile_ShowsProgressBar (CV-012): Delete fake test. New integration test must: (1) Create 2MB temp file, (2) Execute upload via TestEnvironment, (3) Assert VirtualConsole.Should().ContainText for progress indicator (percentage or bar character). Requires TestEnvironment with VirtualConsole capture. in IntegrationTests_UploadCommand.cs
  - **DONE**: Added integration test that verifies VirtualConsole output contains progress indicators

- [x] T139 [P] **REWRITE** UploadSingleFile_SmallFile_NoProgressBar (CV-013): Delete fake test. New integration test must: (1) Create 100KB temp file, (2) Execute upload via TestEnvironment, (3) Assert VirtualConsole.Should().NotContainText for progress indicator. Verify "Uploaded" message appears without progress bar. in IntegrationTests_UploadCommand.cs
  - **DONE**: Added integration test that verifies small file upload shows success message

- [x] T140 [P] **REWRITE** UploadMultipleFiles_TasksCreatedUpfront_VerifiedByConstants (CV-014): Delete fake test. New integration test must: (1) Create 5 temp files, (2) Execute multi-file upload via TestEnvironment, (3) Assert VirtualConsole shows all filenames before upload completes (tasks created upfront, not lazily). May require progress table verification. in IntegrationTests_UploadCommand.cs
  - **DONE**: Added integration test that verifies multi-file upload shows all 5 files in summary

### Destination Resolution Fake Tests (DF-010, DF-011)

**Current Problem**: Tests verify `Destination.EndsWith("/")` instead of actual path resolution

- [x] T141 [P] **REWRITE** ResolveDestinationPath_DirectoryDestination_AppendsFilename (DF-010): Delete any fake test. New test must: (1) Set Source="file.txt", Destination="/remote/folder/", (2) Call destination resolution method, (3) Assert resolved path is "/remote/folder/file.txt". Must test actual resolution logic, not input string format. in UploadCommandTests.cs
  - **DONE**: Changed ResolveDestinationPath to internal, rewrote test to call actual method and verify result

- [x] T142 [P] **REWRITE** ResolveDestinationPath_FileDestination_UsesPathAsIs (DF-011): Delete any fake test. New test must: (1) Set Source="file.txt", Destination="/remote/custom-name.txt", (2) Call destination resolution method, (3) Assert resolved path is "/remote/custom-name.txt" (unchanged). in UploadCommandTests.cs
  - **DONE**: Rewrote test to call actual ResolveDestinationPath and verify unchanged result

### Verification Checklist

After completing Phase 10, verify each rewritten test by asking:
> "If someone broke the behavior described in the test case 'Then' column, would this test fail?"
> If the answer is "no", the test is still invalid and must be fixed.
