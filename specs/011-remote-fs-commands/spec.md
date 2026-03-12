# Feature Specification: Remote File System Management Commands

**Feature Branch**: `011-remote-fs-commands`
**Created**: 2026-03-10
**Status**: Draft
**Input**: User description: "Build remote file system management commands for the SignalR client library. Commands auto-configured when client is configured. Include full command syntax, autocomplete, Spectre Console output, error handling, and documentation updates."

## Overview

This feature adds a set of standard file system management commands to the remote SignalR stack, enabling users to browse, inspect, organize, and manipulate files and directories on the connected remote server directly from the client shell. These commands complement the existing `server upload` and `server download` commands, completing the remote file management experience.

All commands live under the `server` command group in the SignalR server library and are registered in the server hub configuration so they appear as remote command stubs on connected clients. Each command operates exclusively on the remote server's sandboxed file system.

## Clarifications

### Session 2026-03-06

- Q: Should `server cat` retrieve file contents via a new SignalR RPC or reuse the existing HTTP download endpoint? → A: Reuse the existing HTTP download endpoint (`/filedownload`) with optional query parameters for head/tail line limits. Content streams via HTTP, consistent with file download architecture.
- Q: Should `server rm` with glob patterns confirm before deleting matched files? → A: Confirm when a glob matches more than a threshold number of files (e.g., >5). Show match count and prompt before proceeding. `-f` skips the prompt. Matches at or below the threshold proceed without confirmation.
- Q: Should `server mv`, `server cp`, and `server rm -r` be single atomic server-side RPCs or client-orchestrated multi-step operations? → A: Single atomic server-side RPC per operation. The server executes move/copy/delete locally using its file system. Client sends one request, gets one response. This avoids partial-failure states.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse Remote File System (Priority: P1)

As a user connected to a remote server, I want to list files and directories on the server so that I can understand what content exists and navigate the remote file structure.

**Why this priority**: Browsing is the most fundamental file system operation — users need to see what's there before they can act on it. This is a prerequisite for productive use of all other commands.

**Independent Test**: Can be fully tested by connecting to a server with known files and verifying the list output matches expected contents.

**Acceptance Scenarios**:

1. **Given** a connected client, **When** I run `server ls`, **Then** I see a formatted listing of files and directories at the server storage root.
2. **Given** a connected client, **When** I run `server ls reports`, **Then** I see the contents of the specified remote directory.
3. **Given** a connected client with files on the server, **When** I run `server ls -l`, **Then** I see a detailed listing including file sizes, last modified dates, and entry type indicators.
4. **Given** a connected client, **When** I run `server ls *.txt`, **Then** I see only files matching the glob pattern.
5. **Given** a connected client, **When** I run `server ls --recursive`, **Then** I see a recursive listing including subdirectory contents.
6. **Given** a connected client, **When** I run `server ls /nonexistent`, **Then** I see a clear error message indicating the directory does not exist.

---

### User Story 2 - Create Remote Directories (Priority: P1)

As a user connected to a remote server, I want to create directories on the server so that I can organize files before uploading or as part of my workflow.

**Why this priority**: Directory creation is a prerequisite for file organization. Users need to create folder structures before uploading files into them.

**Independent Test**: Can be fully tested by creating a directory and verifying it appears in subsequent `server ls` output.

**Acceptance Scenarios**:

1. **Given** a connected client, **When** I run `server mkdir reports`, **Then** a `reports` directory is created on the server at the storage root.
2. **Given** a connected client, **When** I run `server mkdir -p reports/2025/q1`, **Then** the full nested directory path is created, including all intermediate directories.
3. **Given** a connected client, **When** I run `server mkdir` for a directory that already exists, **Then** I see a message indicating the directory already exists (not an error unless `-p` is not used on a nested path).
4. **Given** a connected client, **When** I run `server mkdir` with an invalid name (e.g., containing prohibited characters), **Then** I see an error describing the naming constraint.

---

### User Story 3 - Remove Remote Files and Directories (Priority: P1)

As a user connected to a remote server, I want to delete files and directories so that I can clean up old or unwanted content.

**Why this priority**: Cleanup is a core file management operation. Without delete, users must rely on server admin intervention to manage storage.

**Independent Test**: Can be fully tested by creating files/directories, deleting them, and verifying they no longer appear in listings.

**Acceptance Scenarios**:

1. **Given** a connected client with a file on the server, **When** I run `server rm report.txt`, **Then** the file is deleted from the server.
2. **Given** a connected client, **When** I run `server rm *.log`, **Then** all files matching the glob pattern are deleted and a summary is displayed.
3. **Given** a connected client with an empty directory, **When** I run `server rm -d empty-dir`, **Then** the empty directory is removed.
4. **Given** a connected client with a non-empty directory, **When** I run `server rm -r old-reports`, **Then** the directory and all its contents are recursively deleted.
5. **Given** a connected client, **When** I run `server rm` on a non-empty directory without `-r`, **Then** I see an error indicating the directory is not empty and suggesting the `-r` flag.
6. **Given** a connected client, **When** I run `server rm important-data -r`, **Then** I am prompted with a confirmation before deletion proceeds (unless `--force` / `-f` is specified).
7. **Given** a connected client, **When** I run `server rm nonexistent.txt`, **Then** I see an error that the file does not exist.

---

### User Story 4 - Move and Rename Remote Files (Priority: P2)

As a user connected to a remote server, I want to move or rename files and directories so that I can reorganize the remote file structure.

**Why this priority**: Renaming and moving are important for organization but less frequent than browsing, creating, or deleting.

**Independent Test**: Can be fully tested by moving a file and verifying it appears at the destination and is removed from the source.

**Acceptance Scenarios**:

1. **Given** a connected client with a file `data.txt` on the server, **When** I run `server mv data.txt archive/data.txt`, **Then** the file is moved to the `archive` directory.
2. **Given** a connected client with a file `old-name.txt`, **When** I run `server mv old-name.txt new-name.txt`, **Then** the file is renamed in place.
3. **Given** a connected client with a directory `temp`, **When** I run `server mv temp archive/temp`, **Then** the directory and all contents are moved.
4. **Given** a connected client, **When** I run `server mv a.txt b.txt` and `b.txt` already exists, **Then** I see an error indicating the destination exists (unless `--force` / `-f` is specified to overwrite).
5. **Given** a connected client, **When** I run `server mv nonexistent.txt dest.txt`, **Then** I see an error that the source does not exist.

---

### User Story 5 - Copy Remote Files (Priority: P2)

As a user connected to a remote server, I want to copy files and directories on the server so that I can duplicate content without downloading and re-uploading.

**Why this priority**: Server-side copy avoids round-trip data transfer. Useful but less common than basic operations.

**Independent Test**: Can be fully tested by copying a file and verifying both source and destination exist with matching content.

**Acceptance Scenarios**:

1. **Given** a connected client with a file `config.json`, **When** I run `server cp config.json config-backup.json`, **Then** a copy is created and both files exist.
2. **Given** a connected client with a directory `templates`, **When** I run `server cp -r templates templates-backup`, **Then** the directory and all contents are copied recursively.
3. **Given** a connected client, **When** I run `server cp a.txt b.txt` and `b.txt` already exists, **Then** I see an error indicating the destination exists (unless `--force` / `-f` is specified).
4. **Given** a connected client, **When** I run `server cp nonexistent.txt dest.txt`, **Then** I see an error that the source does not exist.
5. **Given** a connected client, **When** I run `server cp largedir/ backup/ -r`, **Then** progress feedback is shown for large copy operations.

---

### User Story 6 - View Remote File Contents (Priority: P2)

As a user connected to a remote server, I want to view the contents of a text file on the server so that I can inspect configuration files, logs, or data without downloading them.

**Why this priority**: Quick file inspection is a common need during troubleshooting and verification, avoiding full download for quick checks.

**Independent Test**: Can be fully tested by viewing a file with known contents and verifying the output matches.

**Acceptance Scenarios**:

1. **Given** a connected client with a text file on the server, **When** I run `server cat config.json`, **Then** the file contents are displayed in the console.
2. **Given** a connected client with a large text file, **When** I run `server cat largefile.log -n 50`, **Then** only the first 50 lines are displayed.
3. **Given** a connected client with a large text file, **When** I run `server cat largefile.log --tail 20`, **Then** the last 20 lines are displayed.
4. **Given** a connected client, **When** I run `server cat binary.exe`, **Then** I see a warning that the file appears to be binary and asking to confirm (or display is refused).
5. **Given** a connected client, **When** I run `server cat nonexistent.txt`, **Then** I see an error that the file does not exist.
6. **Given** a file that exceeds the large-file threshold (>1MB), **When** I run `server cat hugefile.log` without line-limiting flags, **Then** I am warned about the file size and prompted to confirm or use `-n` / `--tail`.

---

### User Story 7 - Inspect Remote File Information (Priority: P3)

As a user connected to a remote server, I want to see detailed metadata about a specific file or directory so that I can check sizes, timestamps, and attributes.

