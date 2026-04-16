# Implementation Plan: Client File Access

**Spec**: `012-client-file-access` | **Date**: 2026-04-15
**Input**: `specs/012-client-file-access/spec.md`

## Summary

Add a location-transparent `IClientFileAccess` service that lets commands read/write files on the user's machine regardless of execution context. Two implementations: `LocalClientFileAccess` (direct file I/O for client-side execution) and `RemoteClientFileAccess` (coordinates SignalR push messages + existing HTTP file transfer for server-side execution). The client handles consent prompts, console output buffering, and actual HTTP transfers. The server implementation sends push messages via `HubInvocationContext` and awaits responses via `RpcMessageRegistry`. No new HTTP endpoints — all file transfer reuses existing `/fileupload` and `/filedownload`.

## Technical Context

**Runtime**: .NET 8.0 (VirtualConsole targets .NET Standard 2.0)
**Framework**: ASP.NET Core SignalR (server), Spectre.Console (console UI)
**Key Dependencies**:
- Existing: `System.IO.Abstractions`, `Microsoft.Extensions.FileSystemGlobbing`, `Spectre.Console`, `Microsoft.AspNetCore.SignalR.Client`
- New: None (all dependencies already in the solution)
**Storage**: Server sandbox for temp file staging; client local file system for final storage
**Testing**: MSTest 3.6.1 + FluentAssertions 6.12.0 + Moq + MockFileSystem + TestEnvironment (integration) + VirtualConsole (UX)
**Constraints**:
- `MaxFileSizeBytes` enforced for all transfers
- Path traversal protection via `SandboxedFileSystem` on server staging
- Client consent required for all server-initiated file access unless pre-allowed

## Project Structure

### New/Modified Files

```text
BitPantry.CommandLine/
├── Client/
│   ├── IClientFileAccess.cs                    ← NEW: interface
│   ├── ClientFile.cs                           ← NEW: disposable file handle
│   ├── FileTransferProgress.cs                 ← NEW: progress record
│   ├── FileAccessDeniedException.cs            ← NEW: exception
│   └── LocalClientFileAccess.cs                ← NEW: local implementation
├── ServiceCollectionExtensions.cs              ← MODIFY: register LocalClientFileAccess as default

BitPantry.CommandLine.Remote.SignalR/
├── Envelopes/
│   ├── PushMessage.cs                          ← MODIFY: add PushMessageType values
│   ├── ClientFileUploadRequest.cs              ← NEW: push message envelope
│   ├── ClientFileDownloadRequest.cs            ← NEW: push message envelope
│   ├── ClientFileEnumerateRequest.cs           ← NEW: push message envelope
│   ├── ClientFileAccessResponse.cs             ← NEW: response envelope
│   └── ServerRequest.cs                        ← MODIFY: add ServerRequestType.ClientFileAccessResponse

BitPantry.CommandLine.Remote.SignalR.Server/
├── ClientFileAccess/
│   └── RemoteClientFileAccess.cs               ← NEW: server implementation
├── Configuration/
│   └── IServiceCollectionExtensions.cs         ← MODIFY: register RemoteClientFileAccess as scoped IClientFileAccess
├── CommandLineHub.cs                           ← MODIFY: route ClientFileAccessResponse to RpcMessageRegistry

BitPantry.CommandLine.Remote.SignalR.Client/
├── FileAccessConsentPolicy.cs                  ← NEW: allow-path matching
├── FileAccessConsentHandler.cs                 ← NEW: consent prompt + output buffering
├── SignalRServerProxy.cs                       ← MODIFY: handle new push message types in ReceiveMessage
├── CommandLineApplicationBuilderExtensions.cs  ← MODIFY: register consent infrastructure
├── Commands/Server/
│   └── ConnectCommand.cs                       ← MODIFY: add --allow-path argument
```

### Test Files

```text
BitPantry.CommandLine.Tests/
├── ClientFileAccess/
│   ├── LocalClientFileAccessTests.cs           ← NEW: unit tests for local impl
│   └── ClientFileTests.cs                      ← NEW: unit tests for ClientFile disposal

BitPantry.CommandLine.Tests.Remote.SignalR/
├── ClientFileAccess/
│   ├── RemoteClientFileAccessTests.cs          ← NEW: unit tests (mocked hub context)
│   ├── FileAccessConsentPolicyTests.cs         ← NEW: unit tests for allow-path matching
│   ├── FileAccessConsentHandlerTests.cs        ← NEW: UX tests (VirtualConsole)
│   └── ClientFileAccessIntegrationTests.cs     ← NEW: full integration (TestEnvironment)
```

