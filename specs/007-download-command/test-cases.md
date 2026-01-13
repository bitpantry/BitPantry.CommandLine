# Test Cases: Download Command

> Generated from spec.md, plan.md, data-model.md, and contracts/
> Each test case defines a single "when X, then Y" validation requirement

## User Experience Validation

Test cases validating user-facing behavior from user stories and functional requirements.

### Core Download Functionality (US-001, US-002)

| ID | When (Trigger/Action) | Then (Expected Result) | Source |
|----|----------------------|------------------------|--------|
| UX-001 | User runs `server download config.json ./` while connected | File downloaded to `./config.json` with success message | US-001, FR-001 |
| UX-002 | User runs `server download file.txt ./renamed.txt` | File downloaded as `./renamed.txt` | US-001, FR-001 |
| UX-003 | User runs `server download file.txt ./subdir/` where destination ends with `/` | Filename derived from source, saved to `./subdir/file.txt` | US-001, FR-007 |
| UX-004 | User runs download command when not connected | Error message "Not connected to server" displayed | US-001, FR-008 |
| UX-005 | User runs `server download nonexistent.txt ./` | Error message "File not found: nonexistent.txt" displayed | US-004, FR-005 |

### Glob Pattern Downloads (US-002)

| ID | When (Trigger/Action) | Then (Expected Result) | Source |
|----|----------------------|------------------------|--------|
| UX-006 | User runs `server download "*.txt" ./backup/` with 3 matching files | All 3 files downloaded to `./backup/` | US-002, FR-002 |
| UX-007 | User runs `server download "logs/*.log" ./archive/` | Only files in `logs/` directory matching `*.log` downloaded | US-002, FR-002 |
| UX-008 | User runs `server download "logs/**/*.log" ./flat/` with nested files | All nested `.log` files flattened into `./flat/` | US-002, Clarification |
| UX-009 | User runs `server download "file?.txt" ./` with `file1.txt`, `file2.txt`, `files.txt` | Only `file1.txt` and `file2.txt` downloaded (single char wildcard) | US-002, FR-002 |
| UX-010 | User runs `server download "*.xyz" ./` with no matches | Warning message "No files matched pattern: *.xyz" displayed | US-002, FR-005 |
| UX-011 | Pattern uses `**` for recursive search | Files from all subdirectories included in results | US-002, FR-002 |

### Progress Display (US-003)

| ID | When (Trigger/Action) | Then (Expected Result) | Source |
|----|----------------------|------------------------|--------|
| UX-012 | User downloads file >= `DownloadConstants.ProgressDisplayThreshold` | Progress bar with percentage displayed | US-003, FR-003 |
| UX-013 | User downloads file < `DownloadConstants.ProgressDisplayThreshold` | No progress bar displayed, clean output | US-003, FR-003 |
| UX-014 | User downloads multiple files with aggregate size >= `DownloadConstants.ProgressDisplayThreshold` | Aggregate progress bar shown | US-003, FR-003 |
| UX-015 | Progress bar is active during download | Transfer speed (MB/s) displayed | US-003, FR-003 |
| UX-016 | Download completes successfully | Progress bar clears, success message displayed | US-003 |
| UX-017 | Progress updates during active transfer | Updates occur at least once per second | US-003, SC-003 |

### Error Handling (US-004)

| ID | When (Trigger/Action) | Then (Expected Result) | Source |
|----|----------------------|------------------------|--------|
| UX-018 | Remote file does not exist (literal path) | Error "File not found: [path]" displayed | US-004, FR-005 |
| UX-019 | User lacks write permission to local destination | Error "Permission denied: [details]" displayed | US-004, FR-005 |
| UX-020 | Connection lost during download | Error "Connection lost during download" displayed | US-004, FR-009 |
| UX-021 | Checksum verification fails | Error indicates integrity check failure | US-004, FR-005 |
| UX-022 | Multiple files downloading, one fails | Summary shows success/failure counts and failed file details | US-004, FR-006 |
| UX-023 | Batch download has some failures | Download continues for remaining files | US-004, FR-006 |

### Concurrent Downloads (US-005)

