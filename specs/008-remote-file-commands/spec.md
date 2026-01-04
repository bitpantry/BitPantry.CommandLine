# Feature Specification: Remote File System Commands

**Feature Branch**: `008-remote-file-commands`  
**Created**: 2026-01-02  
**Status**: Draft  
**Input**: User description: "Client-side commands for working with the remote file system - listing files, uploading, downloading, etc."

## Overview

This feature adds a `file` command group for interactive CLI operations on the remote server's sandboxed file system. The commands leverage the existing `IFileSystem` abstraction from `System.IO.Abstractions` and the `SandboxedFileSystem` infrastructure established in 001-unified-file-system.

### Architecture Context

Per the existing architecture (see [FileSystem.md](../../Docs/Remote/FileSystem.md)):

- **Server-executed commands**: Commands that operate on server files (ls, rm, mkdir, cat, info) are **remote commands registered on the server**. When invoked by a connected client, they execute on the server with `IFileSystem` resolving to `SandboxedFileSystem`. Output streams back to the client console.

- **Client-side transfer commands**: Commands that move file content between client and server (upload, download) are **client-side commands** that use the existing `FileTransferService` infrastructure.

| Command Type | Package | Execution | File System |
|--------------|---------|-----------|-------------|
| `file ls/rm/mkdir/cat/info` | Server | Runs on server | `SandboxedFileSystem` |
| `file upload/download` | Client | Runs on client | Local + `FileTransferService` |

**Command Group**: `file`  
**Default Registration**: Commands are registered by default when using the respective packages (no additional configuration required)
**Prerequisites**: 
- Active server connection (via `server connect`)

## Command Reference

### Command Summary

| Command | Syntax | Description | Location |
|---------|--------|-------------|----------|
| `file ls` | `file ls [path] [-r] [-l]` | List files and directories | Server |
| `file upload` | `file upload <local> [remote] [-f]` | Upload file to server | Client |
| `file download` | `file download <remote> [local] [-f]` | Download file from server | Client |
| `file rm` | `file rm <path> [-r] [-f]` | Remove file or directory | Server |
| `file mkdir` | `file mkdir <path>` | Create directory | Server |
| `file cat` | `file cat <path>` | Display file contents | Server |
| `file info` | `file info <path>` | Show file/directory metadata | Server |

---

## Server-Side Commands

These commands are registered in `BitPantry.CommandLine.Remote.SignalR.Server` and execute on the server. They inject `IFileSystem` which resolves to `SandboxedFileSystem` confined to the configured `StorageRootPath`.

### `file ls` - List Files and Directories

**Syntax**: `file ls [path] [--recursive|-r] [--long|-l]`

**Arguments**:
- `path` (positional, optional): Directory path to list. Defaults to `.` (storage root)
- `--recursive`, `-r` (option): List contents recursively
- `--long`, `-l` (option): Show detailed information (size, date)

**Implementation**: Uses `IFileSystem.Directory.EnumerateFileSystemEntries()` with `SandboxedFileSystem` path validation.

**Output Formats**:

*Default (short) format*:
```
documents/
reports/
config.json
data.csv
```

*Long format (`-l`)*:
```
Type  Size       Modified             Name
----  ----       --------             ----
dir   -          2026-01-02 10:30:15  documents/
dir   -          2026-01-02 09:15:00  reports/
file  1.2 KB     2026-01-01 14:22:30  config.json
file  45.6 MB    2025-12-28 08:00:00  data.csv
```

---

### `file rm` - Remove File or Directory

**Syntax**: `file rm <path> [--recursive|-r] [--force|-f]`

**Arguments**:
- `path` (positional, required): Path to remove
- `--recursive`, `-r` (option): Remove directories and contents recursively
- `--force`, `-f` (option): Skip confirmation prompt

**Implementation**: Uses `IFileSystem.File.Delete()` or `IFileSystem.Directory.Delete(path, recursive)`.

