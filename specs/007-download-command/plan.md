# Implementation Plan: Download Command

**Branch**: `007-download-command` | **Date**: 2026-01-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-download-command/spec.md`

## Summary

Implement a download command (`server download`) that enables users to transfer files from a connected remote server to their local machine. The command mirrors the upload command's user experience with glob pattern support, progress display, concurrent transfers, and user-friendly error handling. Key infrastructure gaps must be addressed: remote file enumeration with size information for progress tracking, and streaming download with progress callbacks.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: System.IO.Abstractions, Spectre.Console, Microsoft.Extensions.FileSystemGlobbing, SignalR  
**Storage**: Local file system (destination), Remote sandboxed file system (source)  
**Testing**: MSTest, FluentAssertions, Moq, System.IO.Abstractions.TestingHelpers  
**Target Platform**: Windows/Linux/macOS (cross-platform CLI)  
**Project Type**: Multi-project solution (client library + tests)  
**Performance Goals**: Progress updates ≥1/second, concurrent downloads for batch operations  
**Constraints**: Must work within existing SignalR connection, consistent UX with upload command  
**Scale/Scope**: Support batch downloads up to 1000+ files with aggregate progress

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| TDD Required | ✅ PASS | Tests written before implementation |
| Dependency Injection | ✅ PASS | All services constructor-injected (IFileSystem, IServerProxy, etc.) |
| Security by Design | ✅ PASS | Path validation on server, no credentials in URLs |
| Follow Existing Patterns | ✅ PASS | Mirrors UploadCommand structure, uses existing FileTransferService |
| Integration Testing | ✅ PASS | Cross-project file transfer requires integration tests |

## Infrastructure Gap Analysis

### Current State

The following infrastructure **EXISTS**:

| Component | Location | Capability |
|-----------|----------|------------|
| `FileTransferService.DownloadFile()` | Client | Downloads single file, no progress callback |
| `GET /cli/filedownload` endpoint | Server | Streams file with checksum, no progress events |
| `EnumerateFilesRequest/Response` | SignalR RPC | Lists files matching pattern (paths only - **TO BE REPLACED**) |
| `GetFileInfoRequest/Response` | SignalR RPC | Gets single file metadata including size |
| Upload progress infrastructure | Client/Server | `FileUploadProgressMessage`, progress registry |

### Gaps Requiring Implementation

| Gap ID | Component | Current State | Required State | Impact |
|--------|-----------|---------------|----------------|--------|
| **GAP-001** | Remote file enumeration with sizes | `EnumerateFilesResponse` returns `string[]` (paths only) | Replace with version returning `FileInfoEntry[]` (path, size, date) | Cannot show accurate aggregate progress without knowing total bytes |
| **GAP-002** | Download progress callback | `DownloadFile()` has no progress parameter | Need `Func<FileDownloadProgress, Task>` callback like upload | Cannot display per-file or aggregate progress |
| **GAP-003** | Streaming download with progress | Server loads entire file into memory | Need chunked streaming with progress events | Memory pressure for large files, no progress granularity |
| **GAP-004** | Remote glob pattern expansion | Client must enumerate, then filter | Server-side glob expansion for efficiency | N+1 RPC calls for pattern matching |
| **GAP-005** | Batch file info retrieval | `GetFileInfoRequest` is single-file only | Addressed by GAP-001 solution | N RPC calls to get sizes for N files |

### Gap Resolution Design

#### GAP-001 & GAP-005: Replace EnumerateFiles with Enhanced Version

**Replace Existing RPC**: `EnumerateFilesRequest/Response` (same names, enhanced content)

**No backward compatibility** - the existing `EnumerateFilesResponse` returning `string[]` is removed and replaced with the enhanced version.

```
EnumerateFilesRequest (enhanced):
  - path: string (base directory)
  - searchPattern: string (glob pattern)
  - searchOption: string ("TopDirectoryOnly" | "AllDirectories")

EnumerateFilesResponse (enhanced - replaces old):
  - files: FileInfoEntry[] (path, size, lastModified)
  - error: string?