| ID | When (Trigger/Action) | Then (Expected Result) | Source |
|----|----------------------|------------------------|--------|
| UX-024 | User downloads 10 files via glob pattern | Maximum `DownloadConstants.MaxConcurrentDownloads` concurrent transfers active at once | US-005, FR-004 |
| UX-025 | Concurrent downloads in progress | Aggregate progress bar reflects combined progress | US-005, FR-003 |
| UX-026 | All concurrent downloads complete | Summary shows total file count | US-005, FR-006 |

### Filename Collision Detection (Clarification)

| ID | When (Trigger/Action) | Then (Expected Result) | Source |
|----|----------------------|------------------------|--------|
| UX-027 | Glob matches files with same name in different directories | Error lists all conflicting filenames | Clarification, FR-011 |
| UX-028 | Collision detected | No files downloaded | Clarification, FR-011 |
| UX-029 | Collision error message displayed | All conflicting paths listed in error | Clarification, FR-011 |

### Summary Output (FR-006)

| ID | When (Trigger/Action) | Then (Expected Result) | Source |
|----|----------------------|------------------------|--------|
| UX-030 | Single file downloaded successfully | Message "Downloaded [file] to [path]" displayed | FR-006 |
| UX-031 | Multiple files downloaded successfully | Message "Downloaded [N] files to [destination]" displayed | FR-006 |
| UX-032 | Some files failed in batch | Summary shows "[N] of [M] files" with failure details | FR-006 |

---

## Component/Unit Validation

Test cases validating internal components, services, and architectural elements defined in the plan.

### DownloadCommand Component

| ID | Component | When (Input/State) | Then (Expected Behavior) | Source |
|----|-----------|-------------------|-------------------------|--------|
| CV-001 | DownloadCommand | ConnectionState is not Connected | Execute returns without server calls, shows error | plan.md: DownloadCommand |
| CV-002 | DownloadCommand | Source contains glob characters | `ExpandSourcePattern` called with pattern expansion | plan.md: DownloadCommand |
| CV-003 | DownloadCommand | Source is literal path | Direct file info lookup performed | plan.md: DownloadCommand |
| CV-004 | DownloadCommand.ExpandSourcePattern | Pattern `*.txt` provided | Returns list of FileInfoEntry with matching files | plan.md: DownloadCommand |
| CV-005 | DownloadCommand.DetectCollisions | Files list has duplicate filenames | Returns CollisionGroup with conflicting paths | plan.md: DownloadCommand |
| CV-006 | DownloadCommand.DetectCollisions | Files list has unique filenames | Returns empty list | plan.md: DownloadCommand |
| CV-007 | DownloadCommand.ResolveLocalPath | Destination ends with `/` | Returns destination + source filename | plan.md: DownloadCommand |
| CV-008 | DownloadCommand.ResolveLocalPath | Destination is specific filename | Returns destination as-is | plan.md: DownloadCommand |

### FileTransferService Component

| ID | Component | When (Input/State) | Then (Expected Behavior) | Source |
|----|-----------|-------------------|-------------------------|--------|
| CV-009 | FileTransferService.DownloadFile | Client is disconnected | Throws InvalidOperationException | plan.md: FileTransferService |
| CV-010 | FileTransferService.DownloadFile | Remote file exists | File content written to local path | plan.md: FileTransferService |
| CV-011 | FileTransferService.DownloadFile | progressCallback provided | Progress updates invoked during download | plan.md: FileTransferService |
| CV-012 | FileTransferService.DownloadFile | Server returns 404 | Throws FileNotFoundException | plan.md: FileTransferService |
| CV-013 | FileTransferService.DownloadFile | Checksum mismatch | Throws InvalidDataException, deletes partial file | plan.md: FileTransferService |
| CV-014 | FileTransferService.DownloadFile | Local directory doesn't exist | Creates parent directories before write | plan.md: FileTransferService |
| CV-015 | FileTransferService.DownloadFile | Download fails mid-transfer | Partial file deleted, exception propagated | plan.md: FileTransferService |
| CV-016 | FileTransferService.EnumerateFiles | Valid path and pattern | Returns FileInfoEntry array with sizes | plan.md: FileTransferService |
| CV-017 | FileTransferService.EnumerateFiles | recursive=true | SearchOption.AllDirectories used in RPC | plan.md: FileTransferService |