**Behavior**:
- Prompts for confirmation before deletion (unless `--force`)
- Requires `--recursive` to delete non-empty directories
- Shows count of items deleted for recursive operations

---

### `file mkdir` - Create Directory

**Syntax**: `file mkdir <path>`

**Arguments**:
- `path` (positional, required): Directory path to create

**Implementation**: Uses `IFileSystem.Directory.CreateDirectory()` which creates parent directories as needed.

**Behavior**:
- Creates parent directories as needed (like `mkdir -p`)
- Succeeds silently if directory already exists

---

### `file cat` - Display File Contents

**Syntax**: `file cat <path>`

**Arguments**:
- `path` (positional, required): File path to display

**Implementation**: Uses `IFileSystem.File.ReadAllText()` or streaming read for large files.

**Behavior**:
- Streams file content to console (output goes to connected client)
- Detects binary files and warns before display
- Limits output to 1 MB by default (warns if truncated)

---

### `file info` - Show File/Directory Information

**Syntax**: `file info <path>`

**Arguments**:
- `path` (positional, required): Path to inspect

**Implementation**: Uses `IFileSystem.File.GetAttributes()`, `IFileSystem.FileInfo`, `IFileSystem.DirectoryInfo`.

**Output Example**:
```
Path:         reports/annual-report.pdf
Type:         File
Size:         2.3 MB (2,412,544 bytes)
Created:      2025-12-15 09:30:00
Modified:     2026-01-01 14:22:30
```

---

## Client-Side Transfer Commands

These commands are registered in `BitPantry.CommandLine.Remote.SignalR.Client` and execute on the client. They use the existing `FileTransferService` for HTTP-based file transfer with SignalR progress updates.

### `file upload` - Upload File to Server

**Syntax**: `file upload <local-path> [remote-path] [--force|-f]`

**Arguments**:
- `local-path` (positional, required): Local file path to upload
- `remote-path` (positional, optional): Destination path on server. Defaults to filename in storage root
- `--force`, `-f` (option): Overwrite existing file without confirmation

**Implementation**: Uses existing `FileTransferService.UploadFile()` method with progress callback.

**Behavior**:
- Shows progress bar for files > 1 MB (using existing `FileUploadProgressUpdateFunctionRegistry`)
- Displays transfer speed and estimated time remaining
- Verifies SHA256 checksum after transfer (existing infrastructure)
- Prompts for confirmation if remote file exists (unless `--force`) - requires server endpoint to check existence

**Output Example**:
```
Uploading report.pdf to reports/report.pdf
[████████████████████████████████████████] 100%  45.2 MB  12.3 MB/s
Upload complete. Checksum verified.
```

---

### `file download` - Download File from Server

**Syntax**: `file download <remote-path> [local-path] [--force|-f]`

**Arguments**:
- `remote-path` (positional, required): Remote file path to download
- `local-path` (positional, optional): Local destination path. Defaults to current directory with same filename
- `--force`, `-f` (option): Overwrite existing local file without confirmation

**Implementation**: Uses existing `FileTransferService.DownloadFile()` method.

**Behavior**:
- Shows progress bar for files > 1 MB (download progress may need enhancement)
- Displays transfer speed and completion message
- Verifies SHA256 checksum after transfer (existing infrastructure)
- Creates local directories as needed
- Prompts for confirmation if local file exists (unless `--force`)

**Output Example**:
```
Downloading reports/report.pdf to ./report.pdf
[████████████████████████████████████████] 100%  45.2 MB  15.7 MB/s
Download complete. Checksum verified.
```

---

## User Scenarios & Testing

### User Story 1 - List Remote Files (Priority: P1)

As a CLI user connected to a remote server, I want to list files and directories in the remote storage so that I can see what files are available and navigate the file structure.

**Why this priority**: Listing files is the foundational operation - users need to see what exists before they can download, upload, or manage files.

**Independent Test**: Can be fully tested by connecting to a server with known files and verifying `file ls` returns expected entries. Tests should use the E2E integration test pattern with server/client setup.

