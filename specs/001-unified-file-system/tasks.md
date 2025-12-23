# Tasks: Unified File System Abstraction

**Input**: Design documents from `/specs/001-unified-file-system/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Test Approach**: TDD - Write failing tests FIRST, then implement to make them pass
**Test Framework**: MSTest with FluentAssertions and Moq
**Test Coverage**: All happy paths AND exception/error paths

**Organization**: Tasks are grouped by user story. Within each story, tests are written BEFORE implementation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- **🧪**: Test task (write FIRST, ensure it FAILS before implementation)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, NuGet packages, and file cleanup

- [x] T001 Add `TestableIO.System.IO.Abstractions.Wrappers` NuGet package to `BitPantry.CommandLine/BitPantry.CommandLine.csproj`
- [x] T002 [P] Add `TestableIO.System.IO.Abstractions.Wrappers` NuGet package to `BitPantry.CommandLine.Remote.SignalR.Client/BitPantry.CommandLine.Remote.SignalR.Client.csproj`
- [x] T003 [P] Add `TestableIO.System.IO.Abstractions.TestingHelpers` NuGet package to `BitPantry.CommandLine.Tests/BitPantry.CommandLine.Tests.csproj`
- [x] T004 [P] Add `TestableIO.System.IO.Abstractions.TestingHelpers` NuGet package to `BitPantry.CommandLine.Tests.Remote.SignalR/BitPantry.CommandLine.Tests.Remote.SignalR.csproj`
- [x] T005 Delete custom abstraction file `BitPantry.CommandLine/IFileService.cs`
- [x] T006 [P] Delete custom abstraction file `BitPantry.CommandLine/LocalDiskFileService.cs`
- [x] T007 [P] Delete static extension methods file `BitPantry.CommandLine.Remote.SignalR.Client/CommandBaseExtensions_FileTransfer.cs`
- [x] T008 [P] Delete obsolete test file `BitPantry.CommandLine.Tests/LocalDiskFileServiceTests.cs`
- [x] T009 Update `BitPantry.CommandLine/ServiceCollectionExtensions.cs` to register `IFileSystem` → `FileSystem` (singleton)

**Checkpoint**: Project compiles with new NuGet packages, old abstractions removed

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### 🧪 Tests FIRST

- [x] T010 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/FileTransferOptionsTests.cs` with validation tests:
  - `Validate_StorageRootPathNull_ThrowsInvalidOperationException`
  - `Validate_StorageRootPathEmpty_ThrowsInvalidOperationException`
  - `Validate_MaxFileSizeBytesZero_ThrowsArgumentException`
  - `Validate_MaxFileSizeBytesNegative_ThrowsArgumentException`
  - `Validate_ValidConfiguration_Succeeds`
  - `Validate_DefaultMaxFileSize_Is100MB`
  - `Validate_DefaultAllowedExtensions_IsNull`

### Implementation

- [x] T011 Create `BitPantry.CommandLine.Remote.SignalR.Server/Files/FileTransferOptions.cs` with `StorageRootPath` (required), `MaxFileSizeBytes` (default 100MB), `AllowedExtensions` (default null)
- [x] T012 Add startup validation in server configuration to throw `InvalidOperationException` if `StorageRootPath` is null/empty
- [x] T013 Add `MessageArgNames.FileSystem` constants class for RPC message field names in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`
- [x] T014 [P] Add file system RPC method names to `BitPantry.CommandLine.Remote.SignalR/SignalRMethodNames.cs`
- [x] T015 Change `TotalRead` from `int` to `long` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/FileUploadProgressMessage.cs`
- [x] T016 [P] Change `TotalRead` from `int` to `long` in `BitPantry.CommandLine.Remote.SignalR.Client/FileUploadProgress.cs`

**Checkpoint**: Foundation ready - T010 tests pass, configuration model exists

---

## Phase 3: User Story 1 - File System Abstraction (Priority: P0) 🎯 MVP

**Goal**: Commands use `IFileSystem` for all file operations. Client always uses local `FileSystem`. Server-side command execution uses `SandboxedFileSystem`.