```

**Implementation**: Server-side, combines enumeration with FileInfo gathering in single RPC call.

**Actions**:
- **REMOVE**: Existing `EnumerateFilesRequest.cs` and `EnumerateFilesResponse.cs` 
- **CREATE**: New versions with same names but enhanced response content
- **UPDATE**: All existing usages (if any) to work with new response structure

#### GAP-002 & GAP-003: Streaming Download with Progress

**Enhanced HTTP Endpoint**: Modify `GET /cli/filedownload` behavior:
- Add optional `connectionId` and `correlationId` query parameters (already in contract)
- Server sends `FileDownloadProgressMessage` via SignalR during streaming
- Client registers progress callback in registry (mirror upload pattern)

**New Message**: `FileDownloadProgressMessage` (mirrors `FileUploadProgressMessage`)

```
FileDownloadProgressMessage:
  - correlationId: string
  - totalRead: long
  - totalSize: long
```

**Client Enhancement**: `FileTransferService.DownloadFile()` signature change:

```csharp
Task DownloadFile(
    string remoteFilePath, 
    string localFilePath, 
    Func<FileDownloadProgress, Task>? progressCallback = null,
    CancellationToken token = default)
```

#### GAP-004: Server-Side Glob Expansion

Already addressed by GAP-001 - the enhanced `EnumerateFilesRequest` accepts glob patterns and handles expansion server-side.

## Project Structure

### Documentation (this feature)

```text
specs/007-download-command/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── download-api.md  # New/modified endpoints
└── test-cases.md        # Phase 2 output
```

### Source Code (repository root)

```text
BitPantry.CommandLine.Remote.SignalR/
├── Envelopes/
│   ├── FileDownloadProgressMessage.cs        # NEW (GAP-002)
│   ├── EnumerateFilesRequest.cs              # REPLACE (GAP-001) - enhanced version
│   ├── EnumerateFilesResponse.cs             # REPLACE (GAP-001) - enhanced version
│   └── FileInfoEntry.cs                      # NEW (GAP-001)
└── ServiceEndpointNames.cs                   # (no change needed)

BitPantry.CommandLine.Remote.SignalR.Client/
├── Commands/Server/
│   ├── DownloadCommand.cs                    # NEW
│   └── DownloadConstants.cs                  # NEW (mirrors UploadConstants)
├── FileTransferService.cs                    # MODIFY (GAP-002, GAP-003)
├── FileDownloadProgress.cs                   # NEW (GAP-002)
└── FileDownloadProgressUpdateFunctionRegistry.cs  # NEW (GAP-002)

BitPantry.CommandLine.Remote.SignalR.Server/
├── Files/
│   └── FileTransferEndpointService.cs        # MODIFY (GAP-003)
└── Rpc/
    └── FileSystemRpcHandler.cs               # MODIFY (GAP-001) - update handler

BitPantry.CommandLine.Tests.Remote.SignalR/
├── ClientTests/
│   └── DownloadCommandTests.cs               # NEW
└── IntegrationTests/
    └── IntegrationTests_DownloadCommand.cs   # NEW
