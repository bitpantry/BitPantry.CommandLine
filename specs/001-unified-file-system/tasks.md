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

- [ ] T001 Add `TestableIO.System.IO.Abstractions.Wrappers` NuGet package to `BitPantry.CommandLine/BitPantry.CommandLine.csproj`
- [ ] T002 [P] Add `TestableIO.System.IO.Abstractions.Wrappers` NuGet package to `BitPantry.CommandLine.Remote.SignalR.Client/BitPantry.CommandLine.Remote.SignalR.Client.csproj`
- [ ] T003 [P] Add `TestableIO.System.IO.Abstractions.TestingHelpers` NuGet package to `BitPantry.CommandLine.Tests/BitPantry.CommandLine.Tests.csproj`
- [ ] T004 [P] Add `TestableIO.System.IO.Abstractions.TestingHelpers` NuGet package to `BitPantry.CommandLine.Tests.Remote.SignalR/BitPantry.CommandLine.Tests.Remote.SignalR.csproj`
- [ ] T005 Delete custom abstraction file `BitPantry.CommandLine/IFileService.cs`
- [ ] T006 [P] Delete custom abstraction file `BitPantry.CommandLine/LocalDiskFileService.cs`
- [ ] T007 [P] Delete static extension methods file `BitPantry.CommandLine.Remote.SignalR.Client/CommandBaseExtensions_FileTransfer.cs`
- [ ] T008 [P] Delete obsolete test file `BitPantry.CommandLine.Tests/LocalDiskFileServiceTests.cs`
- [ ] T009 Update `BitPantry.CommandLine/ServiceCollectionExtensions.cs` to register `IFileSystem` → `FileSystem` (singleton)

**Checkpoint**: Project compiles with new NuGet packages, old abstractions removed

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### 🧪 Tests FIRST

- [ ] T010 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/FileTransferOptionsTests.cs` with validation tests:
  - `Validate_StorageRootPathNull_ThrowsInvalidOperationException`
  - `Validate_StorageRootPathEmpty_ThrowsInvalidOperationException`
  - `Validate_MaxFileSizeBytesZero_ThrowsArgumentException`
  - `Validate_MaxFileSizeBytesNegative_ThrowsArgumentException`
  - `Validate_ValidConfiguration_Succeeds`
  - `Validate_DefaultMaxFileSize_Is100MB`
  - `Validate_DefaultAllowedExtensions_IsNull`

### Implementation

- [ ] T011 Create `BitPantry.CommandLine.Remote.SignalR.Server/Files/FileTransferOptions.cs` with `StorageRootPath` (required), `MaxFileSizeBytes` (default 100MB), `AllowedExtensions` (default null)
- [ ] T012 Add startup validation in server configuration to throw `InvalidOperationException` if `StorageRootPath` is null/empty
- [ ] T013 Add `MessageArgNames.FileSystem` constants class for RPC message field names in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`
- [ ] T014 [P] Add file system RPC method names to `BitPantry.CommandLine.Remote.SignalR/SignalRMethodNames.cs`
- [ ] T015 Change `TotalRead` from `int` to `long` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/FileUploadProgressMessage.cs`
- [ ] T016 [P] Change `TotalRead` from `int` to `long` in `BitPantry.CommandLine.Remote.SignalR.Client/FileUploadProgress.cs`

**Checkpoint**: Foundation ready - T010 tests pass, configuration model exists

---

## Phase 3: User Story 1 - Transparent File Access (Priority: P0) 🎯 MVP

**Goal**: Commands use `IFileSystem` for all file operations, working identically locally or remotely

**Independent Test**: Create a command with `IFileSystem` injected, verify it reads/writes files locally without restrictions

### 🧪 Tests FIRST

- [ ] T017 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_FileSystemLifecycle.cs` with tests:
  - `IFileSystem_LocalExecution_IsFileSystemType`
  - `IFileSystem_LocalExecution_HasUnrestrictedAccess`
  - `IFileSystem_AfterConnect_IsSandboxedFileSystemType`
  - `IFileSystem_AfterDisconnect_RevertsToFileSystemType`
  - `Command_InjectsIFileSystem_CanReadWriteLocally`

