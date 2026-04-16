<!--
  STAGED ISSUE — not yet published to GitHub.
  Use /publish-issues to create this issue on GitHub.
  
  Staging Number: 005
  GitHub Issue Number: #55
-->

# Client-side push message handler and consent UX

**Labels**: enhancement, spec-012
**Blocked by**: 002, 004
**Implements**: FR-009, FR-010, FR-012, FR-013, FR-014, FR-020
**Covers**: US-004, US-005

## Summary

Extend the client's `SignalRServerProxy.ReceiveMessage()` to handle the three new push message types (`ClientFileUploadRequest`, `ClientFileDownloadRequest`, `ClientFileEnumerateRequest`). Implement `FileAccessConsentHandler` — the UX component that pauses console output, renders a visually distinct consent prompt, reads the user's response, resumes output, and then performs the actual file transfer using the existing `FileTransferService`.

## Current Behavior

`SignalRServerProxy.ReceiveMessage()` only handles `PushMessageType.FileUploadProgress`. There is no mechanism for the server to request file operations from the client, no consent prompt infrastructure, and no console output buffering during prompts.

## Expected Behavior

When the server sends a file access request push message, the client:
1. Evaluates the path against `FileAccessConsentPolicy`
2. If consent required: pauses console output, renders a Spectre.Console `Panel` prompt showing the actual path, reads Y/N, clears prompt, resumes output
3. If approved (or pre-allowed): performs the file transfer using existing `FileTransferService.UploadFile()` or `DownloadFile()`
4. Sends a `ClientFileAccessResponse` back to the server with success/error

## Affected Area

- **Project(s):** `BitPantry.CommandLine.Remote.SignalR.Client`
- **Key files:**
  - `BitPantry.CommandLine.Remote.SignalR.Client/FileAccessConsentHandler.cs` — NEW
  - `BitPantry.CommandLine.Remote.SignalR.Client/SignalRServerProxy.cs` — MODIFY: add cases in ReceiveMessage, add console output buffering
  - `BitPantry.CommandLine.Remote.SignalR.Client/CommandLineApplicationBuilderExtensions.cs` — MODIFY: register consent handler
- **Spec reference:** See `specs/012-client-file-access/spec.md`
- **Plan reference:** See `specs/012-client-file-access/plan.md`

## Requirements

- [ ] `ReceiveMessage` handles `PushMessageType.ClientFileUploadRequest` — evaluates consent, uploads file to server if approved, sends response (FR-009)
- [ ] `ReceiveMessage` handles `PushMessageType.ClientFileDownloadRequest` — evaluates consent, downloads file from server if approved, sends response (FR-009)
- [ ] `ReceiveMessage` handles `PushMessageType.ClientFileEnumerateRequest` — evaluates consent (batch), expands glob, sends file list response (FR-009)
- [ ] Consent prompt is rendered client-side showing the actual requested path — server does not control prompt text (FR-010)
- [ ] Consent prompt uses a visually distinct Spectre.Console `Panel` with colored border (FR-013)
- [ ] Console output from the server is buffered (paused) while consent prompt is active (FR-012)
- [ ] Buffered console output is flushed in order after the prompt is dismissed (FR-012)
- [ ] When user denies consent, response to server indicates access denied (FR-014)
- [ ] When user denies consent, no file data is transferred (FR-014)
- [ ] File transfers use existing `FileTransferService.UploadFile()` and `DownloadFile()` (FR-016, FR-017)
- [ ] Partial files on client are cleaned up on download failure (FR-020)
- [ ] Client sends `ServerRequest(ClientFileAccessResponse)` back via `ReceiveRequest` channel after operation completes
- [ ] `FileAccessConsentHandler` is registered as singleton in client DI
- [ ] Multiple concurrent consent requests are serialized — only one prompt displays at a time

## Prerequisites

- Blocked by: 002 — Protocol message envelopes must exist
- Blocked by: 004 — `FileAccessConsentPolicy` must exist for consent evaluation

## Implementation Guidance

### Console Output Buffering

Add to `SignalRServerProxy`:

```csharp
private volatile bool _consoleOutputPaused;
private readonly ConcurrentQueue<string> _bufferedConsoleOutput = new();

private void ConsoleOut(string str)
{
    if (_consoleOutputPaused)
        _bufferedConsoleOutput.Enqueue(str);
    else
        _console.Profile.Out.Writer.Write(str);
}

private void FlushBufferedOutput()
{
    while (_bufferedConsoleOutput.TryDequeue(out var output))
        _console.Profile.Out.Writer.Write(output);
}
```

### FileAccessConsentHandler

