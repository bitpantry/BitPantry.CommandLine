# Data Model: Download Command

**Date**: 2026-01-10  
**Feature**: 007-download-command

## Entities

### FileInfoEntry (New)

**Purpose**: Represents a remote file with metadata needed for progress calculation.

**Cross-Platform Note**: The `Path` field uses forward slashes (`/`) as the canonical separator regardless of server platform. The client normalizes to local path separators when computing `LocalDestination`. This ensures consistent behavior when server and client run on different operating systems.

**Fields**:

| Field | Type | Description | Constraints |
|-------|------|-------------|-------------|
| `Path` | `string` | Relative path from search root | Required, non-empty |
| `Size` | `long` | File size in bytes | ≥ 0 |
| `LastModified` | `DateTime` | Last write time (UTC) | Valid DateTime |

**Usage**: Returned by `EnumerateFilesResponse` (enhanced) to provide file sizes for aggregate progress calculation.

---

### FileDownloadProgress (New)

**Purpose**: Progress information for download operations (client-side).

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `TotalRead` | `long` | Total bytes downloaded so far |
| `TotalSize` | `long` | Total file size (from server) |
| `Error` | `string?` | Error message if transfer failed |
| `CorrelationId` | `string` | Unique ID linking progress updates to transfer |

**Derived Properties**:
- `PercentComplete` = `TotalRead / TotalSize * 100` (when TotalSize > 0)

---

### DownloadResult (New)

**Purpose**: Outcome of a single file download operation.

> **Note**: This mirrors `UploadResult` from the upload command for consistent multi-file operation summary patterns. Same fields where applicable, adjusted for download-specific context (no SkippedCount since download has no skip-existing feature).

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `RemotePath` | `string` | Source path on server |
| `LocalPath` | `string` | Destination path on client |
| `Status` | `DownloadStatus` | Success, Failed, or Skipped |
| `BytesTransferred` | `long` | Bytes actually transferred |
| `Error` | `string?` | Error message if failed |

**DownloadStatus Enum**:
- `Success` - File downloaded and verified
- `Failed` - Download failed (see Error field)

---

### RemoteFileMatch (Internal)

**Purpose**: Represents a file matched by a glob pattern, with collision detection info.

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `RemotePath` | `string` | Full relative path on server |
| `FileName` | `string` | Filename only (for collision detection) |
| `Size` | `long` | File size in bytes |
| `LocalDestination` | `string` | Computed local destination path |

---

## SignalR RPC Messages

### EnumerateFilesRequest (Enhanced - Replaces Existing)

**Purpose**: Request file listing with metadata for a pattern. This **replaces** the existing `EnumerateFilesRequest` that returned paths only.

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `CorrelationId` | `string` | Unique request ID |
| `Type` | `string` | Always `"EnumerateFilesRequest"` |
| `Path` | `string` | Base directory to search |
| `SearchPattern` | `string` | Glob pattern (supports `*`, `**`, `?`) |
| `SearchOption` | `string` | `"TopDirectoryOnly"` or `"AllDirectories"` |

**Example**:
```json
{
  "data": {
    "correlationId": "abc-123",
    "type": "EnumerateFilesRequest",
    "path": "logs",
    "searchPattern": "**/*.log",
    "searchOption": "AllDirectories"
  }
}
```

---

### EnumerateFilesResponse (Enhanced - Replaces Existing)

**Purpose**: Response with file listing including size information. This **replaces** the existing `EnumerateFilesResponse` that returned `string[]` only.

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `CorrelationId` | `string` | Matching request ID |
| `Type` | `string` | Always `"EnumerateFilesResponse"` |
| `Files` | `FileInfoEntry[]?` | Array of file info entries (enhanced from string[]) |
| `Error` | `string?` | Error message if operation failed |

**Example**:
```json
{
  "data": {
    "correlationId": "abc-123",
    "type": "EnumerateFilesResponse",
    "files": "[{\"Path\":\"logs/app.log\",\"Size\":1048576,\"LastModified\":\"2026-01-10T10:00:00Z\"},{\"Path\":\"logs/2024/jan.log\",\"Size\":524288,\"LastModified\":\"2026-01-09T15:30:00Z\"}]",
    "error": null
  }
}
```

---