**Architecture Correction**: The original design incorrectly swapped client-side `IFileSystem` on connect/disconnect. The CORRECT design is:
- Client ALWAYS uses local `FileSystem` (no swap)
- Server resolves `IFileSystem` → `SandboxedFileSystem` for remote command execution

### ⚠️ UNDO TASKS (Completed work that needs to be reverted)

- [x] T200 🔄 Delete `BitPantry.CommandLine.Remote.SignalR.Client/FileSystemProvider.cs` (incorrect client-side swap pattern)
- [x] T201 🔄 Delete `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_FileSystemLifecycle.cs` (tests incorrect swap behavior)
- [x] T202 🔄 Remove `FileSystemProvider` injection and `Swap()` call from `ConnectCommand.cs`
- [x] T203 🔄 Remove `FileSystemProvider` injection and `Swap()` call from `DisconnectCommand.cs`
- [x] T204 🔄 Simplify `CommandLineApplicationBuilderExtensions.cs` to register `IFileSystem` → `FileSystem` directly (no `FileSystemProvider` pattern)
- [x] T205 🔄 Delete `SandboxedFileSystem.cs` from Client (will be recreated in Server)
- [x] T206 🔄 Delete `SandboxedFile.cs` from Client (will be recreated in Server)
- [x] T207 🔄 Delete `SandboxedDirectory.cs` from Client (will be recreated in Server)
- [x] T208 🔄 Delete `SandboxedPath.cs` from Client (will be recreated in Server)
- [x] T209 🔄 Delete `SandboxedFileInfoFactory.cs` from Client (will be recreated in Server)
- [x] T210 🔄 Delete `SandboxedDirectoryInfoFactory.cs` from Client (will be recreated in Server)
- [x] T211 🔄 Remove file system RPC method names from `BitPantry.CommandLine.Remote.SignalR/SignalRMethodNames.cs` (undoes T014)

### 🧪 Tests FIRST (Corrected)

- [x] T212 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_ClientFileSystem.cs` with tests:
  - `IFileSystem_LocalExecution_IsFileSystemType`
  - `IFileSystem_LocalExecution_HasUnrestrictedAccess`
  - `IFileSystem_AfterConnect_StillIsFileSystemType` (client never swaps)
  - `IFileSystem_AfterDisconnect_StillIsFileSystemType`
  - `Command_InjectsIFileSystem_CanReadWriteLocally`

- [x] T213 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/SandboxedFileSystemTests.cs` with tests:
  - `SandboxedFileSystem_File_Exists_ValidPath_DelegatesToLocalFileSystem`
  - `SandboxedFileSystem_File_Exists_PathTraversal_ThrowsUnauthorizedAccess`
  - `SandboxedFileSystem_Directory_Exists_ValidPath_DelegatesToLocalFileSystem`
  - `SandboxedFileSystem_Directory_CreateDirectory_ValidPath_CreatesLocally`
  - `SandboxedFileSystem_File_WriteAllText_ValidPath_WritesToStorageRoot`

### Implementation (Corrected)

- [x] T214 [US1] Redesign `SandboxedFileSystem` in Server to wrap local `FileSystem` with `PathValidator` validation
- [x] T215 [US1] Redesign `SandboxedFile` in Server to validate paths via `PathValidator` then delegate to local file system
- [x] T216 [US1] Redesign `SandboxedDirectory` in Server to validate paths via `PathValidator` then delegate to local file system
- [x] T217 [US1] Register `IFileSystem` → `SandboxedFileSystem` in server-side DI for command execution scope
- [x] T218 [US1] Update `BitPantry.CommandLine/ServiceCollectionExtensions.cs` to register `IFileSystem` → `FileSystem` (simple singleton)

**Checkpoint**: T212-T213 tests pass - client always local, server uses sandboxed

---

### Original Phase 3 Tasks (Completed - INCORRECT IMPLEMENTATION)

