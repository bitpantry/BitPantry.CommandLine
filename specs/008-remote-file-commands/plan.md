# Implementation Plan: Remote File System Commands

**Branch**: `008-remote-file-commands` | **Date**: 2026-01-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification for `file` command group (7 commands for remote file operations)

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature adds a `file` command group with 7 commands for interactive file operations on the remote server's sandboxed file system. Server-side commands (ls, rm, mkdir, cat, info) execute on the server using `SandboxedFileSystem`. Client-side commands (upload, download) use the existing `FileTransferService` infrastructure.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: System.IO.Abstractions (existing), Spectre.Console (existing), SignalR (existing)  
**Storage**: `SandboxedFileSystem` confined to `StorageRootPath` (from 001-unified-file-system)  
**Testing**: MSTest + FluentAssertions + existing TestEnvironment infrastructure for E2E SignalR tests  
**Target Platform**: Cross-platform (.NET 8.0)
**Project Type**: Multi-project solution (commands split across Server and Client packages)  
**Performance Goals**: Progress feedback at least once per second during transfers  
**Constraints**: All operations confined to server's `StorageRootPath` by `SandboxedFileSystem`  
**Scale/Scope**: 7 commands (5 server-side, 2 client-side), ~50 functional requirements

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Test-Driven Development** | ✅ PASS | E2E integration tests will use existing `TestEnvironment` infrastructure; tests written first for each command |
| **II. Dependency Injection** | ✅ PASS | `IFileSystem` injected into server commands; `FileTransferService` injected into client commands |
| **III. Security by Design** | ✅ PASS | Inherits `SandboxedFileSystem` path validation; path traversal rejected |
| **IV. Follow Existing Patterns** | ✅ PASS | Uses existing `CommandBase` pattern, existing `FileTransferService`, existing autocomplete infrastructure |
| **V. Integration Testing** | ✅ PASS | Full E2E tests using `TestEnvironment` server/client setup |

## Project Structure

### Documentation (this feature)

```text
specs/008-remote-file-commands/
├── plan.md              # This file
├── checklists/
│   └── requirements.md  # Quality checklist
└── tasks.md             # Task breakdown (created by /speckit.tasks)
```

### Source Code (repository root)

```text
BitPantry.CommandLine.Remote.SignalR.Server/
├── Commands/
│   └── File/                         # NEW: Server-side file commands
│       ├── FileGroup.cs              # Command group definition
│       ├── FileListCommand.cs        # file ls
│       ├── FileRemoveCommand.cs      # file rm
│       ├── FileMkdirCommand.cs       # file mkdir
│       ├── FileCatCommand.cs         # file cat
│       └── FileInfoCommand.cs        # file info

BitPantry.CommandLine.Remote.SignalR.Client/
├── Commands/
│   └── File/                         # NEW: Client-side transfer commands  
│       ├── FileGroup.cs              # Command group (client-side mirror)
│       ├── FileUploadCommand.cs      # file upload
│       └── FileDownloadCommand.cs    # file download

BitPantry.CommandLine.Tests.Remote.SignalR/
└── IntegrationTests/
    └── IntegrationTests_FileCommands.cs  # NEW: E2E tests for all file commands

Docs/Remote/
├── BuiltInCommands.md                # UPDATED: Add file commands
└── [FileSystem.md, FileSystemConfiguration.md unchanged]
```

**Structure Decision**: 
- Server-side commands in Server package (pattern matches existing server commands)
- Client-side commands in Client package (pattern matches `ConnectCommand`, `DisconnectCommand`)
- Both use same `file` group name - registry merges them
- Tests in existing Remote.SignalR test project using `TestEnvironment`

## Testing Strategy

### Test Level Selection

| Test Type | Coverage | Justification |
|-----------|----------|---------------|
| **E2E Integration Tests** | PRIMARY | Commands require full server/client stack to validate behavior (file operations, output streaming, progress) |
| **Unit Tests** | SUPPORTING | For argument parsing logic, output formatting (isolated from I/O) |
| **Visual UX Tests (StepwiseTestRunner)** | NOT NEEDED | File commands produce text output, not interactive menus |

### E2E Test Infrastructure

Uses existing `TestEnvironment` class which provides:
- `TestServer` with full server stack including `SandboxedFileSystem`
- `CommandLineApplication` client with `VirtualAnsiConsole`
- `FileTransferService` for upload/download
- Isolated API key per test for parallel execution