**Acceptance Scenarios**:

1. **Given** a connected client with files in server storage root, **When** I run `file ls`, **Then** I see a list of files and directories in the root (output streamed from server).
2. **Given** a connected client, **When** I run `file ls reports/`, **Then** I see contents of the `reports` subdirectory.
3. **Given** a connected client, **When** I run `file ls --long`, **Then** I see detailed information including size and modified date.
4. **Given** a connected client, **When** I run `file ls --recursive`, **Then** I see all files in all subdirectories with relative paths.
5. **Given** a connected client, **When** I run `file ls nonexistent/`, **Then** I see an error "Directory not found: nonexistent/".
6. **Given** a disconnected client, **When** I run `file ls`, **Then** I see an error "Not connected to server. Use 'server connect' first."
7. **Given** a connected client, **When** I run `file ls ../outside`, **Then** the server's `SandboxedFileSystem` rejects the path traversal with an error.

---

### User Story 2 - Upload Files to Server (Priority: P1)

As a CLI user, I want to upload local files to the remote server with progress feedback so that I can transfer data to the server storage.

**Why this priority**: Upload is a core file transfer operation needed for sending data to the server.

**Independent Test**: Can be fully tested by uploading a local file and verifying it exists on the server via `file ls`. Uses existing `FileTransferService` integration test patterns.

**Acceptance Scenarios**:

1. **Given** a connected client and a local file `data.csv`, **When** I run `file upload data.csv`, **Then** the file is uploaded to the storage root using `FileTransferService` and I see progress and completion message.
2. **Given** a connected client, **When** I run `file upload data.csv reports/data.csv`, **Then** the file is uploaded to the `reports` directory.
3. **Given** a connected client and a large file (>1MB), **When** I run `file upload largefile.bin`, **Then** I see a progress bar with percentage and speed (using existing progress infrastructure).
4. **Given** a connected client and an existing remote file, **When** I run `file upload data.csv` (without `--force`), **Then** I am prompted to confirm overwrite.
5. **Given** a connected client and an existing remote file, **When** I run `file upload data.csv --force`, **Then** the file is overwritten without prompting.
6. **Given** a connected client, **When** I run `file upload nonexistent.txt`, **Then** I see an error "Local file not found: nonexistent.txt".
7. **Given** a connected client, **When** upload completes, **Then** I see "Checksum verified" confirming integrity.

---

### User Story 3 - Download Files from Server (Priority: P1)

As a CLI user, I want to download files from the remote server with progress feedback so that I can retrieve data from the server storage.

**Why this priority**: Download is a core file transfer operation needed for retrieving data from the server.

**Independent Test**: Can be fully tested by downloading a known server file (uploaded in test setup) and verifying local content matches.

**Acceptance Scenarios**:

1. **Given** a connected client and a remote file `config.json`, **When** I run `file download config.json`, **Then** the file is downloaded to the current directory using `FileTransferService`.
2. **Given** a connected client, **When** I run `file download reports/data.csv ./local/data.csv`, **Then** the file is downloaded to the specified local path.
3. **Given** a connected client and a large file (>1MB), **When** I run `file download largefile.bin`, **Then** I see a progress bar with percentage and speed.
4. **Given** a connected client and an existing local file, **When** I run `file download data.csv` (without `--force`), **Then** I am prompted to confirm overwrite.
5. **Given** a connected client and an existing local file, **When** I run `file download data.csv --force`, **Then** the local file is overwritten without prompting.
6. **Given** a connected client, **When** I run `file download nonexistent.txt`, **Then** I see an error "Remote file not found: nonexistent.txt".
7. **Given** a connected client, **When** download completes, **Then** I see "Checksum verified" confirming integrity.

---

### User Story 4 - Remove Remote Files (Priority: P2)

As a CLI user, I want to delete files and directories on the remote server with confirmation safeguards so that I can manage storage without accidental data loss.

**Why this priority**: Delete operations are important for storage management but less frequent than list/upload/download.

