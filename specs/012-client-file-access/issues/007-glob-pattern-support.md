<!--
  STAGED ISSUE — not yet published to GitHub.
  Use /publish-issues to create this issue on GitHub.
  
  Staging Number: 007
  GitHub Issue Number: #57
-->

# Glob pattern support: GetFilesAsync with lazy enumeration

**Labels**: enhancement, spec-012
**Blocked by**: 006
**Implements**: FR-021, FR-022, FR-023, FR-024
**Covers**: US-007

## Summary

Add `GetFilesAsync(string clientGlobPattern, ...)` to `IClientFileAccess` returning `IAsyncEnumerable<ClientFile>`. Files are transferred lazily — each file is uploaded from the client only when the server-side caller iterates to it. Glob expansion happens client-side (where the files are). Consent is batched — one prompt for the entire matched set.

## Current Behavior

`IClientFileAccess` supports single-file operations (`GetFileAsync`, `SaveFileAsync`). To process multiple files, a command would need to know the exact paths upfront and call `GetFileAsync` in a loop. There's no glob pattern expansion.

## Expected Behavior

Commands can call `await foreach (var file in _clientFiles.GetFilesAsync("**/*.csv")) { ... }` and receive files one at a time, lazily transferred. The client expands the glob pattern locally using the existing `GlobPatternHelper` + `FileSystemGlobbing` infrastructure, presents a batch consent prompt (if needed) with tiered display, and uploads each file on demand.

## Affected Area

- **Project(s):** `BitPantry.CommandLine`, `BitPantry.CommandLine.Remote.SignalR.Server`, `BitPantry.CommandLine.Remote.SignalR.Client`
- **Key files:**
  - `BitPantry.CommandLine/Client/IClientFileAccess.cs` — MODIFY: add `GetFilesAsync`
  - `BitPantry.CommandLine/Client/LocalClientFileAccess.cs` — MODIFY: implement local glob expansion
  - `BitPantry.CommandLine.Remote.SignalR.Server/ClientFileAccess/RemoteClientFileAccess.cs` — MODIFY: implement remote glob flow
  - `BitPantry.CommandLine.Remote.SignalR.Client/SignalRServerProxy.cs` — MODIFY: handle `ClientFileEnumerateRequest` in `ReceiveMessage`
  - `BitPantry.CommandLine.Remote.SignalR.Client/FileAccessConsentHandler.cs` — MODIFY: add `RequestBatchConsentAsync` with tiered display
- **Spec reference:** See `specs/012-client-file-access/spec.md`
- **Plan reference:** See `specs/012-client-file-access/plan.md`

## Requirements

- [ ] `IClientFileAccess` has `GetFilesAsync(string clientGlobPattern, IProgress<FileTransferProgress>?, CancellationToken)` returning `IAsyncEnumerable<ClientFile>` (FR-021)
- [ ] Files are transferred lazily — each file transfers when the caller iterates to it, not upfront (FR-021)
- [ ] `LocalClientFileAccess.GetFilesAsync` expands glob locally using `Microsoft.Extensions.FileSystemGlobbing` (FR-024)
- [ ] Local implementation applies `GlobPatternHelper.ApplyQuestionMarkFilter` for `?` wildcards (FR-022)
- [ ] Remote implementation sends `ClientFileEnumerateRequest` to client, client expands glob locally (FR-023)
- [ ] Remote implementation receives file list, then sends individual `ClientFileUploadRequest` per file as caller iterates (FR-023)
- [ ] Client-side glob expansion reuses `GlobPatternHelper` infrastructure (FR-022)
- [ ] Empty pattern result returns empty enumerable (not an error) (US-007 scenario 3)
- [ ] Consent for glob is batched — one prompt for all matched files using tiered display (spec edge case)
- [ ] Tiered consent display: ≤10 files show full list, 11–50 show collapsed (first 5 + last 2), >50 show summary only
- [ ] Each yielded `ClientFile` is independently disposable (FR-021)

## Prerequisites

- Blocked by: 006 — Single-file round-trip must be working end-to-end

## Implementation Guidance

### IClientFileAccess Addition

```csharp
IAsyncEnumerable<ClientFile> GetFilesAsync(string clientGlobPattern, IProgress<FileTransferProgress>? progress = null, [EnumeratorCancellation] CancellationToken ct = default);
```

### LocalClientFileAccess Implementation

Reuse the expansion logic from `UploadCommand.ExpandSource()`:

```csharp
public async IAsyncEnumerable<ClientFile> GetFilesAsync(string pattern, IProgress<FileTransferProgress>? progress, [EnumeratorCancellation] CancellationToken ct)
{
    var (baseDir, searchPattern) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);
    var matcher = new Matcher();
    matcher.AddInclude(searchPattern.Replace('?', '*'));
    var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDir)));

    var files = result.Files
        .Select(f => _fileSystem.Path.GetFullPath(Path.Combine(baseDir, f.Path)))
        .ToList();
    files = GlobPatternHelper.ApplyQuestionMarkFilter(files, searchPattern, Path.GetFileName).ToList();

    foreach (var filePath in files)
    {
        ct.ThrowIfCancellationRequested();
        yield return await GetFileAsync(filePath, progress, ct);
    }
}
```

### RemoteClientFileAccess Implementation