### Implementation

- [ ] T018 [US1] Create base `SandboxedFileSystem : FileSystemBase` class in `BitPantry.CommandLine.Remote.SignalR.Client/SandboxedFileSystem.cs` with constructor accepting HubConnection and HttpClient
- [ ] T019 [P] [US1] Create `SandboxedPath : PathBase` class in `BitPantry.CommandLine.Remote.SignalR.Client/SandboxedPath.cs` delegating to local `System.IO.Path`
- [ ] T020 [US1] Update `BitPantry.CommandLine.Remote.SignalR.Client/CommandLineApplicationBuilderExtensions.cs` to register `IFileSystem` lifecycle (local `FileSystem` by default, swap to `SandboxedFileSystem` on connect)
- [ ] T021 [US1] Update `BitPantry.CommandLine.Remote.SignalR.Client/ConnectCommand.cs` to swap `IFileSystem` to `SandboxedFileSystem` on successful connection
- [ ] T022 [US1] Update `BitPantry.CommandLine.Remote.SignalR.Client/DisconnectCommand.cs` to revert `IFileSystem` to local `FileSystem` on disconnect

**Checkpoint**: T017 tests pass - commands can inject `IFileSystem`, local execution works

---

## Phase 4: User Story 3 - Protected File Storage with Path Validation (Priority: P1)

**Goal**: All remote file operations are restricted to designated storage directory

**Independent Test**: Attempt file operations with path traversal sequences and verify server rejects them

### 🧪 Tests FIRST

- [ ] T023 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/PathValidationTests.cs` with unit tests:
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

- [ ] T024 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_PathTraversal.cs` with integration tests:
  - `Upload_PathTraversalDotDot_Returns403Forbidden`
  - `Upload_AbsolutePathOutsideRoot_Returns403Forbidden`
  - `Upload_EncodedTraversal_Returns403Forbidden`
  - `Upload_ValidRelativePath_Succeeds`
  - `Upload_NestedSubdirectory_CreatesAndSucceeds`

### Implementation

- [ ] T025 [US3] Create `ValidatePath()` method in `BitPantry.CommandLine.Remote.SignalR.Server/Files/FileTransferEndpointService.cs` using `Path.GetFullPath()` comparison
- [ ] T026 [US3] Add path validation to upload endpoint in `FileTransferEndpointService.cs` before any file write
- [ ] T027 [US3] Return 403 Forbidden status for path traversal attempts with structured error response

**Checkpoint**: T023 and T024 tests pass - path traversal attacks are blocked

---

## Phase 5: User Story 4 - File Size and Type Restrictions (Priority: P1)

**Goal**: Server rejects uploads exceeding size limits or with disallowed extensions

**Independent Test**: Configure limits and attempt operations that exceed them

### 🧪 Tests FIRST

- [ ] T028 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/FileSizeValidationTests.cs` with unit tests:
  - `ValidateSize_ContentLengthWithinLimit_Succeeds`
  - `ValidateSize_ContentLengthExceedsLimit_ThrowsException`
  - `ValidateSize_NoContentLengthHeader_StreamingCheckSucceeds`
  - `ValidateSize_StreamingExceedsLimit_AbortsAndThrows`

- [ ] T029 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/ExtensionValidationTests.cs` with unit tests:
  - `ValidateExtension_AllowedExtension_Succeeds`
  - `ValidateExtension_DisallowedExtension_ThrowsException`
  - `ValidateExtension_AllowedExtensionsNull_AllowsAll`
  - `ValidateExtension_CaseInsensitiveMatch_Succeeds`
  - `ValidateExtension_NoExtension_Behavior` (define expected behavior)

