<!--
  STAGED ISSUE — not yet published to GitHub.
  Use /publish-issues to create this issue on GitHub.
  
  Staging Number: 003
  GitHub Issue Number: #53
-->

# RemoteClientFileAccess server implementation

**Labels**: enhancement, spec-012
**Blocked by**: 001, 002
**Implements**: FR-006, FR-007, FR-016, FR-017, FR-018, FR-025
**Covers**: US-001, US-002, US-006

## Summary

Implement `RemoteClientFileAccess` — the server-side `IClientFileAccess` that coordinates file transfers between server and client via SignalR push messages and existing HTTP endpoints. Also wire up the hub routing for client responses and register the service in server DI.

## Current Behavior

Server-side commands have no way to programmatically access files on the client machine. The only file transfer mechanism is the user-initiated `server upload` / `server download` commands.

## Expected Behavior

Server-side commands can inject `IClientFileAccess` and call `GetFileAsync` / `SaveFileAsync`. The `RemoteClientFileAccess` implementation sends push messages to the client, awaits file transfer completion via `RpcMessageRegistry` correlation, and manages temp file staging in the sandbox. The hub routes `ClientFileAccessResponse` messages to the `RpcMessageRegistry`.

## Affected Area

- **Project(s):** `BitPantry.CommandLine.Remote.SignalR.Server`
- **Key files:**
  - `BitPantry.CommandLine.Remote.SignalR.Server/ClientFileAccess/RemoteClientFileAccess.cs` — NEW
  - `BitPantry.CommandLine.Remote.SignalR.Server/Configuration/IServiceCollectionExtensions.cs` — MODIFY: register scoped IClientFileAccess
  - `BitPantry.CommandLine.Remote.SignalR.Server/CommandLineHub.cs` — MODIFY: add ClientFileAccessResponse routing
- **Spec reference:** See `specs/012-client-file-access/spec.md`
- **Plan reference:** See `specs/012-client-file-access/plan.md`

## Requirements

- [ ] `RemoteClientFileAccess` implements `IClientFileAccess` (FR-006)
- [ ] `GetFileAsync` sends `ClientFileUploadRequest` push message via `HubInvocationContext.ClientProxy` and awaits response via `RpcMessageRegistry` (FR-006, FR-017)
- [ ] `GetFileAsync` returns a `ClientFile` wrapping the uploaded temp file, with cleanup that deletes the temp file on dispose (FR-007)
- [ ] `SaveFileAsync(Stream, ...)` writes stream to staging temp file, sends `ClientFileDownloadRequest`, awaits completion, deletes temp (FR-006, FR-016)
- [ ] `SaveFileAsync(string, ...)` sends `ClientFileDownloadRequest` referencing the source path directly — no temp copy needed (FR-006, FR-016)
- [ ] Both save methods support the stream and path overloads (FR-003, FR-004)
- [ ] `GetFileAsync` supports fire-and-forget usage — returns `Task<ClientFile>` that can be awaited later (FR-018)
- [ ] Files exceeding `MaxFileSizeBytes` are rejected with a clear error before transfer starts (FR-025)
- [ ] Progress is reported via optional `IProgress<FileTransferProgress>` if provided (FR-019, implied)
- [ ] `CommandLineHub.ReceiveRequest` routes `ServerRequestType.ClientFileAccessResponse` to `RpcMessageRegistry.SetResponse()` (FR-006)
- [ ] `RemoteClientFileAccess` is registered as scoped `IClientFileAccess` in server DI, overriding the default local implementation (FR-006)
- [ ] Staging temp files are written to `.client-file-staging/` within the sandbox (FR-006)
- [ ] Staging temp files are cleaned up on both success and failure (FR-007)

## Prerequisites

- Blocked by: 001 — `IClientFileAccess` interface and `ClientFile` must exist
- Blocked by: 002 — Protocol message envelopes must exist

## Implementation Guidance

### RemoteClientFileAccess Dependencies

Inject via constructor:
- `HubInvocationContext` — to access `Current.ClientProxy` for sending push messages and `Current.RpcMessageRegistry` for correlation
- `IFileSystem` — sandboxed file system for staging temp files
- `FileTransferOptions` — for `MaxFileSizeBytes` and `StorageRootPath`
- `ILogger<RemoteClientFileAccess>`