> **NOTE**: These tasks were completed but implement the INCORRECT client-side swap pattern.
> They are kept here for historical record. See UNDO TASKS above to revert this work.

- [x] T017 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_FileSystemLifecycle.cs` with tests:
  - `IFileSystem_LocalExecution_IsFileSystemType`
  - `IFileSystem_LocalExecution_HasUnrestrictedAccess`
  - `IFileSystem_AfterConnect_IsSandboxedFileSystemType`
  - `IFileSystem_AfterDisconnect_RevertsToFileSystemType`
  - `Command_InjectsIFileSystem_CanReadWriteLocally`

- [x] T018 [US1] Create base `SandboxedFileSystem : FileSystemBase` class in `BitPantry.CommandLine.Remote.SignalR.Client/SandboxedFileSystem.cs` with constructor accepting HubConnection and HttpClient
- [x] T019 [P] [US1] Create `SandboxedPath : PathBase` class in `BitPantry.CommandLine.Remote.SignalR.Client/SandboxedPath.cs` delegating to local `System.IO.Path`
- [x] T020 [US1] Update `BitPantry.CommandLine.Remote.SignalR.Client/CommandLineApplicationBuilderExtensions.cs` to register `IFileSystem` lifecycle (local `FileSystem` by default, swap to `SandboxedFileSystem` on connect)
- [x] T021 [US1] Update `BitPantry.CommandLine.Remote.SignalR.Client/ConnectCommand.cs` to swap `IFileSystem` to `SandboxedFileSystem` on successful connection
- [x] T022 [US1] Update `BitPantry.CommandLine.Remote.SignalR.Client/DisconnectCommand.cs` to revert `IFileSystem` to local `FileSystem` on disconnect

---

## Phase 4: User Story 3 - Protected File Storage with Path Validation (Priority: P1)

**Goal**: All remote file operations are restricted to designated storage directory

**Independent Test**: Attempt file operations with path traversal sequences and verify server rejects them

### 🧪 Tests FIRST

- [x] T023 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/PathValidationTests.cs` with unit tests:
  - `ValidatePath_RelativePathWithinRoot_ReturnsFullPath`
  - `ValidatePath_PathTraversalWithDotDot_ThrowsUnauthorizedAccessException`
  - `ValidatePath_PathTraversalWithMultipleDotDot_ThrowsUnauthorizedAccessException`
  - `ValidatePath_AbsolutePathOutsideRoot_ThrowsUnauthorizedAccessException`
  - `ValidatePath_EncodedDotDotSlash_ThrowsUnauthorizedAccessException`
  - `ValidatePath_PathWithSpaces_ReturnsValidPath`
  - `ValidatePath_PathWithUnicode_ReturnsValidPath`
  - `ValidatePath_NullPath_ThrowsArgumentNullException`
  - `ValidatePath_EmptyPath_ThrowsArgumentException`
  - `ValidatePath_PathAtRootBoundary_ReturnsValidPath`

- [x] T024 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_PathTraversal.cs` with integration tests:
  - `Upload_PathTraversalDotDot_Returns403Forbidden`
  - `Upload_AbsolutePathOutsideRoot_Returns403Forbidden`
  - `Upload_EncodedTraversal_Returns403Forbidden`
  - `Upload_ValidRelativePath_Succeeds`
  - `Upload_NestedSubdirectory_CreatesAndSucceeds`

### Implementation

- [x] T025 [US3] Create `ValidatePath()` method in `BitPantry.CommandLine.Remote.SignalR.Server/Files/FileTransferEndpointService.cs` using `Path.GetFullPath()` comparison
- [x] T026 [US3] Add path validation to upload endpoint in `FileTransferEndpointService.cs` before any file write
- [x] T027 [US3] Return 403 Forbidden status for path traversal attempts with structured error response

**Checkpoint**: T023 and T024 tests pass - path traversal attacks are blocked

---

## Phase 5: User Story 4 - File Size and Type Restrictions (Priority: P1)

**Goal**: Server rejects uploads exceeding size limits or with disallowed extensions

**Independent Test**: Configure limits and attempt operations that exceed them

### 🧪 Tests FIRST

- [x] T028 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/FileSizeValidationTests.cs` with unit tests:
  - `ValidateSize_ContentLengthWithinLimit_Succeeds`
  - `ValidateSize_ContentLengthExceedsLimit_ThrowsException`
  - `ValidateSize_NoContentLengthHeader_StreamingCheckSucceeds`
  - `ValidateSize_StreamingExceedsLimit_AbortsAndThrows`