- [ ] T030 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_SizeAndExtension.cs` with integration tests:
  - `Upload_ExceedsMaxSize_Returns413`
  - `Upload_WithinMaxSize_Succeeds`
  - `Upload_DisallowedExtension_Returns400`
  - `Upload_AllowedExtension_Succeeds`
  - `Upload_ExtensionCheckWithNullAllowList_AllExtensionsAccepted`

### Implementation

- [ ] T031 [US4] Add extension validation logic in `FileTransferEndpointService.cs` checking against `AllowedExtensions` (skip if null)
- [ ] T032 [US4] Add size limit pre-flight check in upload endpoint using `Content-Length` header vs `MaxFileSizeBytes`
- [ ] T033 [US4] Add streaming size limit check during upload to abort if accumulated bytes exceed limit
- [ ] T034 [US4] Return appropriate HTTP status codes: 413 for size limit, 400 for extension rejection

**Checkpoint**: T028, T029, T030 tests pass - size and extension restrictions enforced

---

## Phase 6: User Story 7 - Secure Token Handling (Priority: P2)

**Goal**: Access tokens transmitted in Authorization headers, not URL query strings

**Independent Test**: Capture HTTP traffic and verify tokens appear in headers, not URLs

### 🧪 Tests FIRST

- [ ] T035 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/FileTransferServiceAuthTests.cs` with unit tests:
  - `UploadFile_SendsAuthorizationBearerHeader`
  - `UploadFile_DoesNotIncludeTokenInQueryString`
  - `DownloadFile_SendsAuthorizationBearerHeader`
  - `DownloadFile_DoesNotIncludeTokenInQueryString`

- [ ] T036 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_TokenSecurity.cs` with integration tests:
  - `Upload_TokenInHeader_Succeeds`
  - `Upload_MissingAuthHeader_Returns401`
  - `Upload_InvalidToken_Returns401`
  - `ServerRequestLog_DoesNotContainToken` (verify URL logging safety)

### Implementation

- [ ] T037 [US7] Modify `BitPantry.CommandLine.Remote.SignalR.Client/FileTransferService.cs` to send token in `Authorization: Bearer` header instead of query string
- [ ] T038 [US7] Update upload endpoint in `FileTransferEndpointService.cs` to read token from `Authorization` header
- [ ] T039 [US7] Remove token from query string handling in server upload endpoint

**Checkpoint**: T035 and T036 tests pass - tokens no longer appear in URLs

---

## Phase 7: User Story 2 - Secure File Upload with Integrity (Priority: P1)

**Goal**: File uploads complete with verified SHA256 checksum

**Independent Test**: Upload a file and verify SHA256 checksum matches on client and server

### 🧪 Tests FIRST

- [ ] T040 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/ChecksumTests.cs` with unit tests:
  - `ComputeIncrementalHash_ValidStream_ReturnsCorrectSha256`
  - `ComputeIncrementalHash_EmptyStream_ReturnsEmptyFileHash`
  - `ComputeIncrementalHash_LargeStream_ComputesCorrectly`
  - `VerifyChecksum_MatchingHash_Succeeds`
  - `VerifyChecksum_MismatchedHash_ThrowsException`
  - `VerifyChecksum_MissingHeader_ThrowsException`
  - `VerifyChecksum_InvalidHexFormat_ThrowsException`

