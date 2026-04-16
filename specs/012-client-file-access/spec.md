# Feature Specification: Client File Access

**Spec**: `012-client-file-access`
**Created**: 2026-04-15
**Status**: Draft

## Overview

This feature introduces a location-transparent file access service (`IClientFileAccess`) that allows commands to read and write files on the calling user's machine, regardless of whether the command runs locally or on a remote server. When running locally, operations go directly to the file system. When running on the server, the service coordinates file transfers over the existing SignalR/HTTP infrastructure invisibly. This eliminates the current two-step workflow of manually uploading/downloading files when a remote command needs client-side data.

## Clarifications

### Session 2026-04-15

- Q: When no `--allow-path` is configured, should the default be prompt-for-everything or allow-everything? → A: Prompt for everything. There is only `--allow-path`, no `--deny-path`. If no allowed paths are set, every server-initiated file access prompts the user.
- Q: Where are allowed paths configured? → A: On the `server connect` command (e.g., `server connect -u http://... --allow-path c:\data\**`). Per-session only.
- Q: For glob GetFiles, should all files transfer eagerly or lazily as enumerated? → A: Lazy. Returns `IAsyncEnumerable<ClientFile>`. Each file transfers when the caller iterates to it. Lower memory, more control for the command.
- Q: For glob GetFiles, should consent be per-file or batched? → A: Batch consent. One prompt showing the matched file list, single approve/deny for the whole set.
- Q: Does `MaxFileSizeBytes` apply to `IClientFileAccess` transfers? → A: Yes, same limits apply.
- Q: Should overwriting existing client files trigger a separate consent prompt even if the path is allowed? → A: No. Overwrite silently if the path is allowed.

## User Stories

### US-001: Server Command Saves Output File to Client Machine

**As a** user running a remote command that produces a file, **I want** the output file saved directly to a path on my local machine, **so that** I don't have to manually download it from the server sandbox afterward.

**Why this priority**: This is the primary motivation for the feature — eliminating the upload-to-sandbox / download-from-sandbox ceremony for command-generated output files.

**Independent Test**: Can be tested by running a remote command that writes a file via the service and verifying the file appears on the client with correct content.

**Acceptance Scenarios**:

1. **Given** a connected client and a remote command that produces a data file, **When** the command saves the file via `IClientFileAccess` specifying a client destination path, **Then** the file appears on the client machine at the specified path with correct content.
2. **Given** a connected client and a remote command, **When** the command saves a file and the client destination's parent directory does not exist, **Then** the parent directories are created automatically and the file is saved.
3. **Given** a connected client, **When** the command saves a file via `IClientFileAccess` and the client destination already exists, **Then** the existing file is overwritten.
4. **Given** a remote command saving a file, **When** the client disconnects mid-transfer, **Then** the command receives an error (exception) and partial files are cleaned up on the client.

---

### US-002: Server Command Reads Input File from Client Machine

**As a** user running a remote command that needs an input file, **I want** the command to read a file from my local machine directly, **so that** I don't have to manually upload it to the server sandbox first.

**Why this priority**: The complement to US-001 — many commands need to consume client-side files (e.g., restore operations, config imports).

**Independent Test**: Can be tested by placing a file on the client, running a remote command that reads it via the service, and verifying the command received the correct content.

**Acceptance Scenarios**:

1. **Given** a connected client and a file on the client machine, **When** a remote command reads the file via `IClientFileAccess`, **Then** the command receives a stream with the file's content and metadata (name, size).
2. **Given** a connected client, **When** a remote command reads a file that does not exist on the client, **Then** the command receives a clear error (e.g., `FileNotFoundException`).
3. **Given** a remote command reading a client file, **When** the command disposes the returned file handle, **Then** any temporary server-side copies are cleaned up automatically.
4. **Given** a remote command, **When** it initiates a file read without awaiting and continues other work, **Then** the file transfer proceeds concurrently and the command can await the result later.

---

### US-003: Same Command Works Locally and Remotely

**As a** command author, **I want** to write file access code once using `IClientFileAccess`, **so that** the same command works whether it runs locally or on a remote server without any code changes.

**Why this priority**: Location transparency is a core design principle of the command framework — commands should not know where they run.

**Independent Test**: Can be tested by running the same command locally and remotely, verifying identical observable behavior in both cases.

