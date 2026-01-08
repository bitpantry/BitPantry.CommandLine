# Implementation Plan: Upload Command

**Branch**: `006-upload-command` | **Date**: 2026-01-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-upload-command/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add an `upload` command to the `server` command group that enables users to upload files from the local machine to the connected remote server. The command supports single and multi-file uploads with recursive glob pattern expansion (`**/*.txt`), progress display for large files (>= 1MB), concurrent multi-file progress visualization, and optional `--skip-existing` flag for non-destructive batch uploads. Leverages existing `FileTransferService` infrastructure with a new batch existence check endpoint.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: Spectre.Console (progress display), Microsoft.AspNetCore.SignalR.Client (server communication), System.IO.Abstractions (file operations), Microsoft.Extensions.FileSystemGlobbing (recursive glob patterns)  
**Storage**: Local filesystem (client), server-side sandboxed storage (via existing infrastructure)  
**Testing**: MSTest with FluentAssertions and Moq; integration tests using TestServer pattern  
**Target Platform**: Cross-platform (.NET 8.0 - Windows, Linux, macOS)  
**Project Type**: Multi-project solution (client library within existing architecture)  
**Performance Goals**: Concurrent multi-file uploads with configurable parallelism  
**Constraints**: Progress display for files >= 1MB; graceful handling of partial failures  
**Scale/Scope**: Single command addition to existing `server` command group; one new REST endpoint for batch file existence check

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Test-Driven Development** | ✅ PASS | Tests will be written first; unit tests for UploadCommand, glob expansion, progress logic; integration tests for end-to-end file transfer |
| **II. Dependency Injection** | ✅ PASS | UploadCommand receives SignalRServerProxy, FileTransferService, IAnsiConsole via constructor injection |
| **III. Security by Design** | ✅ PASS | Leverages existing secure FileTransferService with Authorization header tokens; no new security attack surface |
| **IV. Follow Existing Patterns** | ✅ PASS | Follows ConnectCommand/DisconnectCommand patterns; uses existing ServerGroup; uses existing FileTransferService API |
| **V. Integration Testing** | ✅ PASS | Integration tests using TestServer pattern (existing TestEnvironment infrastructure) |

**Gate Result**: ✅ PASS - All constitutional principles satisfied

## Project Structure

### Documentation (this feature)

```text
specs/006-upload-command/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── upload-files-exist.md  # POST /files/exists endpoint contract
└── test-cases.md        # Phase 2 output (/speckit.plan command)
```

### Source Code (repository root)

```text
BitPantry.CommandLine.Remote.SignalR.Client/
├── UploadCommand.cs                    # NEW - Upload command implementation
├── ServerGroup.cs                      # EXISTING - Command group (no changes)
├── FileTransferService.cs              # MODIFIED - Add CheckFilesExist() method
├── SignalRServerProxy.cs               # EXISTING - Connection state (no changes)
└── ...

BitPantry.CommandLine.Remote.SignalR.Server/
├── FilesExistEndpoint.cs               # NEW - POST /files/exists endpoint
└── ...

BitPantry.CommandLine.Tests.Remote.SignalR/
├── ClientTests/
│   └── UploadCommandTests.cs           # NEW - Unit tests for UploadCommand
├── ServerTests/
│   └── FilesExistEndpointTests.cs      # NEW - Unit tests for endpoint
└── IntegrationTests/
    └── IntegrationTests_UploadCommand.cs  # NEW - Integration tests
```

**Structure Decision**: Command in existing client project. New REST endpoint in server project. Tests distributed across client and server test folders.

## Complexity Tracking

> **No violations detected** - All constitutional principles satisfied without exception.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | - | - |

---

## Technical Design

### Command Implementation: UploadCommand

**Location**: `BitPantry.CommandLine.Remote.SignalR.Client/UploadCommand.cs`

The `UploadCommand` will:
1. Register under `ServerGroup` with name "upload"
2. Accept two positional arguments: `source` (local file/glob) and `destination` (remote path)
3. Accept optional `--skip-existing` / `-s` flag
4. Verify connection state before proceeding
5. Expand glob patterns client-side using `Microsoft.Extensions.FileSystemGlobbing` (supports `**`)
6. If `--skip-existing`, batch-check server for existing files
7. Upload single files with conditional progress (>= 1MB threshold)
8. Upload multiple files concurrently with full progress table display
9. Handle errors gracefully with appropriate messaging

### Key Components