```csharp
public class FileAccessConsentHandler
{
    private readonly FileAccessConsentPolicy _policy;
    private readonly IAnsiConsole _console;
    private readonly SemaphoreSlim _promptLock = new(1, 1); // serialize prompts

    public async Task<bool> RequestConsentAsync(string path, Action pauseOutput, Action resumeOutput, CancellationToken ct)
    {
        if (_policy.IsAllowed(path)) return true;

        await _promptLock.WaitAsync(ct);
        try
        {
            pauseOutput();
            await Task.Delay(50, ct); // let in-flight output arrive

            var panel = new Panel($"Server requests: [bold]{Markup.Escape(path)}[/]\nAllow? [green]y[/]/[red]N[/]")
                .Header("File Access Request")
                .BorderColor(Color.Yellow);
            _console.Write(panel);

            var key = _console.Input.ReadKey(intercept: true);
            var allowed = key?.Key == ConsoleKey.Y;

            // Clear prompt lines via ANSI
            // ... cursor-up + clear-line for panel height ...

            resumeOutput();
            return allowed;
        }
        finally
        {
            _promptLock.Release();
        }
    }
}
```

### ReceiveMessage Extension

```csharp
case PushMessageType.ClientFileUploadRequest:
    var uploadReq = new ClientFileUploadRequestMessage(msg.Data);
    _ = Task.Run(async () =>
    {
        var approved = await _consentHandler.RequestConsentAsync(
            uploadReq.ClientPath,
            () => _consoleOutputPaused = true,
            () => { _consoleOutputPaused = false; FlushBufferedOutput(); },
            CancellationToken.None);

        if (approved)
        {
            await _fileTransferService.UploadFile(
                uploadReq.ClientPath, uploadReq.ServerTempPath,
                progress => Task.CompletedTask, CancellationToken.None);
            await SendFileAccessResponse(uploadReq.CorrelationId, success: true);
        }
        else
        {
            await SendFileAccessResponse(uploadReq.CorrelationId, success: false, error: "FileAccessDenied");
        }
    });
    break;
```

Note: The handler runs on `Task.Run` (not blocking `ReceiveMessage`) — same pattern as `ReceiveRequest`.

### Prompt Clearing

Use `AnsiCodes.CursorUp(n)` + `AnsiCodes.ClearLine` (already in `BitPantry.CommandLine/AutoComplete/Rendering/AnsiCodes.cs`) to erase the panel after the user answers. Calculate panel height from the number of rendered lines.

## Implementer Autonomy

This issue was authored from a specification and plan — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description.
3. **Do not** silently drop requirements or weaken test coverage.

## Testing Requirements

### Test Approach

- **Test level:** Unit + UX (VirtualConsole for prompt rendering)
- **Test project:** `BitPantry.CommandLine.Tests.Remote.SignalR`
- **Existing fixtures to reuse:** `VirtualConsole`, `VirtualConsoleAssertions`, `TestFileTransferServiceFactory`, `FileTransferServiceTestContext`

### Prescribed Test Cases

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `RequestConsent_AllowedPath_ReturnsTrueNoPrompt` | Path is in allowed list | Returns true, no console output for prompt |
| 2 | `RequestConsent_UnallowedPath_ShowsPrompt` | Path not allowed | Panel rendered with correct path text |
| 3 | `RequestConsent_UserApprovesY_ReturnsTrue` | User presses Y | Returns true |
| 4 | `RequestConsent_UserDeniesN_ReturnsFalse` | User presses N | Returns false |
| 5 | `RequestConsent_DefaultDeny_ReturnsFalse` | User presses Enter (default N) | Returns false |
| 6 | `RequestConsent_PromptVisuallyDistinct` | Prompt rendered | Output contains Panel border/markup |
| 7 | `RequestConsent_OutputBufferedDuringPrompt` | Server output arrives during prompt | Output not rendered until after prompt dismissed |
| 8 | `RequestConsent_BufferedOutputFlushedAfter` | Prompt dismissed | All buffered output appears in correct order |
| 9 | `ReceiveMessage_UploadRequest_Approved_UploadsFile` | Approve consent for upload request | `FileTransferService.UploadFile` called with correct paths |
| 10 | `ReceiveMessage_DownloadRequest_Approved_DownloadsFile` | Approve consent for download request | `FileTransferService.DownloadFile` called with correct paths |
| 11 | `ReceiveMessage_UploadRequest_Denied_SendsAccessDenied` | Deny consent | Response sent with success=false, error=FileAccessDenied |
| 12 | `ReceiveMessage_ConcurrentRequests_Serialized` | Two requests arrive simultaneously | Prompts shown one at a time (not interleaved) |

### Discovering Additional Test Cases

The test cases above are a starting point. During implementation, **discover and add additional test cases** as you encounter edge cases or error paths not covered above.

### TDD Workflow

Follow the `tdd-workflow` skill: write failing tests first (RED), implement (GREEN), refactor.
