# Download API Contract

**Version**: 1.0.0  
**Feature**: 007-download-command  
**Base Path**: `{hubUrl}` (configured via `CommandLineServerOptions.HubUrlPattern`)

---

## HTTP Endpoints

### GET /cli/filedownload (Enhanced)

Download a file from the server with integrity verification and progress reporting.

**Authentication**: Bearer token in `Authorization` header (required)

**Query Parameters**:

| Parameter | Required | Description |
|-----------|----------|-------------|
| `filePath` | Yes | URL-encoded file path (relative to storage root) |
| `connectionId` | No | SignalR connection ID for progress updates |
| `correlationId` | No | Unique ID for correlating progress callbacks |

**Request Headers**:

| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes | `Bearer <access_token>` |

**Response Headers**:

| Header | Description |
|--------|-------------|
| `Content-Type` | `application/octet-stream` |
| `Content-Length` | File size in bytes |
| `X-File-Checksum` | SHA256 hash of file (uppercase hex) |

**Response Body**: Raw file bytes (streamed in chunks)

**Responses**:

| Status | Description | Body |
|--------|-------------|------|
| 200 OK | File stream with headers | Binary stream |
| 400 Bad Request | Invalid path (traversal attempt) | `{ "error": "<message>" }` |
| 401 Unauthorized | Invalid or missing token | Empty |
| 403 Forbidden | Path traversal attempt | `{ "error": "Access denied" }` |
| 404 Not Found | File does not exist | `{ "error": "File not found" }` |

**Progress Updates** (via SignalR, when connectionId provided):

Server sends `FileDownloadProgressMessage` to client's `ReceiveMessage` method during streaming:

```json
{
  "type": "FileDownloadProgressMessage",
  "correlationId": "<correlationId>",
  "data": {
    "totalRead": "524288",
    "totalSize": "1048576"
  }
}
```

**Streaming Behavior**:
- File is streamed in 80KB chunks
- Progress messages sent approximately every 100ms during transfer
- Checksum computed incrementally during streaming

---

## SignalR RPC Methods

All RPC requests are sent via `ReceiveRequest` method and responses via `ReceiveResponse`.

### EnumerateFiles (Enhanced - Replaces Existing)

List files matching a glob pattern with metadata (size, last modified). This **replaces** the existing `EnumerateFilesRequest/Response` that returned paths only.

**Request** (`EnumerateFilesRequest`):

```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "EnumerateFilesRequest",
    "path": "logs",
    "searchPattern": "**/*.log",
    "searchOption": "AllDirectories"
  }
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `correlationId` | string | Yes | Unique request identifier |
| `type` | string | Yes | Must be `"EnumerateFilesRequest"` |
| `path` | string | Yes | Base directory to search (relative to storage root) |
| `searchPattern` | string | Yes | Glob pattern (`*`, `**`, `?` supported) |
| `searchOption` | string | Yes | `"TopDirectoryOnly"` or `"AllDirectories"` |

**Response** (`EnumerateFilesResponse`):

```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "EnumerateFilesResponse",
    "files": "[{\"Path\":\"logs/app.log\",\"Size\":1048576,\"LastModified\":\"2026-01-10T10:00:00Z\"},{\"Path\":\"logs/2024/jan.log\",\"Size\":524288,\"LastModified\":\"2026-01-09T15:30:00Z\"}]",
    "error": null
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `correlationId` | string | Matching request identifier |
| `type` | string | Always `"EnumerateFilesResponse"` |
| `files` | string | JSON array of `FileInfoEntry` objects (serialized) |
| `error` | string? | Error message if operation failed |

**FileInfoEntry Structure**:

```json
{
  "Path": "logs/app.log",
  "Size": 1048576,
  "LastModified": "2026-01-10T10:00:00Z"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `Path` | string | Relative path from search root |
| `Size` | long | File size in bytes |
| `LastModified` | DateTime | Last write time (UTC, ISO 8601) |

**Error Responses**:

| Error | Cause |
|-------|-------|
| `"Path traversal is not allowed"` | Path contains `..` or attempts to escape storage root |
| `"Directory not found: {path}"` | Base directory does not exist |
| `"Invalid search pattern"` | Malformed glob pattern |

---

## Progress Messages

### FileDownloadProgressMessage (New)

Server-to-client message sent during file download to report progress.

**Envelope**:

```json
{
  "type": "FileDownloadProgressMessage",
  "correlationId": "<correlationId>",
  "data": {
    "totalRead": "524288",
    "totalSize": "1048576"
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `type` | string | Always `"FileDownloadProgressMessage"` |
| `correlationId` | string | Links progress to specific download operation |
| `data.totalRead` | string (long) | Bytes transferred so far |
| `data.totalSize` | string (long) | Total file size |

**Timing**:
- Messages sent approximately every 100ms during active transfer
- At least one message sent at 0% and one at 100%
- Client should handle out-of-order messages gracefully

---

## Client Service API

### FileTransferService.DownloadFile (Enhanced)

Downloads a file from the remote server with optional progress callback.

**Signature**:

```csharp
Task<DownloadResult> DownloadFile(
    string remoteFilePath,
    string localFilePath,
    Func<FileDownloadProgress, Task>? progressCallback = null,
    CancellationToken token = default
);
```

**Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `remoteFilePath` | string | Path on server (relative to storage root) |
| `localFilePath` | string | Full local path where file will be saved |
| `progressCallback` | Func? | Optional async callback for progress updates |
| `token` | CancellationToken | Cancellation support |

**Returns**: `DownloadResult` with status, bytes transferred, and any error.

**Throws**:

| Exception | Cause |
|-----------|-------|
| `InvalidOperationException` | Client is not connected |
| `FileNotFoundException` | Remote file does not exist |
| `UnauthorizedAccessException` | Cannot write to local path |
| `InvalidDataException` | Checksum verification failed |
| `HttpRequestException` | Network or server error |

---

### FileTransferService.EnumerateFiles (Enhanced)

Lists remote files matching a pattern with metadata. This **replaces** the existing method that returned paths only.

**Signature**:

```csharp
Task<IReadOnlyList<FileInfoEntry>> EnumerateFiles(
    string path,
    string searchPattern,
    bool recursive = false,
    CancellationToken token = default
);
```

**Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `path` | string | Base directory on server |
| `searchPattern` | string | Glob pattern to match |
| `recursive` | bool | True for `AllDirectories`, false for `TopDirectoryOnly` |
| `token` | CancellationToken | Cancellation support |

**Returns**: List of `FileInfoEntry` with path, size, and last modified time.

---

## Security Considerations

1. **Path Validation**: Server validates all paths to prevent traversal attacks
2. **Token in Header**: Access token passed in `Authorization` header, never in URL
3. **Checksum Verification**: SHA256 checksum in response header for integrity
4. **Size Limits**: Server enforces `MaxFileSizeBytes` configuration
5. **Connection Required**: All operations require valid SignalR connection