**Independent Test**: Can be fully tested by uploading a file, deleting it via `file rm`, and verifying it no longer appears in `file ls`.

**Acceptance Scenarios**:

1. **Given** a connected client and a remote file, **When** I run `file rm data.csv`, **Then** I am prompted for confirmation and the file is deleted upon confirming.
2. **Given** a connected client, **When** I run `file rm data.csv --force`, **Then** the file is deleted without prompting.
3. **Given** a connected client and an empty directory, **When** I run `file rm emptydir/`, **Then** the directory is deleted.
4. **Given** a connected client and a non-empty directory, **When** I run `file rm reports/`, **Then** I see an error "Directory not empty. Use --recursive to delete.".
5. **Given** a connected client and a non-empty directory, **When** I run `file rm reports/ --recursive`, **Then** I see confirmation with item count and the directory is deleted upon confirming.
6. **Given** a connected client, **When** I run `file rm nonexistent.txt`, **Then** I see an error "Path not found: nonexistent.txt".
7. **Given** a connected client, **When** I run `file rm ../outside.txt`, **Then** the server's `SandboxedFileSystem` rejects the path traversal.

---

### User Story 5 - Create Remote Directories (Priority: P2)

As a CLI user, I want to create directories on the remote server so that I can organize files before uploading.

**Why this priority**: Directory creation is needed for file organization but less critical than core file operations.

**Independent Test**: Can be fully tested by creating a directory and verifying it appears in `file ls`.

**Acceptance Scenarios**:

1. **Given** a connected client, **When** I run `file mkdir reports`, **Then** the directory is created and I see confirmation.
2. **Given** a connected client, **When** I run `file mkdir reports/2026/q1`, **Then** all nested directories are created.
3. **Given** a connected client and an existing directory, **When** I run `file mkdir reports`, **Then** the command succeeds silently (idempotent).

---

### User Story 6 - View Remote File Contents (Priority: P3)

As a CLI user, I want to view the contents of text files on the remote server without downloading them so that I can quickly inspect configuration or log files.

**Why this priority**: Quick file inspection is convenient but users can alternatively download and view locally.

**Independent Test**: Can be fully tested by viewing a known text file and verifying console output matches content.

**Acceptance Scenarios**:

1. **Given** a connected client and a text file, **When** I run `file cat config.json`, **Then** I see the file contents streamed to my console.
2. **Given** a connected client and a binary file, **When** I run `file cat image.png`, **Then** I see a warning "File appears to be binary. Display anyway? [y/N]".
3. **Given** a connected client and a large text file (>1MB), **When** I run `file cat largefile.log`, **Then** I see truncated output with a warning.
4. **Given** a connected client, **When** I run `file cat nonexistent.txt`, **Then** I see an error "File not found: nonexistent.txt".

---

### User Story 7 - View File/Directory Metadata (Priority: P3)

As a CLI user, I want to see detailed metadata about a file or directory so that I can check size, dates, and other properties.

**Why this priority**: Metadata inspection is useful but less critical; `file ls --long` provides similar information.

**Independent Test**: Can be fully tested by viewing info for a known file and verifying attributes match.

**Acceptance Scenarios**:

1. **Given** a connected client and a file, **When** I run `file info data.csv`, **Then** I see type, size, created date, and modified date.
2. **Given** a connected client and a directory, **When** I run `file info reports/`, **Then** I see type as "Directory" and dates.
3. **Given** a connected client, **When** I run `file info nonexistent`, **Then** I see an error "Path not found: nonexistent".

---

### User Story 8 - Path Autocomplete (Priority: P2)

As a CLI user, I want path arguments to autocomplete so that I can efficiently enter file paths without typing full names.

**Why this priority**: Autocomplete significantly improves usability, especially for remote paths where users can't see the file system. Core infrastructure exists - just needs wiring.

**Independent Test**: Can be tested by connecting to server, typing partial path, pressing Tab, and verifying completions appear.

**Acceptance Scenarios**:

