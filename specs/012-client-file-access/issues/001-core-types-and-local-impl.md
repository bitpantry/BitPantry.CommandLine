<!--
  STAGED ISSUE — not yet published to GitHub.
  Use /publish-issues to create this issue on GitHub.
  
  Staging Number: 001
  GitHub Issue Number: #51
-->

# Core types: IClientFileAccess, ClientFile, and local implementation

**Labels**: enhancement, spec-012
**Blocked by**: None
**Implements**: FR-001, FR-002, FR-003, FR-004, FR-005, FR-008, FR-015, FR-019
**Covers**: US-003

## Summary

Introduce the `IClientFileAccess` interface, `ClientFile` disposable file handle, `FileTransferProgress` record, `FileAccessDeniedException`, and the `LocalClientFileAccess` implementation. This establishes the API contract that all command authors will use and provides the local (client-side) implementation where file operations go directly to disk with no network transfer.

## Current Behavior

No `IClientFileAccess` service exists. Commands that need to interact with client-side files must use `IFileSystem` directly (which is the local file system on the client but the sandboxed server file system on the server). There is no location-transparent file access abstraction.

## Expected Behavior

Commands can inject `IClientFileAccess` and call `GetFileAsync`, `SaveFileAsync(Stream, ...)`, and `SaveFileAsync(string, ...)` to read/write files on the user's machine. When running locally, `LocalClientFileAccess` performs direct file I/O. The interface, types, and local implementation are all available in the core `BitPantry.CommandLine` project.

## Affected Area

- **Project(s):** `BitPantry.CommandLine`
- **Key files:**
  - `BitPantry.CommandLine/Client/IClientFileAccess.cs` — NEW: interface definition
  - `BitPantry.CommandLine/Client/ClientFile.cs` — NEW: disposable file handle
  - `BitPantry.CommandLine/Client/FileTransferProgress.cs` — NEW: progress record
  - `BitPantry.CommandLine/Client/FileAccessDeniedException.cs` — NEW: exception type
  - `BitPantry.CommandLine/Client/LocalClientFileAccess.cs` — NEW: local implementation
  - `BitPantry.CommandLine/ServiceCollectionExtensions.cs` — MODIFY: register default DI
- **Spec reference:** See `specs/012-client-file-access/spec.md`
- **Plan reference:** See `specs/012-client-file-access/plan.md`
- **Data model reference:** See `specs/012-client-file-access/data-model.md`

## Requirements

- [ ] `IClientFileAccess` interface exists with `GetFileAsync(string, IProgress<FileTransferProgress>?, CancellationToken)` returning `Task<ClientFile>` (FR-001, FR-002)
- [ ] `IClientFileAccess` interface has `SaveFileAsync(Stream, string, IProgress<FileTransferProgress>?, CancellationToken)` (FR-003, FR-019)
- [ ] `IClientFileAccess` interface has `SaveFileAsync(string, string, IProgress<FileTransferProgress>?, CancellationToken)` (FR-004, FR-019)
- [ ] All methods accept `CancellationToken` (FR-015)
- [ ] All methods accept optional `IProgress<FileTransferProgress>` callback (FR-019)
- [ ] `ClientFile` implements `IAsyncDisposable` with `Stream`, `FileName`, and `Length` properties (FR-002)
- [ ] `ClientFile` disposal calls cleanup action if provided (FR-002)
- [ ] `LocalClientFileAccess.GetFileAsync` opens a `FileStream` for reading and returns a `ClientFile` (FR-005)
- [ ] `LocalClientFileAccess.GetFileAsync` throws `FileNotFoundException` for missing files (FR-005)
- [ ] `LocalClientFileAccess.SaveFileAsync(Stream, ...)` writes stream to the specified path (FR-005)
- [ ] `LocalClientFileAccess.SaveFileAsync(Stream, ...)` creates parent directories if they don't exist (FR-008)
- [ ] `LocalClientFileAccess.SaveFileAsync(string, ...)` copies the source file to the destination (FR-005)
- [ ] `LocalClientFileAccess.SaveFileAsync(string, ...)` creates parent directories if they don't exist (FR-008)
- [ ] `LocalClientFileAccess` is registered as the default `IClientFileAccess` in DI (FR-001)

## Prerequisites

No prerequisites — this issue can be started independently.

## Implementation Guidance

### Interface Design

Place all types in `BitPantry.CommandLine/Client/` alongside existing `IServerProxy.cs` and `NoopServerProxy.cs`.

