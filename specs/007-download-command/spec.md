# Feature Specification: Download Command

**Feature Branch**: `007-download-command`  
**Created**: January 10, 2026  
**Status**: Draft  
**Input**: User description: "Build a download command that mirrors the upload command features including glob pattern support for remote source, skip existing files option, progress display, multi-file concurrent downloads, and friendly error handling"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Download Single File from Server (Priority: P1)

As a user connected to a remote server, I want to download a single file to my local machine so I can work with server files locally.

**Why this priority**: This is the core functionality - without single file downloads, the command has no value.

**Independent Test**: Can be fully tested by connecting to a server and downloading one file, verifying the file appears locally with correct content.

**Acceptance Scenarios**:

1. **Given** user is connected to server and file exists remotely, **When** user runs `server download remotefile.txt ./local/`, **Then** file is downloaded to `./local/remotefile.txt`
2. **Given** user is connected to server, **When** user runs `server download remotefile.txt ./localfile.txt`, **Then** file is downloaded with the specified local name
3. **Given** user is not connected to server, **When** user runs download command, **Then** user sees "Not connected to server" error message

---

### User Story 2 - Download Multiple Files Using Glob Patterns (Priority: P1)

As a user, I want to download multiple files using glob patterns so I can efficiently transfer groups of related files.

**Why this priority**: Pattern-based downloads are essential for productivity when working with multiple files.

**Independent Test**: Can be tested by downloading files matching a pattern (e.g., `*.log`) and verifying all matching files are transferred.

**Acceptance Scenarios**:

1. **Given** server has files matching pattern, **When** user runs `server download "*.txt" ./local/`, **Then** all matching files are downloaded to local directory
2. **Given** server has files in nested directories, **When** user runs `server download "logs/**/*.log" ./local/`, **Then** all matching files from subdirectories are downloaded and flattened into `./local/`
3. **Given** no files match the pattern, **When** user runs download with pattern, **Then** user sees "No files matched pattern: [pattern]" message
4. **Given** pattern uses `?` wildcard, **When** user runs `server download "file?.txt" ./local/`, **Then** only files matching single-character wildcard are downloaded

---

### User Story 3 - View Download Progress (Priority: P2)

As a user downloading large files, I want to see progress information so I know the transfer is working and estimate completion time.

**Why this priority**: Progress feedback is important for user experience but not required for basic functionality.

**Independent Test**: Can be tested by downloading a large file and observing progress bar updates.

**Acceptance Scenarios**:

1. **Given** file size exceeds progress threshold (`DownloadConstants.ProgressDisplayThreshold`), **When** user downloads file, **Then** progress bar with percentage and transfer speed is displayed
2. **Given** downloading multiple files, **When** user runs download command, **Then** aggregate progress is shown for all files
3. **Given** file size is below progress threshold, **When** user downloads file, **Then** no progress bar is displayed (clean output)

---

### User Story 4 - Handle Download Errors Gracefully (Priority: P2)

As a user, I want clear error messages when downloads fail so I understand what went wrong and how to fix it.

**Why this priority**: Good error handling improves user experience and reduces support burden.

**Independent Test**: Can be tested by attempting downloads with various error conditions and verifying appropriate messages.

**Acceptance Scenarios**:

1. **Given** remote file does not exist, **When** user runs download, **Then** user sees "File not found: [path]" error
2. **Given** user lacks permission to write locally, **When** user runs download, **Then** user sees "Permission denied: [details]" error
3. **Given** connection is lost during download, **When** transfer fails, **Then** user sees "Connection lost during download" error
4. **Given** multiple files are downloading and one fails, **When** downloads complete, **Then** summary shows success/failure counts and failed file details

---

### User Story 5 - Concurrent Multi-File Downloads (Priority: P3)

As a user downloading many files, I want concurrent transfers so large batch downloads complete faster.

**Why this priority**: Performance optimization is valuable but requires single-file downloads to work correctly first.

**Independent Test**: Can be tested by downloading many small files and measuring that total time is less than sequential download time.

**Acceptance Scenarios**:

1. **Given** user downloads multiple files, **When** download runs, **Then** files are transferred concurrently (up to concurrency limit)
2. **Given** concurrent downloads are running, **When** user views progress, **Then** aggregate progress across all transfers is shown
3. **Given** some concurrent downloads fail, **When** all complete, **Then** summary accurately reports successes and failures