```csharp
public async IAsyncEnumerable<ClientFile> GetFilesAsync(string pattern, IProgress<FileTransferProgress>? progress, [EnumeratorCancellation] CancellationToken ct)
{
    // Step 1: Ask client to enumerate files matching pattern
    var enumerateCtx = ctx.RpcMessageRegistry.Register();
    var enumerateMsg = new ClientFileEnumerateRequestMessage(pattern) { CorrelationId = enumerateCtx.CorrelationId };
    await ctx.ClientProxy.SendAsync(SignalRMethodNames.ReceiveMessage, enumerateMsg, ct);
    var enumerateResp = await enumerateCtx.WaitForCompletion<ClientFileAccessResponseMessage>();

    if (!enumerateResp.Success)
        throw MapError(enumerateResp.Error, pattern);

    var fileEntries = enumerateResp.DeserializeFileInfoEntries(); // [{Path, Size}, ...]

    // Step 2: Lazy iteration — upload each file on demand
    foreach (var entry in fileEntries)
    {
        ct.ThrowIfCancellationRequested();
        yield return await GetFileAsync(entry.Path, progress, ct);
    }
}
```

Note: The individual `GetFileAsync` calls in step 2 skip consent — it was already granted in batch during the enumerate step. This may require a flag or separate internal method that bypasses consent.

### Client Enumerate Handler

In `ReceiveMessage`:
```csharp
case PushMessageType.ClientFileEnumerateRequest:
    var enumReq = new ClientFileEnumerateRequestMessage(msg.Data);
    _ = Task.Run(async () =>
    {
        // Expand glob locally
        var files = ExpandGlobLocally(enumReq.GlobPattern);
        
        // Batch consent
        var pathsNeedingConsent = _consentPolicy.GetPathsRequiringConsent(files.Select(f => f.Path));
        if (pathsNeedingConsent.Count > 0)
        {
            var approved = await _consentHandler.RequestBatchConsentAsync(
                pathsNeedingConsent, files.Select(f => f.Size).ToList(),
                enumReq.GlobPattern, ...);
            if (!approved)
            {
                await SendFileAccessResponse(enumReq.CorrelationId, false, "FileAccessDenied");
                return;
            }
        }

        await SendFileAccessResponse(enumReq.CorrelationId, true, fileInfoEntries: files);
    });
    break;
```

### Tiered Batch Consent Display

In `FileAccessConsentHandler.RequestBatchConsentAsync`:
- ≤ `ConsentFullListThreshold` (10): List all files with sizes
- ≤ `ConsentSummaryOnlyThreshold` (50): Show first `ConsentCollapsedHeadCount` (5) + last `ConsentCollapsedTailCount` (2) + "... and N more (total: X MB)"
- \> 50: Summary only — count, pattern, total size, base directory

## Implementer Autonomy

This issue was authored from a specification and plan — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description.
3. **Do not** silently drop requirements or weaken test coverage.

## Testing Requirements

### Test Approach

- **Test level:** Unit + Integration
- **Test project:** `BitPantry.CommandLine.Tests` (local), `BitPantry.CommandLine.Tests.Remote.SignalR` (remote + integration)
- **Existing fixtures to reuse:** `MockFileSystem`, `TestEnvironment`, `VirtualConsole`, `GlobPatternHelper`

### Prescribed Test Cases

**Unit — Local:**

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `GetFilesAsync_StarGlob_ReturnsMatchingFiles` | `*.csv` matches 2 of 3 files | Yields 2 ClientFile instances |
| 2 | `GetFilesAsync_DoubleStarGlob_MatchesRecursive` | `**/*.log` in nested dirs | All matching files returned |
| 3 | `GetFilesAsync_NoMatches_ReturnsEmpty` | Pattern matches nothing | Empty enumerable, no error |
| 4 | `GetFilesAsync_QuestionMark_MatchesSingleChar` | `file?.txt` | Only single-char matches |
| 5 | `GetFilesAsync_LazyEnumeration_OpensFilesOnDemand` | Iterate only first 2 of 5 | Only 2 FileStreams opened |

**Unit — Remote (mocked):**

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 6 | `GetFilesAsync_SendsEnumerateRequest` | Call GetFilesAsync | Push message sent with glob pattern |
| 7 | `GetFilesAsync_ClientDenies_ThrowsAccessDenied` | Enumerate response denied | Throws FileAccessDeniedException |

**Integration (TestEnvironment):**

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 8 | `GetFiles_GlobPattern_AllMatchesReturned` | 3 matching + 1 non-matching | Command receives exactly 3 files |
| 9 | `GetFiles_LazyEnumeration_TransfersPerIteration` | Consume only first 2 of 5 | Only 2 uploads occurred |
| 10 | `GetFiles_BatchConsent_ShowsFileList` | No --allow-path, ≤10 files | Consent panel shows all file paths |
| 11 | `GetFiles_BatchConsent_LargeSet_ShowsSummary` | >50 files matching | Consent panel shows count + total size, not all filenames |

### Discovering Additional Test Cases

The test cases above are a starting point. During implementation, **discover and add additional test cases** as you encounter edge cases or error paths not covered above.

### TDD Workflow

Follow the `tdd-workflow` skill: write failing tests first (RED), implement (GREEN), refactor.