- [ ] T041 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Checksum.cs` with integration tests:
  - `Upload_ValidChecksum_FilePreserved`
  - `Upload_InvalidChecksum_FileDeleted_Returns400`
  - `Upload_MissingChecksumHeader_Returns400`
  - `Upload_ChecksumCaseInsensitive_Succeeds`

### Implementation

- [ ] T042 [US2] Add incremental SHA256 hashing to `FileTransferService.cs` using `IncrementalHash.CreateHash(HashAlgorithmName.SHA256)` during upload streaming
- [ ] T043 [US2] Send computed checksum in `X-File-Checksum` request header from client
- [ ] T044 [US2] Add incremental SHA256 hashing to `FileTransferEndpointService.cs` during file write streaming
- [ ] T045 [US2] Compare client checksum header with server-computed checksum after upload completes
- [ ] T046 [US2] Delete uploaded file and return 400 error if checksum verification fails

**Checkpoint**: T040 and T041 tests pass - all uploads are checksum-verified

---

## Phase 8: User Story 6 - Cancellation and Cleanup (Priority: P2)

**Goal**: Cancelled or failed operations leave no orphaned partial files

**Independent Test**: Start large upload, cancel mid-transfer, verify no partial file remains

### 🧪 Tests FIRST

- [ ] T047 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/PartialFileCleanupTests.cs` with unit tests:
  - `Upload_ExceptionDuringWrite_PartialFileDeleted`
  - `Upload_CancellationTokenTriggered_PartialFileDeleted`
  - `Upload_ChecksumMismatch_PartialFileDeleted`
  - `Upload_DiskFull_PartialFileDeleted`

- [ ] T048 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Cancellation.cs` with integration tests:
  - `Upload_CancelledMidTransfer_NoPartialFileRemains`
  - `Upload_ClientDisconnects_NoPartialFileRemains`
  - `Upload_ServerAborts_NoPartialFileRemains`

### Implementation

- [ ] T049 [US6] Add `CancellationToken` parameter to upload endpoint and pass through to file write operations
- [ ] T050 [US6] Wrap file write in try/finally to delete partial file on exception or cancellation
- [ ] T051 [US6] Monitor `HttpContext.RequestAborted` token to detect client disconnection
- [ ] T052 [US6] Add cleanup logic for disk exhaustion scenario (catch IOException, delete partial, return 507)

**Checkpoint**: T047 and T048 tests pass - no orphaned partial files

---

## Phase 9: User Story 8 - File Download with Integrity (Priority: P2)

**Goal**: Download files from server with SHA256 integrity verification

**Independent Test**: Upload a file, download it, verify content and checksum match

### 🧪 Tests FIRST

- [ ] T053 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/DownloadEndpointTests.cs` with unit tests:
  - `Download_FileExists_ReturnsFileStream`
  - `Download_FileNotExists_Returns404`
  - `Download_IncludesChecksumHeader`
  - `Download_PathTraversal_Returns403`
  - `Download_SetsContentTypeAndLength`

- [ ] T054 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/FileTransferServiceDownloadTests.cs` with unit tests:
  - `DownloadFile_ValidFile_ReturnsContent`
  - `DownloadFile_VerifiesChecksumFromHeader`
  - `DownloadFile_ChecksumMismatch_ThrowsIntegrityException`
  - `DownloadFile_FileNotFound_ThrowsFileNotFoundException`
  - `DownloadFile_Cancelled_ThrowsTaskCancelledException`

- [ ] T055 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_Download.cs` with integration tests:
  - `Download_ExistingFile_ReturnsCorrectContent`
  - `Download_VerifiesIntegrity_EndToEnd`
  - `Download_NonExistentFile_Returns404`
  - `Download_PathTraversal_Returns403`

### Implementation

- [ ] T056 [US8] Create download endpoint `GET /cli/filedownload` in `FileTransferEndpointService.cs`
- [ ] T057 [US8] Add path validation to download endpoint using same `ValidatePath()` method
- [ ] T058 [US8] Compute SHA256 checksum of file and include in `X-File-Checksum` response header
- [ ] T059 [US8] Stream file content with proper `Content-Type` and `Content-Length` headers
- [ ] T060 [US8] Add download method to `FileTransferService.cs` on client side
- [ ] T061 [US8] Add client-side checksum verification comparing response header to computed hash

**Checkpoint**: T053, T054, T055 tests pass - bidirectional file transfer with integrity

---

## Phase 10: User Story 5 - Remote Directory Operations (Priority: P1)

**Goal**: Commands can enumerate, create, and delete directories on remote server

**Independent Test**: Create directories, enumerate contents, delete them via `IFileSystem.Directory`

### 🧪 Tests FIRST - RPC Handler

