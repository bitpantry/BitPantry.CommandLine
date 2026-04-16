<!--
  STAGED ISSUE — not yet published to GitHub.
  Use /publish-issues to create this issue on GitHub.
  
  Staging Number: 008
  GitHub Issue Number: #58
-->

# Edge cases and hardening

**Labels**: enhancement, spec-012
**Blocked by**: 006, 007
**Implements**: FR-020, FR-025
**Covers**: All user stories (hardening)

> Also addresses the 12 prose edge cases defined in `spec.md`.

## Summary

Harden the client file access system against edge cases: client disconnect during transfer, partial file cleanup, MaxFileSizeBytes enforcement, FileNotFoundException propagation, concurrent consent prompt serialization, path traversal validation, and large stream handling.

## Current Behavior

After issues 001–007, the happy-path behaviors work. This issue covers the edge cases and error conditions defined in the spec.

## Expected Behavior

All edge-case scenarios from `spec.md` EC-001 through EC-012 are handled gracefully with appropriate errors, cleanup, and no resource leaks.

## Affected Area

- **Project(s):** `BitPantry.CommandLine`, `BitPantry.CommandLine.Remote.SignalR.Server`, `BitPantry.CommandLine.Remote.SignalR.Client`
- **Key files:**
  - `RemoteClientFileAccess.cs` — MODIFY: disconnect handling, timeout, size limit enforcement
  - `SignalRServerProxy.cs` / ReceiveMessage handler — MODIFY: partial file cleanup on failure
  - `FileAccessConsentHandler.cs` — MODIFY: concurrent prompt serialization
  - `FileTransferService.cs` — MODIFY: MaxFileSizeBytes enforcement, stream size validation
  - `ClientFile.cs` — MODIFY: ensure proper Dispose cleans up temp files
- **Spec reference:** See `specs/012-client-file-access/spec.md` (Edge Cases section)
- **Plan reference:** See `specs/012-client-file-access/plan.md` (Phase 5)

## Requirements

