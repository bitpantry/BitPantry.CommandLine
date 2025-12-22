# Data Model: Unified File System Abstraction

**Date**: 2024-12-22  
**Feature**: 001-unified-file-system

## Entities

### IFileSystem (From System.IO.Abstractions)

**Purpose**: Unified abstraction for all file system operations; injected into commands.

**Sub-Interfaces**:
| Property | Type | Description |
|----------|------|-------------|
| `File` | `IFile` | File operations (read, write, exists, delete) |
| `Directory` | `IDirectory` | Directory operations (create, enumerate, delete) |
| `Path` | `IPath` | Path manipulation (combine, get extension, etc.) |
| `FileInfo` | `IFileInfoFactory` | Create IFileInfo instances |
| `DirectoryInfo` | `IDirectoryInfoFactory` | Create IDirectoryInfo instances |
| `FileStream` | `IFileStreamFactory` | Create file streams |
| `DriveInfo` | `IDriveInfoFactory` | Drive information |
| `FileSystemWatcher` | `IFileSystemWatcherFactory` | File system watchers |

**Implementations**:
- `FileSystem` (library default): Unrestricted local file system access
- `SandboxedFileSystem` (custom): Routes to remote server via HTTP/SignalR

---

### SandboxedFileSystem (Client-Side)

**Purpose**: `IFileSystem` implementation for remote execution; routes operations to server.

**Dependencies** (constructor-injected):
- `IServerProxy` - provides connection state, URI, and connection ID
- `RpcMessageRegistry` - correlates RPC requests/responses
- `IHttpClientFactory` - creates HTTP clients for streaming operations
- `AccessTokenManager` - provides current Bearer token
- `ILogger<SandboxedFileSystem>`

**Sub-Implementations**:
| Property | Implementation | Transport |
|----------|---------------|-----------|
| `File` | `SandboxedFile` | HTTP (streaming) + SignalR RPC (metadata) |
| `Directory` | `SandboxedDirectory` | SignalR RPC |
| `Path` | `SandboxedPath` | Local (client-side manipulation) |
| `FileInfo` | `SandboxedFileInfoFactory` | SignalR RPC |
| `DirectoryInfo` | `SandboxedDirectoryInfoFactory` | SignalR RPC |
| `FileStream` | `SandboxedFileStreamFactory` | HTTP (streaming) |

**Lifecycle**: Registered as `IFileSystem` when connected to remote; replaced with `FileSystem` on disconnect.

---

### SandboxedFile : IFile

**Purpose**: Remote file operations routed via HTTP (streaming) and SignalR RPC (metadata).

**Method Routing**:

| Method | Transport | Notes |
|--------|-----------|-------|
| `Exists(path)` | SignalR RPC | FileExistsRequest/Response |
| `ReadAllBytes(path)` | HTTP GET | Download with checksum verification |
| `ReadAllText(path)` | HTTP GET | Download with checksum verification |
| `WriteAllBytes(path, bytes)` | HTTP POST | Upload with checksum |
| `WriteAllText(path, text)` | HTTP POST | Upload with checksum |
| `Delete(path)` | SignalR RPC | DeleteFileRequest/Response |
| `Copy(src, dest)` | SignalR RPC | CopyFileRequest/Response |
| `Move(src, dest)` | SignalR RPC | MoveFileRequest/Response |
| `GetAttributes(path)` | SignalR RPC | GetFileInfoRequest/Response |
| `OpenRead(path)` | HTTP GET | Returns stream wrapping download |
| `OpenWrite(path)` | HTTP POST | Returns stream wrapping upload |

---

### SandboxedDirectory : IDirectory

**Purpose**: Remote directory operations routed via SignalR RPC.

**Method Routing**:

| Method | Transport | Notes |
|--------|-----------|-------|
| `Exists(path)` | SignalR RPC | DirectoryExistsRequest/Response |
| `CreateDirectory(path)` | SignalR RPC | CreateDirectoryRequest/Response |
| `Delete(path, recursive)` | SignalR RPC | DeleteDirectoryRequest/Response |
| `EnumerateFiles(path, pattern, option)` | SignalR RPC | Full glob pattern + SearchOption support |
| `EnumerateDirectories(path, pattern, option)` | SignalR RPC | Full glob pattern + SearchOption support |
| `GetFiles(path, pattern, option)` | SignalR RPC | Returns string[] |
| `GetDirectories(path, pattern, option)` | SignalR RPC | Returns string[] |

---

### FileTransferOptions (Server-Side Configuration)

**Purpose**: Configuration class for server-side file transfer security settings.

**Fields**:

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `MaxFileSizeBytes` | `long` | `104857600` (100MB) | Maximum allowed file size |
| `AllowedExtensions` | `HashSet<string>` | `null` (all allowed) | Allowed file extensions (e.g., `.txt`, `.pdf`). Null = no restriction |
| `StorageRootPath` | `string` | **(required)** | Base directory for all remote file operations. Server fails to start if not configured. |

**Validation Rules**:
- `StorageRootPath` is required; server throws `InvalidOperationException` at startup if null/empty
- `MaxFileSizeBytes` must be > 0
- `StorageRootPath` directory is created if missing
- Extensions in `AllowedExtensions` must start with `.`

---

### FileUploadProgress (Client-Side Progress)

