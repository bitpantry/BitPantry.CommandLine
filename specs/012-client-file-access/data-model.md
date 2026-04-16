# Data Model: Client File Access (012-client-file-access)

> Defines all new types, message envelopes, and service interfaces introduced by this feature.

---

## Core Interface

### `IClientFileAccess` (new, `BitPantry.CommandLine` project)

Location-transparent file access service. Commands inject this to read/write files on the calling user's machine.

```csharp
public interface IClientFileAccess
{
    Task<ClientFile> GetFileAsync(string clientPath, IProgress<FileTransferProgress>? progress = null, CancellationToken ct = default);

    IAsyncEnumerable<ClientFile> GetFilesAsync(string clientGlobPattern, IProgress<FileTransferProgress>? progress = null, CancellationToken ct = default);

    Task SaveFileAsync(Stream content, string clientPath, IProgress<FileTransferProgress>? progress = null, CancellationToken ct = default);

    Task SaveFileAsync(string sourcePath, string clientPath, IProgress<FileTransferProgress>? progress = null, CancellationToken ct = default);
}
```

Design notes:
- Uses `IProgress<T>` (standard .NET pattern) instead of `Func<T, Task>` — composable, non-blocking, well-understood.
- `GetFilesAsync` returns `IAsyncEnumerable<ClientFile>` for lazy per-file transfer.
- `CancellationToken` is last parameter (convention).

### `ClientFile` (new, `BitPantry.CommandLine` project)

Disposable handle returned by `GetFileAsync` / `GetFilesAsync`. Provides stream access and metadata.

| Property | Type | Description |
|----------|------|-------------|
| `Stream` | `Stream` | Readable stream of file content |
| `FileName` | `string` | File name (no directory) |
| `Length` | `long` | File size in bytes |

```csharp
public sealed class ClientFile : IAsyncDisposable
{
    public Stream Stream { get; }
    public string FileName { get; }
    public long Length { get; }

    private readonly Func<ValueTask>? _cleanupAsync;

    public ClientFile(Stream stream, string fileName, long length, Func<ValueTask>? cleanupAsync = null)
    {
        Stream = stream;
        FileName = fileName;
        Length = length;
        _cleanupAsync = cleanupAsync;
    }

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
        if (_cleanupAsync != null)
            await _cleanupAsync();
    }
}
```

**Local implementation**: `cleanupAsync` is null — just closes the `FileStream`.
**Remote implementation**: `cleanupAsync` deletes the server-side temp file.

### `FileTransferProgress` (new, `BitPantry.CommandLine` project)

```csharp
public record FileTransferProgress(long BytesTransferred, long TotalBytes);
```

### `FileAccessDeniedException` (new, `BitPantry.CommandLine` project)

```csharp
public class FileAccessDeniedException : Exception
{
    public string RequestedPath { get; }
    public FileAccessDeniedException(string requestedPath)
        : base($"File access denied by client: {requestedPath}")
    {
        RequestedPath = requestedPath;
    }
}
```

---

## Implementations

### `LocalClientFileAccess` (new, `BitPantry.CommandLine` project)

Used when commands run locally. Direct file system operations, no network.

| Method | Implementation |
|--------|---------------|
| `GetFileAsync` | Open `FileStream(path, Read, Share.Read)`, return `ClientFile` |
| `GetFilesAsync` | Expand glob via `GlobPatternHelper` + `FileSystemGlobbing`, yield `ClientFile` per match |
| `SaveFileAsync(stream, ...)` | Create parent dirs, write stream to path |
| `SaveFileAsync(path, ...)` | Create parent dirs, `File.Copy(source, dest, overwrite: true)` |

Dependencies: `IFileSystem`

### `RemoteClientFileAccess` (new, `BitPantry.CommandLine.Remote.SignalR.Server` project)

Used when commands run on the server. Coordinates file transfers via SignalR push messages + existing HTTP endpoints.

| Method | Implementation |
|--------|---------------|
| `GetFileAsync` | Send `ClientFileUploadRequest` push → client uploads to server temp → open temp file → return `ClientFile` (cleanup deletes temp) |
| `GetFilesAsync` | Send `ClientFileEnumerateRequest` push → client expands glob → sends file list → consent → yield each file via individual `GetFileAsync` |
| `SaveFileAsync(stream, ...)` | Write stream to server temp → send `ClientFileDownloadRequest` push → client downloads from server temp → delete temp |
| `SaveFileAsync(path, ...)` | Send `ClientFileDownloadRequest` push referencing the source path directly → client downloads from server |

Dependencies: `HubInvocationContext`, `RpcMessageRegistry`, `IFileSystem` (for temp file management), `FileTransferOptions` (for `MaxFileSizeBytes`, staging path)

---

## Protocol Messages

All messages follow existing `MessageBase` pattern with `Dictionary<string, string>` data.

### New `PushMessageType` Values