- [ ] T062 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/FileSystemRpcHandlerTests.cs` with unit tests:
  - `HandleFileExistsRequest_FileExists_ReturnsTrue`
  - `HandleFileExistsRequest_FileNotExists_ReturnsFalse`
  - `HandleFileExistsRequest_PathTraversal_ReturnsErrorResponse`
  - `HandleDirectoryExistsRequest_DirectoryExists_ReturnsTrue`
  - `HandleDirectoryExistsRequest_DirectoryNotExists_ReturnsFalse`
  - `HandleGetFileInfoRequest_FileExists_ReturnsFileInfo`
  - `HandleGetFileInfoRequest_FileNotExists_ReturnsErrorResponse`
  - `HandleEnumerateFilesRequest_WithPattern_ReturnsMatchingFiles`
  - `HandleEnumerateFilesRequest_Recursive_ReturnsAllFiles`
  - `HandleEnumerateFilesRequest_EmptyDirectory_ReturnsEmptyArray`
  - `HandleEnumerateDirectoriesRequest_ReturnsSubdirectories`
  - `HandleCreateDirectoryRequest_ValidPath_CreatesDirectory`
  - `HandleCreateDirectoryRequest_AlreadyExists_Succeeds`
  - `HandleDeleteFileRequest_FileExists_DeletesFile`
  - `HandleDeleteFileRequest_FileNotExists_ReturnsErrorResponse`
  - `HandleDeleteDirectoryRequest_EmptyDirectory_Succeeds`
  - `HandleDeleteDirectoryRequest_NonEmptyNonRecursive_ReturnsErrorResponse`
  - `HandleDeleteDirectoryRequest_NonEmptyRecursive_DeletesAll`

### 🧪 Tests FIRST - Client SandboxedFile/Directory

- [ ] T063 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/SandboxedFileTests.cs` with unit tests (mocked RPC):
  - `Exists_RpcReturnsTrue_ReturnsTrue`
  - `Exists_RpcReturnsFalse_ReturnsFalse`
  - `Exists_RpcTimeout_ThrowsTimeoutException`
  - `ReadAllText_UsesHttpDownload_ReturnsContent`
  - `ReadAllText_FileNotExists_ThrowsFileNotFoundException`
  - `WriteAllBytes_UsesHttpUpload_Succeeds`
  - `WriteAllBytes_ServerRejectsChecksum_ThrowsException`
  - `WriteAllBytes_PathWithSpecialCharacters_UrlEncodesCorrectly` *(FR-012)*
  - `Delete_RpcSucceeds_Completes`
  - `Delete_FileNotExists_ThrowsFileNotFoundException`

- [ ] T064 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/SandboxedDirectoryTests.cs` with unit tests (mocked RPC):
  - `Exists_RpcReturnsTrue_ReturnsTrue`
  - `CreateDirectory_RpcSucceeds_Completes`
  - `EnumerateFiles_RpcReturnsFiles_ReturnsEnumerable`
  - `EnumerateFiles_RecursiveOption_PassedToRpc`
  - `EnumerateDirectories_RpcReturnsDirectories_ReturnsEnumerable`
  - `Delete_EmptyDirectory_Succeeds`
  - `Delete_NonEmptyRecursive_Succeeds`
  - `Delete_NonEmptyNonRecursive_ThrowsIOException`

### 🧪 Tests FIRST - Integration

- [ ] T065 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_DirectoryOperations.cs` with integration tests:
  - `Directory_Create_Succeeds`
  - `Directory_CreateNested_CreatesAllLevels`
  - `Directory_Exists_ReturnsCorrectly`
  - `Directory_EnumerateFiles_ReturnsFiles`
  - `Directory_EnumerateFilesRecursive_ReturnsAllFiles`
  - `Directory_EnumerateFilesWithPattern_ReturnsMatchingOnly`
  - `Directory_DeleteEmpty_Succeeds`
  - `Directory_DeleteRecursive_DeletesAll`
  - `File_Exists_ReturnsCorrectly`
  - `File_WriteAndRead_RoundTrip`
  - `File_Delete_Succeeds`