**Why this priority**: Useful for debugging and verification, but most metadata is visible in `ls -l` output. This provides a deeper single-item view.

**Independent Test**: Can be fully tested by inspecting a file with known properties and verifying the reported metadata.

**Acceptance Scenarios**:

1. **Given** a connected client with a file on the server, **When** I run `server stat report.txt`, **Then** I see detailed metadata: full path, size, created date, last modified date, and type (file).
2. **Given** a connected client with a directory on the server, **When** I run `server stat reports/`, **Then** I see directory metadata: full path, item count, total size, created date, last modified date, and type (directory).
3. **Given** a connected client, **When** I run `server stat nonexistent`, **Then** I see an error that the path does not exist.

---

### User Story 8 - Documentation Updated (Priority: P1)

As a developer or user, I want all new file system commands to be documented so that I can discover and learn command syntax, options, and behavior.

**Why this priority**: Documentation is essential for discoverability and adoption. New users and contributors need reference material.

**Independent Test**: Can be verified by reviewing documentation and confirming each command has complete coverage.

**Acceptance Scenarios**:

1. **Given** the completion of this feature, **When** a user reads the project documentation, **Then** each new command is documented with syntax, options, examples, and expected behavior.
2. **Given** any new command, **When** a user runs `server <command> --help`, **Then** inline help text accurately describes the command, all arguments, all flags, and usage examples.
3. **Given** the README or docs, **When** a developer reviews the file system commands section, **Then** there is a command reference table summarizing all available commands.

---

### Edge Cases

- What happens when the user invokes a remote command while disconnected? The command should fail fast with a clear "Not connected" message.
- What happens when the user targets the storage root for deletion (`server rm -r /`)? This should be blocked with a specific safety error.
- What happens when a glob pattern matches nothing? A clear "no matches found" message should be shown, not a silent success.
- What happens when `server cat` is used on an extremely large file? Size thresholds and confirmation prompts should protect the user.

---

## Command Reference

This section defines the full syntax, expected behavior, error handling, and user experience for each command.

---

### Command: `server ls`

**Purpose**: List files and directories on the remote server.

#### Syntax

```
server ls [path] [--long|-l] [--recursive] [--sort <field>] [--reverse]

Arguments:
  path                 Remote path or glob pattern to list (default: server storage root)

Options:
  -l, --long           Display detailed listing with sizes, dates, and type indicators
  --recursive          List contents recursively, including subdirectories
  --sort <field>       Sort results by: name (default), size, modified
  --reverse            Reverse the sort order
  -h, --help           Show help for this command
```

#### Expected Behavior

| Scenario                          | Behavior                                                              |
|-----------------------------------|-----------------------------------------------------------------------|
| No arguments                      | Lists contents of the server storage root                             |
| Path to directory                 | Lists contents of specified directory                                 |
| Path to file                      | Shows just that file's entry                                          |
| Glob pattern (e.g., `*.txt`)      | Lists only entries matching the pattern                               |
| `--long` flag                     | Shows table with columns: Type, Name, Size, Last Modified             |
| `--recursive` flag                | Recursively lists subdirectories with indented/hierarchical display   |
| Empty directory                   | Displays "(empty)" message                                            |
| Multiple flags (`-l --recursive`) | Recursive detailed listing (each flag/option must be specified as a separate token; combined alias syntax like `-lR` is not supported by the command parser) |

#### Output / User Experience

**Default (short) listing:**
```
  documents/
  images/
  config.json
  readme.txt
```
- Directories shown with trailing `/` indicator
- Directories listed first, then files
- Entries sorted alphabetically by name (default)
- Uses Spectre Console markup: directories in one color, files in another

**Long listing (`-l`):**
```
  Type  Name              Size       Last Modified
  ───── ────────────────  ────────── ─────────────────────
  📁    documents/        -          2026-02-15 10:30 UTC
  📁    images/           -          2026-01-20 08:15 UTC
  📄    config.json       2.4 KB     2026-03-01 14:22 UTC
  📄    readme.txt        512 B      2026-02-28 09:00 UTC
```
- Uses Spectre Console `Table` for formatted output
- File sizes displayed in human-readable format (B, KB, MB, GB)
- Type column uses visual indicators (folder vs. file icons or symbols)
- Dates displayed in UTC with clear formatting

**Recursive listing (`--recursive`):**
```
  ./
    documents/
      report.txt
      summary.pdf
    images/
      logo.png
    config.json
```

#### Autocomplete Spec