```

**Structure Decision**: Follows existing multi-project solution layout. New files mirror upload command patterns.

## User Experience Flows

> **Note**: UX patterns mirror `UploadCommand` exactly. Progress bar uses `AutoClear(true)` so it is removed before any final message. All threshold values reference `DownloadConstants` (which mirrors `UploadConstants` for consistency).

### Scenario 1: Single Small File Download (below ProgressDisplayThreshold)

**Command**: `server download config.json ./`

**Starting State** (console before command):
```
server>
```

**During** (no progress bar - below `DownloadConstants.ProgressDisplayThreshold`):
```
server> server download config.json ./
```

**Ending** (final console output):
```
server> server download config.json ./
Downloaded config.json to ./config.json
server>
```

**Test Cases**:
- UX-001: Single file download completes with success message
- UX-002: Destination filename derived from source when path ends with `/`
- UX-003: No progress bar shown for files below `DownloadConstants.ProgressDisplayThreshold`

---

### Scenario 2: Single Large File Download (at or above ProgressDisplayThreshold)

**Command**: `server download dataset.csv ./data/`

**Starting State** (console before command):
```
server>
```

**During** (progress bar visible, updates ~1/second):
```
server> server download dataset.csv ./data/
Downloading dataset.csv ━━━━━━━━━━━━━━━━━━━━━━╸                    55% 11.2 MB/s ⠙
```

**Ending** (progress bar auto-cleared, then final message):
```
server> server download dataset.csv ./data/
Downloaded dataset.csv to ./data/dataset.csv
server>
```

**Test Cases**:
- UX-004: Progress bar appears for files >= `DownloadConstants.ProgressDisplayThreshold`
- UX-005: Progress bar shows percentage and transfer speed
- UX-006: Progress bar updates at least once per second
- UX-007: Progress bar auto-clears before final success message (no leftover progress text)

---

### Scenario 3: Multiple Small Files via Glob Pattern (below ProgressDisplayThreshold total)

**Command**: `server download "*.txt" ./backup/`

**Starting State** (console before command):
```
server>
```

**During** (no progress bar - total below threshold):
```
server> server download "*.txt" ./backup/
```

**Ending** (final console output):
```
server> server download "*.txt" ./backup/
Downloaded 3 files to ./backup/
server>
```

**Test Cases**:
- UX-008: Glob pattern matches all expected files
- UX-009: Summary shows correct file count
- UX-010: No progress bar when aggregate size < `DownloadConstants.ProgressDisplayThreshold`

---

### Scenario 4: Multiple Large Files via Glob Pattern (at or above ProgressDisplayThreshold total)

**Command**: `server download "logs/*.log" ./archive/`

**Starting State** (console before command):
```
server>
```

**During** (aggregate progress bar, concurrent downloads up to `MaxConcurrentDownloads`):
```
server> server download "logs/*.log" ./archive/
Downloading to ./archive/ ━━━━━━━━━━━━━━━╸                          35% 14.8 MB/s ⠙
```

**Ending** (progress bar auto-cleared, then final message):
```
server> server download "logs/*.log" ./archive/
Downloaded 3 files to ./archive/
server>
```

**Test Cases**:
- UX-011: Aggregate progress bar for multi-file download
- UX-012: Up to `DownloadConstants.MaxConcurrentDownloads` concurrent downloads
- UX-013: Summary shows total file count on completion
- UX-014: Progress bar auto-clears before final message

---

### Scenario 5: Recursive Glob with Flattening

**Command**: `server download "logs/**/*.log" ./flat-logs/`

**Starting State** (console before command):
```
server>
```

**During** (aggregate progress bar):
```
server> server download "logs/**/*.log" ./flat-logs/
Downloading to ./flat-logs/ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 75% 10.1 MB/s ⠙
```

**Ending** (progress bar auto-cleared, then final message):
```
server> server download "logs/**/*.log" ./flat-logs/
Downloaded 3 files to ./flat-logs/
server>
```

**Files Created**: `app.log`, `january.log`, `february.log` (all flattened, no subdirectories)

**Test Cases**:
- UX-015: Recursive glob pattern `**` matches nested files
- UX-016: Downloaded files flattened into destination directory
- UX-017: Directory structure from source NOT recreated locally

---

### Scenario 6: Filename Collision Detection

**Command**: `server download "**/*.config" ./configs/`

**Starting State** (console before command):
```
server>
```

**During** (collision detected BEFORE any download starts):
```
server> server download "**/*.config" ./configs/
```

**Ending** (error with collision details):
```
server> server download "**/*.config" ./configs/
[red]Error: Filename collision detected. The following files would overwrite each other:[/]
  - app/settings.config
  - db/settings.config