**Purpose**: Progress information for upload operations.

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `TotalRead` | `long` | Total bytes uploaded so far |
| `Error` | `string?` | Error message if transfer failed |
| `CorrelationId` | `string` | Unique ID linking progress updates to transfer |

---

### FileDownloadProgress (Client-Side Progress)

**Purpose**: Progress information for download operations.

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `TotalRead` | `long` | Total bytes downloaded so far |
| `TotalSize` | `long` | Total file size (from Content-Length header) |
| `Error` | `string?` | Error message if transfer failed |

---

### FileUploadProgressMessage (SignalR Envelope)

**Purpose**: Server-to-client SignalR message for upload progress updates.

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `TotalRead` | `long` | Total bytes written on server (changed from `int`) |
| `CorrelationId` | `string` | Links message to client callback |

---

## RPC Message Envelopes

All envelopes extend `MessageBase` and follow existing patterns.

### File Operations

| Request | Response | Purpose |
|---------|----------|---------|
| `FileExistsRequest` | `FileExistsResponse` | Check if file exists |
| `GetFileInfoRequest` | `GetFileInfoResponse` | Get file metadata (size, dates, attributes) |
| `DeleteFileRequest` | `DeleteFileResponse` | Delete a file |
| `CopyFileRequest` | `CopyFileResponse` | Copy file on server |
| `MoveFileRequest` | `MoveFileResponse` | Move/rename file on server |

### Directory Operations

| Request | Response | Purpose |
|---------|----------|---------|
| `DirectoryExistsRequest` | `DirectoryExistsResponse` | Check if directory exists |
| `CreateDirectoryRequest` | `CreateDirectoryResponse` | Create directory (recursive) |
| `DeleteDirectoryRequest` | `DeleteDirectoryResponse` | Delete directory (with recursive flag) |
| `EnumerateFilesRequest` | `EnumerateFilesResponse` | List files with pattern and search option |
| `EnumerateDirectoriesRequest` | `EnumerateDirectoriesResponse` | List directories with pattern and search option |

### Common Fields

All request envelopes include:
- `CorrelationId` (string): For RPC response correlation
- `Path` (string): Relative path within storage root

All response envelopes include:
- `CorrelationId` (string): Matches request
- `Success` (bool): Operation succeeded
- `Error` (string?): Error message if failed

---

## Server-Side Components

### FileTransferEndpointService

**Purpose**: Handles HTTP endpoints for file upload and download with security validation.

**Dependencies** (constructor-injected):
- `ILogger<FileTransferEndpointService>`
- `IOptions<FileTransferOptions>`
- `IHubContext<CommandLineHub>`

**Endpoints**:
- `POST /cli/fileupload` - Upload with checksum verification
- `GET /cli/filedownload` - Download with checksum header

**Security Methods**:
- `ValidatePath(string relativePath)` - Returns absolute path or throws
- `ValidateExtension(string path)` - Throws if not in allowlist
- `ValidateSize(long contentLength)` - Throws if exceeds limit

---

### FileSystemRpcHandler

**Purpose**: Handles SignalR RPC requests for file system operations.

**Dependencies** (constructor-injected):
- `ILogger<FileSystemRpcHandler>`
- `IOptions<FileTransferOptions>`

**Methods**:
- `HandleFileExistsRequest(FileExistsRequest, IClientProxy)`
- `HandleDirectoryExistsRequest(DirectoryExistsRequest, IClientProxy)`
- `HandleGetFileInfoRequest(GetFileInfoRequest, IClientProxy)`
- `HandleEnumerateFilesRequest(EnumerateFilesRequest, IClientProxy)`
- `HandleEnumerateDirectoriesRequest(EnumerateDirectoriesRequest, IClientProxy)`
- `HandleCreateDirectoryRequest(CreateDirectoryRequest, IClientProxy)`
- `HandleDeleteFileRequest(DeleteFileRequest, IClientProxy)`
- `HandleDeleteDirectoryRequest(DeleteDirectoryRequest, IClientProxy)`

All methods use `ValidatePath()` before performing operations.

---

## State Transitions

### IFileSystem Registration Lifecycle

```
[App Startup]
    │
    ▼
┌─────────────────────────────────┐
│ IFileSystem → FileSystem        │ (local, unrestricted)
└─────────────────────────────────┘
    │
    │ ConnectCommand success
    ▼
┌─────────────────────────────────┐
│ IFileSystem → SandboxedFileSystem│ (remote, sandboxed)
└─────────────────────────────────┘
    │
    │ DisconnectCommand
    ▼
┌─────────────────────────────────┐
│ IFileSystem → FileSystem        │ (local, unrestricted)
└─────────────────────────────────┘
```

### File Upload Flow

```
[Client: fileSystem.File.WriteAllBytes("data.bin", bytes)]
    │
    ▼
[SandboxedFile.WriteAllBytes]
    │ Compute SHA256 checksum
    │ Create HTTP POST request
    │   - Authorization: Bearer <token>
    │   - X-File-Checksum: <hash>
    │   - Content-Type: application/octet-stream
    ▼
[HTTP POST /cli/fileupload]
    │
    ▼
[FileTransferEndpointService]
    │ ValidatePath() → throws if traversal
    │ ValidateExtension() → throws if not allowed
    │ ValidateSize() → throws if too large
    │ Stream to disk with incremental hash
    │ Compare checksums
    │   - Match: return 200 OK
    │   - Mismatch: delete file, return 400
    ▼
[Response to client]
```
