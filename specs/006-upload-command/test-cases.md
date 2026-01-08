# Test Cases: Upload Command

> Generated from spec.md, plan.md, data-model.md, and contracts/
> Each test case defines a single "when X, then Y" validation requirement

## User Experience Validation

Test cases validating user-facing behavior from user stories and functional requirements.

| ID | When (Trigger/Action) | Then (Expected Result) | Source |
|----|----------------------|------------------------|--------|
| UX-001 | User executes `server upload myfile.txt /remote/` with valid local file | File is uploaded and success message shows "Uploaded myfile.txt to /remote/" | US-001, FR-009 |
| UX-002 | User executes `server upload *.txt /remote/` with 3 matching files | All 3 files are uploaded and summary shows "Uploaded 3 files to /remote/" | US-002, FR-009 |
| UX-003 | User uploads a single file >= 1MB | Progress bar displays with filename and percentage | US-003, FR-008 |
| UX-004 | User uploads a single file < 1MB | No progress bar displayed, only success message | US-003, FR-008 |
| UX-005 | User uploads multiple files of any size | Progress table displays all files with status (Pending → percentage → Completed) | US-003, FR-008 |
| UX-006 | User executes upload from external shell with quoted glob `"*.txt"` | Glob is preserved and expanded by command, files uploaded | US-004, FR-005 |
| UX-007 | User executes upload when not connected to server | Error message "Not connected to server" displayed, command returns 1 | FR-003, Edge Cases |
| UX-008 | User executes `server upload` without arguments | Error indicating source and destination are required | FR-002 |
| UX-009 | User executes `server upload nonexistent.txt /remote/` | Error "File not found: nonexistent.txt" displayed, returns 1 | FR-010, Edge Cases |
| UX-010 | User executes `server upload *.xyz /remote/` with no matches | Warning "No files matched pattern: *.xyz" displayed, returns 0 | FR-007, Edge Cases |
| UX-011 | User uploads to destination that is a directory path | Source filename is appended to destination path | FR-002, Edge Cases |
| UX-012 | User uploads to destination that is a file path | Destination path used as-is for single file | FR-002, Edge Cases |
| UX-013 | User executes `server upload "**/*.txt" /remote/` with recursive matches | All .txt files in subdirectories uploaded | US-002, FR-012 |
| UX-014 | User executes `server upload *.txt /remote/ --skip-existing` with 1 existing | Existing file skipped, others uploaded, summary shows "1 skipped" | US-005, FR-011 |
| UX-015 | User executes `server upload *.txt /remote/ -s` (short flag) | Same behavior as --skip-existing | US-005, FR-011 |
| UX-016 | User executes upload with all files existing and --skip-existing | All files skipped, success message "0 files uploaded. 3 skipped." | US-005, FR-011 |

## Component/Unit Validation

Test cases validating internal components, services, and architectural elements defined in the plan.