1. **Given** a connected client, **When** I type `file ls doc` and press Tab, **Then** I see completions for remote paths starting with "doc" (e.g., "documents/").
2. **Given** a connected client, **When** I type `file upload ./local` and press Tab, **Then** I see completions for local files starting with "local".
3. **Given** a disconnected client, **When** I type `file download rem` and press Tab, **Then** no remote completions appear (graceful fallback to empty).
4. **Given** a connected client in storage root, **When** I type `file cat ` and press Tab, **Then** I see all files/directories in the remote storage root.

---

### Edge Cases

- **Path traversal attempts**: Server-side `SandboxedFileSystem` rejects paths containing `..` or absolute paths outside storage root with `UnauthorizedAccessException`
- **Connection loss during transfer**: `FileTransferService` handles cleanup; partial files are deleted on server
- **Very long filenames**: Display should truncate or wrap appropriately
- **Special characters in paths**: Paths with spaces, unicode, and special characters should be handled (URL-encoded in HTTP transfers)
- **Empty directories**: `file ls` on empty directory shows informative message
- **Large file transfers**: Operations support files > 2GB using `long` byte counts (existing infrastructure)
- **Cancellation (Ctrl+C)**: In-progress operations cancel gracefully; server cleans up partial files
- **Server not exposing file commands**: Client shows error that command is not available on this server

## Requirements

### Functional Requirements

#### Command Group

- **FR-001**: System MUST provide a `file` command group with server-side commands in `BitPantry.CommandLine.Remote.SignalR.Server` package
- **FR-002**: System MUST provide `file upload` and `file download` commands in `BitPantry.CommandLine.Remote.SignalR.Client` package
- **FR-003**: All `file` commands MUST require an active server connection
- **FR-004**: All `file` commands MUST display "Not connected to server. Use 'server connect' first." when disconnected
- **FR-005**: Server-side commands MUST inject `IFileSystem` which resolves to `SandboxedFileSystem`

#### Default Registration

- **FR-006**: Server package (`BitPantry.CommandLine.Remote.SignalR.Server`) MUST register the `file` group and server-side commands (`file ls`, `file rm`, `file mkdir`, `file cat`, `file info`) by default when command line hub is configured
- **FR-007**: Client package (`BitPantry.CommandLine.Remote.SignalR.Client`) MUST register client-side transfer commands (`file upload`, `file download`) by default when remote client is configured
- **FR-008**: Default registration MUST NOT require additional configuration beyond standard package setup

#### List Command (`file ls`) - Server-Side

- **FR-010**: `file ls` MUST use `IFileSystem.Directory.EnumerateFileSystemEntries()` to list contents
- **FR-011**: `file ls` MUST differentiate directories from files in output (directories end with `/`)
- **FR-012**: `file ls --long` MUST display type, size, modified date, and name columns
- **FR-013**: `file ls --recursive` MUST use `SearchOption.AllDirectories` to list all contents
- **FR-014**: `file ls` MUST return error for non-existent paths

#### Upload Command (`file upload`) - Client-Side

- **FR-020**: `file upload` MUST use existing `FileTransferService.UploadFile()` method
- **FR-021**: `file upload` MUST default remote path to filename in storage root when not specified
- **FR-022**: `file upload` MUST use existing `FileUploadProgressUpdateFunctionRegistry` for progress
- **FR-023**: `file upload` MUST display progress bar for files larger than 1 MB
- **FR-024**: `file upload` MUST display transfer speed and completion message
- **FR-025**: `file upload` MUST show checksum verification result (existing infrastructure)
- **FR-026**: `file upload` MUST prompt for confirmation when remote file exists (unless `--force`)
- **FR-027**: `file upload` MUST return error for non-existent local files

#### Download Command (`file download`) - Client-Side