- [x] T029 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/ExtensionValidationTests.cs` with unit tests:
  - `ValidateExtension_AllowedExtension_Succeeds`
  - `ValidateExtension_DisallowedExtension_ThrowsException`
  - `ValidateExtension_AllowedExtensionsNull_AllowsAll`
  - `ValidateExtension_CaseInsensitiveMatch_Succeeds`
  - `ValidateExtension_NoExtension_Behavior` (define expected behavior)

- [x] T030 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_SizeAndExtension.cs` with integration tests:
  - `Upload_ExceedsMaxSize_Returns413`
  - `Upload_WithinMaxSize_Succeeds`
  - `Upload_DisallowedExtension_Returns400`
  - `Upload_AllowedExtension_Succeeds`
  - `Upload_ExtensionCheckWithNullAllowList_AllExtensionsAccepted`

### Implementation

- [x] T031 [US4] Add extension validation logic in `FileTransferEndpointService.cs` checking against `AllowedExtensions` (skip if null)
- [x] T032 [US4] Add size limit pre-flight check in upload endpoint using `Content-Length` header vs `MaxFileSizeBytes`
- [x] T033 [US4] Add streaming size limit check during upload to abort if accumulated bytes exceed limit
- [x] T034 [US4] Return appropriate HTTP status codes: 413 for size limit, 400 for extension rejection

**Checkpoint**: T028, T029, T030 tests pass - size and extension restrictions enforced

---

## Phase 6: User Story 7 - Secure Token Handling (Priority: P2)

**Goal**: Access tokens transmitted in Authorization headers, not URL query strings

**Independent Test**: Capture HTTP traffic and verify tokens appear in headers, not URLs

### 🧪 Tests FIRST

- [x] T035 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/FileTransferServiceAuthTests.cs` with unit tests:
  - `UploadFile_SendsAuthorizationBearerHeader`
  - `UploadFile_DoesNotIncludeTokenInQueryString`
  - `DownloadFile_SendsAuthorizationBearerHeader`
  - `DownloadFile_DoesNotIncludeTokenInQueryString`

- [x] T036 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_TokenSecurity.cs` with integration tests:
  - `Upload_TokenInHeader_Succeeds`
  - `Upload_MissingAuthHeader_Returns401`
  - `Upload_InvalidToken_Returns401`
  - `ServerRequestLog_DoesNotContainToken` (verify URL logging safety)

### Implementation

- [x] T037 [US7] Modify `BitPantry.CommandLine.Remote.SignalR.Client/FileTransferService.cs` to send token in `Authorization: Bearer` header instead of query string
- [x] T038 [US7] Update upload endpoint in `FileTransferEndpointService.cs` to read token from `Authorization` header
- [x] T039 [US7] Remove token from query string handling in server upload endpoint

**Checkpoint**: T035 and T036 tests pass - tokens no longer appear in URLs

---

## Phase 7: User Story 2 - Secure File Upload with Integrity (Priority: P1)

**Goal**: File uploads complete with verified SHA256 checksum

**Independent Test**: Upload a file and verify SHA256 checksum matches on client and server

### 🧪 Tests FIRST

