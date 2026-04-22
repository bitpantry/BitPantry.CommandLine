# Client File Access

`IClientFileAccess` gives commands location-transparent access to files on the calling user's machine. When a command runs locally, operations go directly to the file system. When it runs on a remote server, the service handles file transfers over SignalR/HTTP invisibly.

---

## Using IClientFileAccess

Inject `IClientFileAccess` into any command via the constructor:

```csharp
[Command(Name = "import")]
[InGroup<ServerGroup>]
[Description("Imports data from the client machine")]
public class ImportCommand : CommandBase
{
    private readonly IClientFileAccess _fileAccess;

    public ImportCommand(IClientFileAccess fileAccess) => _fileAccess = fileAccess;

    public async Task Execute(CommandExecutionContext ctx)
    {
        await using var file = await _fileAccess.GetFileAsync(@"c:\data\input.csv", ct: ctx.CancellationToken);
        // file.Stream, file.FileName, file.Length are available
    }
}
```

The same command works identically whether it runs locally or on a remote server — no code changes needed.

---

## Reading a Single File

```csharp
await using var file = await _fileAccess.GetFileAsync(clientPath, progress, ct);
// file.Stream  — the file content
// file.FileName — the file name
// file.Length   — the file size in bytes
```

The returned `ClientFile` is `IAsyncDisposable`. When running remotely, disposing it cleans up temporary server-side copies.

---

## Reading Multiple Files (Glob)

```csharp
await foreach (var file in _fileAccess.GetFilesAsync(@"c:\data\**\*.csv", ct: ctx.CancellationToken))
{
    await using (file)
    {
        // Process each file lazily — transferred one at a time
    }
}
```

Glob expansion happens client-side (where the files are). Files transfer lazily as the caller iterates, keeping memory usage low.

Supported glob patterns:

| Pattern | Matches |
|---------|---------|
| `*.csv` | All CSV files in the directory |
| `**/*.log` | All log files recursively |
| `data?.json` | Single-character wildcard |

---

## Saving Files to the Client

### From a stream (in-memory data)

```csharp
using var stream = new MemoryStream(Encoding.UTF8.GetBytes(reportJson));
await _fileAccess.SaveFileAsync(stream, @"c:\output\report.json", ct: ctx.CancellationToken);
```

### From a server-side file path

```csharp
await _fileAccess.SaveFileAsync(serverFilePath, @"c:\output\result.dat", ct: ctx.CancellationToken);
```

Parent directories are created automatically if they don't exist. Existing files are overwritten.

---

## Consent System

When a command runs on a remote server, the server cannot access client files without the user's consent. This prevents compromised or malicious servers from silently reading or writing arbitrary files.

### How Consent Works

1. The server sends a file access request to the client
2. The client checks the path against configured allow-path patterns
3. If the path is covered, the transfer proceeds silently
4. If not, a consent prompt is displayed (in `Prompt` mode)

The consent prompt is rendered client-side using the actual file path — the server cannot control or misrepresent the prompt text.

### Consent Prompt

When an uncovered path is accessed, the client displays a visually distinct panel:

```
╭─ File Access Request ──────────────────────────────╮
│ Server requests: c:\users\jeff\documents\data.csv  │
│ Allow? y/N                                         │
╰────────────────────────────────────────────────────╯
```

Console output from the running command is temporarily paused while the prompt is active, then resumed after the user responds. This prevents the prompt from interleaving with command output.

### Consent Modes

Configure the consent mode on `server connect` or in a profile:

| Mode | Behavior |
|------|----------|
| `Prompt` | Ask the user for each uncovered path (default) |
| `AllowAll` | Allow all server file access without prompting |
| `DenyAll` | Silently deny uncovered paths — no prompts, no transfers |

```
app> server connect --uri http://server --consent-mode DenyAll
```

### Allow-Path Patterns

Pre-approve paths using glob patterns on `server connect`:

```
app> server connect --uri http://server --allow-path "c:\data\**" --allow-path "c:\exports\*.csv"
```

Or configure them in a [server profile](profiles.md):

```
app> server profile add --name prod --uri https://prod.example.com --allow-path "c:\data\**"
```

Patterns support `*`, `**`, and `?` wildcards. Paths matching any pattern bypass the consent prompt.

### Session-Scoped Remembered Consent

When the user approves a consent prompt, the path is added to the session's allow list. The same path won't prompt again during the current connection. Consent is not persisted across sessions.

### Batch Consent for Glob Operations

When `GetFilesAsync` matches multiple files requiring consent, a single batch prompt is shown:

- **≤ 10 files**: All file paths listed
- **11–50 files**: First 5 and last 2 shown, middle collapsed
- **> 50 files**: Summary with total count and size only

The user approves or denies the entire batch with a single keypress.

---

## Local vs. Remote Behavior

| Aspect | Local | Remote |
|--------|-------|--------|
| File I/O | Direct file system access | HTTP upload/download over SignalR |
| Consent | Not applicable — user is running locally | Consent system gates all access |
| Glob expansion | `FileSystemGlobbing` on local FS | Client-side expansion, files transferred individually |
| Temp cleanup | N/A | Server temp files cleaned on `ClientFile.DisposeAsync()` |
| MaxFileSizeBytes | Not enforced | Server limit applies to all transfers |

---

## Constraints

- File transfers respect the server's `MaxFileSizeBytes` limit. Files exceeding the limit are rejected.
- Transfers are cancellable via `CancellationToken`.
- Partial files are cleaned up on transfer failure.
- The service does not render progress UI itself — callers provide an `IProgress<FileTransferProgress>` callback.

---

## See Also

- [Connecting & Disconnecting](connecting.md) — `--allow-path` and `--consent-mode` arguments
- [Server Profiles](profiles.md) — Persisting consent settings in profiles
- [File Transfers](file-transfers.md) — Explicit `server upload` / `server download` commands
- [File System & Sandboxing](../server/sandboxing.md) — Server-side file constraints