### Test Coverage Plan

Per spec requirements (SC-006: All user stories have corresponding E2E integration tests):

| Command | Happy Path Tests | Error/Edge Cases |
|---------|------------------|------------------|
| `file ls` | List root, subdirectory, --long, --recursive | Non-existent path, path traversal rejection, empty directory |
| `file upload` | Upload small/large file, custom dest path | Local not found, overwrite prompt, checksum verification |
| `file download` | Download to current dir, custom path | Remote not found, overwrite prompt, checksum verification |
| `file rm` | Remove file, empty dir, --recursive | Non-existent, non-empty without --recursive, path traversal |
| `file mkdir` | Create single, nested dirs | Idempotent (exists), path traversal |
| `file cat` | Display text file | Binary warning, truncation warning, not found |
| `file info` | File metadata, directory metadata | Not found, path traversal |

### TDD Approach

1. Write failing E2E test using `TestEnvironment`
2. Implement minimal command code to pass
3. Add edge case tests, implement error handling
4. Refactor for clarity while tests pass

## Documentation Decision

### Analysis: Should Documentation Be Reorganized?

**Current Structure:**
- `Docs/Remote/BuiltInCommands.md` - Documents `server` commands (connect, disconnect, status)
- `Docs/Remote/ProfileManagement.md` - Documents `server profile` commands
- `Docs/Remote/FileSystem.md` - Documents `IFileSystem` abstraction for implementers
- `Docs/Remote/FileSystemConfiguration.md` - Server-side configuration

**Decision: ADD to existing structure (no reorganization)**

**Rationale:**
1. `BuiltInCommands.md` is designed for end-user command reference - `file` commands fit here
2. Adding 7 commands to `BuiltInCommands.md` (~80-100 lines each = ~700 lines) is manageable
3. `FileSystem.md` is for implementers, not end-users; keep separate concerns
4. Pattern matches existing `server` commands documentation

**If reorganization were needed (rejected):**
- Could split into `ServerCommands.md` and `FileCommands.md`
- Rejected because: adds navigation complexity, one-command-group-per-file is excessive

### Documentation Updates

1. **`Docs/Remote/BuiltInCommands.md`**:
   - Add `file` group to command overview table
   - Add sections for all 7 commands following existing format
   - Add "See Also" cross-references to `FileSystem.md` and `FileSystemConfiguration.md`

2. **No changes to**:
   - `FileSystem.md` (implementer-focused, already complete)
   - `FileSystemConfiguration.md` (configuration unchanged)

## Command Registration

### Default Registration (FR-006, FR-007, FR-008)

Both packages auto-register commands when their extension methods are called:

**Server Package** (`AddCommandLineServer`):
```csharp
// In ServiceCollectionExtensions or similar
registry.RegisterGroup<FileGroup>();
registry.RegisterCommand<FileListCommand>();
registry.RegisterCommand<FileRemoveCommand>();
// ... etc
```

**Client Package** (`ConfigureSignalRClient`):
```csharp
registry.RegisterGroup<FileGroup>(); // Same group name, merged by registry
registry.RegisterCommand<FileUploadCommand>();
registry.RegisterCommand<FileDownloadCommand>();
```

## Key Implementation Notes

### Server-Side Commands

- All inject `IFileSystem` (resolved to `SandboxedFileSystem` on server)
- All inject `IAnsiConsole` for output (Spectre Console, per DESIGN.md)
- Use `[Execute]` attribute pattern like existing commands
- Path validation inherited from `SandboxedFileSystem` - no additional checks needed

### Client-Side Commands

- `FileUploadCommand`: Uses `FileTransferService.UploadFile()` with existing progress infrastructure
- `FileDownloadCommand`: Uses `FileTransferService.DownloadFile()` with progress callback
- Both check connection status before executing
- Both handle overwrite confirmation with `--force` flag

### Output Formatting (per DESIGN.md)

- Use default terminal colors for normal output
- Use `[red]` for errors only
- Use `[green]` for success confirmations only
- No decorative symbols or bold text
- 2-space indentation for nested output

## Autocomplete Implementation

### Current State (from 005-autocomplete-redesign)

| Component | Status | Location |
|-----------|--------|----------|
| `FilePathProvider` | ✅ Implemented | Core - fallback for any `ArgumentValue`, uses `IFileSystem` |
| `RemoteCompletionProvider` | ✅ Implemented | Core - calls server via SignalR `AutoCompleteRequest` |
| `ServerLogic.AutoComplete()` | ✅ Implemented | Server - receives request, calls `ICompletionOrchestrator` |
| `ICompletionOrchestrator` on server | ❌ Not registered | Server DI missing `AddCompletionServices()` call |