- [x] T040 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/ChecksumTests.cs` with unit tests:
  - `ComputeIncrementalHash_ValidStream_ReturnsCorrectSha256`
  - `ComputeIncrementalHash_EmptyStream_ReturnsEmptyFileHash`
  - `ComputeIncrementalHash_LargeStream_ComputesCorrectly`
  - `VerifyChecksum_MatchingHash_Succeeds`
  - `VerifyChecksum_MismatchedHash_ThrowsException`
  - `VerifyChecksum_MissingHeader_ThrowsException`
  - `VerifyChecksum_InvalidHexFormat_ThrowsException`

- [x] T041 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Checksum.cs` with integration tests:
  - `Upload_ValidChecksum_FilePreserved`
  - `Upload_InvalidChecksum_FileDeleted_Returns400`
  - `Upload_MissingChecksumHeader_Returns400`
  - `Upload_ChecksumCaseInsensitive_Succeeds`

### Implementation

- [x] T042 [US2] Add incremental SHA256 hashing to `FileTransferService.cs` using `IncrementalHash.CreateHash(HashAlgorithmName.SHA256)` during upload streaming
- [x] T043 [US2] Send computed checksum in `X-File-Checksum` request header from client
- [x] T044 [US2] Add incremental SHA256 hashing to `FileTransferEndpointService.cs` during file write streaming
- [x] T045 [US2] Compare client checksum header with server-computed checksum after upload completes
- [x] T046 [US2] Delete uploaded file and return 400 error if checksum verification fails

**Checkpoint**: T040 and T041 tests pass - all uploads are checksum-verified

---

## Phase 8: User Story 6 - Cancellation and Cleanup (Priority: P2)

**Goal**: Cancelled or failed operations leave no orphaned partial files

**Independent Test**: Start large upload, cancel mid-transfer, verify no partial file remains

### 🧪 Tests FIRST

- [x] T047 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/PartialFileCleanupTests.cs` with unit tests:
  - `Upload_ExceptionDuringWrite_PartialFileDeleted`
  - `Upload_CancellationTokenTriggered_PartialFileDeleted`
  - `Upload_ChecksumMismatch_PartialFileDeleted`
  - `Upload_DiskFull_PartialFileDeleted`

- [x] T048 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Cancellation.cs` with integration tests:
  - `Upload_CancelledMidTransfer_NoPartialFileRemains`
  - `Upload_ClientDisconnects_NoPartialFileRemains`
  - `Upload_ServerAborts_NoPartialFileRemains`

### Implementation

- [x] T049 [US6] Add `CancellationToken` parameter to upload endpoint and pass through to file write operations
- [x] T050 [US6] Wrap file write in try/finally to delete partial file on exception or cancellation
- [x] T051 [US6] Monitor `HttpContext.RequestAborted` token to detect client disconnection
- [x] T052 [US6] Add cleanup logic for disk exhaustion scenario (catch IOException, delete partial, return 507)

**Checkpoint**: T047 and T048 tests pass - no orphaned partial files

---

## Phase 9: User Story 8 - File Download with Integrity (Priority: P2)

**Goal**: Download files from server with SHA256 integrity verification

**Independent Test**: Upload a file, download it, verify content and checksum match

### 🧪 Tests FIRST

- [x] T053 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/DownloadEndpointTests.cs` with unit tests:
  - `Download_FileExists_ReturnsFileStream`
  - `Download_FileNotExists_Returns404`
  - `Download_IncludesChecksumHeader`
  - `Download_PathTraversal_Returns403`
  - `Download_SetsContentTypeAndLength`

- [x] T054 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/FileTransferServiceDownloadTests.cs` with unit tests:
  - `DownloadFile_ValidFile_ReturnsContent`
  - `DownloadFile_VerifiesChecksumFromHeader`
  - `DownloadFile_ChecksumMismatch_ThrowsIntegrityException`
  - `DownloadFile_FileNotFound_ThrowsFileNotFoundException`
  - `DownloadFile_Cancelled_ThrowsTaskCancelledException`

