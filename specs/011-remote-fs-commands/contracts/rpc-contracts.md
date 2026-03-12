# Contracts: Remote File System Management Commands (011-remote-fs-commands)

## RPC Contracts

**No new RPC contracts.** The server-side command architecture eliminates the need for custom request/response envelopes. All seven commands execute via the existing `RunRequest` / `RunResponse` path through `ServerLogic.Run()`.

Existing contracts unchanged:
- `RunRequest` / `RunResponse` — command execution (used by all 7 new commands)
- `CreateClientResponse` — sends server command list to client on connect (automatically includes new commands)
- `EnumeratePathEntriesRequest` / `EnumeratePathEntriesResponse` — autocomplete Tab completion (unchanged)

---

## `ServerGroup` Resolution

Server-side commands use `[InGroup<ServerGroup>]` where `ServerGroup` is defined **in the server project** (`BitPantry.CommandLine.Remote.SignalR.Server/Commands/ServerGroup.cs`):

```csharp
[Group(Name = "server")]
[Description("Remote server connection commands")]
public class ServerGroup { }
```

This is a separate class from the client's `ServerGroup` — no type sharing required. When the server serializes its command list, each command's `GroupPath` becomes the string `"server"`. On the client, `RegisterCommandsAsRemote()` calls `EnsureRemoteGroupHierarchy("server")`, which searches existing `_localGroups` by name and finds the already-registered client group. It reuses that group rather than creating a duplicate. The server's `[InGroup<ServerGroup>]` commands are merged under the same `server` group node as `server connect`, `server upload`, etc. — exactly the right behaviour.

---

## Dependency Notes

No new project references required. The server project already references:
- `BitPantry.CommandLine.Remote.SignalR` (shared) — `ServerFilePathAutoCompleteAttribute`, `ServerDirectoryPathAutoCompleteAttribute`
- `Microsoft.Extensions.FileSystemGlobbing` — `Matcher` for glob patterns
- `System.IO.Abstractions` — `IFileSystem`, `MockFileSystem` in tests