| Argument/Option      | Autocomplete Provider                       | Behavior                              |
|----------------------|---------------------------------------------|---------------------------------------|
| `path` (position 0)  | `ServerDirectoryPathAutoComplete` + files   | Suggests remote directories and files |
| `--sort`             | Static values: `name`, `size`, `modified`   | Suggests valid sort field names       |

#### Error Handling

| Condition                       | Error message                                                                 |
|---------------------------------|-------------------------------------------------------------------------------|
| Not connected to server         | "Not connected to a server. Use `server connect` to establish a connection."  |
| Directory does not exist        | "Directory not found: `{path}`"                                               |
| Permission denied (traversal)   | "Access denied: the specified path is outside the allowed storage area."      |
| Connection lost during listing  | "Connection to server lost. Please reconnect and try again."                  |
| Glob pattern matches nothing    | "No files or directories match the pattern: `{pattern}`"                      |

---

### Command: `server mkdir`

**Purpose**: Create directories on the remote server.

#### Syntax

```
server mkdir <path> [--parents|-p]

Arguments:
  path                 Path of the directory to create (required)

Options:
  -p, --parents        Create intermediate parent directories as needed; no error if directory exists
  -h, --help           Show help for this command
```

#### Expected Behavior

| Scenario                          | Behavior                                                              |
|-----------------------------------|-----------------------------------------------------------------------|
| Simple directory name             | Creates directory at the server storage root                          |
| Nested path without `-p`          | Fails if any intermediate directories do not exist                    |
| Nested path with `-p`             | Creates all intermediate directories as needed                        |
| Directory already exists (no -p)  | Reports an error that the directory already exists                    |
| Directory already exists with -p  | Succeeds silently (no error, no action)                               |
| Multiple paths                    | Creates each specified directory                                      |

#### Output / User Experience

```
> server mkdir reports
Created directory: /reports

> server mkdir -p archive/2025/q1/data
Created directory: /archive/2025/q1/data
```
- Confirmation message showing the absolute path of the created directory
- With `-p`, only the final confirmation is shown (no output for each intermediate directory)

#### Autocomplete Spec

| Argument/Option     | Autocomplete Provider              | Behavior                                                        |
|---------------------|------------------------------------|-----------------------------------------------------------------|
| `path` (position 0) | `ServerDirectoryPathAutoComplete`  | Suggests existing remote directories for path prefix completion |

#### Error Handling

| Condition                                  | Error message                                                                      |
|--------------------------------------------|------------------------------------------------------------------------------------|
| Not connected to server                    | "Not connected to a server. Use `server connect` to establish a connection."       |
| Parent directory does not exist (no `-p`)  | "Parent directory not found: `{parent}`. Use `-p` to create parent directories."  |
| Directory already exists (no `-p`)         | "Directory already exists: `{path}`"                                               |
| Invalid directory name                     | "Invalid directory name: `{name}`. Directory names cannot contain: {characters}"  |
| Permission denied (traversal)              | "Access denied: the specified path is outside the allowed storage area."           |

---

### Command: `server rm`

**Purpose**: Remove files and directories from the remote server.

#### Syntax

```
server rm <path> [--recursive|-r] [--directory|-d] [--force|-f]

Arguments:
  path                 Remote file/directory path or glob pattern to remove (required)

Options:
  -r, --recursive      Remove directories and their contents recursively
  -d, --directory      Remove empty directories
  -f, --force          Skip confirmation prompts
  -h, --help           Show help for this command
```

#### Expected Behavior

| Scenario                                | Behavior                                                                         |
|-----------------------------------------|----------------------------------------------------------------------------------|
| Single file path                        | Deletes the file                                                                 |
| Glob pattern (`*.log`)                  | Server resolves the glob pattern and deletes each matched file individually. If matches exceed a threshold (e.g., >5), prompts for confirmation before proceeding (unless `-f` is specified). |
| Empty directory with `-d`               | Removes the empty directory                                                      |
| Non-empty directory with `-r`           | Recursively deletes directory and all contents after confirmation                |
| Non-empty directory without `-r`        | Error: directory is not empty                                                    |
| Root path (`/`) with `-r`               | Blocked: safety error preventing accidental deletion of all server content       |
| Recursive delete without `-f`           | Prompts for confirmation showing item count before proceeding                    |
| Recursive delete with `-f`              | Proceeds without confirmation                                                    |

#### Output / User Experience

**Single file:**
```
> server rm old-report.txt
Removed: old-report.txt
```