### Documentation

```text
specs/012-client-file-access/
├── spec.md
├── plan.md              ← this file
└── data-model.md        ← entity definitions and message schemas
```

## Data Model

See [data-model.md](./data-model.md) for entity definitions, message envelopes, and existing entity relationships.

## Technical Design

### 1. Interface & Types (`BitPantry.CommandLine`)

`IClientFileAccess`, `ClientFile`, `FileTransferProgress`, and `FileAccessDeniedException` live in the core project so commands can reference them without depending on SignalR. See data-model.md for full definitions.

`LocalClientFileAccess` depends only on `IFileSystem` (already registered as singleton in `ServiceCollectionExtensions.AddFileSystem()`).

### 2. Protocol: Server → Client File Access Requests

Uses the existing `PushMessage` / `ReceiveMessage` channel (server→client push) with three new message types. The client responds via `ServerRequest` / `ReceiveRequest` channel using new `ServerRequestType.ClientFileAccessResponse`.

**GetFile flow** (server reads file from client):
```
Server: RemoteClientFileAccess.GetFileAsync("c:\data\report.csv")
  1. Generate correlationId, register in RpcMessageRegistry
  2. Send PushMessage(ClientFileUploadRequest) via HubInvocationContext.ClientProxy
  3. Await RpcMessageRegistry.WaitForCompletion<ClientFileAccessResponse>()

Client: SignalRServerProxy.ReceiveMessage()
  1. Receives ClientFileUploadRequest
  2. FileAccessConsentHandler checks policy / prompts user
  3. If approved: calls FileTransferService.UploadFile(clientPath, serverTempPath)
  4. Sends ServerRequest(ClientFileAccessResponse) with success/error
  5. If denied: sends response with FileAccessDenied error

Server: 
  4. RpcMessageRegistry completes → open temp file → return ClientFile
  5. ClientFile.DisposeAsync() deletes temp file
```

**SaveFile flow** (server writes file to client):
```
Server: RemoteClientFileAccess.SaveFileAsync(stream, "c:\backups\export.json")
  1. Write stream to staging temp file in sandbox
  2. Generate correlationId, register in RpcMessageRegistry
  3. Send PushMessage(ClientFileDownloadRequest) via HubInvocationContext.ClientProxy
  4. Await RpcMessageRegistry.WaitForCompletion<ClientFileAccessResponse>()

Client: SignalRServerProxy.ReceiveMessage()
  1. Receives ClientFileDownloadRequest
  2. FileAccessConsentHandler checks policy / prompts user
  3. If approved: calls FileTransferService.DownloadFile(serverPath, clientPath)
  4. Sends ServerRequest(ClientFileAccessResponse) with success/error

Server:
  5. RpcMessageRegistry completes → delete staging temp file → return
```

**SaveFile(sourcePath, ...) flow** — same as above but skips writing to temp; uses `sourcePath` directly for the download.

**GetFiles flow** (server reads multiple files from client via glob):
```
Server: RemoteClientFileAccess.GetFilesAsync("**/*.csv")
  1. Send PushMessage(ClientFileEnumerateRequest) with glob pattern
  2. Await response with matched file list

Client:
  1. Expand glob locally via GlobPatternHelper + FileSystemGlobbing
  2. Apply ? wildcard post-filter
  3. FileAccessConsentHandler checks/prompts for the batch
  4. Send response with file list (path + size per file)

Server:
  3. For each file in response, yield via IAsyncEnumerable:
     - Send individual ClientFileUploadRequest per file
     - Await upload → return ClientFile
```

### 3. Consent Handler (`BitPantry.CommandLine.Remote.SignalR.Client`)

`FileAccessConsentPolicy` is a simple class storing `List<string>` of allowed glob patterns from `--allow-path`. Uses `GlobPatternHelper.ContainsGlobCharacters` and `Matcher` for pattern matching against requested paths.

`FileAccessConsentHandler` manages the UX:

1. Check `FileAccessConsentPolicy.IsAllowed(path)` — if allowed, return immediately
2. Set `_consoleOutputPaused = true` on the proxy's console output handler
3. Wait 50ms for in-flight output to arrive and be buffered
4. Flush buffered output
5. Render consent prompt using Spectre.Console `Panel` with `Color.Yellow` border:
   ```
   ┌─ File Access Request ────────────────────┐
   │ Server requests: c:\data\report.csv      │
   │ Allow? [y/N]                             │
   └──────────────────────────────────────────┘
   ```
6. Read key via local `Console.ReadKey()` (NOT via SignalR — this is client-local input)
7. Clear prompt lines (ANSI cursor-up + clear-line, using existing `AnsiCodes`)
8. Set `_consoleOutputPaused = false`, flush buffered output
9. Return allow/deny decision

For batch consent (glob), tiered display based on match count:

**Small batch (≤ 10 files)** — full file list:
```
┌─ File Access Request ────────────────────────┐
│ Server requests 3 files matching **/*.csv:   │
│   c:\data\a.csv (12 KB)                     │
│   c:\data\b.csv (8 KB)                      │
│   c:\data\c.csv (45 KB)                     │
│ Allow all? [y/N]                             │
└──────────────────────────────────────────────┘
```

**Medium batch (11–50 files)** — first 5, last 2, collapsed middle:
```
┌─ File Access Request ────────────────────────┐
│ Server requests 27 files matching **/*.csv:  │
│   c:\data\a.csv (12 KB)                     │
│   c:\data\b.csv (8 KB)                      │
│   c:\data\c.csv (45 KB)                     │
│   c:\data\d.csv (3 KB)                      │
│   c:\data\e.csv (91 KB)                     │
│   ... and 20 more (total: 1.2 MB)           │
│   c:\data\z.csv (6 KB)                      │
│   c:\data\zz.csv (14 KB)                    │
│ Allow all 27 files? [y/N]                    │
└──────────────────────────────────────────────┘
```

**Large batch (> 50 files)** — summary only:
```
┌─ File Access Request ────────────────────────┐
│ Server requests 347 files matching **/*.log  │
│ Total size: 84.5 MB                          │
│ Directory: c:\logs\                          │
│ Allow all 347 files? [y/N]                   │
└──────────────────────────────────────────────┘
```

Thresholds are defined as constants in `FileAccessConsentHandler` (see data-model.md).

**Console output buffering**: The `ConsoleOut` handler in `SignalRServerProxy` checks a volatile `bool _consoleOutputPaused` flag. When paused, incoming output is appended to a `ConcurrentQueue<string>`. When unpaused, the queue is drained.

### 4. ConnectCommand Extension

Add `--allow-path` to `ConnectCommand`:

```csharp
[Argument(Name = "allow-path")]
[Alias('a')]
[Rest]  // collects multiple values
[Description("Client paths the server may access without prompting (glob patterns)")]
public string[] AllowPaths { get; set; }
```

After successful connection, store in `FileAccessConsentPolicy`:
```csharp
_consentPolicy.SetAllowedPatterns(AllowPaths ?? Array.Empty<string>());
```

### 5. DI Registration

**Server** (`IServiceCollectionExtensions.cs`):
```csharp
services.AddScoped<IClientFileAccess, RemoteClientFileAccess>();
```
Scoped because it depends on `HubInvocationContext` (per-request ambient context).

**Client** (`CommandLineApplicationBuilderExtensions.cs`):
```csharp
builder.Services.AddSingleton<FileAccessConsentPolicy>();
builder.Services.AddSingleton<FileAccessConsentHandler>();
```

**Core** (`ServiceCollectionExtensions.cs` or `CommandLineApplicationBuilder`):
```csharp
services.AddSingleton<IClientFileAccess, LocalClientFileAccess>();
```
The client project overrides this only if needed — in practice, client-side commands use `LocalClientFileAccess` and server-side commands use `RemoteClientFileAccess` (registered in server DI scope).

### 6. Hub Routing

`CommandLineHub.ReceiveRequest()` gets a new case:
```csharp
case ServerRequestType.ClientFileAccessResponse:
    _rpcMsgReg.SetResponse(req);
    break;
```