- **FR-030**: `file download` MUST use existing `FileTransferService.DownloadFile()` method
- **FR-031**: `file download` MUST default local path to current directory with remote filename when not specified
- **FR-032**: `file download` MUST display progress via a callback parameter (requires adding `Func<FileDownloadProgress, Task>` callback to `FileTransferService.DownloadFile()`)
- **FR-033**: `file download` MUST display transfer speed and completion message
- **FR-034**: `file download` MUST show checksum verification result (existing infrastructure)
- **FR-035**: `file download` MUST prompt for confirmation when local file exists (unless `--force`)
- **FR-036**: `file download` MUST return error for non-existent remote files
- **FR-037**: `file download` MUST create local directories as needed for the target path

#### Remove Command (`file rm`) - Server-Side

- **FR-040**: `file rm` MUST use `IFileSystem.File.Delete()` or `IFileSystem.Directory.Delete()`
- **FR-041**: `file rm` MUST prompt for confirmation before deletion (unless `--force`)
- **FR-042**: `file rm --recursive` MUST use `Directory.Delete(path, recursive: true)`
- **FR-043**: `file rm` on non-empty directory without `--recursive` MUST return error
- **FR-044**: `file rm --recursive` MUST display count of deleted items

#### Directory Command (`file mkdir`) - Server-Side

- **FR-050**: `file mkdir` MUST use `IFileSystem.Directory.CreateDirectory()`
- **FR-051**: `file mkdir` MUST create parent directories as needed (built into `CreateDirectory`)
- **FR-052**: `file mkdir` MUST succeed silently if directory already exists

#### Cat Command (`file cat`) - Server-Side

- **FR-060**: `file cat` MUST use `IFileSystem.File.ReadAllText()` or streaming read
- **FR-061**: `file cat` MUST detect binary files (null bytes in first 8KB) and prompt before displaying
- **FR-062**: `file cat` MUST truncate output for files larger than 1 MB with warning

#### Info Command (`file info`) - Server-Side

- **FR-070**: `file info` MUST use `IFileSystem.FileInfo` or `IFileSystem.DirectoryInfo`
- **FR-071**: `file info` MUST display path, type, size, created date, and modified date
- **FR-072**: `file info` MUST distinguish between files and directories

#### Autocomplete

**Context**: The autocomplete infrastructure (005-autocomplete-redesign) is implemented. `FilePathProvider` provides local file path completion as a fallback for any `ArgumentValue`. `RemoteCompletionProvider` exists and calls the server via SignalR. However, the server-side `AddCommandLineHub` does **not** currently register `AddCompletionServices()`, causing server-side completion to return empty results.

##### Infrastructure Enhancement (Required)

- **FR-080**: `AddCommandLineHub` MUST call `AddCompletionServices()` to register `ICompletionOrchestrator` and built-in providers on the server
- **FR-081**: Server-side `FilePathProvider` uses `IFileSystem` which resolves to `SandboxedFileSystem` - path completions will be sandboxed automatically

##### Command Autocomplete Behavior

- **FR-082**: `file upload <local-path>` MUST provide autocomplete for local files (existing `FilePathProvider` fallback behavior - no changes needed)
- **FR-083**: `file download <remote-path>` MUST provide autocomplete for remote paths via `RemoteCompletionProvider`
- **FR-084**: Server-side path arguments (`file ls`, `file rm`, `file cat`, `file info` paths) MUST provide autocomplete via server-side `FilePathProvider` when connected
- **FR-085**: When disconnected from server, remote path completion MUST gracefully return empty (existing `RemoteCompletionProvider` behavior)

#### Security (Inherited from SandboxedFileSystem)

- **FR-090**: All server-side commands inherit `SandboxedFileSystem` path validation
- **FR-091**: Path traversal attempts (`..`) result in `UnauthorizedAccessException` from `SandboxedFileSystem`
- **FR-092**: All operations are confined to server's configured `StorageRootPath`

### Key Entities

