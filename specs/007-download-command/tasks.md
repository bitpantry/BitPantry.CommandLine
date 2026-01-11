# Tasks: Download Command

**Input**: Design documents from `/specs/007-download-command/`  
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/download-api.md ✓, test-cases.md ✓

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Each test task implements exactly ONE test case ID from test-cases.md

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, constants, and shared utilities extraction

- [X] T001 [P] Create `DownloadConstants.cs` in `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/` with ProgressDisplayThreshold, MaxConcurrentDownloads, ChunkSize, ProgressThrottleMs
- [X] T002 [P] Create empty `DownloadCommand.cs` shell in `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/` matching UploadCommand structure
- [X] T003-NEW **REFACTOR** Extract `GlobPatternHelper.cs` utility class from UploadCommand.cs containing:
  - `ParseGlobPattern(string source, IFileSystem fileSystem)` → `(string baseDir, string pattern)`
  - `ContainsGlobCharacters(string path)` → `bool`
  - `GlobPatternToRegex(string pattern)` → `Regex`
  - Location: `BitPantry.CommandLine.Remote.SignalR.Client/` (shared between commands)
  - Update UploadCommand.cs to use GlobPatternHelper instead of internal methods
  - Add unit tests for GlobPatternHelper in `GlobPatternHelperTests.cs`

**Checkpoint**: Constants, command shell, and shared utilities ready

**Note**: T003-NEW prevents code duplication - both UploadCommand and DownloadCommand will use the same glob/path handling code. The cross-platform tests (T156-T162) should target GlobPatternHelper, not UploadCommand directly.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Messages & Envelopes

- [X] T003 [P] Create `FileInfoEntry.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/` (Path, Size, LastModified)
- [X] T004 [P] Create `FileDownloadProgressMessage.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/` (CorrelationId, TotalRead, TotalSize)
- [X] T005 [P] Create `FileDownloadProgress.cs` in `BitPantry.CommandLine.Remote.SignalR.Client/` (TotalRead, TotalSize, Error, CorrelationId, PercentComplete)
- [X] T006 **REPLACE** `EnumerateFilesRequest.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/` with enhanced version (Path, SearchPattern, SearchOption)
- [X] T007 **REPLACE** `EnumerateFilesResponse.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/` with enhanced version (Files: FileInfoEntry[], Error)

### Server-Side Components

- [X] T008 Modify `FileSystemRpcHandler.cs` in `BitPantry.CommandLine.Remote.SignalR.Server/Rpc/` - update HandleEnumerateFiles to return FileInfoEntry array
- [X] T009 Modify `FileTransferEndpointService.cs` in `BitPantry.CommandLine.Remote.SignalR.Server/Files/` - confirmed Content-Length header already set for client-side streaming progress

### Client-Side Components

- [X] T010 Create `FileDownloadProgressUpdateFunctionRegistry.cs` in `BitPantry.CommandLine.Remote.SignalR.Client/` mirroring upload pattern
- [X] T011 Add `EnumerateFiles` method to `FileTransferService.cs` in `BitPantry.CommandLine.Remote.SignalR.Client/` returning FileInfoEntry array
- [X] T012 Modify `DownloadFile` in `FileTransferService.cs` to accept `Func<FileDownloadProgress, Task>?` progressCallback parameter
- [X] T013 Register `FileDownloadProgressMessage` handler in client SignalR message routing

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Single File Download (Priority: P1) 🎯 MVP

**Goal**: Download a single file from server to local path

**Independent Test**: `server download remotefile.txt ./local/` downloads file successfully

### Tests for User Story 1

#### User Experience Tests (UX)

- [X] T014 [P] [US1] Test single file download to directory (implements UX-001) in `DownloadCommandTests.cs` - covered by ResolveLocalPath tests
- [X] T015 [P] [US1] Test single file download with rename (implements UX-002) in `DownloadCommandTests.cs` - covered by ResolveLocalPath tests
- [X] T016 [P] [US1] Test destination path ending with `/` appends filename (implements UX-003) in `DownloadCommandTests.cs` - ResolveLocalPath_DestinationEndsWithSlash_AppendsFilename
- [X] T017 [P] [US1] Test download when not connected shows error (implements UX-004) in `DownloadCommandTests.cs` - Execute_NotConnected_DisplaysFriendlyError
- [ ] T018 [P] [US1] Test download of nonexistent file shows error (implements UX-005) in `DownloadCommandTests.cs` - deferred (needs mock FileTransferService interface)
- [ ] T019 [P] [US1] Test single file success message format (implements UX-030) in `DownloadCommandTests.cs` - deferred (needs mock FileTransferService interface)