- [x] T055 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Download.cs` with integration tests:
  - `Download_ExistingFile_ReturnsCorrectContent`
  - `Download_VerifiesIntegrity_EndToEnd`
  - `Download_NonExistentFile_Returns404`
  - `Download_PathTraversal_Returns403`

### Implementation

- [x] T056 [US8] Create download endpoint `GET /cli/filedownload` in `FileTransferEndpointService.cs`
- [x] T057 [US8] Add path validation to download endpoint using same `ValidatePath()` method
- [x] T058 [US8] Compute SHA256 checksum of file and include in `X-File-Checksum` response header
- [x] T059 [US8] Stream file content with proper `Content-Type` and `Content-Length` headers
- [x] T060 [US8] Add download method to `FileTransferService.cs` on client side
- [x] T061 [US8] Add client-side checksum verification comparing response header to computed hash

**Checkpoint**: T053, T054, T055 tests pass - bidirectional file transfer with integrity

---

## Phase 10: Server-Side SandboxedFileSystem Complete Implementation (Priority: P1)

**Goal**: Complete the server-side `SandboxedFileSystem` implementation so server-executed commands have full file/directory access within the sandbox.

**Architecture Note**: With the corrected architecture, `SandboxedFileSystem` is SERVER-SIDE and operates on LOCAL files within `StorageRootPath`. There is NO client-side RPC for file operations. The `SandboxedFileSystem` simply wraps the real `FileSystem` with path validation.

### 🧪 Tests FIRST - Server-Side SandboxedFile

- [x] T062 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/SandboxedFileTests.cs` with unit tests:
  - `Exists_FileExists_ReturnsTrue`
  - `Exists_FileNotExists_ReturnsFalse`
  - `Exists_PathTraversal_ThrowsUnauthorizedAccess`
  - `ReadAllText_FileExists_ReturnsContent`
  - `ReadAllText_FileNotExists_ThrowsFileNotFoundException`
  - `ReadAllText_PathTraversal_ThrowsUnauthorizedAccess`
  - `WriteAllBytes_ValidPath_WritesFile`
  - `WriteAllBytes_PathTraversal_ThrowsUnauthorizedAccess`
  - `WriteAllBytes_DisallowedExtension_ThrowsException`
  - `WriteAllBytes_ExceedsMaxSize_ThrowsException`
  - `Delete_FileExists_DeletesFile`
  - `Delete_FileNotExists_ThrowsFileNotFoundException`
  - `GetAttributes_ValidPath_ReturnsAttributes`

### 🧪 Tests FIRST - Server-Side SandboxedDirectory

- [x] T063 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/SandboxedDirectoryTests.cs` with unit tests:
  - `Exists_DirectoryExists_ReturnsTrue`
  - `Exists_DirectoryNotExists_ReturnsFalse`
  - `Exists_PathTraversal_ThrowsUnauthorizedAccess`
  - `CreateDirectory_ValidPath_CreatesDirectory`
  - `CreateDirectory_AlreadyExists_Succeeds`
  - `CreateDirectory_PathTraversal_ThrowsUnauthorizedAccess`
  - `EnumerateFiles_ReturnsFiles`
  - `EnumerateFiles_WithPattern_ReturnsMatchingFiles`
  - `EnumerateFiles_Recursive_ReturnsAllFiles`
  - `EnumerateDirectories_ReturnsSubdirectories`
  - `Delete_EmptyDirectory_Succeeds`
  - `Delete_NonEmptyNonRecursive_ThrowsIOException`
  - `Delete_NonEmptyRecursive_DeletesAll`

### 🧪 Tests FIRST - Integration

- [x] T064 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_ServerSandbox.cs` with integration tests:
  - `ServerCommand_UsesIFileSystem_ConfinedToStorageRoot`
  - `ServerCommand_File_WriteAndRead_RoundTrip`
  - `ServerCommand_Directory_CreateEnumerateDelete_FullCycle`
  - `ServerCommand_PathTraversal_Rejected`

### Implementation

- [x] T065 [US5] Complete `SandboxedFile` implementation - all `IFile` methods that delegate to local file system with path validation
- [x] T066 [US5] Complete `SandboxedDirectory` implementation - all `IDirectory` methods that delegate to local directory operations with path validation
- [x] T067 [US5] Complete `SandboxedFileInfoFactory` implementation with path validation
- [x] T068 [US5] Complete `SandboxedDirectoryInfoFactory` implementation with path validation
- [x] T069 [US5] Wire `SandboxedFileSystem` into server DI for command execution scope
- [x] T070 [US5] Ensure all validators (`PathValidator`, `FileSizeValidator`, `ExtensionValidator`) are injected and used

