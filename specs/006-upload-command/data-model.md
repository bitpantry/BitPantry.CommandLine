# Data Model: Upload Command

> Phase 1 output - entities, fields, relationships, validation rules

## Overview

The Upload Command feature operates on existing data structures. No new persistent entities are created. This document describes the runtime data flow and transient state.

## Entities

### UploadOperation (Transient)

Runtime state for a single file upload within a multi-file operation.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `FilePath` | `string` | Local absolute path to source file | Must exist on local filesystem |
| `DestinationPath` | `string` | Remote destination path | Required, non-empty |
| `FileSize` | `long` | Size in bytes | Read from FileInfo |
| `Status` | `UploadStatus` | Current status | Enum: Pending, InProgress, Completed, Failed, NotFound |
| `Progress` | `double` | Upload progress 0-100 | Updated from FileUploadProgress callback |
| `ErrorMessage` | `string?` | Error details if failed | Null until failure |

### UploadStatus (Enum)

```csharp
public enum UploadStatus
{
    Pending,    // Not yet started
    InProgress, // Currently uploading
    Completed,  // Successfully uploaded
    Failed,     // Upload error occurred
    NotFound,   // Local file not found
    Skipped     // Skipped (file exists on server, --skip-existing flag)
}
```

### UploadResult (Transient)

Summary result of the upload operation.

| Field | Type | Description |
|-------|------|-------------|
| `SuccessCount` | `int` | Number of files successfully uploaded |
| `FailureCount` | `int` | Number of files that failed to upload |
| `NotFoundCount` | `int` | Number of files not found locally |
| `SkippedCount` | `int` | Number of files skipped (already exist on server) |
| `FailedFiles` | `List<(string Path, string Error)>` | Details of failed uploads |
| `NotFoundFiles` | `List<string>` | Paths of files not found |
| `SkippedFiles` | `List<string>` | Paths of files skipped |

## New Entities

### FilesExistRequest (API Request)

Request payload for batch file existence check.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `Directory` | `string` | Remote directory path to check | Required, valid path |
| `Filenames` | `string[]` | List of filenames to check | Required, non-empty |

```csharp
public record FilesExistRequest(string Directory, string[] Filenames);
```

### FilesExistResponse (API Response)

Response payload indicating which files exist.

| Field | Type | Description |
|-------|------|-------------|
| `Exists` | `Dictionary<string, bool>` | Map of filename to existence status |

```csharp
public record FilesExistResponse(Dictionary<string, bool> Exists);
```

### FileUploadResponse (API Response - Enhanced)

Response from file upload endpoint indicating outcome.

| Field | Type | Description |
|-------|------|-------------|
| `Status` | `string` | Outcome: "uploaded", "skipped", "error" |
| `Reason` | `string?` | Explanation when status is "skipped" or "error" |
| `BytesWritten` | `long?` | Bytes written (for "uploaded" status) |

```csharp
public record FileUploadResponse(string Status, string? Reason = null, long? BytesWritten = null);
```

## Constants

### Batch Processing Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `BATCH_EXISTS_CHUNK_SIZE` | `100` | Max files per batch existence check request |
| `MAX_CONCURRENT_UPLOADS` | `4` | Max parallel upload operations |

```csharp
public static class UploadConstants
{
    public const int BatchExistsChunkSize = 100;
    public const int MaxConcurrentUploads = 4;
}
```

## Existing Entities (Referenced)

### FileUploadProgress (Existing)

From `BitPantry.CommandLine.Remote.SignalR.Client`:

```csharp
public record FileUploadProgress(long TotalRead, string Error = null);
```

- Used as callback parameter from `FileTransferService.UploadFile()`
- `TotalRead`: Cumulative bytes uploaded
- `Error`: Error message if upload failed

## State Transitions

```
┌─────────┐     Start     ┌────────────┐
│ Pending │──────────────▶│ InProgress │
└─────────┘               └─────┬──────┘
                                │
                     ┌──────────┼──────────┐
                     │          │          │
                     ▼          ▼          ▼
              ┌──────────┐ ┌────────┐ ┌─────────┐
              │ Completed│ │ Failed │ │NotFound │
              └──────────┘ └────────┘ └─────────┘
```