| Component | Responsibility |
|-----------|----------------|
| `UploadCommand` | Command entry point, glob expansion, orchestration |
| `FileTransferService` | HTTP file upload with progress; batch file existence check |
| `SignalRServerProxy` | Connection state verification |
| `IAnsiConsole` | Spectre.Console output for progress bars |
| `FilesExistEndpoint` | Server REST endpoint for batch existence check |

### Concurrency Strategy

- Use `SemaphoreSlim` for configurable parallelism (default: 4 concurrent uploads)
- Create all progress tasks upfront showing "Pending" state
- Update task description and progress as uploads proceed
- Use `Interlocked` for thread-safe success/failure counting

### Batch File Existence Check

**Endpoint**: `POST /files/exists`

**Chunking Strategy**: 
- Client chunks requests >100 files into batches of 100
- Constant: `BATCH_EXISTS_CHUNK_SIZE = 100`
- Client merges results from all batch responses

```csharp
// Client-side (FileTransferService) with chunking
private const int BATCH_EXISTS_CHUNK_SIZE = 100;

public async Task<Dictionary<string, bool>> CheckFilesExist(
    string directory, 
    IEnumerable<string> filenames,
    CancellationToken ct = default)
{
    var allFiles = filenames.ToArray();
    var result = new Dictionary<string, bool>();
    
    foreach (var chunk in allFiles.Chunk(BATCH_EXISTS_CHUNK_SIZE))
    {
        var request = new { Directory = directory, Filenames = chunk };
        var response = await _httpClient.PostAsJsonAsync("/files/exists", request, ct);
        var chunkResult = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(ct);
        foreach (var kvp in chunkResult)
            result[kvp.Key] = kvp.Value;
    }
    return result;
}

// Server-side (FilesExistEndpoint)
app.MapPost("/files/exists", (FilesExistRequest request, IFileSystem fs) =>
{
    var basePath = Path.Combine(storageRoot, request.Directory.TrimStart('/'));
    var exists = request.Filenames.ToDictionary(
        f => f,
        f => fs.File.Exists(Path.Combine(basePath, f)));
    return Results.Ok(new { Exists = exists });
}).RequireAuthorization();
```

### Server-Side skipIfExists Enforcement (TOCTOU Mitigation)

To mitigate the race condition between batch check and upload, each upload request includes `skipIfExists` parameter:

**Upload Endpoint Enhancement**:
```
POST /{ServiceEndpointNames.FileUpload}?fileName=...&path=...&skipIfExists=true
```

**Server Logic**:
```csharp
// In file upload handler
if (skipIfExists && fs.File.Exists(destinationPath))
{
    // File appeared after client's batch check - honor skip semantics
    return Results.Ok(new FileUploadResponse("skipped", "File already exists"));
}

// Proceed with upload (overwrite if exists, create if not)
await SaveFileAsync(destinationPath, content);
return Results.Ok(new FileUploadResponse("uploaded", BytesWritten: content.Length));
```

**Client Handling**:
```csharp
var result = await _fileTransferService.UploadFile(..., skipIfExists: skipExisting);
if (result.Status == "skipped")
{
    // Handle as skipped even though we expected to upload
    skippedAtServer.Add(file);
    task.Description = $"{fileName} [yellow]Skipped (server)[/]";
}
```

### Progress Display

**Single File (>= 1MB)**:
```
database-backup.sql [████████████░░░░░░░░] 60% ⠋
```

**Multi-File**:
```
file1.txt [████████████████████] 100% Completed
file2.txt [████████░░░░░░░░░░░░]  40% ⠋
file3.txt [░░░░░░░░░░░░░░░░░░░░]   0% Pending
```

**Multi-File with Skip Existing**:
```
report.txt [████████████████████] 100% Completed
readme.txt Skipped (exists)
notes.txt  [████████████████████] 100% Completed

Uploaded 2 files. 1 skipped (already exist).
```

---

## Post-Design Constitution Check

*Re-evaluation after Phase 1 design completion.*

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| **I. Test-Driven Development** | ✅ PASS | 48 test cases defined in test-cases.md covering UX, component, data flow, and error handling |
| **II. Dependency Injection** | ✅ PASS | Design confirms constructor injection for IServerProxy, FileTransferService, IAnsiConsole |
| **III. Security by Design** | ✅ PASS | No new endpoints; leverages existing token-based authentication via FileTransferService |
| **IV. Follow Existing Patterns** | ✅ PASS | Command pattern matches ConnectCommand/DisconnectCommand; uses ServerGroup registration |
| **V. Integration Testing** | ✅ PASS | 5 integration test cases defined (IT-001 through IT-005) using TestEnvironment infrastructure |

**Post-Design Gate Result**: ✅ PASS - All constitutional principles verified in final design
