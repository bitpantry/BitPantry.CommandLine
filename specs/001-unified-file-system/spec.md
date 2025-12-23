# Feature Specification: Unified File System Abstraction

**Feature Branch**: `001-unified-file-system`  
**Created**: 2024-12-22  
**Updated**: 2024-12-22  
**Status**: Draft  
**Input**: User description: "Adopt System.IO.Abstractions as unified file abstraction. Local execution uses unrestricted FileSystem. Remote execution uses SandboxedFileSystem that routes operations through HTTP/SignalR with server-side path confinement to StorageRootPath. Includes file transfer security hardening with SHA256 checksums, proper DI, cancellation support."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - File System Abstraction for Local and Remote Commands (Priority: P0)

As a command developer, I want to use `IFileSystem` for all file operations so that my commands have a consistent abstraction. Local commands access the local filesystem, and **server-executed remote commands** operate on the server's sandboxed storage.

**Why this priority**: This is the core abstraction that enables testable file operations. The key insight is that **client never swaps IFileSystem** - client always uses local `FileSystem`. When commands are executed on the server (remote command execution), the server's DI resolves `IFileSystem` to `SandboxedFileSystem` which is confined to `StorageRootPath`.

**Independent Test**: Can be fully tested by creating a command that reads/writes files locally, and verifying server-executed commands are confined to `StorageRootPath`.

**Acceptance Scenarios**:

1. **Given** a command with `IFileSystem` injected running **locally on client**, **When** calling `fileSystem.File.ReadAllText("data.json")`, **Then** it reads from the local filesystem with no path restrictions.
2. **Given** a command with `IFileSystem` injected **executed on the server** (remote command execution), **When** calling `fileSystem.File.ReadAllText("data.json")`, **Then** it reads from the server's `StorageRootPath/data.json` via `SandboxedFileSystem`.
3. **Given** a server-executed command, **When** it attempts to access `../secret.txt` (path traversal), **Then** the `SandboxedFileSystem` rejects the operation with an `UnauthorizedAccessException`.

---

### User Story 2 - Secure File Upload with Integrity Verification (Priority: P1)

As a command developer, I want to upload files to the remote server with automatic integrity verification so that I can be confident the file arrived without corruption.

**Why this priority**: File integrity is core to reliable file transfer - without verified delivery, file transfers cannot be trusted.

**Independent Test**: Can be fully tested by uploading a file and verifying the SHA256 checksum matches on both client and server.

**Acceptance Scenarios**:

1. **Given** a connected client, **When** I call `fileSystem.File.WriteAllBytes("remote.bin", localBytes)`, **Then** the file is uploaded and the server confirms the SHA256 checksum matches.
2. **Given** a file being uploaded, **When** the file becomes corrupted during transfer, **Then** the server detects checksum mismatch, deletes the partial file, and returns an error.
3. **Given** a file upload in progress, **When** the upload completes successfully, **Then** the operation returns without error (checksum verified server-side).

---

### User Story 3 - Protected File Storage with Path Validation (Priority: P1)

As a server administrator, I want all remote file operations to be restricted to a designated storage directory so that malicious clients cannot access arbitrary locations on the server.

**Why this priority**: Path traversal is a critical security vulnerability - without this protection, the entire server filesystem is at risk.

**Independent Test**: Can be fully tested by attempting file operations with path traversal sequences (e.g., `../etc/passwd`) and verifying the server rejects them.

**Acceptance Scenarios**:

1. **Given** a configured `StorageRootPath`, **When** a client accesses a file with a relative path within the root, **Then** the operation succeeds.
2. **Given** a configured `StorageRootPath`, **When** a client attempts access with a path containing `../` traversal sequences, **Then** the server rejects the request with an error.
3. **Given** a configured `StorageRootPath`, **When** a client attempts access with an absolute path outside the root, **Then** the server rejects the request with an error.

---

### User Story 4 - File Size and Type Restrictions (Priority: P1)

As a server administrator, I want to configure maximum file sizes and allowed file extensions so that I can prevent denial-of-service attacks and restrict uploads to approved file types.

**Why this priority**: Without size limits, a single malicious upload could exhaust server disk space.

**Independent Test**: Can be fully tested by configuring limits and attempting operations that exceed them.