### RPC Envelopes Implementation

- [ ] T066 [P] [US5] Create `FileExistsRequest.cs` and `FileExistsResponse.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`
- [ ] T067 [P] [US5] Create `DirectoryExistsRequest.cs` and `DirectoryExistsResponse.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`
- [ ] T068 [P] [US5] Create `GetFileInfoRequest.cs` and `GetFileInfoResponse.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`
- [ ] T069 [P] [US5] Create `EnumerateFilesRequest.cs` and `EnumerateFilesResponse.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`
- [ ] T070 [P] [US5] Create `EnumerateDirectoriesRequest.cs` and `EnumerateDirectoriesResponse.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`
- [ ] T071 [P] [US5] Create `CreateDirectoryRequest.cs` and `CreateDirectoryResponse.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`
- [ ] T072 [P] [US5] Create `DeleteFileRequest.cs` and `DeleteFileResponse.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`
- [ ] T073 [P] [US5] Create `DeleteDirectoryRequest.cs` and `DeleteDirectoryResponse.cs` in `BitPantry.CommandLine.Remote.SignalR/Envelopes/`

### Server-Side RPC Handler Implementation

- [ ] T074 [US5] Create `FileSystemRpcHandler.cs` in `BitPantry.CommandLine.Remote.SignalR.Server/Files/` with path validation
- [ ] T075 [US5] Implement `HandleFileExistsRequest` method in `FileSystemRpcHandler.cs`
- [ ] T076 [US5] Implement `HandleDirectoryExistsRequest` method in `FileSystemRpcHandler.cs`
- [ ] T077 [US5] Implement `HandleGetFileInfoRequest` method in `FileSystemRpcHandler.cs`
- [ ] T078 [US5] Implement `HandleEnumerateFilesRequest` method with glob pattern support in `FileSystemRpcHandler.cs`
- [ ] T079 [US5] Implement `HandleEnumerateDirectoriesRequest` method in `FileSystemRpcHandler.cs`
- [ ] T080 [US5] Implement `HandleCreateDirectoryRequest` method in `FileSystemRpcHandler.cs`
- [ ] T081 [US5] Implement `HandleDeleteFileRequest` method in `FileSystemRpcHandler.cs`
- [ ] T082 [US5] Implement `HandleDeleteDirectoryRequest` method with recursive flag in `FileSystemRpcHandler.cs`
- [ ] T083 [US5] Route RPC messages to `FileSystemRpcHandler` from `CommandLineHub.cs`

### Client-Side SandboxedFileSystem Implementation

- [ ] T084 [US5] Create `SandboxedFile : FileBase` in `BitPantry.CommandLine.Remote.SignalR.Client/SandboxedFile.cs` with RPC for Exists, Delete, GetAttributes
- [ ] T085 [US5] Add HTTP upload integration to `SandboxedFile.WriteAllBytes()` and `WriteAllText()`
- [ ] T086 [US5] Add HTTP download integration to `SandboxedFile.ReadAllBytes()` and `ReadAllText()`
- [ ] T087 [US5] Create `SandboxedDirectory : DirectoryBase` in `BitPantry.CommandLine.Remote.SignalR.Client/SandboxedDirectory.cs`
- [ ] T088 [US5] Implement directory RPC methods: Exists, CreateDirectory, Delete, EnumerateFiles, EnumerateDirectories
- [ ] T089 [US5] Create `SandboxedFileInfoFactory : IFileInfoFactory` in `BitPantry.CommandLine.Remote.SignalR.Client/SandboxedFileInfoFactory.cs`
- [ ] T090 [P] [US5] Create `SandboxedDirectoryInfoFactory : IDirectoryInfoFactory` in `BitPantry.CommandLine.Remote.SignalR.Client/SandboxedDirectoryInfoFactory.cs`
- [ ] T091 [US5] Wire all sub-implementations into `SandboxedFileSystem` constructor