```csharp
public enum PushMessageType
{
    FileUploadProgress,           // existing
    ClientFileUploadRequest,      // NEW: server asks client to upload a file
    ClientFileDownloadRequest,    // NEW: server asks client to download a file
    ClientFileEnumerateRequest    // NEW: server asks client to expand a glob pattern
}
```

### `ClientFileUploadRequest` (server → client push)

Server asks client to upload a specific file to the server.

| Field | Type | Description |
|-------|------|-------------|
| `CorrelationId` | `string` | For matching response |
| `ClientPath` | `string` | Path on client machine |
| `ServerTempPath` | `string` | Where to upload on server (via existing `/fileupload` endpoint) |

### `ClientFileDownloadRequest` (server → client push)

Server asks client to download a file from the server.

| Field | Type | Description |
|-------|------|-------------|
| `CorrelationId` | `string` | For matching response |
| `ServerPath` | `string` | Path on server to download (via existing `/filedownload` endpoint) |
| `ClientPath` | `string` | Where to save on client |
| `FileSize` | `long` | For progress tracking |

### `ClientFileEnumerateRequest` (server → client push)

Server asks client to expand a glob pattern and return matched file info.

| Field | Type | Description |
|-------|------|-------------|
| `CorrelationId` | `string` | For matching response |
| `GlobPattern` | `string` | Glob pattern to expand on client |

### New `ServerRequestType` Value (for responses)

```csharp
public enum ServerRequestType
{
    // ... existing ...
    ClientFileAccessResponse = 6   // NEW: client sends result of file access operation
}
```

### `ClientFileAccessResponse` (client → server)

Generic response for all client file access operations.

| Field | Type | Description |
|-------|------|-------------|
| `CorrelationId` | `string` | Matches the originating push message |
| `Success` | `bool` | Whether the operation succeeded |
| `Error` | `string?` | Error message if failed |
| `FileInfoEntries` | `string?` | JSON array of `{Path, Size}` for enumerate responses |

---

## Consent Infrastructure

### `FileAccessConsentPolicy` (new, `BitPantry.CommandLine.Remote.SignalR.Client` project)

Evaluates whether a file access request requires user consent.

| Property | Type | Description |
|----------|------|-------------|
| `AllowedPatterns` | `IReadOnlyList<string>` | Glob patterns from `--allow-path` |

| Method | Behavior |
|--------|----------|
| `IsAllowed(string path)` | Returns `true` if path matches any allowed pattern |
| `RequiresConsent(string path)` | Returns `!IsAllowed(path)` |
| `RequiresConsent(IEnumerable<string> paths)` | Returns paths not covered by allowed patterns |

### `FileAccessConsentHandler` (new, `BitPantry.CommandLine.Remote.SignalR.Client` project)

Manages the consent prompt UX including console output buffering.

| Method | Behavior |
|--------|----------|
| `RequestConsentAsync(string path, CancellationToken)` | Evaluates policy → if allowed, returns true; else pauses output, shows prompt, resumes output |
| `RequestBatchConsentAsync(IReadOnlyList<string> paths, IReadOnlyList<long> sizes, string pattern, CancellationToken)` | Same but tiered batch display based on count thresholds |

Dependencies: `FileAccessConsentPolicy`, `IAnsiConsole` (for prompt rendering), console output pause/resume mechanism.

---

## Constants

| Constant | Value | Used By |
|----------|-------|---------|
| `ClientFileAccessStagingDir` | `.client-file-staging` | `RemoteClientFileAccess` — temp dir within sandbox for staging transfers |
| `ConsentFullListThreshold` | `10` | `FileAccessConsentHandler` — show all files individually at or below this count |
| `ConsentSummaryOnlyThreshold` | `50` | `FileAccessConsentHandler` — show summary only (no file list) above this count |
| `ConsentCollapsedHeadCount` | `5` | `FileAccessConsentHandler` — number of files to show at top of collapsed list |
| `ConsentCollapsedTailCount` | `2` | `FileAccessConsentHandler` — number of files to show at bottom of collapsed list |

---

## Relationship to Existing Entities

| Existing Entity | Relationship |
|----------------|-------------|
| `FileTransferService` | Client-side handler calls `UploadFile()` / `DownloadFile()` to perform actual HTTP transfer |
| `FileTransferEndpointService` | Server HTTP endpoints serve/receive files — no changes needed |
| `PushMessage` / `ReceiveMessage` | Extended with new message types for server→client file access requests |
| `ServerRequest` / `ReceiveRequest` | Extended with response type for client→server file access completions |
| `HubInvocationContext` | `RemoteClientFileAccess` uses this to send push messages to the calling client |
| `RpcMessageRegistry` | Used for correlation between push request and client response |
| `GlobPatternHelper` | Reused by both `LocalClientFileAccess` (local expansion) and client handler (remote expansion) |
| `ConnectCommand` | Extended with `--allow-path` argument |
| `CommandLineClientSettings` | Extended to store allowed path patterns for the session |