| ID | Component | When (Input/State) | Then (Expected Behavior) | Source |
|----|-----------|-------------------|-------------------------|--------|
| CV-001 | UploadCommand | Constructed with dependencies | SignalRServerProxy, FileTransferService, IAnsiConsole injected via constructor | Constitution II, plan.md |
| CV-002 | UploadCommand.ExpandSource | Given literal file path that exists | Returns (existing: [path], missing: []) | FR-004, data-model.md |
| CV-003 | UploadCommand.ExpandSource | Given literal file path that does not exist | Returns (existing: [], missing: [path]) | FR-004, data-model.md |
| CV-004 | UploadCommand.ExpandSource | Given glob pattern `*.txt` with 3 matches | Returns (existing: [file1.txt, file2.txt, file3.txt], missing: []) | FR-004 |
| CV-005 | UploadCommand.ExpandSource | Given glob pattern `*.xyz` with 0 matches | Returns (existing: [], missing: []) | FR-004, FR-007 |
| CV-006 | UploadCommand.ExpandSource | Given glob pattern `data?.json` | Pattern `?` matches single character correctly | FR-004 |
| CV-007 | UploadCommand.ExpandSource | Given relative glob pattern `*.txt` | Uses current directory as base for pattern matching | FR-004 |
| CV-008 | UploadCommand.ExpandSource | Given absolute glob pattern `/path/*.txt` | Uses specified directory as base for pattern matching | FR-004 |
| CV-009 | UploadCommand | Connection state is Disconnected | Returns 1 with error message without attempting upload | FR-003 |
| CV-010 | UploadCommand | Connection state is Connecting | Returns 1 with error message without attempting upload | FR-003 |
| CV-011 | UploadCommand | Connection state is Connected | Proceeds with upload operation | FR-003 |
| CV-012 | UploadCommand.UploadSingleFileAsync | File size >= 1MB | Progress bar displayed via IAnsiConsole.Progress() | FR-008, research.md |
| CV-013 | UploadCommand.UploadSingleFileAsync | File size < 1MB | FileTransferService.UploadFile called without progress display | FR-008 |
| CV-014 | UploadCommand.UploadMultipleFilesAsync | Given 5 files | All 5 progress tasks created upfront with "Pending" state | FR-008, research.md |
| CV-015 | UploadCommand.UploadMultipleFilesAsync | Given 10 files with max concurrency 4 | Only 4 uploads active simultaneously | FR-006, research.md |
| CV-016 | UploadCommand.UploadMultipleFilesAsync | Upload completes successfully | Task description updated to "[green]Completed[/]" | FR-008 |
| CV-017 | UploadCommand.UploadMultipleFilesAsync | Upload fails with exception | Task description updated to "[red]Failed[/]", continues with remaining | FR-006, FR-010 |
| CV-018 | UploadCommand | FileTransferService.UploadFile progress callback invoked | Progress percentage calculated from TotalRead / FileSize * 100 | research.md: RT-004 |
| CV-019 | UploadCommand | CancellationToken is cancelled | Upload operation cancelled, throws TaskCanceledException | Edge Cases |
| CV-020 | UploadCommand.ExpandSource | Given recursive glob pattern `**/*.txt` | Uses FileSystemGlobbing Matcher to find files recursively | FR-012, research.md: RT-003 |
| CV-021 | UploadCommand.ExpandSource | Given pattern `logs/**/*.log` | Matches .log files in logs/ and all subdirectories | FR-012 |
| CV-022 | FileTransferService.CheckFilesExist | Given directory and list of filenames | Returns dictionary mapping filename to existence boolean | FR-011, contracts |
| CV-023 | UploadCommand | --skip-existing flag set, some files exist | Calls CheckFilesExist, filters out existing files before upload | FR-011 |
| CV-024 | UploadCommand | --skip-existing flag not set | Does not call CheckFilesExist, uploads all files (overwrite) | FR-011 |
| CV-025 | FilesExistEndpoint | POST with valid directory and filenames | Returns 200 with exists map | contracts |
| CV-026 | FilesExistEndpoint | POST without Authorization header | Returns 401 Unauthorized | contracts |
| CV-027 | FilesExistEndpoint | POST with path traversal in directory | Returns 400 Bad Request or sanitized path | contracts |
| CV-028 | FileTransferService.CheckFilesExist | 150 files to check (> BATCH_EXISTS_CHUNK_SIZE) | Makes 2 batch requests (100 + 50), merges results | research.md: RT-006 |
| CV-029 | FileTransferService.CheckFilesExist | Exactly 100 files | Makes single batch request | research.md: RT-006 |
| CV-030 | FileTransferService.UploadFile | skipIfExists=true, server returns "skipped" | Returns result with Status="skipped" | contracts |
| CV-031 | UploadCommand | Server returns "skipped" for file expected to upload | File counted as skipped (TOCTOU race), task shows "Skipped (server)" | plan.md |

## Data Flow Validation

Test cases validating data transformations, state transitions, and cross-component interactions.