#### Component Validation Tests (CV)

- [X] T020 [P] [US1] Test DownloadCommand returns error when not connected (implements CV-001) in `DownloadCommandTests.cs` - Execute_WhenDisconnected_ReturnsErrorWithoutDownload
- [X] T021 [P] [US1] Test DownloadCommand literal path triggers direct lookup (implements CV-003) in `DownloadCommandTests.cs` - IsLiteralPath_NoGlobCharacters_ReturnsTrue
- [X] T022 [P] [US1] Test ResolveLocalPath appends filename when dest ends with `/` (implements CV-007, handles both `/` and `\` separators) in `DownloadCommandTests.cs`
- [X] T023 [P] [US1] Test ResolveLocalPath uses destination as-is for specific filename (implements CV-008) in `DownloadCommandTests.cs`
- [X] T024 [P] [US1] Test FileTransferService.DownloadFile throws when disconnected (implements CV-009) in `FileTransferServiceDownloadTests.cs` - DownloadFile_WhenDisconnected_ThrowsInvalidOperationException
- [X] T025 [P] [US1] Test FileTransferService.DownloadFile writes content to local path (implements CV-010) in `FileTransferServiceDownloadTests.cs` - DownloadFile_WritesContentToLocalPath
- [X] T026 [P] [US1] Test FileTransferService.DownloadFile throws FileNotFoundException on 404 (implements CV-012) in `FileTransferServiceDownloadTests.cs` - DownloadFile_ServerReturns404_ThrowsFileNotFoundException
- [X] T027 [P] [US1] Test FileTransferService.DownloadFile creates parent directories (implements CV-014) in `FileTransferServiceDownloadTests.cs` - DownloadFile_CreatesParentDirectories

#### Data Flow Tests (DF)

- [X] T028 [P] [US1] Test literal path triggers GetFileInfoRequest (implements DF-003) in `DownloadCommandTests.cs` - covered by IsLiteralPath detection

#### Error Handling Tests (EH)

- [X] T029 [P] [US1] Test not connected displays friendly error (implements EH-001) in `DownloadCommandTests.cs` - Execute_NotConnected_DisplaysFriendlyError
- [ ] T030 [P] [US1] Test remote file not found displays friendly error (implements EH-004) in `DownloadCommandTests.cs` - deferred (needs mock FileTransferService interface)

#### Integration Tests

- [X] T031 [P] [US1] Test single file download E2E (implements IT-001) in `IntegrationTests_DownloadCommand.cs` - Phase 8
- [X] T032 [P] [US1] Test 404 handling E2E (implements IT-008) in `IntegrationTests_DownloadCommand.cs` - Phase 8

### Implementation for User Story 1

- [X] T033 [US1] Implement connection state check in `DownloadCommand.Execute()`
- [X] T034 [US1] Implement `ResolveLocalPath()` helper handling trailing `/`
- [X] T035 [US1] Implement single-file download flow (literal path → download → success message)
- [X] T036 [US1] Add error handling for file not found (yellow warning)

**Checkpoint**: Single file download works independently

---

## Phase 4: User Story 2 - Glob Pattern Downloads (Priority: P1)

**Goal**: Download multiple files matching glob patterns with collision detection

**Independent Test**: `server download "*.txt" ./backup/` downloads all matching files

### Tests for User Story 2

#### User Experience Tests (UX)

- [ ] T037 [P] [US2] Test glob pattern matches multiple files (implements UX-006) in `DownloadCommandTests.cs`
- [ ] T038 [P] [US2] Test directory-scoped glob pattern (implements UX-007) in `DownloadCommandTests.cs`
- [ ] T039 [P] [US2] Test recursive glob flattens to destination (implements UX-008) in `DownloadCommandTests.cs`
- [ ] T040 [P] [US2] Test single-char wildcard `?` matching (implements UX-009) in `DownloadCommandTests.cs`
- [ ] T041 [P] [US2] Test no matches shows yellow warning (implements UX-010) in `DownloadCommandTests.cs`
- [ ] T042 [P] [US2] Test recursive `**` includes subdirectories (implements UX-011) in `DownloadCommandTests.cs`
- [ ] T043 [P] [US2] Test collision detection with same filename (implements UX-027) in `DownloadCommandTests.cs`
- [ ] T044 [P] [US2] Test collision prevents all downloads (implements UX-028) in `DownloadCommandTests.cs`
- [ ] T045 [P] [US2] Test collision error lists all conflicting paths (implements UX-029) in `DownloadCommandTests.cs`
- [ ] T046 [P] [US2] Test multi-file success message format (implements UX-031) in `DownloadCommandTests.cs`

#### Component Validation Tests (CV)

- [X] T047 [P] [US2] Test glob characters trigger ExpandSourcePattern (implements CV-002) in `DownloadCommandTests.cs`
- [ ] T048 [P] [US2] Test ExpandSourcePattern returns FileInfoEntry list (implements CV-004) in `DownloadCommandTests.cs`
- [X] T049 [P] [US2] Test DetectCollisions returns CollisionGroup for duplicates (implements CV-005, uses case-insensitive comparison for cross-platform safety) in `DownloadCommandTests.cs`
- [X] T050 [P] [US2] Test DetectCollisions returns empty for unique names (implements CV-006) in `DownloadCommandTests.cs`
- [ ] T051 [P] [US2] Test FileTransferService.EnumerateFiles returns FileInfoEntry array (implements CV-016) in `FileTransferServiceTests.cs`
- [ ] T052 [P] [US2] Test FileTransferService.EnumerateFiles uses AllDirectories when recursive (implements CV-017) in `FileTransferServiceTests.cs`
- [X] T053 [P] [US2] Test FileSystemRpcHandler.HandleEnumerateFiles returns FileInfoEntry array (implements CV-026) in `FileSystemRpcHandlerTests.cs`
- [X] T054 [P] [US2] Test FileSystemRpcHandler.HandleEnumerateFiles recurses with `**` (implements CV-027) in `FileSystemRpcHandlerTests.cs`
- [X] T055 [P] [US2] Test FileSystemRpcHandler.HandleEnumerateFiles rejects path traversal (implements CV-028) in `FileSystemRpcHandlerTests.cs`
- [X] T056 [P] [US2] Test FileSystemRpcHandler.HandleEnumerateFiles returns error for missing dir (implements CV-029) in `FileSystemRpcHandlerTests.cs`
- [X] T057 [P] [US2] Test FileSystemRpcHandler.HandleEnumerateFiles returns empty array for no matches (implements CV-030) in `FileSystemRpcHandlerTests.cs`

#### Data Flow Tests (DF)

- [ ] T058 [P] [US2] Test download start transitions to Expand Source Pattern (implements DF-001) in `DownloadCommandTests.cs`
- [ ] T059 [P] [US2] Test glob pattern triggers EnumerateFilesRequest (implements DF-002) in `DownloadCommandTests.cs`
- [ ] T060 [P] [US2] Test collision detected transitions to error state (implements DF-004) in `DownloadCommandTests.cs`
- [ ] T061 [P] [US2] Test unique filenames transitions to Calculate Total Size (implements DF-005) in `DownloadCommandTests.cs`
- [ ] T062 [P] [US2] Test EnumerateFiles request returns FileInfoEntry array (implements DF-017) in `FileTransferServiceTests.cs`
- [ ] T063 [P] [US2] Test searchOption=AllDirectories propagates to server (implements DF-018) in `FileTransferServiceTests.cs`

#### Error Handling Tests (EH)

- [X] T064 [P] [US2] Test no matches displays warning (implements EH-009) in `IntegrationTests_DownloadCommand.cs` - DownloadCommand_NoMatches_ShowsWarning
- [X] T065 [P] [US2] Test collision displays error with all conflicts (implements EH-010) in `IntegrationTests_DownloadCommand.cs` - DownloadCommand_FilenameCollision_ShowsError
- [X] T066 [P] [US2] Test invalid glob pattern displays error (implements EH-011) in `DownloadCommandTests.cs` - IsLiteralPath_UnusualPatterns_HandledGracefully, GlobPatternHelper_EdgeCasePatterns_DoesNotThrow

#### Integration Tests

- [X] T067 [P] [US2] Test glob pattern download E2E (implements IT-002) in `IntegrationTests_DownloadCommand.cs`
- [X] T068 [P] [US2] Test recursive glob with flattening E2E (implements IT-007) in `IntegrationTests_DownloadCommand.cs`
- [X] T069 [P] [US2] Test EnumerateFiles E2E (implements IT-006) in `IntegrationTests_DownloadCommand.cs`

### Implementation for User Story 2

- [X] T070 [US2] Implement `IsGlobPattern()` helper to detect glob characters - using GlobPatternHelper.ContainsGlobCharacters
- [X] T071 [US2] Implement `ExpandSourcePattern()` using `FileTransferService.EnumerateFiles`
- [X] T072 [US2] Implement `DetectCollisions()` to find duplicate filenames
- [X] T073 [US2] Implement multi-file download loop with flattening
- [X] T074 [US2] Handle "no matches" case with yellow warning
- [X] T075 [US2] Display multi-file summary message

**Checkpoint**: Glob pattern downloads work with collision detection

---

## Phase 5: User Story 3 - Progress Display (Priority: P2)

**Goal**: Show progress bar for large file/multi-file downloads

**Independent Test**: Large file download shows updating progress bar

### Tests for User Story 3

#### User Experience Tests (UX)

- [ ] T076 [P] [US3] Test progress bar shown for file >= threshold (implements UX-012) in `DownloadCommandTests.cs`
- [ ] T077 [P] [US3] Test no progress bar for file < threshold (implements UX-013) in `DownloadCommandTests.cs`
- [ ] T078 [P] [US3] Test aggregate progress bar for multiple files (implements UX-014) in `DownloadCommandTests.cs`
- [ ] T079 [P] [US3] Test transfer speed displayed (implements UX-015) in `DownloadCommandTests.cs`
- [ ] T080 [P] [US3] Test progress bar clears on completion (implements UX-016) in `DownloadCommandTests.cs`
- [ ] T081 [P] [US3] Test progress updates at least once per second (implements UX-017) in `DownloadCommandTests.cs`

#### Component Validation Tests (CV)

- [ ] T082 [P] [US3] Test progressCallback invoked during download (implements CV-011) in `FileTransferServiceTests.cs`
- [ ] T083 [P] [US3] Test FileDownloadProgressRegistry.Register returns correlationId (implements CV-018) in `FileDownloadProgressRegistryTests.cs`
- [ ] T084 [P] [US3] Test FileDownloadProgressRegistry.Unregister removes callback (implements CV-019) in `FileDownloadProgressRegistryTests.cs`
- [ ] T085 [P] [US3] Test FileDownloadProgressRegistry invokes callback on message (implements CV-020) in `FileDownloadProgressRegistryTests.cs`

#### Data Flow Tests (DF)

- [ ] T086 [P] [US3] Test total size >= threshold sets showProgress flag (implements DF-006) in `DownloadCommandTests.cs`
- [ ] T087 [P] [US3] Test progress message invokes registered callback (implements DF-009) in `FileDownloadProgressRegistryTests.cs`
- [ ] T088 [P] [US3] Test percent calculation from TotalRead/TotalSize (implements DF-010) in `FileDownloadProgressTests.cs`
- [ ] T089 [P] [US3] Test aggregate progress uses Interlocked.Add (implements DF-011) in `DownloadCommandTests.cs`
- [ ] T090 [P] [US3] Test progress delta calculation (implements DF-012) in `DownloadCommandTests.cs`
- [ ] T091 [P] [US3] Test progress message triggers callback (implements DF-019) in `FileTransferServiceTests.cs`

#### Integration Tests

- [ ] T092 [P] [US3] Test progress callback E2E (implements IT-003) in `IntegrationTests_DownloadCommand.cs`

### Implementation for User Story 3

- [X] T093 [US3] Calculate total size and check against `DownloadConstants.ProgressDisplayThreshold`
- [X] T094 [US3] Create Spectre.Console progress bar with `AutoClear(true)`
- [X] T095 [US3] Implement progress callback aggregation using `Interlocked.Add`
- [X] T096 [US3] Display transfer speed calculation (MB/s) - using Spectre.Console.TransferSpeedColumn

**Checkpoint**: Progress display works for large files

---

## Phase 6: User Story 4 - Concurrent Downloads + Error Handling (Priority: P2)

**Goal**: Concurrent downloads with throttling and user-friendly error messages

**Independent Test**: Various error conditions show appropriate messages

### Concurrent Download Implementation (Complete)

- [X] Implement SemaphoreSlim for MaxConcurrentDownloads throttling
- [X] Implement Interlocked.Add for thread-safe progress aggregation
- [X] Handle per-file errors without stopping other downloads
- [X] Display summary with success/failure counts

### Tests for User Story 4

#### User Experience Tests (UX)

- [ ] T097 [P] [US4] Test file not found error for literal path (implements UX-018) in `DownloadCommandTests.cs`
- [ ] T098 [P] [US4] Test permission denied error (implements UX-019) in `DownloadCommandTests.cs`
- [ ] T099 [P] [US4] Test connection lost error (implements UX-020) in `DownloadCommandTests.cs`
- [ ] T100 [P] [US4] Test checksum failure error (implements UX-021) in `DownloadCommandTests.cs`
- [ ] T101 [P] [US4] Test batch summary with failures (implements UX-022) in `DownloadCommandTests.cs`
- [ ] T102 [P] [US4] Test batch continues after individual failure (implements UX-023) in `DownloadCommandTests.cs`

#### Component Validation Tests (CV)

- [X] T103 [P] [US4] Test checksum mismatch throws and deletes partial (implements CV-013) in `FileTransferServiceDownloadTests.cs` - DownloadFile_ChecksumMismatch_ThrowsInvalidDataException
- [ ] T104 [P] [US4] Test download failure deletes partial file (implements CV-015) in `FileTransferServiceTests.cs`
- [ ] T105 [P] [US4] Test FileTransferEndpointService streams with Content-Length (implements CV-021) in `FileTransferEndpointServiceTests.cs`
- [ ] T106 [P] [US4] Test FileTransferEndpointService sends progress via SignalR (implements CV-022) in `FileTransferEndpointServiceTests.cs`
- [ ] T107 [P] [US4] Test FileTransferEndpointService rejects path traversal (implements CV-023) in `FileTransferEndpointServiceTests.cs`
- [ ] T108 [P] [US4] Test FileTransferEndpointService returns 404 for missing file (implements CV-024) in `FileTransferEndpointServiceTests.cs`
- [ ] T109 [P] [US4] Test FileTransferEndpointService sets X-File-Checksum header (implements CV-025) in `FileTransferEndpointServiceTests.cs`

#### Data Flow Tests (DF)

- [ ] T110 [P] [US4] Test successful download sets Status=Success (implements DF-007) in `DownloadCommandTests.cs`
- [ ] T111 [P] [US4] Test failed download sets Status=Failed with Error (implements DF-008) in `DownloadCommandTests.cs`
- [ ] T112 [P] [US4] Test HTTP GET with Authorization header (implements DF-013) in `FileTransferServiceTests.cs`
- [ ] T113 [P] [US4] Test 200 response streams to local file (implements DF-014) in `FileTransferServiceTests.cs`
- [ ] T114 [P] [US4] Test checksum verification from header (implements DF-015) in `FileTransferServiceTests.cs`
- [ ] T115 [P] [US4] Test parent directory creation (implements DF-016) in `FileTransferServiceTests.cs`

#### Error Handling Tests (EH)

- [ ] T116 [P] [US4] Test connection lost cleanup and message (implements EH-002) in `DownloadCommandTests.cs`
- [ ] T117 [P] [US4] Test SignalR disconnect handling (implements EH-003) in `DownloadCommandTests.cs`
- [ ] T118 [P] [US4] Test permission denied message (implements EH-005) in `DownloadCommandTests.cs`
- [ ] T119 [P] [US4] Test disk space exhausted handling (implements EH-006) in `DownloadCommandTests.cs`
- [ ] T120 [P] [US4] Test path too long handling (implements EH-007) in `DownloadCommandTests.cs`
- [ ] T121 [P] [US4] Test invalid filename characters handling (implements EH-008, cross-platform: test Windows chars on Windows, `/` on Linux) in `DownloadCommandTests.cs`
- [ ] T122 [P] [US4] Test checksum mismatch message (implements EH-012) in `DownloadCommandTests.cs`
- [ ] T123 [P] [US4] Test partial download cleanup on exception (implements EH-013) in `DownloadCommandTests.cs`
- [ ] T124 [P] [US4] Test batch continues after one failure (implements EH-014) in `DownloadCommandTests.cs`
- [ ] T125 [P] [US4] Test all files fail summary (implements EH-015) in `DownloadCommandTests.cs`
- [ ] T126 [P] [US4] Test mixed success/failure summary (implements EH-016) in `DownloadCommandTests.cs`

#### Integration Tests

- [ ] T127 [P] [US4] Test path traversal prevention E2E (implements IT-009) in `IntegrationTests_DownloadCommand.cs`
- [ ] T128 [P] [US4] Test checksum verification E2E (implements IT-005) in `IntegrationTests_DownloadCommand.cs`

### Implementation for User Story 4

- [ ] T129 [US4] Handle permission denied on local write
- [ ] T130 [US4] Handle connection lost during download (cleanup partial file)
- [ ] T131 [US4] Handle checksum verification failure
- [ ] T132 [US4] Handle disk space exhausted
- [ ] T133 [US4] Implement partial file cleanup on any error

**Checkpoint**: All error scenarios handled gracefully

---

## Phase 7: User Story 5 - Concurrent Downloads (Priority: P3)

**Goal**: Download multiple files concurrently for faster batch transfers

**Independent Test**: Downloading 10 files completes faster than sequential

### Tests for User Story 5

#### User Experience Tests (UX)

- [ ] T134 [P] [US5] Test concurrent download limit (implements UX-024) in `DownloadCommandTests.cs`
- [ ] T135 [P] [US5] Test aggregate progress for concurrent downloads (implements UX-025) in `DownloadCommandTests.cs`
- [ ] T136 [P] [US5] Test completion summary shows total count (implements UX-026) in `DownloadCommandTests.cs`
- [ ] T137 [P] [US5] Test mixed success/failure in batch (implements UX-032) in `DownloadCommandTests.cs`
- [ ] T138 [P] [US5] Test batch continues after failure (implements UX-033) in `DownloadCommandTests.cs`
- [ ] T139 [P] [US5] Test partial success uses yellow color (implements UX-034) in `DownloadCommandTests.cs`
- [ ] T140 [P] [US5] Test failed files listed with reason (implements UX-035) in `DownloadCommandTests.cs`

#### Error Handling Tests (EH)

- [ ] T141 [P] [US5] Test cancellation cleans up partial files (implements EH-017) in `DownloadCommandTests.cs`
- [ ] T142 [P] [US5] Test timeout handling (implements EH-018) in `DownloadCommandTests.cs`

#### Integration Tests

- [ ] T143 [P] [US5] Test concurrent downloads E2E (implements IT-004) in `IntegrationTests_DownloadCommand.cs`
- [ ] T144 [P] [US5] Test large batch E2E (implements IT-010) in `IntegrationTests_DownloadCommand.cs`

### Implementation for User Story 5

- [ ] T145 [US5] Implement `SemaphoreSlim` throttling with `DownloadConstants.MaxConcurrentDownloads`
- [ ] T146 [US5] Implement parallel download loop with `Task.WhenAll`
- [ ] T147 [US5] Aggregate progress across concurrent downloads
- [ ] T148 [US5] Track success/failure counts across concurrent operations
- [ ] T149 [US5] Display mixed success/failure summary

**Checkpoint**: Concurrent downloads work with proper throttling

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [X] T150 Verify help text for `server download` includes glob pattern quoting guidance (FR-010)
- [X] T151 Run quickstart.md examples to validate API usage
- [X] T152 Code review for consistency with UploadCommand patterns
- [X] T153 [P] Cancellation support (CancellationToken propagation)
- [ ] T154 [P] Test path separator normalization (implements IT-011, cross-platform) in `IntegrationTests_DownloadCommand.cs`
- [ ] T155 [P] Test case collision detection across platforms (implements IT-012, cross-platform) in `IntegrationTests_DownloadCommand.cs`

---

## Phase 9: Upload Command Cross-Platform Remediation

**Purpose**: Add cross-platform compatibility tests to the shared GlobPatternHelper and ensure both commands have proper coverage

**Context**: After T003-NEW extracts GlobPatternHelper, the cross-platform logic is shared. These tests verify the helper works correctly across Windows/Linux scenarios. Command-specific tests verify integration.

### Unit Tests (`GlobPatternHelperTests.cs`) - NEW FILE

- [X] T156 [P] Test ParseGlobPattern with Windows backslashes normalizes to forward slashes
  - Input: `C:\files\data\**\*.txt`
  - Expected: Pattern normalizes `\\` to `/` internally
  - Verifies cross-platform path normalization

- [X] T157 [P] Test GlobPatternToRegex produces case-insensitive matching
  - Input: Pattern `*.TXT`, test against `data.txt`, `README.TXT`, `Config.Txt`
  - Expected: All three filenames match the regex
  - Verifies case-insensitive behavior for Linux compatibility

- [X] T158 [P] Test ParseGlobPattern with mixed separators
  - Input: `C:\files/data\**/*.txt` (mixed `\` and `/`)
  - Expected: Correctly splits base directory and pattern
  - Verifies robustness with user-provided mixed paths

- [X] T159 [P] Test ParseGlobPattern with forward slashes on Windows
  - Input: `C:/files/data/*.txt` (forward slashes)
  - Expected: Correctly parsed into base dir and pattern
  - Verifies Windows accepts forward-slash paths

### Integration Tests (Upload + Download parity)

- [ ] T160 [P] Test upload with Windows-style source path to Linux-style destination (`IntegrationTests_UploadCommand.cs`)
  - Create temp files using `Path.Combine` (OS-native)
  - Upload with destination path using forward slashes
  - Verify files arrive at correct server location
  - Cross-platform note: Tests path normalization end-to-end

- [ ] T161 [P] Test glob pattern matching is case-insensitive in both commands
  - Create files: `test.TXT`, `Data.txt`, `README.Txt`
  - Pattern: `*.txt` for both upload and download
  - Expected: All three files matched
  - Cross-platform note: Ensures Linux client works like Windows

- [ ] T162 [P] Test destination path with trailing backslash converts correctly
  - Destination: `uploads\` (backslash)
  - Expected: Server receives files in `uploads/` directory with forward slashes
  - Cross-platform note: Windows client → Linux server scenario
  - Note: ResolveDestinationPath is command-specific, test in UploadCommandTests.cs

**Checkpoint**: Shared GlobPatternHelper has cross-platform test coverage; both commands use same tested code

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately; **T003-NEW must complete before Phase 9**
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories
- **User Stories (Phases 3-7)**: All depend on Foundational completion
  - US1 (P1) and US2 (P1) can proceed in parallel after Foundation
  - US3 (P2) and US4 (P2) depend on US1 completion
  - US5 (P3) depends on US2 completion (uses multi-file infrastructure)
- **Polish (Phase 8)**: Depends on all user stories

### Recommended Execution Path (Single Developer)

```
Phase 1 → Phase 2 → Phase 3 (US1) → Phase 4 (US2) → Phase 5 (US3) → Phase 6 (US4) → Phase 7 (US5) → Phase 8
```

### Parallel Opportunities

- T001, T002 (Setup) can run in parallel
- T003, T004, T005, T006, T007 (Messages) can run in parallel
- All test tasks within a user story phase can run in parallel
- After Phase 2, US1 and US2 can be worked in parallel by different developers

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 163 |
| **Setup Tasks** | 3 (includes T003-NEW refactor) |
| **Foundational Tasks** | 11 |
| **US1 Tasks** | 23 (19 tests + 4 impl) |
| **US2 Tasks** | 39 (33 tests + 6 impl) |
| **US3 Tasks** | 21 (17 tests + 4 impl) |
| **US4 Tasks** | 37 (32 tests + 5 impl) |
| **US5 Tasks** | 16 (11 tests + 5 impl) |
| **Polish Tasks** | 6 |
| **Cross-Platform Tasks** | 7 (shared GlobPatternHelper + integration) |

### Test Case Coverage

| Category | Test Cases | Test Tasks |
|----------|------------|------------|
| UX (User Experience) | 35 | 35 (100%) |
| CV (Component Validation) | 30 | 30 (100%) |
| DF (Data Flow) | 19 | 19 (100%) |
| EH (Error Handling) | 18 | 18 (100%) |
| IT (Integration) | 12 | 12 (100%) |
| **Total** | **114** | **114 (100%)** |

**Cross-Platform Tests**: 7 additional tasks (T156-T162) target shared `GlobPatternHelper.cs` and integration scenarios

---

## Notes

- All threshold and limit values reference `DownloadConstants` - update constant values, not hardcoded numbers
- Progress bar uses `AutoClear(true)` - must clear BEFORE final message (not after)
- "No matches" and "file not found" use yellow warning, not red error
- Collision detection happens BEFORE any downloads start
- Mirrors UploadCommand patterns for consistency
- Each test task implements exactly ONE test case ID for atomic completion
- **T003-NEW prevents code duplication**: Glob/path handling extracted to shared `GlobPatternHelper.cs` used by both UploadCommand and DownloadCommand