### ~~FileDownloadProgressUpdateFunctionRegistry Component~~ **REMOVED** - Dead code; download progress calculated client-side

~~These test cases have been removed. Download progress is calculated client-side from HTTP Content-Length header and stream reading, not via SignalR RPC messages like uploads.~~

| ID | Component | Status |
|----|-----------|--------|
| ~~CV-018~~ | ~~FileDownloadProgressUpdateFunctionRegistry.Register~~ | **REMOVED** |
| ~~CV-019~~ | ~~FileDownloadProgressUpdateFunctionRegistry.Unregister~~ | **REMOVED** |
| ~~CV-020~~ | ~~FileDownloadProgressUpdateFunctionRegistry~~ | **REMOVED** |

### Server-Side FileTransferEndpointService

| ID | Component | When (Input/State) | Then (Expected Behavior) | Source |
|----|-----------|-------------------|-------------------------|--------|
| CV-021 | FileTransferEndpointService.DownloadFile | Valid file path | File streamed with Content-Length header | plan.md: FileTransferEndpointService |
| CV-022 | FileTransferEndpointService.DownloadFile | connectionId provided | Progress messages sent via SignalR | plan.md: FileTransferEndpointService |
| CV-023 | FileTransferEndpointService.DownloadFile | Path traversal attempted | Returns 403 Forbidden | plan.md: FileTransferEndpointService |
| CV-024 | FileTransferEndpointService.DownloadFile | File not found | Returns 404 Not Found | plan.md: FileTransferEndpointService |
| CV-025 | FileTransferEndpointService.DownloadFile | Streaming completes | X-File-Checksum header set with SHA256 | plan.md: FileTransferEndpointService |

### Server-Side RPC Handler (EnumerateFiles - Enhanced)

| ID | Component | When (Input/State) | Then (Expected Behavior) | Source |
|----|-----------|-------------------|-------------------------|--------|
| CV-026 | FileSystemRpcHandler.HandleEnumerateFiles | Valid path and pattern | Returns FileInfoEntry array with size/dates | plan.md: FileSystemRpcHandler |
| CV-027 | FileSystemRpcHandler.HandleEnumerateFiles | Glob pattern with `**` | Recursively searches subdirectories | plan.md: FileSystemRpcHandler |
| CV-028 | FileSystemRpcHandler.HandleEnumerateFiles | Path traversal in pattern | Returns error, no file info leaked | plan.md: FileSystemRpcHandler |
| CV-029 | FileSystemRpcHandler.HandleEnumerateFiles | Directory doesn't exist | Returns error "Directory not found" | plan.md: FileSystemRpcHandler |
| CV-030 | FileSystemRpcHandler.HandleEnumerateFiles | No files match pattern | Returns empty array, no error | plan.md: FileSystemRpcHandler |

---

## Data Flow Validation

Test cases validating data transformations, state transitions, and cross-component interactions.

### Download State Machine

| ID | Flow | When (Condition) | Then (State/Data Change) | Source |
|----|------|-----------------|-------------------------|--------|
| DF-001 | Download Start | Execute called while connected | State transitions to "Expand Source Pattern" | data-model.md: State Machine |
| DF-002 | Pattern Expansion | Glob pattern provided | EnumerateFilesRequest sent to server | data-model.md: State Machine |
| DF-003 | Pattern Expansion | Literal path provided | GetFileInfoRequest sent to server | data-model.md: State Machine |
| DF-004 | Collision Check | Multiple files with same filename | State transitions to "Error: List collisions" | data-model.md: State Machine |
| DF-005 | Collision Check | All filenames unique | State transitions to "Calculate Total Size" | data-model.md: State Machine |
| DF-006 | Size Calculation | Total size >= `DownloadConstants.ProgressDisplayThreshold` | showProgress flag set to true | data-model.md: State Machine |
| DF-007 | Download Files | File download succeeds | DownloadResult.Status = Success | data-model.md: State Machine |
| DF-008 | Download Files | File download fails | DownloadResult.Status = Failed, Error populated | data-model.md: State Machine |