| ID | Flow | When (Condition) | Then (State/Data Change) | Source |
|----|------|-----------------|-------------------------|--------|
| DF-001 | Single file upload | Execute with existing file | UploadStatus: Pending → InProgress → Completed | data-model.md |
| DF-002 | Single file upload | File not found | UploadStatus: remains NotFound (never starts) | data-model.md |
| DF-003 | Single file upload | FileTransferService throws | UploadStatus: InProgress → Failed | data-model.md |
| DF-004 | Multi-file upload | 3 files, all succeed | UploadResult: SuccessCount=3, FailureCount=0 | data-model.md |
| DF-005 | Multi-file upload | 3 files, 1 fails | UploadResult: SuccessCount=2, FailureCount=1, FailedFiles has 1 entry | data-model.md, FR-010 |
| DF-006 | Multi-file upload | 3 files, 1 not found before upload | UploadResult: SuccessCount=2, NotFoundCount=1, NotFoundFiles has 1 entry | data-model.md, FR-010 |
| DF-007 | Progress callback | FileUploadProgress received with TotalRead=500000, FileSize=1000000 | Progress task value set to 50% | data-model.md, research.md |
| DF-008 | Progress callback | FileUploadProgress received with Error="Network error" | TaskCompletionSource set to exception state | data-model.md |
| DF-009 | Glob expansion | Pattern `*.txt` in directory with 100 files, 5 match | Returns 5 file paths in existing array | FR-004 |
| DF-010 | Destination resolution | Destination is directory `/remote/dir/` | Final path = `/remote/dir/` + source filename | FR-002 |
| DF-011 | Destination resolution | Destination is file `/remote/file.txt` | Final path = `/remote/file.txt` as-is | FR-002 |
| DF-012 | Recursive glob expansion | Pattern `**/*.txt` with nested structure | Returns files from all subdirectory levels | FR-012 |
| DF-013 | Skip existing flow | --skip-existing with 3 files, 1 exists | CheckFilesExist called, 1 file filtered, 2 uploaded | FR-011, data-model.md |
| DF-014 | Skip existing status | File exists on server | UploadStatus set to Skipped, not shown in progress | FR-011, data-model.md |
| DF-015 | Skip existing summary | 2 uploaded, 1 skipped | UploadResult: SuccessCount=2, SkippedCount=1 | data-model.md |
| DF-016 | Batch chunking | 250 files with --skip-existing | CheckFilesExist chunks into 3 requests (100+100+50) | research.md: RT-006 |
| DF-017 | Server-side skip (TOCTOU) | File created after batch check, skipIfExists=true | Server returns "skipped", client handles gracefully | plan.md |
| DF-018 | Server-side overwrite (no flag) | File exists, skipIfExists=false | Server overwrites file, returns "uploaded" | contracts |

## Error Handling Validation

Test cases validating error conditions, exception handling, and recovery behaviors.