This routes the client's file access completion response to the `RpcMessageRegistry`, unblocking the `RemoteClientFileAccess` call that's `await`ing it.

## Testing Strategy

### Unit Tests

| User Story | Test Class | What's Tested | Fixtures/Helpers |
|-----------|-----------|---------------|------------------|
| US-003, US-005 | `LocalClientFileAccessTests` | GetFile reads local file, SaveFile writes local file, SaveFile creates parent dirs, GetFiles expands glob | `MockFileSystem` |
| US-002 | `ClientFileTests` | Disposal closes stream, disposal calls cleanup action, double-dispose is safe | None (pure unit) |
| US-004 | `FileAccessConsentPolicyTests` | Pattern matching: exact, glob *, glob **, ?, no patterns = not allowed, case handling | None (pure unit) |
| US-001, US-002 | `RemoteClientFileAccessTests` | GetFile sends push + awaits response, SaveFile writes temp + sends push, errors propagate, MaxFileSizeBytes enforced, temp cleanup on failure | `Mock<HubInvocationContext>`, `Mock<RpcMessageRegistry>`, `MockFileSystem` |

### UX Tests

| User Story | Test Class | What's Tested | Fixtures/Helpers |
|-----------|-----------|---------------|------------------|
| US-004, US-005 | `FileAccessConsentHandlerTests` | Prompt renders with correct path, approve returns true, deny returns false, prompt visually distinct (Panel markup), buffered output flushes after prompt, batch prompt shows file list | `VirtualConsole`, `VirtualConsoleAssertions` |

### Integration Tests (TestEnvironment — real client/server)

| User Story | Test Name | What's Tested | Infrastructure |
|-----------|-----------|---------------|----------------|
| US-001 | `SaveFile_RemoteCommand_FileAppearsOnClient` | Register test command on server that saves file via `IClientFileAccess` → verify file exists on client temp dir | `TestEnvironment`, temp dirs, `--allow-path` |
| US-002 | `GetFile_RemoteCommand_ReadsClientFile` | Place file in client temp → register test command that reads via `IClientFileAccess` → verify command received correct content | `TestEnvironment`, temp dirs, `--allow-path` |
| US-003 | `SaveFile_LocalCommand_WritesDirectly` | Register command locally → run → verify file written without server | `CommandLineApplication` (no server) |
| US-003 | `GetFile_LocalCommand_ReadsDirectly` | Place file → register command locally → run → verify read | `CommandLineApplication` (no server) |
| US-004 | `GetFile_NoAllowPath_PromptsForConsent` | Don't set `--allow-path` → server requests file → verify prompt appears on `VirtualConsole` | `TestEnvironment`, `VirtualConsole`, keyboard input |
| US-004 | `GetFile_AllowPathConfigured_NoPrompt` | Set `--allow-path` → server requests file in allowed path → verify no prompt, transfer succeeds | `TestEnvironment`, `--allow-path` |
| US-004 | `GetFile_UserDenies_CommandReceivesError` | Server requests file → simulate 'N' keypress → verify command gets `FileAccessDeniedException` | `TestEnvironment`, `VirtualConsole`, keyboard input |
| US-005 | `ConsentPrompt_DuringOutput_OutputBuffered` | Run command that streams output AND requests file → verify output pauses during prompt → resumes after | `TestEnvironment`, `VirtualConsole` |
| US-006 | `SaveFile_Stream_ContentArrivesOnClient` | Register command that saves `MemoryStream` → verify content on client | `TestEnvironment`, `--allow-path` |
| US-007 | `GetFiles_GlobPattern_AllMatchesReturned` | Place 3 matching + 1 non-matching file → run command with glob → verify correct files received | `TestEnvironment`, `--allow-path` |
| US-007 | `GetFiles_LazyEnumeration_TransfersPerIteration` | Run command consuming only first 2 of 5 matches → verify only 2 HTTP uploads occurred | `TestEnvironment`, HTTP request counting |
| Edge | `GetFile_ClientDisconnects_ServerGetsError` | Start file request → disconnect client → verify server command gets exception | `TestEnvironment`, forced disconnect |
| Edge | `SaveFile_ExceedsMaxSize_Rejected` | Generate file larger than `MaxFileSizeBytes` → verify rejection error | `TestEnvironment` |
| Edge | `GetFile_FileNotFound_ReturnsError` | Request non-existent client file → verify `FileNotFoundException` | `TestEnvironment`, `--allow-path` |

