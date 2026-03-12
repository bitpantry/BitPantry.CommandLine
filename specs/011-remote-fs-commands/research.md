# Research: Remote File System Management Commands (011-remote-fs-commands)

> Phase 0 output — all unknowns resolved before Phase 1 design begins.

---

## R-001: Command Architecture — Client vs. Server Package

**Question**: Should `ls`, `mkdir`, `rm`, `mv`, `cp`, `stat`, `cat` live in the client project or the server project?

**Decision**: All seven commands are implemented as **server-side commands** in `BitPantry.CommandLine.Remote.SignalR.Server/Commands/`.

**Rationale**:
- `ServerLogic.Run()` creates a `CommandLineApplicationCore` using the **server's own command registry** and runs the command string through it directly. Output from `IAnsiConsole` flows back to the client via `SignalRAnsiConsole` (which wraps `IClientProxy`).
- When a client connects, `ClientLogic.OnConnect()` calls `_commandRegistry.RegisterCommandsAsRemote(server.Commands)`, which registers all server-registered commands as remote stubs on the client. From the user's perspective, they just type `ls` or `mkdir` — the routing is transparent.
- `IFileSystem` on the server is already configured as `SandboxedFileSystem` (path validation, size/extension bounds built in). Server commands inject `IFileSystem` and get sandboxed access automatically — no custom path validation code needed per command.
- All these commands are **purely server-side operations** (they operate on the server's sandboxed storage, never touching the local machine). Putting them on the server eliminates the entire RPC envelope/handler layer.
- Interactive prompts (`AnsiConsole.Confirm()`, `AnsiConsole.Ask<>()`) work over SignalR via `SignalRAnsiInput`, which brokers keyboard input back to the client terminal as an RPC round-trip.

**Comparison with `UploadCommand` and `DownloadCommand`**: Those commands live client-side because they inherently involve local file system access (reading local files to send, writing downloaded bytes to local disk). These new commands have no local file system interaction — they are entirely remote operations, so they belong on the server.

**Alternatives considered**:
- Client-side commands with new RPC calls: Rejected — requires significant additional infrastructure (new `ServerRequestType` enum values, request/response envelope classes, hub switch cases, RPC handler methods) for every operation. Server-side eliminates all of this.

---

## R-002: Infrastructure Required

**Question**: What new infrastructure (RPCs, envelopes, handlers) is needed?

**Decision**: **None**. All existing infrastructure is sufficient.

| Component | Status |
|-----------|--------|
| `ServerRequestType` enum | No new values needed |
| Request/Response envelope classes | No new classes needed |
| `CommandLineHub` switch cases | No new cases needed |
| `FileSystemRpcHandler` methods | No new methods needed |
| HTTP endpoint changes | No changes needed |
| `FileContentService` / new client services | Not needed |

**Rationale**: `RunRequest` / `RunResponse` already handle arbitrary command execution. `SignalRAnsiConsole` streams output. `SignalRAnsiInput` handles interactive prompts. `SandboxedFileSystem` handles path security. The entire RPC plumbing layer is bypassed.

---

## R-003: `server ls` — Directory Listing

**Decision**: `LsCommand` is server-side. It injects `IFileSystem` and calls `Directory.GetFileSystemEntries()` (for both files and dirs) directly. For `--recursive`, uses `SearchOption.AllDirectories`. For glob patterns, applies `Microsoft.Extensions.FileSystemGlobbing.Matcher` (same as `FileSystemRpcHandler.HandleEnumerateFiles`). Formats output via `IAnsiConsole`:
- Default: one entry per line, directory names suffixed with `/`
- `--long` / `-l`: `Spectre.Console.Table` with columns (Type, Name, Size, Last Modified)
- `--recursive`: `Spectre.Console.Tree` view showing hierarchy

For sorting, the command sorts the in-memory collection by name (default), size, or last modified, optionally reversed.

---

## R-004: `server cat` — Text Output

**Decision**: `CatCommand` is server-side. It reads file content via `IFileSystem.File.ReadAllLines()` / `OpenRead()` and writes lines to `IAnsiConsole`.

- `--head=N` / `-n N`: Read and output only the first N lines (bounded output, no large SignalR payload)
- `--tail=N` / `-t N`: Read all lines, output only the last N
- **Binary detection**: Before outputting, read the first 8KB via `IFileSystem.File.OpenRead()` and scan for null bytes (`\0`). If binary content detected and no `--force` flag, display an error and abort.
- **Large file warning**: If file size > 1MB and neither `--head` nor `--tail` specified, prompt: `"File is {size}. Display all? [y/N]"` (default No). With `--force`, skip the prompt.

No HTTP endpoint extension or new client service is needed. All output is normal console output streamed back via `SignalRAnsiConsole`.

**Why this is safe in SignalR**: `--head` and `--tail` bound the output to N lines. Without them, the user explicitly chose to dump the whole file (same behaviour as `cat` in any terminal). `SignalRAnsiConsole` writes each line as a message; there is no single huge payload.

---

## R-005: `server rm` — Deletion Flow

**Decision**: `RmCommand` is server-side. It resolves the target directly via `IFileSystem`.

- **Glob pattern** (e.g., `rm *.log`): Apply `Matcher` against `IFileSystem.Directory.GetFileSystemEntries()` to enumerate matching entries. If match count ≥ 5 (constant `RmConfirmationThreshold`) and no `--force`, prompt `"Delete N items? [y/N]"` (default No).
- **Single path**: Determine if file or directory, then:
  - File → `IFileSystem.File.Delete()`
  - Empty directory → `IFileSystem.Directory.Delete()` (requires `-d`)
  - Non-empty directory → `IFileSystem.Directory.Delete(recursive: true)` (requires `-r`)
- **Safety**: Block deletion of the storage root path (compare normalized paths).
- **Force flag (`-f`)**: Skips confirmation prompts and suppresses "does not exist" errors.
- Per-item feedback written to `IAnsiConsole`.

---

## R-006: `server mv`, `server cp` — Move and Copy

**Decision**: Both commands are server-side, operating directly on `IFileSystem`.

**`MvCommand`**:
- `IFileSystem.File.Move(src, dst, overwrite)` for files
- `IFileSystem.Directory.Move(src, dst)` for directories (target must not exist on .NET unless OS-specific)
- Checks that source exists, destination parent directory exists, source ≠ destination
- `--force` / `-f`: allows overwrite for file moves

**`CpCommand`**:
- `IFileSystem.File.Copy(src, dst, overwrite)` for files
- Recursive directory copy implemented as a helper that walks `GetFiles()` + `GetDirectories()`
- `--recursive` / `-r`: required when source is a directory
- `--force` / `-f`: allows overwriting existing destination files

Both display a summary line on success (e.g., `Moved: /src → /dst`).

---

## R-007: `server stat` — Entry Metadata

**Decision**: `StatCommand` is server-side. Uses `IFileSystem.FileInfo.New(path)` for files, `IFileSystem.DirectoryInfo.New(path)` for directories.

For directories, recursively sums file sizes via `IFileSystem.Directory.GetFiles(path, "*", SearchOption.AllDirectories)` to compute `TotalSize`.

Output rendered as a two-column `Spectre.Console.Table` (no border, label-aligned), showing path, type, size (formatted), created, last modified; for directories adds item counts.

---

## R-008: Testing Approach

**Question**: What mix of unit and integration tests should be used?

**Decision**: Two test levels. No `ClientTests/` for these commands because there is no client code.

### Level 1 — Server Unit Tests (`ServerTests/`)

Inject `MockFileSystem` + mock `IAnsiConsole` (Spectre `TestConsole`). No SignalR, no network. Tests are fast and isolated.

**Setup pattern**:
```csharp
[TestInitialize]
public void Setup()
{
    _fileSystem = new MockFileSystem();
    _console = new TestConsole();
    _command = new LsCommand(_fileSystem, _console);
}
```

**Use for**:
- Every flag/argument combination (per user requirement)
- Happy path output format
- Error conditions (path not found, wrong type, etc.)
- Security: path traversal attempts rejected by `SandboxedFileSystem`
- Glob expansion correctness
- Confirmation prompt logic (bypass with `--force`, trigger at threshold)
- Binary detection in `cat`
- Recursive operations in `cp` / `ls --recursive`

### Level 2 — Integration Tests (`IntegrationTests/`)

Using `TestEnvironment` with in-memory ASP.NET `TestServer`, `VirtualConsole` output capture, real temp file system, real SignalR over HTTP long-polling.

```csharp
using var env = new TestEnvironment(opts =>
{
    opts.ServerOptions = new TestServerOptions { StorageRootPath = _tempDir };
});
await env.Cli.Run($"server connect -u {env.ServerUrl}");
await env.Cli.Run("server mkdir /data/reports");
env.Console.Should().ContainText("Created: /data/reports");
```

**Use for**:
- Happy path end-to-end: command input → output in VirtualConsole
- Confirmation that server-registered commands appear as remote stubs on client after connect
- File system state changes (directory actually created, file actually deleted)
- `cat` output of files with known content
- Error display end-to-end

**Rationale**: Two tiers instead of three. The original `UploadCommand`/`DownloadCommand` pattern used `ClientTests/` for unit tests because those commands live in the client library and have client-side logic (HTTP requests, progress display, `IServerProxy` mocking). Server-side commands have no client stub to unit-test; their logic lives on the server and uses `IFileSystem`/`IAnsiConsole` directly.

---

## R-009: Auto-Registration on Server

**Question**: How do the new server-side commands get registered in the server's command registry?

**Decision**: In `IServiceCollectionExtensions.AddCommandLineHub()`, add explicit `opt.RegisterCommand<T>()` calls for each new command — the same pattern as `builder.RegisterCommand<UploadCommand>()` in the client library's `CommandLineApplicationBuilderExtensions.cs`.

```csharp
// In AddCommandLineHub(), after existing setup:
opt.RegisterCommand<LsCommand>();
opt.RegisterCommand<MkdirCommand>();
opt.RegisterCommand<RmCommand>();
opt.RegisterCommand<MvCommand>();
opt.RegisterCommand<CpCommand>();
opt.RegisterCommand<CatCommand>();
opt.RegisterCommand<StatCommand>();
```

These registrations happen before `opt.CommandRegistryBuilder.Build(services)` is called. The resulting `commandRegistry` includes all seven commands, which then flow to the client via `CreateClientResponse.Commands` on connection.

---

## R-010: Autocomplete for Server-Side Commands

**Question**: Does `[ServerFilePathAutoComplete]` work on server-side command arguments?

**Decision**: Yes, it works exactly as desired.

`ServerFilePathAutoCompleteAttribute` and `ServerFilePathAutoCompleteHandler` are defined in the **shared** project (`BitPantry.CommandLine.Remote.SignalR`) which the server project already references. The handler uses `[FromKeyedServices(PathEntryProviderKeys.Server)] IPathEntryProvider` — on the server, this resolves to `LocalPathEntryProvider(IFileSystem)`, which enumerates the sandboxed file system directly. No RPC round-trip for autocomplete.

**Attribute assignments**:

| Command | Argument | Attribute |
|---------|----------|-----------|
| `ls` | `path` | `[ServerFilePathAutoComplete]` |
| `mkdir` | `path` | `[ServerDirectoryPathAutoComplete]` |
| `rm` | `path` | `[ServerFilePathAutoComplete]` |
| `mv` | `source`, `destination` | `[ServerFilePathAutoComplete]` |
| `cp` | `source`, `destination` | `[ServerFilePathAutoComplete]` |
| `cat` | `path` | `[ServerFilePathAutoComplete]` (files only if `ServerFilesOnlyPathAutoComplete` exists, else standard) |
| `stat` | `path` | `[ServerFilePathAutoComplete]` |

---

## R-011: Command Group for Server-Side Commands

**Question**: The existing client commands use `[InGroup<ServerGroup>]`. Is there an equivalent for server-side commands? Can two `ServerGroup` classes with the same name exist in separate projects?

**Decision**: Define a separate `ServerGroup` class in the server project (`Server/Commands/ServerGroup.cs`). The two types are independent — no sharing required.

**How it works** (verified from `CommandRegistry.RegisterCommandsAsRemote` and `EnsureRemoteGroupHierarchy`):

When the server serializes its commands, `CommandInfo.GroupPath` is stored as the string `"server"` (the `[Group(Name = "server")]` value).

On the client, `RegisterCommandsAsRemote()` calls `EnsureRemoteGroupHierarchy("server")`, which runs:
```csharp
var existingGroup = _localGroups.Concat(_remoteGroups).FirstOrDefault(g =>
    g.Name.Equals(part, NameComparison) && g.Parent == parent);
```

This finds the **already-registered** client-side `ServerGroup` by its name `"server"` and parent `null`. It reuses that group — no duplicate is created. The server's `[InGroup<ServerGroup>]` commands are registered under the same group node as `server connect`, `server upload`, etc.

**The server's `ServerGroup.cs` exists only so the server project compiles** — it is never transmitted over the wire. The group identity is entirely string-based at runtime.

**Two groups with the same name cannot co-exist**: `EnsureRemoteGroupHierarchy` will always find and reuse an existing local group before creating a new one, so there is no risk of duplication.

---

## Summary of Decisions

| Unknown | Decision |
|---------|----------|
| Client vs. server package for commands | **All server-side**, in `BitPantry.CommandLine.Remote.SignalR.Server/Commands/` |
| New RPC infrastructure needed | **None** — `RunRequest/RunResponse` + `SignalRAnsiConsole` is sufficient |
| `cat` streaming mechanism | Server-side command via `IFileSystem + IAnsiConsole`, `--head`/`--tail` bound output |
| `ls` file+directory enumeration | `IFileSystem.Directory.GetFileSystemEntries()` + `Matcher` for glob, sorted in memory |
| `rm` glob deletion | `Matcher` against `IFileSystem` on server, `IAnsiConsole.Confirm()` for confirmation |
| `mv`/`cp` implementation | `IFileSystem.File.Move/Copy` + recursive dir helper on server |
| `stat` metadata | `IFileSystem.FileInfo/DirectoryInfo` + recursive sum for directories |
| Testing approach | Server unit tests (MockFileSystem + TestConsole) + Integration tests (TestEnvironment) |
| Auto-registration | `opt.RegisterCommand<T>()` in `AddCommandLineHub()` before `Build(services)` |
| Autocomplete path | `[ServerFilePathAutoComplete]` from shared project → `LocalPathEntryProvider` on server |
| Command group | `[InGroup<ServerGroup>]` — verify `ServerGroup` is accessible from server project |