| ID | Scenario | When (Error Condition) | Then (Recovery/Message) | Source |
|----|----------|----------------------|------------------------|--------|
| EH-001 | Not connected | Proxy.ConnectionState == Disconnected | Console displays "Not connected to server", returns 1 | FR-003, Edge Cases |
| EH-002 | Single file not found | File.Exists returns false for literal path | Console displays "File not found: {path}", returns 1 | FR-010, Edge Cases |
| EH-003 | Glob matches zero files | Directory.GetFiles returns empty array | Console displays "No files matched pattern: {pattern}", returns 0 | FR-007, Edge Cases |
| EH-004 | Permission denied (local) | FileStream throws UnauthorizedAccessException | Console displays underlying error message, returns 1 | FR-010, Edge Cases |
| EH-005 | Permission denied (remote) | FileTransferService throws HttpRequestException 403 | Console displays underlying error message, returns 1 | FR-010, Edge Cases |
| EH-006 | Network error during upload | FileTransferService throws HttpRequestException | Task marked as Failed, error logged, upload continues for remaining files | FR-010, Edge Cases |
| EH-007 | Partial failure multi-file | 2 of 5 files fail | Summary shows "Uploaded 3 of 5 files to {destination}" with failure details | FR-010, Edge Cases |
| EH-008 | Missing files in multi-upload | Glob matches files, some deleted before upload | Files excluded from progress, included in summary: "X files not found: {list}" | FR-010, Edge Cases |
| EH-009 | Upload interrupted | CancellationToken cancelled mid-upload | TaskCanceledException propagated, partial file may exist on server | Edge Cases |
| EH-010 | Server connection lost | SignalR connection drops during upload | FileTransferService throws, handled as upload failure | Edge Cases |
| EH-011 | Invalid destination path | Server rejects destination (path traversal) | HttpRequestException with 400, displayed to user | FR-010 |
| EH-012 | All files in multi-upload fail | 3 files, all throw exceptions | Summary shows "Uploaded 0 of 3 files", returns 1 | FR-010 |
| EH-013 | Mixed missing and failed | 2 not found, 1 fails, 2 succeed | Summary: "Uploaded 2 of 3 files. 2 files not found: {list}" | FR-010 |
| EH-014 | CheckFilesExist fails | Server returns 500 during batch check | Fall back to upload all (no skip), log warning | FR-011 |
| EH-015 | CheckFilesExist timeout | Request times out | Fall back to upload all (no skip), log warning | FR-011 |
| EH-016 | Recursive glob in inaccessible dir | **/*.txt includes permission-denied folder | Skip inaccessible folders, continue with accessible | FR-012 |
| EH-017 | Server returns unknown status | UploadFile returns Status != "uploaded"/"skipped" | Log warning, treat as success if not error | contracts |

---

## Integration Test Cases

These test cases require the full TestEnvironment infrastructure with TestServer.

| ID | Scenario | Prerequisites | Validation |
|----|----------|---------------|------------|
| IT-001 | End-to-end single file upload | Connected to TestServer | File exists on server storage with correct content |
| IT-002 | End-to-end multi-file upload | Connected to TestServer, 3 temp files | All 3 files exist on server storage |
| IT-003 | Upload with progress callback | Connected to TestServer, file >= 1MB | Progress callback invoked with increasing TotalRead |
| IT-004 | Upload when disconnected | Not connected | Error returned without HTTP call |
| IT-005 | Upload large file with cancellation | CancellationTokenSource, large file | TaskCanceledException thrown |
| IT-006 | Recursive glob upload | Nested directory structure with .txt files | All matching files uploaded with correct paths |
| IT-007 | Skip existing integration | File already on server, --skip-existing | File skipped, summary shows skipped count |
| IT-008 | Batch exists check | Multiple files, some exist | CheckFilesExist returns correct existence map |
| IT-009 | Overwrite existing (default) | File exists on server, no flag | File overwritten with new content |
| IT-010 | Server-side skip (TOCTOU) | File created between check and upload, --skip-existing | Server skips, client shows "Skipped (server)" in summary |
| IT-011 | Large batch exists check | 250 files, connected to server | Chunked requests processed correctly, all results merged |

---

## Source Reference Guide

The **Source** column supports flexible references to trace test cases back to their origin:

- **User Stories**: `US-001`, `US-002`, `US-003`, `US-004`, `US-005`
- **Functional Requirements**: `FR-001` through `FR-012`
- **Plan Components**: `plan.md: UploadCommand`, `plan.md: Concurrency Strategy`, `plan.md: Batch File Existence Check`
- **Data Model**: `data-model.md: UploadOperation`, `data-model.md: UploadStatus`, `data-model.md: FilesExistRequest`
- **Research**: `research.md: RT-001` through `research.md: RT-006`
- **Contracts**: `contracts/README.md: POST /files/exists`
- **Edge Cases**: `Edge Cases` table in spec.md
- **Constitution**: `Constitution II` (DI), `Constitution I` (TDD)