**Acceptance Scenarios**:

1. **Given** a command that uses `IClientFileAccess` to save a file, **When** the command runs locally (no server connection), **Then** the file is written directly to the local file system.
2. **Given** a command that uses `IClientFileAccess` to read a file, **When** the command runs locally, **Then** the file is read directly from the local file system.
3. **Given** a command that uses `IClientFileAccess`, **When** the command runs on the server, **Then** the service transparently handles the network file transfer — the command code is identical.

---

### US-004: Client Consents to Server File Access

**As a** user, **I want** to be asked for consent before the server reads or writes files on my machine, **so that** a compromised or malicious server cannot silently access my files.

**Why this priority**: Security is critical — the server must not be able to exfiltrate or overwrite arbitrary client files without the user's knowledge.

**Independent Test**: Can be tested by issuing a file access request from the server and verifying the client prompts the user and respects their answer.

**Acceptance Scenarios**:

1. **Given** a connected client with default settings, **When** the server requests access to a file path not in the allowed list, **Then** the client displays a consent prompt showing the actual file path and waits for the user to approve or deny.
2. **Given** a consent prompt is displayed, **When** the user approves, **Then** the file transfer proceeds.
3. **Given** a consent prompt is displayed, **When** the user denies, **Then** the server receives an access-denied error and no file data is transferred.
4. **Given** a connected client with pre-configured allowed paths (e.g., `--allow-path c:\data\**` on the `server connect` command), **When** the server requests a file within an allowed path, **Then** no prompt is shown and the transfer proceeds automatically.
5. **Given** a connected client with no allowed paths configured, **When** the server requests any file, **Then** a consent prompt is displayed.

---

### US-005: Consent Prompt Does Not Corrupt Console Output

**As a** user running a remote command that produces console output, **I want** the consent prompt to display cleanly without corrupting the command's output, **so that** I can read both the prompt and the command output without confusion.

**Why this priority**: If the prompt disrupts the output stream (renders on top of progress bars, interleaves with text), users won't trust or understand it.

**Independent Test**: Can be tested by running a command that streams console output and concurrently triggers a file access request, verifying the prompt renders distinctly and output resumes cleanly.

**Acceptance Scenarios**:

1. **Given** a remote command is streaming console output, **When** a consent prompt needs to display, **Then** the console output is temporarily buffered (paused) so the prompt renders on a clean line.
2. **Given** a consent prompt is displayed and the user answers, **When** the prompt is dismissed, **Then** any buffered console output resumes and renders in order without loss.
3. **Given** a consent prompt is displayed, **Then** the prompt is visually distinct from server-generated output (e.g., bordered panel, distinct color) so the user can clearly distinguish it from command output.
4. **Given** a consent prompt is displayed, **Then** the prompt text is generated client-side (not by the server), showing the actual requested path, so the server cannot misrepresent what file is being accessed.

---

### US-006: Stream-Based Save for In-Memory Data

**As a** command author, **I want** to save a stream (not just a file on disk) to the client's file system, **so that** I can generate data in memory and send it to the client without first writing a temporary file.

**Why this priority**: Many commands generate output in memory (serialized JSON, computed reports). Requiring a temp file intermediary is wasteful and error-prone.

**Independent Test**: Can be tested by a command that generates a `MemoryStream` and saves it via the service, verifying the content arrives on the client.

**Acceptance Scenarios**:

1. **Given** a remote command with a `MemoryStream` of generated data, **When** the command calls save with the stream and a client destination, **Then** the data is transferred to the client and written to the specified path.
2. **Given** a remote command saving a stream, **When** no progress callback is provided, **Then** the transfer completes silently without competing for console output.

---

### US-007: Read Multiple Client Files by Glob Pattern

**As a** command author, **I want** to read multiple files from the client's file system using a glob pattern, **so that** I can process batches of files (e.g., import all CSVs from a directory) without the user listing each one individually.

**Why this priority**: Glob patterns are already a first-class concept in the existing upload/download commands. Consistency matters, and batch operations are a common need.

**Independent Test**: Can be tested by placing multiple files on the client matching a pattern, running a remote command that reads them via the service, and verifying all matching files are received.

**Acceptance Scenarios**:

1. **Given** a client directory with `a.csv`, `b.csv`, `c.txt`, **When** a remote command calls get-files with pattern `*.csv`, **Then** the command receives file handles for `a.csv` and `b.csv` only.
2. **Given** a client directory with nested subdirectories, **When** a remote command calls get-files with pattern `**/*.log`, **Then** all `.log` files in all subdirectories are returned.
3. **Given** a glob pattern that matches no files, **When** a remote command calls get-files, **Then** the command receives an empty result (not an error).
4. **Given** a glob pattern with `?` wildcards (e.g., `file?.txt`), **When** a remote command calls get-files, **Then** only files matching the single-character wildcard are returned.

## Functional Requirements

| ID | Requirement | User Stories | Priority |
|----|------------|-------------|----------|
| FR-001 | System MUST provide an `IClientFileAccess` service injectable into commands via DI | US-003 | MUST |
| FR-002 | `IClientFileAccess` MUST expose a method to open a client-side file for reading, returning a disposable handle with stream access, file name, and file size | US-002, US-003 | MUST |
| FR-003 | `IClientFileAccess` MUST expose a method to save a `Stream` to a client-side path | US-001, US-003, US-006 | MUST |
| FR-004 | `IClientFileAccess` MUST expose a method to save a local (server-side or client-side) file path to a client-side path | US-001, US-003 | MUST |
| FR-005 | When running locally, `IClientFileAccess` MUST perform direct file system operations without network transfer | US-003 | MUST |
| FR-006 | When running on the server, `IClientFileAccess` MUST coordinate file transfers over the existing SignalR/HTTP infrastructure | US-001, US-002, US-003 | MUST |
| FR-007 | The disposable file handle returned by the read method MUST clean up server-side temporary files on disposal when running remotely | US-002 | MUST |
| FR-008 | The save methods MUST create parent directories on the destination if they do not exist | US-001 | MUST |
| FR-009 | The client MUST prompt the user for consent before allowing server-initiated file reads or writes to paths not in the allowed list | US-004 | MUST |
| FR-010 | The consent prompt MUST be rendered client-side using the actual requested path — the server MUST NOT control the prompt text | US-004, US-005 | MUST |
| FR-011 | The client MUST support pre-configured allowed path patterns (via `--allow-path` on the `server connect` command) that bypass the consent prompt. There is no deny-path — paths not in the allowed list are prompted | US-004 | MUST |
| FR-012 | The client MUST buffer console output while a consent prompt is active and flush it after the prompt is dismissed | US-005 | MUST |
| FR-013 | The consent prompt MUST be visually distinct from server-generated command output | US-005 | MUST |
| FR-014 | When the user denies consent, the server-side operation MUST receive an access-denied error | US-004 | MUST |
| FR-015 | File transfer operations MUST be cancellable via `CancellationToken` | US-001, US-002 | MUST |
| FR-016 | The server-to-client file transfer (save) MUST reuse the existing HTTP download endpoint and streaming infrastructure | US-001 | MUST |
| FR-017 | The client-to-server file transfer (get) MUST reuse the existing HTTP upload endpoint and streaming infrastructure | US-002 | MUST |
| FR-018 | The read method MUST support fire-and-forget usage (non-blocking initiation with later await) | US-002 | MUST |
| FR-019 | All transfer methods MUST accept an optional progress callback parameter. The service MUST NOT render progress UI itself — progress display is the caller's responsibility | US-001, US-002, US-006 | MUST |
| FR-020 | Partial files on the client SHOULD be cleaned up on transfer failure | US-001 | SHOULD |
| FR-021 | `IClientFileAccess` MUST expose a method to read multiple client-side files matching a glob pattern, returning an `IAsyncEnumerable<ClientFile>` that transfers each file lazily as the caller iterates | US-007 | MUST |
| FR-022 | Glob pattern expansion MUST reuse the existing `GlobPatternHelper` infrastructure including `?` wildcard post-filtering | US-007 | MUST |
| FR-023 | When running on the server, glob expansion MUST happen client-side (where the files are) and results transferred individually | US-007 | MUST |
| FR-024 | When running locally, glob expansion MUST use `Microsoft.Extensions.FileSystemGlobbing` on the local file system (consistent with existing upload command) | US-007 | MUST |
| FR-025 | File transfers via `IClientFileAccess` MUST respect the server's `MaxFileSizeBytes` limit. Files exceeding the limit MUST be rejected with a clear error | US-001, US-002, US-007 | MUST |