**Transitions**:
- `Pending → InProgress`: Upload starts, semaphore acquired
- `InProgress → Completed`: FileTransferService completes successfully
- `InProgress → Failed`: FileTransferService throws exception
- `InProgress → Skipped`: Server returns "skipped" (TOCTOU - file appeared after check)
- `Pending → NotFound`: File existence check fails before upload starts
- `Pending → Skipped`: Server file exists and `--skip-existing` flag set (pre-check)

## Data Flow

### Single File Upload Flow

```
User Input          ExpandSource()           UploadSingleFileAsync()        FileTransferService
    │                    │                           │                            │
    │  source path       │                           │                            │
    ├───────────────────▶│                           │                            │
    │                    │  Check exists             │                            │
    │                    │──────────────────────────▶│                            │
    │                    │  (existing[], missing[])  │                            │
    │                    │◀──────────────────────────│                            │
    │                    │                           │     UploadFile()           │
    │                    │                           │───────────────────────────▶│
    │                    │                           │     Progress callback      │
    │                    │                           │◀───────────────────────────│
    │                    │                           │     (update progress bar)  │
    │                    │                           │                            │
```

### Multi-File Upload Flow with Skip Existing

```
User Input    ExpandGlob()    CheckFilesExist()    Parallel Upload    FileTransferService
    │              │                 │                   │                   │
    │  *.txt -s    │                 │                   │                   │
    ├─────────────▶│                 │                   │                   │
    │              │  Matcher.Execute()                  │                   │
    │              │─────────────────│                   │                   │
    │              │  [a.txt, b.txt, c.txt]              │                   │
    │              │◀────────────────│                   │                   │
    │              │                 │                   │                   │
    │              │  POST /files/exists                 │                   │
    │              │────────────────▶│                   │                   │
    │              │  {b.txt: true, a.txt: false, ...}   │                   │
    │              │◀────────────────│                   │                   │
    │              │                 │                   │                   │
    │              │  Skip b.txt, upload a.txt, c.txt    │                   │
    │              │─────────────────────────────────────▶│                  │
    │              │                 │                   │  UploadFile()    │
    │              │                 │                   │─────────────────▶│
```

### Multi-File Upload Flow

```
User Input    ExpandGlob()    Parallel.ForEachAsync()    FileTransferService
    │              │                  │                         │
    │  *.txt       │                  │                         │
    ├─────────────▶│                  │                         │
    │              │  Matcher.Execute()                         │
    │              │─────────────────▶│                         │
    │              │  file1.txt       │                         │
    │              │  file2.txt       │                         │
    │              │  file3.txt       │                         │
    │              │◀─────────────────│                         │
    │              │                  │   [Concurrent, max 4]   │
    │              │                  │   UploadFile(file1)     │
    │              │                  │────────────────────────▶│
    │              │                  │   UploadFile(file2)     │
    │              │                  │────────────────────────▶│
    │              │                  │   Progress callbacks... │
    │              │                  │◀────────────────────────│
```

## Validation Rules

| Rule | Location | Behavior |
|------|----------|----------|
| Source required | UploadCommand | Enforced by `[Argument(IsRequired = true)]` |
| Destination required | UploadCommand | Enforced by `[Argument(IsRequired = true)]` |
| Connection required | UploadCommand.Execute | Check `_proxy.IsConnected`, return error if false |
| File exists (single) | ExpandSource | Return in `missing[]` array if not found |
| File exists (multi) | ExpandSource | Separate into `existing[]` and `missing[]` |

## Integration Points

| Component | Integration |
|-----------|-------------|
| `SignalRServerProxy` | `IsConnected` property for connection state check |
| `FileTransferService` | `UploadFile()` method with progress callback |
| `IAnsiConsole` | Spectre.Console for progress bar rendering |
| `ServerGroup` | Command registration via `[Command(Group = typeof(ServerGroup))]` |