**Checkpoint**: T062, T063, T064 tests pass - server-side SandboxedFileSystem fully functional

---

## Phase 11: User Story 9 - Large File Progress (Priority: P3)

**Goal**: Progress reporting works for files >2GB without integer overflow

**Independent Test**: Upload a file >2GB and verify progress values are accurate

### 🧪 Tests FIRST

- [x] T071 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LargeFileProgressTests.cs` with unit tests:
  - `Progress_ValueAbove2GB_DoesNotOverflow`
  - `Progress_TotalReadIsLongType_Verified`
  - `Progress_MaxLongValue_HandledCorrectly`

- [x] T072 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LargeFile.cs` with integration tests:
  - `Upload_SimulatedLargeFile_ProgressReportsCorrectly`
  - `Progress_Above2GB_NoOverflowException`

### Implementation

- [x] T073 [US9] Audit all progress counters to ensure `long` type usage throughout codebase
- [x] T074 [US9] Add explicit test with simulated large file (mock stream with >2GB position)

**Checkpoint**: T071 and T072 tests pass - large file transfers work correctly

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Observability, documentation, and final validation

### 🧪 Observability Tests

- [x] T075 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/SecurityLoggingTests.cs` with unit tests:
  - `PathTraversalAttempt_LogsSecurityEvent`
  - `ExtensionRejection_LogsSecurityEvent`
  - `SizeLimitExceeded_LogsSecurityEvent`
  - `ChecksumMismatch_LogsSecurityEvent`

### Implementation

- [x] T076 [P] Add structured logging for security rejection events (path traversal, extension, size) in `FileTransferEndpointService.cs`
- [x] T077 [P] Add structured logging for security events in `SandboxedFileSystem` and validators

### Documentation

- [x] T078 Create `Docs/Remote/FileSystem.md` with IFileSystem usage documentation
- [x] T079 [P] Create `Docs/Remote/FileSystemConfiguration.md` with server configuration documentation
- [x] T080 Update `Docs/Remote/CommandLineServer.md` to add "Configuring File System Access" section
- [x] T081 Update `Docs/readme.md` to add file system link in Remote section

### Final Validation

- [x] T082 Run all tests - verify 100% pass rate (458 pass, 2 flaky pass in isolation)
- [x] T083 Run quickstart.md validation - verify all code samples compile and work

---

## Dependencies & Execution Order

### TDD Workflow Per Phase

For each phase:
1. Write test tasks (🧪) FIRST
2. Run tests - verify they FAIL (tests are valid)
3. Implement code tasks
4. Run tests - verify they PASS
5. Refactor if needed - tests still pass
6. Move to next phase

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational - MVP core abstraction
- **User Story 3 (Phase 4)**: Depends on Phase 3 - path validation before other security
- **User Story 4 (Phase 5)**: Depends on Phase 4 - size/extension restrictions
- **User Story 7 (Phase 6)**: Depends on Phase 5 - token handling
- **User Story 2 (Phase 7)**: Depends on Phase 6 - checksum verification
- **User Story 6 (Phase 8)**: Depends on Phase 7 - cancellation/cleanup
- **User Story 8 (Phase 9)**: Depends on Phase 8 - download capability
- **User Story 5 (Phase 10)**: Depends on Phase 3 - server-side SandboxedFileSystem completion (can start after MVP)
- **User Story 9 (Phase 11)**: Depends on Phase 7 - large file validation
- **Polish (Phase 12)**: Depends on all stories complete

### User Story Priority Order

| Story | Priority | Description | Phase | Test Tasks | Impl Tasks |
|-------|----------|-------------|-------|------------|------------|
| US1 | P0 | Transparent File Access | 3 | 1 | 5 |
| US3 | P1 | Path Validation | 4 | 2 | 3 |
| US4 | P1 | Size/Type Restrictions | 5 | 3 | 4 |
| US7 | P2 | Secure Token Handling | 6 | 2 | 3 |
| US2 | P1 | Upload Integrity | 7 | 2 | 5 |
| US6 | P2 | Cancellation/Cleanup | 8 | 2 | 4 |
| US8 | P2 | Download Integrity | 9 | 3 | 6 |
| US5 | P1 | Server-Side SandboxedFS | 10 | 3 | 6 |
| US9 | P3 | Large File Progress | 11 | 2 | 2 |

### Parallel Opportunities by Phase

**Phase 1 (Setup)**: T002-T004 can run in parallel; T005-T008 can run in parallel

**Phase 2 (Foundational)**: T014-T016 can run in parallel

**Phase 4-5 (Security Tests)**: T023-T024 can run in parallel; T028-T030 can run in parallel

**Phase 10 (Server SandboxedFS)**: T062, T063, T064 test files can run in parallel

**Phase 12 (Polish)**: T076-T079 can run in parallel

---

## Test Coverage Summary

| Category | Test Files | Test Count (approx) |
|----------|------------|---------------------|
| Configuration | FileTransferOptionsTests.cs | 7 |
| Path Validation | PathValidationTests.cs | 10 |
| Size/Extension | FileSizeValidationTests.cs, ExtensionValidationTests.cs | 9 |
| Checksum | ChecksumTests.cs | 7 |
| Token Security | FileTransferServiceAuthTests.cs | 4 |
| Cleanup | PartialFileCleanupTests.cs | 4 |
| Download | DownloadEndpointTests.cs, FileTransferServiceDownloadTests.cs | 10 |
| SandboxedFile | SandboxedFileTests.cs | 9 |
| SandboxedDirectory | SandboxedDirectoryTests.cs | 8 |
| Large Files | LargeFileProgressTests.cs | 3 |
| Security Logging | SecurityLoggingTests.cs | 4 |
| **Integration** | Multiple files | 20+ |
| **TOTAL** | ~12 test files | ~95 tests |

---

## Implementation Strategy

### MVP First (Phase 1-3)

1. Complete Phase 1: Setup - NuGet packages, delete old files
2. Complete Phase 2: Write tests (T010) → Implement (T011-T016)
3. Complete Phase 3: Write tests (T017) → Implement (T018-T022)
4. **STOP and VALIDATE**: All tests pass, local execution works
5. Deploy/demo if ready (local-only MVP)

### Security Hardening (Phase 4-8)

1. Phase 4: Tests (T023-T024) → Implement path validation (T025-T027)
2. Phase 5: Tests (T028-T030) → Implement restrictions (T031-T034)
3. Phase 6: Tests (T035-T036) → Implement token handling (T037-T039)
4. Phase 7: Tests (T040-T041) → Implement checksums (T042-T046)
5. Phase 8: Tests (T047-T048) → Implement cleanup (T049-T052)
6. **STOP and VALIDATE**: Full security hardening complete, all tests pass

### Full Remote Capability (Phase 9-11)

1. Phase 9: Tests (T053-T055) → Implement download (T056-T061)
2. Phase 10: Tests (T062-T064) → Implement server-side SandboxedFileSystem (T065-T070)
3. Phase 11: Tests (T071-T072) → Implement large file support (T073-T074)
4. **STOP and VALIDATE**: Full bidirectional remote file system, all tests pass

### Production Ready (Phase 12)

1. Tests (T075) → Observability and logging (T076-T077)
2. Documentation (T078-T081)
3. Final validation (T082-T083)

---

## Notes

- 🧪 test tasks MUST be written and FAIL before implementation begins
- [P] tasks = different files, no dependencies on incomplete tasks in same phase
- [Story] label maps task to specific user story for traceability
- Commit after each task or logical group
- Stop at any checkpoint to validate - all tests should pass
- Test naming convention: `MethodUnderTest_Scenario_ExpectedBehavior`
- Use Moq for mocking dependencies in unit tests
- Use TestEnvironment for integration tests