### FileDownloadProgressMessage (New)

**Purpose**: Server-to-client SignalR message for download progress updates.

**Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `CorrelationId` | `string` | Unique ID linking to download operation |
| `TotalRead` | `long` | Bytes transferred so far |
| `TotalSize` | `long` | Total file size |

**Example**:
```json
{
  "type": "FileDownloadProgressMessage",
  "correlationId": "download-xyz",
  "data": {
    "totalRead": "524288",
    "totalSize": "1048576"
  }
}
```

---

## Message Registry

| Request Type | Response Type | Purpose |
|--------------|---------------|--------|
| `EnumerateFilesRequest` | `EnumerateFilesResponse` | List files with size info for pattern (enhanced) |

| Progress Message Type | Direction | Purpose |
|----------------------|-----------|---------|
| `FileDownloadProgressMessage` | Server → Client | Download progress updates |

---

## State Transitions

### Download Command State Machine

```
[Start] 
    │
    ▼
[Check Connection] ──(not connected)──► [Error: Not connected]
    │
    │ (connected)
    ▼
[Expand Source Pattern]
    │
    ├──(no matches)──► [Warning: No files matched]
    │
    ├──(literal path not found)──► [Error: File not found]
    │
    │ (matches found)
    ▼
[Check Filename Collisions]
    │
    ├──(collisions detected)──► [Error: List collisions] ──► [Exit: No downloads]
    │
    │ (no collisions)
    ▼
[Calculate Total Size] ──► [Determine: Show Progress?]
    │
    ▼
[Download Files] ◄────────────────────────────────┐
    │                                              │
    ├──(file success)──► [Update Progress] ───────┤
    │                                              │
    ├──(file failure)──► [Record Failure] ────────┤
    │                                              │
    │ (all files processed)                        │
    ▼                                              │
[Display Summary] ◄─────(more files)──────────────┘
    │
    ▼
[End]
```

### Single File Download State Machine

```
[Start Download]
    │
    ▼
[Create Local Directory] ──(permission denied)──► [Fail: Permission error]
    │
    │ (success)
    ▼
[Begin HTTP GET]
    │
    ├──(404)──► [Fail: File not found]
    │
    ├──(403)──► [Fail: Access denied]
    │
    │ (200 OK)
    ▼
[Stream Chunks] ◄─────────────────────────┐
    │                                      │
    ├──(chunk received)──► [Write to file] │
    │        │                             │
    │        ▼                             │
    │  [Update Progress] ─────────────────►│
    │                                      │
    ├──(connection lost)──► [Cleanup partial] ──► [Fail: Connection lost]
    │
    │ (stream complete)
    ▼
[Verify Checksum]
    │
    ├──(mismatch)──► [Cleanup file] ──► [Fail: Checksum error]
    │
    │ (match)
    ▼
[Success]
```

---

## Validation Rules

### Source Path Validation

| Rule | Validation | Error |
|------|------------|-------|
| Not empty | `!string.IsNullOrWhiteSpace(source)` | "Source path is required" |
| Valid pattern | No invalid path characters | "Invalid source pattern" |

### Destination Path Validation

| Rule | Validation | Error |
|------|------------|-------|
| Not empty | `!string.IsNullOrWhiteSpace(destination)` | "Destination path is required" |
| Valid path | No invalid path characters | "Invalid destination path" |
| Writable | Parent directory writable | "Permission denied" |

### Collision Detection

| Rule | Validation | Error |
|------|------------|-------|
| Unique filenames | No duplicate `Path.GetFileName()` values | "Filename collision detected" |

---

## Constants

> **Note**: All thresholds mirror `UploadConstants` for consistency between upload and download commands. Documentation references constant **names** (not values) so specs remain accurate if values change.

| Constant | Initial Value | Purpose |
|----------|---------------|--------|
| `ProgressDisplayThreshold` | `25 * 1024 * 1024` | Minimum total size to show progress bar (mirrors `UploadConstants`) |
| `MaxConcurrentDownloads` | `4` | Maximum parallel file transfers (mirrors `UploadConstants.MaxConcurrentUploads`) |
| `ChunkSize` | `81920` (80KB) | Streaming buffer size |
| `ProgressThrottleMs` | `100` | Minimum ms between progress updates |