- [ ] Client disconnect during GetFileAsync → `RemoteClientFileAccess` throws `RemoteMessagingException` / `TaskCanceledException`, no temp file left behind (EC-001)
- [ ] Client disconnect during SaveFileAsync → partial file cleaned up on server or client depending on direction (EC-002)
- [ ] Partial file cleanup: if transfer fails mid-stream, any partially-written temp file is deleted (EC-003)
- [ ] MaxFileSizeBytes exceeded → `InvalidOperationException` with size details, transfer aborted early (EC-004)
- [ ] FileNotFoundException on client for GetFileAsync → propagated to server as `FileNotFoundException` (EC-005)
- [ ] FileNotFoundException on client for SaveFileAsync → propagated appropriately (path doesn't exist) (EC-006)
- [ ] Path traversal in glob pattern → rejected before expansion (EC-007)
- [ ] Concurrent GetFileAsync calls from same command → prompts serialized, not interleaved (EC-008)
- [ ] CancellationToken honored at every async boundary (EC-009)
- [ ] ClientFile.Dispose cleans up temp stream and file if applicable (EC-010)
- [ ] Empty file (0 bytes) transfers correctly for both GetFile and SaveFile (EC-011)
- [ ] Very large file (>MaxFileSizeBytes if configured) rejected before transfer starts (EC-012)
- [ ] ClientFile does not throw on finalizer if not disposed (MAY log warning, MUST NOT crash) (spec edge case #4)

## Prerequisites

- Blocked by: 006, 007 — Happy-path single-file and glob must work first

## Implementation Guidance

### Client Disconnect Handling (EC-001, EC-002)

The existing `RpcMessageRegistry.WaitForCompletion` has a timeout mechanism. Ensure:
1. `RemoteClientFileAccess.GetFileAsync` wraps the await in try/catch for `OperationCanceledException` and `HubException`
2. On failure, check if a temp file was created and delete it
3. `SaveFileAsync` server-side failure should trigger client cleanup of the partially-downloaded file

### Partial File Cleanup (EC-003)

```csharp
// In client handler for file upload:
string? tempPath = null;
try
{
    tempPath = GetTempFilePath();
    await DownloadToTempFile(url, tempPath, ct);
    // ... send response
}
catch
{
    if (tempPath != null && _fileSystem.File.Exists(tempPath))
        _fileSystem.File.Delete(tempPath);
    throw;
}
```

### MaxFileSizeBytes Enforcement (EC-004, EC-012)

Check file size before initiating transfer:
```csharp
if (_options.MaxFileSizeBytes > 0 && fileInfo.Length > _options.MaxFileSizeBytes)
    throw new InvalidOperationException($"File '{path}' ({fileInfo.Length:N0} bytes) exceeds maximum allowed size ({_options.MaxFileSizeBytes:N0} bytes).");
```

For server→client downloads (SaveFile), the server knows the size upfront and includes it in the message. The client validates before downloading.

### Concurrent Prompt Serialization (EC-008)

```csharp
private readonly SemaphoreSlim _consentSemaphore = new(1, 1);

public async Task<bool> RequestConsentAsync(...)
{
    await _consentSemaphore.WaitAsync(ct);
    try
    {
        // Render prompt, collect response
    }
    finally
    {
        _consentSemaphore.Release();
    }
}
```

### Path Traversal in Glob (EC-007)

Before expanding any glob pattern, validate it doesn't contain path traversal:
```csharp
if (pattern.Contains("..") || Path.IsPathRooted(pattern))
    throw new ArgumentException($"Glob pattern must not contain path traversal: '{pattern}'");
```

## Implementer Autonomy

This issue was authored from a specification and plan — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description.
3. **Do not** silently drop requirements or weaken test coverage.

## Testing Requirements

### Test Approach

- **Test level:** Integration (primarily) + Unit for isolated logic
- **Test project:** `BitPantry.CommandLine.Tests.Remote.SignalR`
- **Existing fixtures to reuse:** `TestEnvironment`, `VirtualConsole`, `MockFileSystem`, `TempFileScope`

### Prescribed Test Cases

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `GetFile_ClientDisconnects_ThrowsAndCleansUp` | Kill connection mid-transfer | Exception thrown, no temp file remains |
| 2 | `SaveFile_ClientDisconnects_PartialFileRemoved` | Kill connection mid-download | Partial file deleted from client |
| 3 | `GetFile_ExceedsMaxSize_ThrowsBeforeTransfer` | MaxFileSizeBytes=1KB, file is 10KB | InvalidOperationException, no upload started |
| 4 | `GetFile_FileNotFound_ThrowsFileNotFoundException` | Request non-existent file | FileNotFoundException with original path |
| 5 | `GetFile_PathTraversal_Rejected` | Pattern `../../etc/passwd` | ArgumentException |
| 6 | `GetFile_ConcurrentCalls_PromptsNotInterleaved` | Two GetFileAsync calls, no --allow-path | Consent prompts appear sequentially |
| 7 | `GetFile_Cancelled_ThrowsOperationCanceled` | Cancel token mid-stream | OperationCanceledException |
| 8 | `ClientFile_Dispose_CleansUpTempFile` | Dispose a ClientFile | Temp file deleted |
| 9 | `GetFile_EmptyFile_TransfersCorrectly` | 0-byte file | ClientFile with 0-length stream |
| 10 | `SaveFile_EmptyFile_CreatesZeroByteFile` | Save 0-byte stream | File exists with 0 bytes |
| 11 | `GetFiles_GlobWithPathTraversal_Rejected` | `../**/*.txt` | ArgumentException before expansion |
| 12 | `ClientFile_NotDisposed_FinalizerDoesNotThrow` | Create ClientFile, don't dispose, let GC collect | No exception, optional log warning |

### Discovering Additional Test Cases

The test cases above are a starting point. During implementation, **discover and add additional test cases** as you encounter edge cases or error paths not covered above.

### TDD Workflow

Follow the `tdd-workflow` skill: write failing tests first (RED), implement (GREEN), refactor.