**Glob pattern (above confirmation threshold):**
```
> server rm *.log
Found 12 files matching '*.log'.
This will permanently delete 12 files. Are you sure? [y/N]: y
Removing 12 files...
  ✓ access.log
  ✓ error.log
  ...
Removed 12 files.
```

**Glob pattern (at or below threshold):**
```
> server rm *.log
Found 3 files matching '*.log'.
Removing 3 files...
  ✓ access.log
  ✓ error.log
  ✓ debug.log
Removed 3 files.
```

**Recursive directory with confirmation:**
```
> server rm old-reports -r
This will permanently delete the directory 'old-reports' and all 47 items inside it.
Are you sure? [y/N]: y
Removing old-reports/...
Removed 47 items from old-reports/.
```

- Uses Spectre Console for confirmation prompts
- Displays item count before confirming recursive deletion
- Shows per-item status for multi-file operations (checkmarks for success)
- Summary line at the end of batch operations

#### Autocomplete Spec

| Argument/Option     | Autocomplete Provider         | Behavior                              |
|---------------------|-------------------------------|---------------------------------------|
| `path` (position 0) | `ServerFilePathAutoComplete`  | Suggests remote files and directories |

#### Error Handling

| Condition                                 | Error message                                                                         |
|-------------------------------------------|---------------------------------------------------------------------------------------|
| Not connected to server                   | "Not connected to a server. Use `server connect` to establish a connection."          |
| File does not exist                       | "File not found: `{path}`"                                                            |
| Directory not empty (no `-r`)             | "Directory `{path}` is not empty. Use `-r` to delete recursively."                    |
| Attempt to delete storage root            | "Cannot delete the server storage root directory."                                    |
| Permission denied (traversal)             | "Access denied: the specified path is outside the allowed storage area."              |
| Glob pattern matches nothing              | "No files or directories match the pattern: `{pattern}`"                              |
| Connection lost during deletion           | "Connection to server lost during deletion. Some items may have been deleted. Please reconnect and verify." |

---

### Command: `server mv`

**Purpose**: Move or rename files and directories on the remote server.

#### Syntax

```
server mv <source> <destination> [--force|-f]

Arguments:
  source               Remote path of the file or directory to move (required)
  destination          Remote destination path (required)

Options:
  -f, --force          Overwrite destination if it already exists
  -h, --help           Show help for this command
```

#### Expected Behavior

| Scenario                              | Behavior                                                             |
|---------------------------------------|----------------------------------------------------------------------|
| Rename file (same directory)          | Renames the file in place                                            |
| Move file to directory                | Moves file into the target directory                                 |
| Rename directory                      | Renames the directory in place                                       |
| Move directory into another           | Moves directory (and contents) to new location                       |
| Destination exists (no `--force`)     | Error: destination already exists                                    |
| Destination exists with `--force`     | Overwrites/replaces the destination                                  |
| Move into non-existent directory      | Error: destination directory does not exist                          |

#### Output / User Experience

```
> server mv report.txt archive/report.txt
Moved: report.txt → archive/report.txt

> server mv old-name.txt new-name.txt
Renamed: old-name.txt → new-name.txt
```
- Output indicates whether the operation was a move or rename based on path comparison
- Uses arrow (`→`) to clearly show source and destination

#### Autocomplete Spec

| Argument/Option            | Autocomplete Provider         | Behavior                              |
|----------------------------|-------------------------------|---------------------------------------|
| `source` (position 0)      | `ServerFilePathAutoComplete`  | Suggests remote files and directories |
| `destination` (position 1) | `ServerFilePathAutoComplete`  | Suggests remote files and directories |

#### Error Handling

| Condition                                  | Error message                                                                      |
|--------------------------------------------|------------------------------------------------------------------------------------|
| Not connected to server                    | "Not connected to a server. Use `server connect` to establish a connection."       |
| Source does not exist                      | "Source not found: `{source}`"                                                     |
| Destination already exists (no `--force`)  | "Destination already exists: `{destination}`. Use `-f` to overwrite."             |
| Destination directory does not exist       | "Destination directory not found: `{directory}`"                                   |
| Source and destination are the same        | "Source and destination are the same path."                                        |
| Permission denied (traversal)              | "Access denied: the specified path is outside the allowed storage area."           |

---

### Command: `server cp`

**Purpose**: Copy files and directories on the remote server.

#### Syntax

```
server cp <source> <destination> [--recursive|-r] [--force|-f]

Arguments:
  source               Remote path of the file or directory to copy (required)
  destination          Remote destination path (required)

Options:
  -r, --recursive      Copy directories recursively
  -f, --force          Overwrite destination if it already exists
  -h, --help           Show help for this command
```