### Progress Data Flow

| ID | Flow | When (Condition) | Then (State/Data Change) | Source |
|----|------|-----------------|-------------------------|--------|
| ~~DF-009~~ | ~~Progress Callback~~ | ~~Server sends FileDownloadProgressMessage~~ | **REMOVED** - Client calculates progress from HTTP stream | |
| ~~DF-010~~ | ~~Progress Calculation~~ | ~~TotalRead and TotalSize received~~ | **REMOVED** - Client calculates progress from HTTP stream | |
| DF-011 | Aggregate Progress | Multiple concurrent downloads | totalBytesDownloaded accumulated via Interlocked.Add | quickstart.md: DownloadMultipleFilesAsync |
| DF-012 | Progress Delta | Same file reports progress twice | Delta calculated as TotalRead - lastValue | quickstart.md: DownloadMultipleFilesAsync |

### File Transfer Data Flow

| ID | Flow | When (Condition) | Then (State/Data Change) | Source |
|----|------|-----------------|-------------------------|--------|
| DF-013 | HTTP GET | DownloadFile called | Request sent with Authorization Bearer header | contracts/download-api.md |
| DF-014 | HTTP Response | Server responds with 200 | Content streamed to local file | contracts/download-api.md |
| DF-015 | Checksum Verification | X-File-Checksum header present | SHA256 computed during stream, compared at end | contracts/download-api.md |
| DF-016 | Directory Creation | Parent directory doesn't exist | Directory.CreateDirectory called | quickstart.md: FileTransferService |

### RPC Message Flow

| ID | Flow | When (Condition) | Then (State/Data Change) | Source |
|----|------|-----------------|-------------------------|--------|
| DF-017 | EnumerateFiles | Request sent with pattern | Server returns FileInfoEntry[] with path, size, lastModified | contracts/download-api.md |
| DF-018 | EnumerateFiles | searchOption=AllDirectories | Server uses SearchOption.AllDirectories | contracts/download-api.md |
| DF-019 | Progress Message | Server streams chunk | FileDownloadProgressMessage sent with totalRead | contracts/download-api.md |

---

## Error Handling Validation

Test cases validating error conditions, exception handling, and recovery behaviors.

### Connection Errors

| ID | Scenario | When (Error Condition) | Then (Recovery/Message) | Source |
|----|----------|----------------------|------------------------|--------|
| EH-001 | Not connected | ConnectionState != Connected | Display "Not connected to server", exit gracefully | FR-008, Edge Cases |
| EH-002 | Connection lost mid-download | Network failure during stream | Display "Connection lost during download", cleanup partial file | FR-009, Edge Cases |
| EH-003 | Server disconnected | SignalR hub disconnects | RemoteMessagingException caught, user-friendly message | FR-009 |

### File System Errors

| ID | Scenario | When (Error Condition) | Then (Recovery/Message) | Source |
|----|----------|----------------------|------------------------|--------|
| EH-004 | Remote file not found | Server returns 404 | Display "File not found: [path]" | FR-005, Edge Cases |
| EH-005 | Local permission denied | UnauthorizedAccessException on write | Display "Permission denied: [path]" | FR-005, Edge Cases |
| EH-006 | Disk space exhausted | IOException during write | Display "Disk space error", cleanup partial file | Edge Cases |
| EH-007 | Path too long | PathTooLongException | Display error with path, no crash | Edge Cases |
| EH-008 | Invalid characters in filename | Invalid path on local filesystem | Display error, skip file if in batch | Edge Cases |

### Pattern Matching Errors

| ID | Scenario | When (Error Condition) | Then (Recovery/Message) | Source |
|----|----------|----------------------|------------------------|--------|
| EH-009 | No files match pattern | EnumerateFiles returns empty | Display warning "No files matched pattern: [pattern]" | FR-005 |
| EH-010 | Filename collision detected | Multiple files resolve to same local name | Display error listing all conflicts, no downloads | FR-011, Clarification |
| EH-011 | Invalid glob pattern | Malformed pattern syntax | Display error, suggest valid pattern format | Edge Cases |