- **IFileSystem**: Standard interface from `System.IO.Abstractions` (existing)
- **SandboxedFileSystem**: Server-side implementation confined to `StorageRootPath` (existing)
- **FileTransferService**: Client-side service for file upload/download (existing)
- **FileGroup**: Command group class for the `file` command hierarchy
- **FileListCommand**: Server-side command for `file ls`
- **FileUploadCommand**: Client-side command for `file upload`
- **FileDownloadCommand**: Client-side command for `file download`
- **FileRemoveCommand**: Server-side command for `file rm`
- **FileMkdirCommand**: Server-side command for `file mkdir`
- **FileCatCommand**: Server-side command for `file cat`
- **FileInfoCommand**: Server-side command for `file info`

## Success Criteria

### Measurable Outcomes

- **SC-001**: Users can list, upload, download, and manage remote files through CLI commands within a single session
- **SC-002**: Server-side commands correctly use `SandboxedFileSystem` and reject path traversal attempts
- **SC-003**: File transfers use existing `FileTransferService` with verified checksums
- **SC-004**: All commands provide helpful error messages when operations fail
- **SC-005**: Progress feedback updates at least once per second during file transfers
- **SC-006**: All user stories have corresponding E2E integration tests
- **SC-007**: Remote path arguments show autocomplete when connected to server

## Assumptions

- 001-unified-file-system infrastructure is complete (`SandboxedFileSystem`, `FileTransferService`, path validation)
- 005-autocomplete-redesign is complete (`ICompletionOrchestrator`, `FilePathProvider`, `RemoteCompletionProvider` infrastructure)
- Server-side commands output streams to connected client via existing command execution infrastructure
- `FileTransferService.UploadFile()` and `DownloadFile()` are available and tested
- Binary detection uses simple heuristics (null bytes in first 8KB)

## Dependencies

- **001-unified-file-system**: Provides `SandboxedFileSystem`, `FileTransferService`, path validation
- **005-autocomplete-redesign**: Provides `ICompletionOrchestrator`, `FilePathProvider`, `RemoteCompletionProvider`
- **Existing command infrastructure**: Remote command execution, output streaming to client
- **Existing progress infrastructure**: `FileUploadProgressUpdateFunctionRegistry`

## Documentation

### Documentation Update Requirements

This feature MUST update existing documentation to include the new `file` command group.

#### Update `Docs/Remote/BuiltInCommands.md`

- **DOC-001**: Add `file` command group to the command overview table with all 7 commands
- **DOC-002**: Add individual documentation sections for each command following existing structure:
  - Command class path
  - Description
  - Syntax code block
  - Arguments table (Argument | Alias | Type | Required | Description)
  - Behavior numbered list
  - Examples with code blocks
- **DOC-003**: Server-side commands (`file ls`, `file rm`, `file mkdir`, `file cat`, `file info`) MUST be documented as server-executed commands
- **DOC-004**: Client-side commands (`file upload`, `file download`) MUST be documented as client-side transfer commands
- **DOC-005**: Documentation MUST include the `--force` and `--recursive` flags and their behaviors
- **DOC-006**: Documentation MUST include example output for each command
- **DOC-007**: Add cross-reference to `FileSystem.md` and `FileSystemConfiguration.md` in "See Also" section

#### Documentation Structure Example

Each command section should follow the pattern established in `BuiltInCommands.md`:

```markdown
### `file ls` - List Remote Files

**Class**: `BitPantry.CommandLine.Remote.SignalR.Server.Commands.FileListCommand`

Lists files and directories in the remote server's storage.

#### Syntax

\`\`\`
file ls [path] [--recursive|-r] [--long|-l]
\`\`\`

#### Arguments

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `path` | - | `string` | No | Directory path to list. Defaults to storage root |
| `--recursive` | `-r` | `Switch` | No | List contents recursively |
| `--long` | `-l` | `Switch` | No | Show detailed information |

#### Behavior

1. Executes on the remote server
2. Output streams back to connected client console
3. Directories are displayed with trailing `/`
4. ...

#### Examples

\`\`\`
> file ls
documents/
config.json

> file ls --long
Type  Size       Modified             Name
...
\`\`\`
```