## Edge Cases

- **Server requests file outside allowed paths and user denies** — The command receives an access-denied exception. The command is responsible for handling this gracefully (e.g., logging or reporting to the user). (Relates to: US-004, FR-014)
- **Multiple concurrent file access requests from one command** — Each request generates its own consent prompt (if needed). Prompts are serialized on the client — only one displays at a time. (Relates to: US-005, FR-012)
- **Client disconnects during file transfer** — The awaiting server-side task receives a cancellation/connection-lost exception. Temporary files on both sides are cleaned up. (Relates to: US-001, US-002, FR-015)
- **Returned file handle not disposed** — Temporary server-side files leak. This is a caller bug, consistent with standard .NET `IAsyncDisposable` expectations. Finalizer/logging MAY warn but MUST NOT crash. (Relates to: US-002, FR-007)
- **Save to a path that is read-only on the client** — The client file system throws an `IOException` or `UnauthorizedAccessException`. The error propagates back to the server command. (Relates to: US-001)
- **File requested by server does not exist on client** — Client returns a file-not-found error via the protocol. The server command receives a `FileNotFoundException`. (Relates to: US-002)
- **Very large file transfer** — Transfer uses streaming; file is not loaded into memory all at once. Consistent with existing upload/download behavior. (Relates to: US-001, US-002, FR-016, FR-017)
- **Command runs locally and path does not exist** — Standard `FileNotFoundException` from the local file system. No special handling needed. (Relates to: US-003, FR-005)
- **Consent prompt timeout** — If the user does not respond within a reasonable period, the system SHOULD NOT auto-approve. The transfer remains pending until the user acts or the command's cancellation token fires. (Relates to: US-004)
- **Glob pattern matches many files requiring consent** — Consent prompt SHOULD batch-display the matched file list and request a single approve/deny for the group, rather than prompting per-file. (Relates to: US-004, US-007)
- **Glob pattern expansion returns very large result set** — The service returns all matches lazily via `IAsyncEnumerable`. The command controls concurrency and memory by consuming items at its own pace. (Relates to: US-007)
- **File exceeds MaxFileSizeBytes** — Transfer is rejected with a clear error. The command receives an exception indicating the file exceeds the size limit. (Relates to: FR-025)

## Key Entities

- **IClientFileAccess**: A service representing access to the calling user's file system. Abstracts the difference between local and remote execution. Commands inject this to read or write user-side files.
- **ClientFile**: A disposable handle returned when reading a file. Provides stream access and metadata. On disposal, cleans up any temporary resources (e.g., server-side temp copies in the remote case).
- **File Access Consent**: The client-side decision gate that evaluates each server-initiated file access request against configured allow/deny path patterns and optionally prompts the user. Controls whether a transfer proceeds.

## Assumptions

- The existing HTTP upload/download endpoints and `FileTransferService` are stable and sufficient for the file transfer payloads this feature generates. No changes to the HTTP transfer layer are assumed.
- Commands that use `IClientFileAccess` accept that file transfers may take significant time and should use `CancellationToken` appropriately.
- The consent mechanism applies only to server-initiated file access. The existing `server upload` and `server download` commands are explicitly user-initiated and do not require consent prompts.
- Path allow configuration is per-session (configured at `server connect` time), not persisted across sessions by this feature.
- The `--location` argument pattern (and similar client-side path arguments) will use the existing client-side file system autocomplete handler for tab completion.

## Out of Scope

- **Modification of existing `server upload` / `server download` commands** — Those remain as explicit user-initiated transfers. This feature adds a programmatic service for command authors.
- **Directory transfer as a unit** — The glob method returns individual files, not directory trees. Preserving directory structure on the receiving end is the command's responsibility.
- **Persistent consent rules** — Allow path configuration is session-scoped. Persisting rules to disk or a config file is a future enhancement.
- **Bi-directional streaming** — The service is request/response per file. Continuous streaming (e.g., tailing a remote log to a local file) is not included.
- **Save with glob destination** — Glob patterns apply only to the read/get direction (selecting source files). Save always targets a specific path.