### Data Integrity Errors

| ID | Scenario | When (Error Condition) | Then (Recovery/Message) | Source |
|----|----------|----------------------|------------------------|--------|
| EH-012 | Checksum mismatch | Server checksum != computed checksum | Delete partial file, display "Checksum verification failed" | Edge Cases |
| EH-013 | Partial download cleanup | Any exception during download | Delete partial local file before propagating | Edge Cases |

### Batch Download Errors

| ID | Scenario | When (Error Condition) | Then (Recovery/Message) | Source |
|----|----------|----------------------|------------------------|--------|
| EH-014 | One file fails in batch | Individual download throws | Increment failureCount, continue with remaining | FR-006 |
| EH-015 | All files fail in batch | Every download throws | Display summary with 0 success, N failures | FR-006 |
| EH-016 | Mixed success/failure | Some succeed, some fail | Display "[N] of [M] files" with failure details | FR-006 |

### Cancellation

| ID | Scenario | When (Error Condition) | Then (Recovery/Message) | Source |
|----|----------|----------------------|------------------------|--------|
| EH-017 | User cancels download | CancellationToken signaled | Cleanup partial files, display cancellation message | Edge Cases |
| EH-018 | Timeout during download | HTTP timeout | Display timeout error, cleanup partial file | Edge Cases |

---

## Integration Test Cases

These test cases require end-to-end server-client communication.

| ID | Scenario | Setup | Action | Expected Result | Source |
|----|----------|-------|--------|-----------------|--------|
| IT-001 | Single file download E2E | File exists on server | `server download file.txt ./` | File appears locally with correct content | US-001 |
| IT-002 | Glob pattern download E2E | Multiple files on server | `server download "*.log" ./logs/` | All matching files downloaded | US-002 |
| IT-003 | Progress callback E2E | Large file on server | Download with progress callback | Callback invoked multiple times with increasing TotalRead | US-003 |
| IT-004 | Concurrent downloads E2E | 10 files on server | Download via glob | All 10 files downloaded, ≤`DownloadConstants.MaxConcurrentDownloads` concurrent at any time | US-005 |
| IT-005 | Checksum verification E2E | File with known checksum | Download file | Downloaded file checksum matches server checksum | contracts/download-api.md |
| IT-006 | EnumerateFiles E2E | Files with known sizes | Query file list | Response contains correct sizes for all files | contracts/download-api.md |
| IT-007 | Recursive glob E2E | Nested directory structure | `server download "**/*.txt" ./flat/` | All nested files flattened to destination | US-002, Clarification |
| IT-008 | 404 handling E2E | File doesn't exist on server | Download nonexistent file | FileNotFoundException thrown | EH-004 |
| IT-009 | Path traversal prevention E2E | Malicious path attempted | Download `../../../etc/passwd` | Server rejects with 403 | Security |
| IT-010 | Large batch E2E | 100+ files on server | Download via glob | All files downloaded, summary shows count | SC-002 |

### Cross-Platform Validation

| ID | Scenario | Setup | Action | Expected Result | Source |
|----|----------|-------|--------|-----------------|--------|
| IT-011 | Path separator normalization | Server returns paths with `/` | Client on Windows processes paths | Paths converted to `\` for local filesystem | Cross-Platform |
| IT-012 | Case collision detection | Files `File.txt` and `file.txt` on server | Download to Windows client | Collision detected before download | Cross-Platform |

---

## Source Reference Guide

- **User Stories**: US-001 (Single file), US-002 (Glob), US-003 (Progress), US-004 (Errors), US-005 (Concurrent)
- **Functional Requirements**: FR-001 through FR-011
- **Clarification**: Session 2026-01-10 decisions (flattening, collision handling)
- **Plan Components**: plan.md sections on DownloadCommand, FileTransferService
- **Data Model**: data-model.md entities and state machines
- **Contracts**: contracts/download-api.md HTTP and RPC specifications
- **Edge Cases**: spec.md Edge Cases section
- **Success Criteria**: SC-001 through SC-006