### GetFileAsync Flow

```csharp
public async Task<ClientFile> GetFileAsync(string clientPath, IProgress<FileTransferProgress>? progress, CancellationToken ct)
{
    var ctx = _hubInvocationContext.Current ?? throw new InvalidOperationException("No hub invocation context");
    
    var tempPath = Path.Combine(".client-file-staging", $"{Guid.NewGuid():N}");
    _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
    
    var rpcCtx = ctx.RpcMessageRegistry.Register();
    
    var msg = new ClientFileUploadRequestMessage(clientPath, tempPath);
    msg.CorrelationId = rpcCtx.CorrelationId;
    await ctx.ClientProxy.SendAsync(SignalRMethodNames.ReceiveMessage, msg, ct);
    
    var response = await rpcCtx.WaitForCompletion<ClientFileAccessResponseMessage>();
    if (!response.Success)
        throw MapError(response.Error, clientPath);
    
    var fileInfo = _fileSystem.FileInfo.New(tempPath);
    var stream = _fileSystem.File.OpenRead(tempPath);
    
    return new ClientFile(stream, Path.GetFileName(clientPath), fileInfo.Length,
        async () => { if (_fileSystem.File.Exists(tempPath)) _fileSystem.File.Delete(tempPath); });
}
```

### Hub Routing

In `CommandLineHub.ReceiveRequest()`, add:
```csharp
case ServerRequestType.ClientFileAccessResponse:
    _rpcMsgReg.SetResponse(req);
    break;
```

### DI Registration

In `IServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IClientFileAccess, RemoteClientFileAccess>();
```

This overrides the default `LocalClientFileAccess` singleton for server-side command execution scope.

## Implementer Autonomy

This issue was authored from a specification and plan — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description.
3. **Do not** silently drop requirements or weaken test coverage.

## Testing Requirements

### Test Approach

- **Test level:** Unit (mocked hub context, RPC registry, file system)
- **Test project:** `BitPantry.CommandLine.Tests.Remote.SignalR`
- **Existing fixtures to reuse:** `MockFileSystem`, `Mock<IClientProxy>` (Moq)

### Prescribed Test Cases

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `GetFileAsync_SendsPushMessage_WithCorrectClientPath` | Call GetFileAsync | Push message sent to ClientProxy with correct ClientPath |
| 2 | `GetFileAsync_SuccessfulUpload_ReturnsClientFileWithStream` | Simulate successful response | Returns ClientFile with readable stream from temp file |
| 3 | `GetFileAsync_ClientError_ThrowsException` | Simulate error response | Throws appropriate exception, temp file cleaned up |
| 4 | `GetFileAsync_Dispose_DeletesTempFile` | Get file, dispose ClientFile | Temp file deleted from staging |
| 5 | `SaveFileAsync_Stream_WritesTempAndSendsPush` | Save a stream | Temp file written, push message sent with correct paths |
| 6 | `SaveFileAsync_Stream_SuccessfulDownload_DeletesTemp` | Simulate successful response | Staging temp file cleaned up |
| 7 | `SaveFileAsync_Path_SendsPushWithSourcePath` | Save from source path | Push message references source directly (no temp copy) |
| 8 | `SaveFileAsync_ExceedsMaxFileSize_ThrowsBeforeTransfer` | Source file exceeds MaxFileSizeBytes | Exception thrown, no push message sent |
| 9 | `GetFileAsync_NoHubContext_ThrowsInvalidOperation` | Call outside hub invocation | Throws `InvalidOperationException` |
| 10 | `GetFileAsync_CancellationRequested_ThrowsOperationCanceled` | Cancel during await | Throws `OperationCanceledException` |
| 11 | `HubReceiveRequest_ClientFileAccessResponse_SetsRpcResponse` | Client sends response | RpcMessageRegistry.SetResponse called with correct correlation |

### Discovering Additional Test Cases

The test cases above are a starting point. During implementation, **discover and add additional test cases** as you encounter edge cases or error paths not covered above.

### TDD Workflow

Follow the `tdd-workflow` skill: write failing tests first (RED), implement (GREEN), refactor.