**Acceptance Scenarios**:

1. **Given** a maximum file size of 100MB configured, **When** a client attempts to write a 150MB file, **Then** the server rejects the operation before writing significant data.
2. **Given** an allowed extensions list of `.txt,.pdf,.doc`, **When** a client attempts to create a `.exe` file, **Then** the server rejects the operation.
3. **Given** file size and extension restrictions configured, **When** a client creates a compliant file, **Then** the operation succeeds normally.

---

### User Story 5 - Server-Side Directory Operations (Priority: P1)

As a command developer, I want server-side command execution to use `SandboxedFileSystem` with full directory operations (enumerate, create, delete) so that commands executing on the server can safely manipulate files within `StorageRootPath`.

**Corrected Architecture Context**: This applies to commands executed ON THE SERVER from remote client invocations. The server-side `SandboxedFileSystem` wraps a real `FileSystem` with path validation.

**Independent Test**: Can be fully tested by unit testing `SandboxedDirectory` operations against a mock `IDirectory`.

**Acceptance Scenarios**:

1. **Given** a command executing on the server with `SandboxedFileSystem` injected, **When** the command calls `fileSystem.Directory.CreateDirectory("reports/2024")`, **Then** the directory is created under `StorageRootPath`.
2. **Given** a command executing on the server, **When** the command calls `fileSystem.Directory.EnumerateFiles("data", "*.csv", SearchOption.AllDirectories)`, **Then** it receives all matching files recursively within `StorageRootPath/data`.
3. **Given** a command executing on the server, **When** the command calls `fileSystem.Directory.Delete("temp", recursive: true)`, **Then** the directory and all contents are deleted from within `StorageRootPath`.

---

### User Story 6 - Cancellation and Cleanup (Priority: P2)

As a command developer, I want to cancel in-progress file operations and have partial files cleaned up so that cancelled operations don't leave orphaned data on the server.

**Why this priority**: Proper cancellation handling improves reliability and prevents disk space waste.

**Independent Test**: Can be fully tested by starting a large file upload, cancelling mid-transfer, and verifying no partial file remains.

**Acceptance Scenarios**:

1. **Given** a file write in progress, **When** the client cancels via `CancellationToken`, **Then** the server stops processing and deletes any partial file.
2. **Given** a file write in progress, **When** the client disconnects unexpectedly, **Then** the server detects disconnection and cleans up partial files.
3. **Given** an operation that fails due to an error, **When** the error occurs after partial data is written, **Then** the partial file is deleted.

---

### User Story 7 - Secure Token Handling (Priority: P2)

As a security-conscious administrator, I want access tokens to be transmitted in Authorization headers rather than URL query strings so that tokens are not exposed in server logs.

**Why this priority**: Token exposure in logs is a security risk that could lead to unauthorized access.

**Independent Test**: Can be fully tested by capturing HTTP traffic and verifying tokens appear in headers, not URLs.

**Acceptance Scenarios**:

1. **Given** an authenticated client, **When** performing file operations, **Then** the access token is sent in the `Authorization: Bearer` header, not in the query string.
2. **Given** server request logging enabled, **When** a file operation request is logged, **Then** the logged URL does not contain the access token.

---

### User Story 8 - File Download with Integrity Verification (Priority: P2)

As a command developer, I want to download files from the remote server with automatic integrity verification so that I can retrieve files with confidence they are complete and uncorrupted.

**Why this priority**: Download capability completes bidirectional file transfer; integrity verification ensures parity with upload security.

**Independent Test**: Can be fully tested by uploading a file, downloading it, and verifying content and checksum match.

**Acceptance Scenarios**:

1. **Given** a file exists on the server, **When** I call `fileSystem.File.ReadAllBytes("data.bin")`, **Then** the file is downloaded and the SHA256 checksum is verified client-side.
2. **Given** a file download in progress, **When** corruption occurs during transfer, **Then** the client detects checksum mismatch and reports an error.
3. **Given** a file download request, **When** the file does not exist, **Then** the client receives a `FileNotFoundException`.

---

### User Story 9 - Large File Progress Reporting (Priority: P3)

As a command developer, I want progress reporting to work correctly for files larger than 2GB so that I can transfer large files without integer overflow errors.