#### Expected Behavior

| Scenario                              | Behavior                                                               |
|---------------------------------------|------------------------------------------------------------------------|
| Copy file to new name                 | Creates a copy with the new name                                       |
| Copy file into directory              | Copies file into the target directory                                  |
| Copy directory with `-r`              | Recursively copies directory and all contents                          |
| Copy directory without `-r`           | Error: source is a directory                                           |
| Destination exists (no `--force`)     | Error: destination already exists                                      |
| Destination exists with `--force`     | Overwrites the destination                                             |
| Large recursive copy                  | Shows progress indication via Spectre Console                          |

#### Output / User Experience

**Single file:**
```
> server cp config.json config-backup.json
Copied: config.json → config-backup.json
```

**Recursive directory:**
```
> server cp -r templates templates-backup
Copying templates/ → templates-backup/...
  Copied 23 items.
```

- For recursive copies of many files (more than a small threshold), a progress line shows item count
- Uses arrow (`→`) for clarity

#### Autocomplete Spec

| Argument/Option            | Autocomplete Provider         | Behavior                              |
|----------------------------|-------------------------------|---------------------------------------|
| `source` (position 0)      | `ServerFilePathAutoComplete`  | Suggests remote files and directories |
| `destination` (position 1) | `ServerFilePathAutoComplete`  | Suggests remote files and directories |

#### Error Handling

| Condition                                  | Error message                                                                      |
|--------------------------------------------|------------------------------------------------------------------------------------|
| Not connected to server                    | "Not connected to a server. Use `server connect` to establish a connection."       |
| Source does not exist                      | "Source not found: `{source}`"                                                     |
| Source is directory (no `-r`)              | "`{source}` is a directory. Use `-r` to copy recursively."                         |
| Destination already exists (no `--force`)  | "Destination already exists: `{destination}`. Use `-f` to overwrite."             |
| Destination directory does not exist       | "Destination directory not found: `{directory}`"                                   |
| Insufficient disk space on server          | "Insufficient disk space on the server to complete the copy."                      |
| Permission denied (traversal)              | "Access denied: the specified path is outside the allowed storage area."           |

---

### Command: `server cat`

**Purpose**: Display the contents of a remote text file.

#### Syntax

```
server cat <path> [--lines|-n <count>] [--tail|-t <count>]

Arguments:
  path                 Remote file path to display (required)

Options:
  -n, --lines <count>  Display only the first <count> lines
  -t, --tail <count>   Display only the last <count> lines
  -h, --help           Show help for this command
```

#### Expected Behavior

| Scenario                            | Behavior                                                                  |
|-------------------------------------|---------------------------------------------------------------------------|
| Text file                           | Displays full file contents to console                                    |
| `--lines 50`                        | Displays only the first 50 lines                                          |
| `--tail 20`                         | Displays only the last 20 lines                                           |
| Both `--lines` and `--tail`         | Error: these options are mutually exclusive                               |
| Binary file detection               | Warns user the file appears binary; prompts for confirmation              |
| File exceeds display size limit     | Warns about large file size; prompts to use `--lines`/`--tail` or confirm |
| Empty file                          | Displays "(empty file)" message                                           |

#### Output / User Experience

```
> server cat config.json
{
  "setting": "value",
  "debug": false
}

> server cat access.log -n 5
[2026-03-01 10:00:00] GET /api/status 200
[2026-03-01 10:00:01] POST /api/data 201
[2026-03-01 10:00:02] GET /api/users 200
[2026-03-01 10:00:03] DELETE /api/cache 204
[2026-03-01 10:00:04] GET /api/health 200
(showing first 5 of 10,432 lines)
```
- File contents displayed as-is to the console
- When using `--lines` or `--tail`, an informational footer shows context (e.g., total line count)
- Binary file detection based on scanning initial bytes for null characters
- Size limit for full display: 1 MB. Files above this trigger a confirmation prompt.
- Content is streamed via the existing HTTP download endpoint (`/filedownload`) with optional query parameters for line limiting (head/tail), consistent with the file transfer architecture.

#### Autocomplete Spec

| Argument/Option     | Autocomplete Provider         | Behavior                             |
|---------------------|-------------------------------|--------------------------------------|
| `path` (position 0) | `ServerFilePathAutoComplete`  | Suggests remote files only (no dirs) |

#### Error Handling