```csharp
public interface IClientFileAccess
{
    Task<ClientFile> GetFileAsync(string clientPath, IProgress<FileTransferProgress>? progress = null, CancellationToken ct = default);
    Task SaveFileAsync(Stream content, string clientPath, IProgress<FileTransferProgress>? progress = null, CancellationToken ct = default);
    Task SaveFileAsync(string sourcePath, string clientPath, IProgress<FileTransferProgress>? progress = null, CancellationToken ct = default);
}
```

Note: `GetFilesAsync` (glob) is added in issue 007. Do not include it here.

### ClientFile

```csharp
public sealed class ClientFile : IAsyncDisposable
{
    public Stream Stream { get; }
    public string FileName { get; }
    public long Length { get; }
    private readonly Func<ValueTask>? _cleanupAsync;

    public ClientFile(Stream stream, string fileName, long length, Func<ValueTask>? cleanupAsync = null) { ... }

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
        if (_cleanupAsync != null) await _cleanupAsync();
    }
}
```

### LocalClientFileAccess

Depends on `IFileSystem` (already registered as singleton). For `SaveFileAsync(Stream, ...)`, stream the content to a `FileStream` in chunks (same 81920-byte buffer as existing `FileTransferService`). Report progress via `IProgress<T>` if provided.

### DI Registration

In `ServiceCollectionExtensions.cs`, add `LocalClientFileAccess` as the default singleton:

```csharp
services.AddSingleton<IClientFileAccess, LocalClientFileAccess>();
```

The server project will override this with `RemoteClientFileAccess` (scoped) in issue 003.

## Implementer Autonomy

This issue was authored from a specification and plan — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description.
3. **Do not** silently drop requirements or weaken test coverage.

## Testing Requirements

### Test Approach

- **Test level:** Unit (MockFileSystem for isolation)
- **Test project:** `BitPantry.CommandLine.Tests`
- **Existing fixtures to reuse:** None needed — pure unit tests with `MockFileSystem` from `System.IO.Abstractions.TestingHelpers`

### Prescribed Test Cases

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `GetFileAsync_ExistingFile_ReturnsClientFileWithStream` | File exists on disk | Returns `ClientFile` with readable stream, correct `FileName` and `Length` |
| 2 | `GetFileAsync_MissingFile_ThrowsFileNotFoundException` | File does not exist | Throws `FileNotFoundException` |
| 3 | `SaveFileAsync_Stream_WritesToDisk` | Save a MemoryStream to a path | File exists at path with correct content |
| 4 | `SaveFileAsync_Stream_CreatesParentDirectories` | Save to path where parent dir doesn't exist | Parent dirs created, file written |
| 5 | `SaveFileAsync_Path_CopiesFile` | Save from existing source path to destination | Destination has same content as source |
| 6 | `SaveFileAsync_Path_CreatesParentDirectories` | Destination parent dir doesn't exist | Parent dirs created, file copied |
| 7 | `SaveFileAsync_Stream_OverwritesExistingFile` | Destination already exists | File overwritten with new content |
| 8 | `ClientFile_DisposeAsync_ClosesStream` | Dispose a ClientFile | Stream is disposed |
| 9 | `ClientFile_DisposeAsync_CallsCleanupAction` | Dispose with cleanup action | Cleanup action invoked |
| 10 | `ClientFile_DisposeAsync_NullCleanup_NoError` | Dispose without cleanup action | No error thrown |
| 11 | `GetFileAsync_WithProgress_ReportsProgress` | Read file with IProgress provided | Progress reported with correct BytesTransferred/TotalBytes |
| 12 | `SaveFileAsync_WithCancellation_ThrowsOperationCanceled` | Cancel during save | Throws `OperationCanceledException` |

### Integration Tests

| # | Test Name Pattern | Scenario | Expected Outcome | Infrastructure |
|---|-------------------|----------|------------------|----------------|
| 13 | `SaveFile_LocalCommand_WritesDirectly` | Register command locally that saves via `IClientFileAccess` → run | File written to temp dir | `CommandLineApplication` (no server) |
| 14 | `GetFile_LocalCommand_ReadsDirectly` | Place file → register command locally that reads via `IClientFileAccess` → run | Command receives correct content | `CommandLineApplication` (no server) |

### Discovering Additional Test Cases

The test cases above are a starting point. During implementation, **discover and add additional test cases** as you encounter edge cases or error paths not covered above.

### TDD Workflow

Follow the `tdd-workflow` skill: write failing tests first (RED), implement (GREEN), refactor.