**Why this priority**: While most files are smaller, supporting large files prevents unexpected failures.

**Independent Test**: Can be fully tested by uploading a file larger than 2GB and verifying progress values are accurate.

**Acceptance Scenarios**:

1. **Given** a 3GB file being transferred, **When** progress updates are received, **Then** byte counts correctly report values above 2GB.
2. **Given** progress reporting during transfer, **When** file size exceeds `int.MaxValue`, **Then** no overflow exception occurs.

---

### Edge Cases

- File paths with special characters (spaces, unicode, URL-reserved characters) must work correctly.
- Concurrent writes to the same path: Last-write-wins (overwrite silently).
- Disk space exhaustion mid-upload: Server aborts, deletes partial file, returns "insufficient disk space" error.
- Very large files that take a long time to hash: Streaming/incremental hashing handles this.
- Storage root directory doesn't exist: Server creates it on startup if missing.
- Remote file enumeration with glob patterns containing special regex characters: Must be properly escaped.

## Requirements *(mandatory)*

### Functional Requirements

#### Core Abstraction

- **FR-001**: System MUST use `System.IO.Abstractions` library (`IFileSystem` interface) as the unified file abstraction.
- **FR-002**: Client-side execution MUST always register `FileSystem` (unrestricted wrapper) in DI container. The client NEVER swaps `IFileSystem` based on connection state.
- **FR-003**: Server-side command execution MUST register `SandboxedFileSystem` in DI container, so that commands executed on the server on behalf of remote clients receive the sandboxed implementation.
- **FR-004**: `SandboxedFileSystem` MUST implement all `IFileSystem` sub-interfaces (`IFile`, `IDirectory`, `IPath`, etc.) and confine all operations to `StorageRootPath`.
- **FR-005**: Commands MUST inject `IFileSystem` via constructor, not concrete implementations.
- **FR-006**: System MUST delete existing `IFileService` and `LocalDiskFileService` custom abstractions.

#### Security

- **FR-007**: System MUST transmit access tokens in HTTP `Authorization: Bearer` header, not in URL query strings.
- **FR-008**: Server MUST validate that all file paths resolve within the configured `StorageRootPath`.
- **FR-009**: Server MUST reject operations with paths containing traversal sequences that would escape the storage root.
- **FR-010**: Server MUST validate file extensions against a configurable allowlist before accepting writes. When `AllowedExtensions` is null/unset, all extensions are permitted (opt-in restriction).
- **FR-011**: Server MUST validate file size against a configurable maximum before and during streaming. Default: 100MB (104,857,600 bytes).
- **FR-012**: Server MUST URL-encode all query string parameters to handle special characters.

#### Integrity

- **FR-013**: System MUST compute SHA256 checksum incrementally during upload using `IncrementalHash`.
- **FR-014**: Client MUST send computed checksum in `X-File-Checksum` request header for uploads.
- **FR-015**: Server MUST compute checksum incrementally during file write and compare to client-provided checksum.
- **FR-016**: Server MUST delete uploaded file and return error if checksum verification fails.
- **FR-017**: Download responses MUST include `X-File-Checksum` header for client-side verification.

#### Reliability

- **FR-018**: Server MUST honor cancellation tokens to detect client disconnection.
- **FR-019**: Server MUST delete partial files when operations are cancelled, fail, or client disconnects.
- **FR-020**: Progress reporting MUST use `long` type for byte counts to support files larger than 2GB.

#### ~~Remote Operations (SignalR RPC)~~ - REMOVED

> **NOTE**: FR-021 to FR-024 have been REMOVED. The corrected architecture has SandboxedFileSystem
> operating on local files (server-side only). There is no client-side RPC for file metadata operations.

#### Remote Operations (HTTP Streaming - FileTransferService)

- **FR-021**: `FileTransferService` on client MUST send file content via HTTP streaming when explicit file upload is needed.
- **FR-022**: Upload endpoint MUST be `POST /cli/fileupload` (existing endpoint, hardened).
- **FR-023**: Download endpoint MUST be `GET /cli/filedownload` (existing endpoint).
- **FR-024**: Progress updates MUST be sent via SignalR using existing `FileUploadProgressMessage` pattern.

#### Configuration