**Test Commands**: Integration tests will register purpose-built test commands on the server (e.g., `TestSaveFileCommand`, `TestGetFileCommand`) that use `IClientFileAccess` and expose results via pipeline data or temp file markers. These test commands live in the test project, registered via `TestEnvironment` server options.

**TDD Approach**: Follow the `tdd-workflow` skill — write failing tests first (RED), implement to pass (GREEN), refactor.

## Implementation Phases

### Phase 1: Core Types & Local Implementation
- **Purpose**: Deliver `IClientFileAccess` interface, `ClientFile`, `FileTransferProgress`, `FileAccessDeniedException`, and `LocalClientFileAccess`. Commands can use the service locally.
- **Dependencies**: None (foundational)
- **Delivers**: US-003 (local execution path), FR-001 through FR-005, FR-008, FR-015, FR-019
- **Tests**: `LocalClientFileAccessTests`, `ClientFileTests` (unit)
- **Integration**: `SaveFile_LocalCommand_WritesDirectly`, `GetFile_LocalCommand_ReadsDirectly`

### Phase 2: Protocol Messages & Server Implementation
- **Purpose**: Add message envelopes, extend `PushMessageType` / `ServerRequestType`, implement `RemoteClientFileAccess`, register in server DI. Server-side commands can now call `IClientFileAccess` methods.
- **Dependencies**: Phase 1 (interface must exist)
- **Delivers**: FR-006, FR-007, FR-016, FR-017, FR-018, FR-025
- **Tests**: `RemoteClientFileAccessTests` (unit with mocked hub context)
- **Note**: Client handler not yet implemented — server sends push messages but client can't handle them yet. Unit tests mock the response side.

### Phase 3: Client Handler & Consent Infrastructure
- **Purpose**: Implement `FileAccessConsentPolicy`, `FileAccessConsentHandler`, and the `ReceiveMessage` handler extensions in `SignalRServerProxy`. Add `--allow-path` to `ConnectCommand`. Wire up client DI. This completes the round-trip.
- **Dependencies**: Phase 2 (server must send push messages)
- **Delivers**: FR-009 through FR-014, FR-020, US-004, US-005
- **Tests**: `FileAccessConsentPolicyTests` (unit), `FileAccessConsentHandlerTests` (UX/VirtualConsole)
- **Integration**: `SaveFile_RemoteCommand_FileAppearsOnClient`, `GetFile_RemoteCommand_ReadsClientFile`, all consent integration tests, `SaveFile_Stream_ContentArrivesOnClient`

### Phase 4: Glob Pattern Support
- **Purpose**: Implement `GetFilesAsync` with lazy `IAsyncEnumerable` transfer, client-side glob expansion, and batch consent.
- **Dependencies**: Phase 3 (single-file round-trip must work)
- **Delivers**: FR-021 through FR-024, US-007
- **Tests**: `GetFiles_GlobPattern_AllMatchesReturned`, `GetFiles_LazyEnumeration_TransfersPerIteration` (integration)

### Phase 5: Edge Cases & Hardening
- **Purpose**: Handle disconnection, partial file cleanup, max file size enforcement, file-not-found propagation, concurrent prompt serialization.
- **Dependencies**: Phase 3, Phase 4
- **Parallel with**: Can partially overlap Phase 4
- **Delivers**: FR-020, FR-025, all edge cases from spec
- **Tests**: `GetFile_ClientDisconnects_ServerGetsError`, `SaveFile_ExceedsMaxSize_Rejected`, `GetFile_FileNotFound_ReturnsError` (integration)

### Phase Dependency Graph

```
Phase 1 (Core Types + Local)
    │
    ▼
Phase 2 (Protocol + Server Impl)
    │
    ▼
Phase 3 (Client Handler + Consent)
    │         │
    ▼         ▼
Phase 4    Phase 5
(Glob)     (Hardening) ← can partially overlap Phase 4
```

## Complexity Tracking

> No constitution violations. Feature reuses all existing HTTP transfer infrastructure, SignalR messaging patterns, and DI registration conventions. No new external dependencies introduced.