| Condition                           | Error message                                                                      |
|-------------------------------------|------------------------------------------------------------------------------------|
| Not connected to server             | "Not connected to a server. Use `server connect` to establish a connection."       |
| File does not exist                 | "File not found: `{path}`"                                                         |
| Path is a directory                 | "`{path}` is a directory, not a file."                                             |
| Both `--lines` and `--tail` used    | "Options `--lines` and `--tail` are mutually exclusive."                           |
| Binary file detected                | "File `{path}` appears to be a binary file. Display anyway? [y/N]"                |
| File exceeds size limit             | "File `{path}` is {size}. Display full contents? Use `--lines` or `--tail` to limit. [y/N]" |
| Permission denied (traversal)       | "Access denied: the specified path is outside the allowed storage area."           |

---

### Command: `server stat`

**Purpose**: Display detailed metadata for a remote file or directory.

#### Syntax

```
server stat <path>

Arguments:
  path                 Remote file or directory path to inspect (required)

Options:
  -h, --help           Show help for this command
```

#### Expected Behavior

| Scenario         | Behavior                                                        |
|------------------|-----------------------------------------------------------------|
| File path        | Displays file metadata (size, dates, type)                      |
| Directory path   | Displays directory metadata (item count, total size, dates)     |

#### Output / User Experience

**File:**
```
> server stat config.json
  Path:          /reports/config.json
  Type:          File
  Size:          2.4 KB (2,458 bytes)
  Created:       2026-01-15 09:30:00 UTC
  Last Modified: 2026-03-01 14:22:00 UTC
```

**Directory:**
```
> server stat reports/
  Path:          /reports
  Type:          Directory
  Items:         47 (12 directories, 35 files)
  Total Size:    156.3 MB
  Created:       2025-11-01 08:00:00 UTC
  Last Modified: 2026-03-05 16:45:00 UTC
```

- Uses Spectre Console formatting for aligned label-value display
- File sizes in human-readable format with exact byte count in parentheses
- Directory total size includes all nested contents

#### Autocomplete Spec

| Argument/Option     | Autocomplete Provider         | Behavior                              |
|---------------------|-------------------------------|---------------------------------------|
| `path` (position 0) | `ServerFilePathAutoComplete`  | Suggests remote files and directories |

#### Error Handling

| Condition                     | Error message                                                                      |
|-------------------------------|------------------------------------------------------------------------------------|
| Not connected to server       | "Not connected to a server. Use `server connect` to establish a connection."       |
| Path does not exist           | "Path not found: `{path}`"                                                         |
| Permission denied (traversal) | "Access denied: the specified path is outside the allowed storage area."           |

---

## Cross-Cutting Specifications

### Path Arguments

All path arguments are relative to the server storage root. Users specify paths directly (e.g., `server ls reports/2025`). There is no client-side working directory concept — all paths are either absolute (from storage root) or provided as-is and resolved server-side.

### Connection Prerequisite

All commands in this feature require an active server connection. Every command MUST:
1. Check connection state before executing
2. Display the standard "Not connected" message if disconnected
3. Handle mid-operation disconnection gracefully

### Glob Pattern Support

Commands that accept glob patterns (`server ls`, `server rm`) MUST:
1. Support `*` (any characters in filename), `?` (single character), and `**` (recursive directory matching)
2. Use the existing `GlobPatternHelper` infrastructure for pattern parsing and validation
3. Resolve glob matches server-side inside command execution using the server filesystem and globbing infrastructure, then apply command-specific behavior to the resolved entries.
4. Display "no matches" messages when patterns match nothing (never silent failure)

### Path Resolution

All path arguments MUST:
1. Support paths relative to the server storage root
2. Support `..` for parent navigation (resolved and validated server-side)
3. Be validated server-side against the sandbox boundary
4. Use forward slashes as the path separator in display and communication

### Confirmation Prompts

Destructive operations (`server rm -r`, `server mv --force`, `server cp --force`) MUST:
1. Prompt for user confirmation before proceeding (unless `--force`/`-f` is specified)
2. Use Spectre Console prompt components for consistent UX
3. Default to "No" (safe default) on confirmation prompts

### Auto-Registration

All new commands MUST:
1. Be placed in the SignalR server library project under the appropriate command group namespace
2. Be registered in `AddCommandLineHub()` so they appear as remote stubs to connected clients
3. Require no manual registration steps by consuming applications
4. Be decorated with appropriate `[InGroup<ServerGroup>]` attributes

### Remote Execution Model

This feature uses the existing remote command execution pipeline (`RunRequest` / `RunResponse`) and introduces no new RPC message types.