**Checkpoint**: T062, T063, T064, T065 tests pass - full IFileSystem abstraction works for remote

---

## Phase 11: User Story 9 - Large File Progress (Priority: P3)

**Goal**: Progress reporting works for files >2GB without integer overflow

**Independent Test**: Upload a file >2GB and verify progress values are accurate

### 🧪 Tests FIRST

- [ ] T092 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LargeFileProgressTests.cs` with unit tests:
  - `Progress_ValueAbove2GB_DoesNotOverflow`
  - `Progress_TotalReadIsLongType_Verified`
  - `Progress_MaxLongValue_HandledCorrectly`

- [ ] T093 🧪 [P] Create `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_LargeFile.cs` with integration tests:
  - `Upload_SimulatedLargeFile_ProgressReportsCorrectly`
  - `Progress_Above2GB_NoOverflowException`

### Implementation

- [ ] T094 [US9] Audit all progress counters to ensure `long` type usage throughout codebase
- [ ] T095 [US9] Add explicit test with simulated large file (mock stream with >2GB position)

**Checkpoint**: T092 and T093 tests pass - large file transfers work correctly

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Observability, documentation, and final validation

### 🧪 Observability Tests

- [ ] T096 🧪 Create `BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/SecurityLoggingTests.cs` with unit tests:
  - `PathTraversalAttempt_LogsSecurityEvent`
  - `ExtensionRejection_LogsSecurityEvent`
  - `SizeLimitExceeded_LogsSecurityEvent`
  - `ChecksumMismatch_LogsSecurityEvent`

### Implementation

- [ ] T097 [P] Add structured logging for security rejection events (path traversal, extension, size) in `FileTransferEndpointService.cs`
- [ ] T098 [P] Add structured logging for security events in `FileSystemRpcHandler.cs`

### Documentation

- [ ] T099 Create `Docs/Remote/FileSystem.md` with IFileSystem usage documentation
- [ ] T100 [P] Create `Docs/Remote/FileSystemConfiguration.md` with server configuration documentation
- [ ] T101 Update `Docs/Remote/CommandLineServer.md` to add "Configuring File System Access" section
- [ ] T102 Update `Docs/readme.md` to add file system link in Remote section

### Final Validation

- [ ] T103 Run all tests - verify 100% pass rate
- [ ] T104 Run quickstart.md validation - verify all code samples compile and work

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
- **User Story 5 (Phase 10)**: Depends on Phase 3 - RPC directory operations (can start after MVP)
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
| US5 | P1 | Directory Operations | 10 | 4 | 26 |
| US9 | P3 | Large File Progress | 11 | 2 | 2 |

### Parallel Opportunities by Phase

**Phase 1 (Setup)**: T002-T004 can run in parallel; T005-T008 can run in parallel

**Phase 2 (Foundational)**: T014-T016 can run in parallel

**Phase 4-5 (Security Tests)**: T023-T024 can run in parallel; T028-T030 can run in parallel

**Phase 10 (US5 RPC Envelopes)**: T066-T073 can all run in parallel (different files)

**Phase 12 (Polish)**: T097-T100 can run in parallel

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
| RPC Handler | FileSystemRpcHandlerTests.cs | 18 |
| SandboxedFile | SandboxedFileTests.cs | 9 |
| SandboxedDirectory | SandboxedDirectoryTests.cs | 8 |
| Large Files | LargeFileProgressTests.cs | 3 |
| Security Logging | SecurityLoggingTests.cs | 4 |
| **Integration** | Multiple files | 30+ |
| **TOTAL** | ~15 test files | ~120 tests |

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
2. Phase 10: Tests (T062-T065) → Implement RPC (T066-T091)
3. Phase 11: Tests (T092-T093) → Implement large file support (T094-T095)
4. **STOP and VALIDATE**: Full bidirectional remote file system, all tests pass

### Production Ready (Phase 12)

1. Tests (T096) → Observability and logging (T097-T098)
2. Documentation (T099-T102)
3. Final validation (T103-T104)

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