---

### Edge Cases

- What happens when destination path does not exist? (Create parent directories automatically)
- What happens when filename collisions occur during flattened downloads? (Fail with error listing all conflicting filenames before any download starts)
- What happens when disk space runs out during download? (Show clear error, partial file handling)
- What happens when remote path is a directory instead of a file? (Show error: "Path is a directory. Use a glob pattern to download contents, e.g., 'directory/*'")
- What happens when user cancels download mid-transfer? (Clean up partial files, show cancellation message)
- What happens with very long file paths? (Display error "Path too long: [path]" with suggestion to use shorter destination path, skip file in batch)
- What happens with special characters in filenames? (Detect invalid characters for client's local filesystem - Windows restricts `<>:"/\|?*`, Linux/macOS only restricts `/` and null. Display error with invalid character, skip file in batch)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow downloading a single file from the remote server to a local path
- **FR-002**: System MUST support glob patterns for specifying multiple remote files (including `*`, `**`, and `?` wildcards)
- **FR-003**: System MUST display progress (bar, percentage, transfer speed) for downloads exceeding `DownloadConstants.ProgressDisplayThreshold`
- **FR-004**: System MUST support concurrent downloads with a configurable limit (default: `DownloadConstants.MaxConcurrentDownloads`)
- **FR-005**: System MUST provide user-friendly error messages for common failure scenarios
- **FR-006**: System MUST display a summary upon completion showing files downloaded and failed
- **FR-007**: System MUST resolve destination paths, appending source filename when destination ends with `/`
- **FR-008**: System MUST verify connection state before attempting download
- **FR-009**: System MUST handle connection loss gracefully during transfers
- **FR-010**: System MUST quote glob patterns to prevent local shell expansion (document in help text)
- **FR-011**: System MUST detect filename collisions before downloading and fail with an error listing all conflicting filenames

### Key Entities

- **Remote Source**: Path or glob pattern on the remote server identifying files to download
- **Local Destination**: Local file path or directory where downloaded files will be saved
- **Download Progress**: Tracks bytes transferred, percentage complete, and transfer speed per file and aggregate
- **Transfer Result**: Outcome of each file transfer (success, failed with reason)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can download a single file in 3 or fewer command invocations (connect, download, done)
- **SC-002**: Glob pattern downloads complete all matching files with a single command
- **SC-003**: Progress display updates at least once per second during active transfers
- **SC-004**: Error messages clearly indicate the problem without exposing technical implementation details
- **SC-005**: Multi-file downloads complete faster than sequential transfers (measurable speedup with concurrent transfers)
- **SC-006**: Command provides consistent user experience with the upload command (symmetric command structure and output format)

## Clarifications

### Session 2026-01-10

- Q: Should recursive glob downloads preserve remote directory structure? → A: Flatten all files into destination directory (no subdirectories created)
- Q: How should filename collisions be handled when flattening? → A: Fail with error listing conflicting filenames before any download starts

## Cross-Platform Considerations

The download command supports all combinations of Windows/Linux client and server:

| Consideration | Approach |
|---------------|----------|
| **Path separators** | Server returns paths using forward slashes (`/`) as canonical separator. Client normalizes to local OS separator. |
| **Case sensitivity** | Collision detection uses case-insensitive comparison for safety (prevents data loss when Linux server → Windows client). |
| **Invalid characters** | Client detects invalid characters for its local filesystem: Windows restricts `<>:"/\|?*`, Linux/macOS only restricts `/` and null byte. |
| **Path length limits** | Windows has 260-char limit (unless long paths enabled), Linux typically 4096 chars. Client validates before download. |
| **Line endings** | Binary transfer - no line ending conversion. Files transferred byte-for-byte. |

## Assumptions

- The remote server supports file listing operations to resolve glob patterns
- The FileTransferService (or equivalent) provides download functionality similar to upload
- Server capabilities include information about available files for pattern matching
- Local filesystem access follows standard permissions model
- Concurrent download limit defined by `DownloadConstants.MaxConcurrentDownloads` (mirrors `UploadConstants.MaxConcurrentUploads`)
- Progress display threshold defined by `DownloadConstants.ProgressDisplayThreshold` (mirrors `UploadConstants.ProgressDisplayThreshold`)