Each command MUST:
1. Execute atomically on the server via command invocation (not client-orchestrated multi-step RPC sequences)
2. Use server-side sandboxed file system enforcement
3. Return one command result payload to the client shell

`server cat` continues to use the existing HTTP download endpoint (`/filedownload`) for content transfer semantics where appropriate.

### Documentation Requirements

The following documentation MUST be updated with this increment:
1. **README.md** — Add a "Remote File System Commands" section with a command reference table
2. **Inline help** — Every command, argument, and option MUST have `[Description]` attributes providing clear, concise help text
3. **CLAUDE.md** — Update the development guidelines with the new commands and any new patterns introduced
4. **Command examples** — Each command's documentation should include at least 2 usage examples

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide `server ls` to list files and directories on the remote server with support for glob patterns, detailed output mode, recursive listing, and configurable sort order.
- **FR-002**: System MUST provide `server mkdir` to create directories on the remote server, with optional parent directory creation via `-p` flag.
- **FR-003**: System MUST provide `server rm` to remove files and directories from the remote server, supporting glob patterns, recursive deletion with confirmation, and empty directory removal.
- **FR-004**: System MUST provide `server mv` to move or rename files and directories on the remote server.
- **FR-005**: System MUST provide `server cp` to copy files and directories on the remote server, with recursive copy support for directories.
- **FR-006**: System MUST provide `server cat` to display the contents of a remote text file, with line-limiting options and binary file detection.
- **FR-007**: System MUST provide `server stat` to display detailed metadata (size, dates, type, item count) for a remote file or directory.
- **FR-008**: All commands MUST require an active server connection and display a clear message if disconnected.
- **FR-009**: All commands MUST auto-register when the SignalR server hub is configured (`AddCommandLineHub`) and be available to connected clients without manual setup.
- **FR-011**: All destructive operations MUST prompt for user confirmation unless `--force`/`-f` is specified, defaulting to "No."
- **FR-012**: All server-side file operations MUST enforce path sandboxing to prevent access outside the configured storage root.
- **FR-013**: All commands MUST display errors using consistent, actionable error messages that guide the user toward resolution.
- **FR-014**: All commands MUST use Spectre Console for formatted output (tables, markup, progress indicators, prompts).
- **FR-015**: System documentation (README, inline help, CLAUDE.md) MUST be updated to reflect all new commands and their usage.
- **FR-016**: Glob pattern operations MUST display a clear message when no entries match (never fail silently).
- **FR-017**: `server rm` MUST block deletion of the server storage root directory as a safety measure.
- **FR-018**: `server cat` MUST detect binary files and files exceeding a size threshold, prompting the user before displaying.
- **FR-019**: Glob-based commands (`server ls`, `server rm`) MUST resolve matches server-side and apply deterministic behavior to each matched entry.

### Key Entities

- **Remote File Entry**: Represents a file or directory on the server. Attributes: path, name, type (file/directory), size, created date, last modified date.
- **RPC File System Operation**: A request/response pair sent over the SignalR connection to perform a server-side file system action. Each operation includes path validation and sandbox enforcement.

## Assumptions

- Server-side file operations can reuse `System.IO.Abstractions` (`IFileSystem`) already in use on the server for sandboxed operations.
- The Spectre Console `IAnsiConsole` is available via DI in all commands (consistent with existing command infrastructure).
- Binary file detection for `server cat` uses a heuristic scan of the first N bytes for null characters — an exact detection method is an implementation detail.
- Performance thresholds (e.g., progress display for long copy operations, cat size limit) follow the patterns established by upload/download commands (25MB progress threshold) but can be tuned per-command as appropriate.
- File locking behavior depends on the server OS and file system; errors from locked files are surfaced as-is from the server.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can perform all standard file management tasks (list, create, delete, move, copy, view, inspect) on the remote server without requiring direct server access or external tools.
- **SC-003**: Users receive clear, actionable error messages for every failure condition, including guidance on how to resolve the issue.
- **SC-004**: Destructive operations require explicit confirmation, preventing accidental data loss in 100% of recursive delete and overwrite scenarios (unless `--force` is specified).
- **SC-005**: All new commands are automatically available to connected clients when the SignalR server hub is configured — zero additional setup steps for consuming applications.
- **SC-006**: `server <command> --help` provides complete documentation for every new command, including all arguments, options, and usage examples.
- **SC-007**: All file system operations enforce server-side path sandboxing, preventing any access outside the configured storage root.
- **SC-008**: Project documentation (README, CLAUDE.md) is updated and accurately reflects all new commands and their usage.