- **FR-025**: Server MUST support `MaxFileSizeBytes` configuration option.
- **FR-026**: Server MUST support `AllowedExtensions` configuration option as a collection of permitted file extensions.
- **FR-027**: Server MUST require `StorageRootPath` configuration option defining the base directory for file storage. Server MUST fail to start if not configured.

#### Observability

- **FR-028**: Server MUST log security rejection events (path traversal attempts, disallowed extensions, size limit violations) with structured data.

### Key Entities

- **IFileSystem**: Standard interface from `System.IO.Abstractions`; injected into commands for all file operations.
- **FileSystem**: Default implementation from library; used for client-side and local execution with no restrictions.
- **SandboxedFileSystem**: Server-side implementation for remote command execution; wraps local file operations with path validation to confine all access to `StorageRootPath`. Located in `BitPantry.CommandLine.Remote.SignalR.Server/Files/`.
- **SandboxedFile**: Server-side implementation of `IFile`; validates paths and delegates to local file system.
- **SandboxedDirectory**: Server-side implementation of `IDirectory`; validates paths and delegates to local directory operations.
- **FileTransferOptions**: Server configuration class with `MaxFileSizeBytes`, `AllowedExtensions`, `StorageRootPath`.
- **FileTransferService**: Client-side service for explicit file uploads/downloads via HTTP. Uses `FileTransferEndpointService` on server.
- **FileTransferEndpointService**: Server-side HTTP endpoint handler with security validation and checksum verification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Commands using `IFileSystem` work identically locally and remotely without code changes.
- **SC-002**: All file uploads complete with verified SHA256 checksum match between client and server.
- **SC-003**: 100% of path traversal attack attempts are blocked and logged.
- **SC-004**: Files exceeding configured size limits are rejected before more than one chunk is written.
- **SC-005**: Cancelled or failed operations leave zero orphaned partial files on the server.
- **SC-006**: Access tokens do not appear in any server request logs.
- **SC-007**: Files larger than 2GB transfer successfully with accurate progress reporting.
- **SC-008**: Commands using `IFileSystem` can be unit tested using `MockFileSystem` from library.
- **SC-009**: File paths with spaces, unicode characters, and URL-reserved characters work correctly.
- **SC-010**: Directory enumeration with recursive search and glob patterns works on remote server.

## Assumptions

- `System.IO.Abstractions` NuGet package is acceptable as a new dependency.
- Custom `IFileService` and `LocalDiskFileService` can be deleted (replaced by library).
- The existing HTTP POST upload with SignalR progress architecture is retained (not migrating to SignalR streaming).
- SHA256 is an acceptable checksum algorithm (not MD5 or SHA1).
- Incremental hashing during streaming is preferred over pre-computing full hash before transfer.
- Server-side configuration options are set at application startup and don't change at runtime.
- Resume capability for interrupted uploads is not required for this feature.
- Existing `RpcMessageRegistry` pattern is used for SignalR RPC (no new RPC infrastructure).
- Existing `MessageBase` envelope pattern is used for new RPC messages.

## Clarifications

### Session 2024-12-22

- Q: How does the system handle concurrent uploads to the same destination path? → A: Last-write-wins (overwrite silently)
- Q: What happens when disk space runs out mid-upload? → A: Abort, delete partial file, return specific "insufficient disk space" error
- Q: What security events should be logged for file transfers? → A: Log security rejections only (path traversal, extension, size violations)
- Q: Should we use System.IO.Abstractions library? → A: Yes, adopt it as the standard abstraction for natural, best-practice file operations
- Q: Local vs remote access restrictions? → A: Local has no restrictions (any drive/folder); remote is confined to StorageRootPath
- Q: Keep existing IFileService? → A: No, delete it and LocalDiskFileService - replaced by library
- Q: What naming for remote file system? → A: SandboxedFileSystem (sandboxed is acceptable name)
- Q: Directory enumeration flexibility? → A: Full flexibility - recursive search with glob patterns
- Q: Default behavior when AllowedExtensions not configured? → A: Allow all extensions (null/unset); opt-in restriction model
- Q: Default value for MaxFileSizeBytes? → A: 100MB - balanced default for general use
- Q: Default value for StorageRootPath? → A: Required on server (no default); server fails to start if not configured. Local execution has no path restrictions.