### Gap Analysis

The autocomplete infrastructure is 90% complete. The only gap:

**Problem**: `AddCommandLineHub()` does not call `AddCompletionServices()`, so `ICompletionOrchestrator` is null on server.

**Solution**: Add `services.AddCompletionServices()` call in `AddCommandLineHub()` after registering file system.

### Implementation Approach

1. **Infrastructure fix** (1 task):
   - Add `AddCompletionServices()` to `IServiceCollectionExtensions.cs` in Server package
   - Server-side `FilePathProvider` already uses `IFileSystem` → gets `SandboxedFileSystem` automatically
   
2. **No new providers needed**:
   - Local paths: `FilePathProvider` fallback (existing, works on client)
   - Remote paths: `RemoteCompletionProvider` → `AutoCompleteRequest` → server's `FilePathProvider` (will work once orchestrator registered)

3. **Test coverage** (1-2 tasks):
   - E2E test: Connect, type partial path, press Tab, verify completions from server
   - E2E test: Disconnect, type partial path, press Tab, verify empty result

### Why This Is Low Risk

- No new protocols or providers needed
- Single line addition to DI registration
- Leverages fully-tested existing infrastructure
- `SandboxedFileSystem` path confinement applies automatically to completions

## Complexity Tracking

No constitution violations to justify - all gates pass.

## Phase 0 Output: Research

**No NEEDS CLARIFICATION items identified.** All technical context is clear:

| Decision | Rationale | Alternatives Considered |
|----------|-----------|-------------------------|
| E2E tests (not unit tests) as primary validation | Commands require full stack (server + client + file system) to be meaningful; matches constitution "Integration Testing for Cross-Cutting Concerns" | Unit tests alone: rejected because file operations span server/client boundary |
| Add to BuiltInCommands.md | Matches existing pattern; ~700 lines is manageable for one doc | Separate FileCommands.md: rejected as unnecessary fragmentation |
| Commands/File/ subfolder | Organizes 5 server commands; matches pattern used in Client for server commands | Commands/ root: rejected to avoid clutter with 5 related files |

## Phase 1 Output: Design & Contracts

### Data Model

No new persistent entities. Commands operate on:

| Entity | Source | Operations |
|--------|--------|------------|
| File paths | User input | ls, rm, cat, info, upload source, download target |
| Directories | User input | ls, rm, mkdir, info, upload target path |
| `SandboxedFileSystem` | DI (existing) | All server-side operations |
| `FileTransferService` | DI (existing) | Upload/download operations |

### API Contracts

No new HTTP endpoints or RPC messages required. Commands use:

| Command | Transport | Existing Infrastructure |
|---------|-----------|-------------------------|
| Server commands | RPC via SignalR | Existing command execution pipeline |
| `file upload` | HTTP POST to `/files` | `FileTransferService.UploadFile()` |
| `file download` | HTTP GET from `/files/{path}` | `FileTransferService.DownloadFile()` |

### Quickstart

Once implemented:

```bash
# Connect to server
sandbox> server connect production

# List files
production@server> file ls
documents/
config.json

# List with details
production@server> file ls --long
Type  Size       Modified             Name
----  ----       --------             ----
dir   -          2026-01-02 10:30:15  documents/
file  1.2 KB     2026-01-01 14:22:30  config.json

# Upload a file
production@server> file upload ./mydata.csv reports/mydata.csv
Uploading mydata.csv to reports/mydata.csv
[████████████████████████████████████████] 100%  2.1 MB  4.2 MB/s
Upload complete. Checksum verified.

# Download a file  
production@server> file download config.json ./local-config.json
Downloading config.json to ./local-config.json
Download complete. Checksum verified.

# View file contents
production@server> file cat config.json
{
  "setting": "value"
}

# Create directory
production@server> file mkdir reports/2026/q1

# Remove file
production@server> file rm reports/old-report.csv
Delete reports/old-report.csv? [y/N]: y
Deleted.

# File info
production@server> file info config.json
Path:         config.json
Type:         File
Size:         1.2 KB (1,234 bytes)
Created:      2025-12-15 09:30:00
Modified:     2026-01-01 14:22:30
```