No files were downloaded.
server>
```

**Test Cases**:
- UX-018: Collision detection occurs BEFORE any download starts
- UX-019: Error message lists ALL conflicting filenames with paths
- UX-020: No partial downloads when collision detected

---

### Scenario 7: No Files Match Pattern

**Command**: `server download "*.xyz" ./output/`

**Starting State** (console before command):
```
server>
```

**During** (server query returns empty):
```
server> server download "*.xyz" ./output/
```

**Ending** (warning message in yellow):
```
server> server download "*.xyz" ./output/
[yellow]No files matched pattern: *.xyz[/]
server>
```

**Test Cases**:
- UX-021: Empty match displays WARNING (yellow, not red)
- UX-022: Message includes the pattern that matched nothing

---

### Scenario 8: Remote File Not Found (Literal Path)

**Command**: `server download nonexistent.txt ./`

**Starting State** (console before command):
```
server>
```

**During** (server returns 404):
```
server> server download nonexistent.txt ./
```

**Ending** (warning message in yellow - consistent with upload "file not found" pattern):
```
server> server download nonexistent.txt ./
[yellow]File not found: nonexistent.txt[/]
server>
```

**Test Cases**:
- UX-023: Literal path not found shows WARNING (yellow, consistent with glob no-match)
- UX-024: Message includes the missing filename

---

### Scenario 9: Not Connected to Server

**Command**: `server download file.txt ./`

**Starting State** (console before command):
```
server>
```

**During** (connection check fails immediately):
```
server> server download file.txt ./
```

**Ending** (error message):
```
server> server download file.txt ./
[red]Not connected to server[/]
server>
```

**Test Cases**:
- UX-025: Download blocked when not connected
- UX-026: Connection state checked before any server calls

---

### Scenario 10: Connection Lost During Download

**Command**: `server download largefile.bin ./`

**Starting State** (console before command):
```
server>
```

**During** (progress bar visible, then connection drops):
```
server> server download largefile.bin ./
Downloading largefile.bin ━━━━━━━━━━━━━━━━━━━━━╸                   50% 8.5 MB/s ⠙
```

**Ending** (progress bar auto-cleared FIRST, then error message):
```
server> server download largefile.bin ./
[red]Connection lost during download[/]
server>
```

**Post-Condition**: Partial file deleted from disk (clean state)

**Test Cases**:
- UX-027: Progress bar is CLEARED before error message appears
- UX-028: Connection loss shows clear error message (red)
- UX-029: Partial files cleaned up on failure

---

### Scenario 11: Permission Denied (Local Write)

**Command**: `server download file.txt /protected/`

**Starting State** (console before command):
```
server>
```

**During** (local write fails):
```
server> server download file.txt /protected/
```

**Ending** (error message):
```
server> server download file.txt /protected/
[red]Permission denied: Cannot write to /protected/file.txt[/]
server>
```

**Test Cases**:
- UX-030: Write permission errors caught and displayed (red)
- UX-031: Error message includes target path

---

### Scenario 12: Mixed Success/Failure in Batch

**Command**: `server download "*.dat" ./output/`

**Starting State** (console before command):
```
server>
```

**During** (aggregate progress bar, one file fails mid-batch):
```
server> server download "*.dat" ./output/
Downloading to ./output/ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  80% 5.2 MB/s ⠙
```

**Ending** (progress bar auto-cleared FIRST, then summary with failures):
```
server> server download "*.dat" ./output/
[yellow]Downloaded 2 of 3 files to ./output/[/]
[red]Failed: corrupted.dat - Checksum verification failed[/]
server>
```

**Test Cases**:
- UX-032: Progress bar is CLEARED before final messages appear
- UX-033: Batch download continues after individual failure
- UX-034: Summary shows success AND failure counts (yellow for partial success)
- UX-035: Failed files listed with error reason (red)

## Complexity Tracking

> No constitution violations requiring justification.

## Phase Outputs

### Phase 0: research.md
- Infrastructure gap validation
- Streaming download best practices
- Glob pattern matching on remote file systems

### Phase 1: Design Artifacts
- data-model.md (entities, messages, state transitions)
- contracts/download-api.md (HTTP + RPC specifications)
- quickstart.md (implementation examples)

### Phase 2: Test Cases
- test-cases.md (comprehensive from UX flows + component design)
