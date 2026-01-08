# Contracts: Upload Command

> Phase 1 output - API contracts and schemas

## Overview

The Upload Command introduces one new REST endpoint for batch file existence checking, and leverages existing file upload infrastructure.

### New Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/files/exists` | POST | Batch check if files exist on server |

### Existing Endpoints Used

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/{ServiceEndpointNames.FileUpload}` | POST | Existing file upload endpoint |

### Upload Endpoint Enhancement

The existing file upload endpoint gains a new query parameter:

```http
POST /{ServiceEndpointNames.FileUpload}?fileName=...&path=...&skipIfExists=true
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `skipIfExists` | `bool` | `false` | If true, server skips upload if file exists instead of overwriting |

**Server Behavior**:
- `skipIfExists=true` + file exists → Return `200 OK` with `{"status": "skipped", "reason": "File already exists"}`
- `skipIfExists=true` + file not exists → Upload file normally
- `skipIfExists=false` (default) → Overwrite if exists, create if not

**Purpose**: Mitigates TOCTOU race condition when used with `--skip-existing` flag. Server becomes final arbiter of skip semantics.

### Existing Services Used

| Service | Method | Description |
|---------|--------|-------------|
| `FileTransferService` | `UploadFile()` | HTTP-based file upload with progress |
| `FileTransferService` | `CheckFilesExist()` | NEW - Batch existence check |
| `SignalRServerProxy` | `IsConnected` | Connection state verification |

## New Endpoint: POST /files/exists

Batch check if files exist in a directory on the server.

### Request

```http
POST /files/exists HTTP/1.1
Content-Type: application/json
Authorization: Bearer {token}

{
  "directory": "/remote/path",
  "filenames": ["file1.txt", "file2.txt", "file3.txt"]
}
```

### Chunking

Client should chunk requests with more than **100 filenames** into multiple requests.
- Threshold: 100 files per batch (configurable constant `BATCH_EXISTS_CHUNK_SIZE`)
- Client merges responses from chunked requests
- Recommended for: performance, timeout prevention, server memory management

### Response (200 OK)

```json
{
  "exists": {
    "file1.txt": true,
    "file2.txt": false,
    "file3.txt": true
  }
}
```

### Response (401 Unauthorized)

Missing or invalid authorization token.

### Response (400 Bad Request)

Invalid request body (missing directory or filenames).

### Security

- Requires valid Bearer token (same as file upload)
- Directory path is sandboxed to storage root
- Path traversal attempts rejected

## Command Interface (CLI Contract)

```
server upload <source> <destination> [--skip-existing|-s]

Arguments:
  source       Local file path or glob pattern (supports **, *, ?)
  destination  Remote destination path

Options:
  -s, --skip-existing  Skip files that already exist on server

Examples:
  server upload myfile.txt /remote/path/
  server upload "*.txt" /remote/documents/
  server upload "**/*.log" /remote/logs/ --skip-existing
  server upload ./data.json /remote/data.json
```

## Return Codes

| Code | Meaning |
|------|---------|
| 0 | All files uploaded successfully (or no files matched glob, or all skipped) |
| 1 | One or more files failed to upload or not found |
