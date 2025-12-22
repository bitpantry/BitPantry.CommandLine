# File System API Contract

**Version**: 2.0.0  
**Feature**: 001-unified-file-system  
**Base Path**: `{hubUrl}` (configured via `CommandLineServerOptions.HubUrlPattern`)

---

## HTTP Endpoints

### POST /cli/fileupload

Upload a file to the server with integrity verification.

**Authentication**: Bearer token in `Authorization` header (required)

**Request Headers**:

| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes | `Bearer <access_token>` |
| `Content-Type` | Yes | `application/octet-stream` |
| `Content-Length` | Yes | File size in bytes |
| `X-File-Checksum` | Yes | SHA256 hash of file (lowercase hex) |

**Query Parameters**:

| Parameter | Required | Description |
|-----------|----------|-------------|
| `toFilePath` | Yes | URL-encoded destination path (relative to storage root) |
| `connectionId` | No | SignalR connection ID for progress updates |
| `correlationId` | No | Unique ID for correlating progress callbacks |

**Request Body**: Raw file bytes (`application/octet-stream`)

**Responses**:

| Status | Description | Body |
|--------|-------------|------|
| 200 OK | Upload successful, checksum verified | Empty |
| 400 Bad Request | Invalid path, extension, or checksum mismatch | `{ "error": "<message>" }` |
| 401 Unauthorized | Invalid or missing token | Empty |
| 403 Forbidden | Path traversal attempt | `{ "error": "Access denied" }` |
| 413 Payload Too Large | File exceeds configured limit | `{ "error": "File exceeds size limit" }` |
| 507 Insufficient Storage | Disk space exhausted | `{ "error": "Insufficient disk space" }` |

**Progress Updates** (via SignalR):

Server sends `FileUploadProgressMessage` to client's `ReceiveMessage` method:

```json
{
  "type": "FileUploadProgressMessage",
  "correlationId": "<correlationId>",
  "data": {
    "totalRead": "1048576"
  }
}
```

---

### GET /cli/filedownload

Download a file from the server with integrity verification.

**Authentication**: Bearer token in `Authorization` header (required)

**Query Parameters**:

| Parameter | Required | Description |
|-----------|----------|-------------|
| `filePath` | Yes | URL-encoded file path (relative to storage root) |
| `connectionId` | No | SignalR connection ID for progress updates |
| `correlationId` | No | Unique ID for correlating progress callbacks |

**Response Headers**:

| Header | Description |
|--------|-------------|
| `Content-Type` | `application/octet-stream` |
| `Content-Length` | File size in bytes |
| `X-File-Checksum` | SHA256 hash of file (lowercase hex) |

**Response Body**: Raw file bytes (streamed)

**Responses**:

| Status | Description |
|--------|-------------|
| 200 OK | File stream with headers |
| 400 Bad Request | Invalid path (traversal attempt) |
| 401 Unauthorized | Invalid or missing token |
| 403 Forbidden | Path traversal attempt |
| 404 Not Found | File does not exist |

---

## SignalR RPC Methods

All RPC requests are sent via `ReceiveRequest` method and responses via `ReceiveResponse`.

### FileExists

Check if a file exists.

**Request** (`FileExistsRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "FileExistsRequest",
    "path": "reports/data.csv"
  }
}
```

**Response** (`FileExistsResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "FileExistsResponse",
    "exists": "true",
    "error": null
  }
}
```

---

### DirectoryExists

Check if a directory exists.

**Request** (`DirectoryExistsRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "DirectoryExistsRequest",
    "path": "reports/2024"
  }
}
```

**Response** (`DirectoryExistsResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "DirectoryExistsResponse",
    "exists": "true",
    "error": null
  }
}
```

---

### GetFileInfo

Get file metadata (size, dates, attributes).

**Request** (`GetFileInfoRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "GetFileInfoRequest",
    "path": "reports/data.csv"
  }
}
```

**Response** (`GetFileInfoResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "GetFileInfoResponse",
    "exists": "true",
    "length": "1048576",
    "creationTimeUtc": "2024-12-22T10:30:00Z",
    "lastWriteTimeUtc": "2024-12-22T14:45:00Z",
    "lastAccessTimeUtc": "2024-12-22T15:00:00Z",
    "attributes": "Archive",
    "error": null
  }
}
```

---

### EnumerateFiles

List files matching a pattern.

**Request** (`EnumerateFilesRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "EnumerateFilesRequest",
    "path": "reports",
    "searchPattern": "*.csv",
    "searchOption": "AllDirectories"
  }
}
```

**Response** (`EnumerateFilesResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "EnumerateFilesResponse",
    "files": "[\"reports/data.csv\",\"reports/2024/q1.csv\",\"reports/2024/q2.csv\"]",
    "error": null
  }
}
```

---

### EnumerateDirectories

List directories matching a pattern.

**Request** (`EnumerateDirectoriesRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "EnumerateDirectoriesRequest",
    "path": "reports",
    "searchPattern": "*",
    "searchOption": "TopDirectoryOnly"
  }
}
```

**Response** (`EnumerateDirectoriesResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "EnumerateDirectoriesResponse",
    "directories": "[\"reports/2024\",\"reports/2023\"]",
    "error": null
  }
}
```

---

### CreateDirectory

Create a directory (recursive).

**Request** (`CreateDirectoryRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "CreateDirectoryRequest",
    "path": "reports/2024/q4"
  }
}
```

**Response** (`CreateDirectoryResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "CreateDirectoryResponse",
    "success": "true",
    "error": null
  }
}
```

---

### DeleteFile

Delete a file.

**Request** (`DeleteFileRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "DeleteFileRequest",
    "path": "temp/output.tmp"
  }
}
```

**Response** (`DeleteFileResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "DeleteFileResponse",
    "success": "true",
    "error": null
  }
}
```

---

### DeleteDirectory

Delete a directory.

**Request** (`DeleteDirectoryRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "DeleteDirectoryRequest",
    "path": "temp",
    "recursive": "true"
  }
}
```

**Response** (`DeleteDirectoryResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "DeleteDirectoryResponse",
    "success": "true",
    "error": null
  }
}
```

---

### CopyFile

Copy a file within the storage root.

**Request** (`CopyFileRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "CopyFileRequest",
    "sourcePath": "reports/data.csv",
    "destPath": "backup/data.csv",
    "overwrite": "true"
  }
}
```

**Response** (`CopyFileResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "CopyFileResponse",
    "success": "true",
    "error": null
  }
}
```

---

### MoveFile

Move/rename a file within the storage root.

**Request** (`MoveFileRequest`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "MoveFileRequest",
    "sourcePath": "temp/output.csv",
    "destPath": "reports/output.csv",
    "overwrite": "false"
  }
}
```

**Response** (`MoveFileResponse`):
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "MoveFileResponse",
    "success": "true",
    "error": null
  }
}
```

---

## Security Validation Order

For all operations, validations occur in this order:

1. **Authentication**: Verify Bearer token is valid
2. **Path Validation**: Ensure resolved path is within storage root
3. **Extension Validation** (write operations only): Check against allowlist
4. **Size Validation** (upload only): Check against maximum

---

## Error Response Format

All error responses include:
```json
{
  "error": "<human-readable message>"
}
```

For RPC responses, errors are in the `error` field of the response data:
```json
{
  "data": {
    "correlationId": "<guid>",
    "type": "<ResponseType>",
    "success": "false",
    "error": "Path traversal attempt detected"
  }
}
```

---

## SearchOption Values

For enumeration operations:
- `TopDirectoryOnly` (default): Only immediate children
- `AllDirectories`: Recursive search
