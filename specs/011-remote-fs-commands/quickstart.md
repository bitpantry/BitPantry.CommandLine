# Quickstart: Remote File System Management Commands (011-remote-fs-commands)

> Developer onboarding guide. Start with `MkdirCommand` — the simplest command.

---

## Architecture in 30 Seconds

Server-side commands run on the server and stream output back to the client via `SignalRAnsiConsole`. No custom RPC is needed.

```
User types: server mkdir /data/reports

Client → RunRequest("mkdir /data/reports") → Server
Server → ServerLogic.Run() → CommandLineApplicationCore.Run()
                           → MkdirCommand.Execute()
                              → IFileSystem.Directory.CreateDirectory()
                              → IAnsiConsole.MarkupLine("Created: ...")
                                 ↓ (SignalRAnsiConsole writes to IClientProxy)
Client ← console output streams back ← Server
```

After `server connect`, the server's commands appear in the client's registry as remote stubs. Running them sends a `RunRequest` to the server; output flows back through `SignalRAnsiConsole`.

---

## Implementing `MkdirCommand` — Step by Step

### Step 1: Create `ServerGroup.cs`

```csharp
// BitPantry.CommandLine.Remote.SignalR.Server/Commands/ServerGroup.cs
using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands;

[Group(Name = "server")]
[Description("Remote server connection commands")]
public class ServerGroup { }
```

The group name `"server"` matches the client's `ServerGroup`. Remote stubs merge into the existing group automatically.

### Step 2: Write the failing unit test

```csharp
// BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/MkdirCommandTests.cs
[TestClass]
public class MkdirCommandTests
{
    private MockFileSystem _fileSystem;
    private TestConsole _console;
    private MkdirCommand _command;

    [TestInitialize]
    public void Setup()
    {
        _fileSystem = new MockFileSystem();
        _fileSystem.AddDirectory("/storage");  // simulate storage root
        _console = new TestConsole();
        _command = new MkdirCommand(_fileSystem, _console);
    }

    [TestMethod]
    public async Task Execute_WithValidPath_CreatesDirectory()
    {
        // Arrange
        _command.Path = "/storage/reports";

        // Act
        await _command.Execute(new CommandExecutionContext());

        // Assert
        _fileSystem.Directory.Exists("/storage/reports").Should().BeTrue();
        _console.Output.Should().Contain("Created: /storage/reports");
    }

    [TestMethod]
    public async Task Execute_WhenParentMissing_WithoutParentsFlag_DisplaysError()
    {
        _command.Path = "/storage/deep/nested/path";

        await _command.Execute(new CommandExecutionContext());

        _fileSystem.Directory.Exists("/storage/deep/nested/path").Should().BeFalse();
        _console.Output.Should().Contain("Parent directory does not exist");
    }

    [TestMethod]
    public async Task Execute_WhenParentMissing_WithParentsFlag_CreatesAll()
    {
        _command.Path = "/storage/deep/nested/path";
        _command.Parents = true;

        await _command.Execute(new CommandExecutionContext());

        _fileSystem.Directory.Exists("/storage/deep/nested/path").Should().BeTrue();
    }
}
```

Run tests — they fail (MkdirCommand doesn't exist yet). ✓

### Step 3: Create `MkdirCommand.cs`

```csharp
// BitPantry.CommandLine.Remote.SignalR.Server/Commands/MkdirCommand.cs
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using System.IO.Abstractions;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands;

[InGroup<ServerGroup>]
[Command(Name = "mkdir")]
[Description("Create a directory on the remote server")]
public class MkdirCommand : CommandBase
{
    private readonly IFileSystem _fileSystem;
    private readonly IAnsiConsole _console;

    [Argument(Position = 0, Name = "path", IsRequired = true)]
    [Description("Directory path to create")]
    [ServerDirectoryPathAutoComplete]
    public string Path { get; set; }

    [Argument(Name = "parents"), Alias('p'), Flag]
    [Description("Create parent directories as needed")]
    public bool Parents { get; set; }

    public MkdirCommand(IFileSystem fileSystem, IAnsiConsole console)
    {
        _fileSystem = fileSystem;
        _console = console;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        if (!Parents)
        {
            var parent = _fileSystem.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(parent) && !_fileSystem.Directory.Exists(parent))
            {
                _console.MarkupLine($"[red]Parent directory does not exist. Use --parents to create.[/]");
                return;
            }
        }

        _fileSystem.Directory.CreateDirectory(Path);
        _console.MarkupLine($"Created: {Path}");
    }
}
```

Run tests — they pass. ✓

### Step 4: Register in `AddCommandLineHub`

```csharp
// In IServiceCollectionExtensions.AddCommandLineHub(), before Build(services):
opt.RegisterCommand<MkdirCommand>();
```

### Step 5: Write the integration test

```csharp
// IntegrationTests/IntegrationTests_MkdirCommand.cs
[TestClass]
public class IntegrationTests_MkdirCommand
{
    private string _tempDir;

    [TestInitialize]
    public void Setup() => _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [TestCleanup]
    public void Cleanup() { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }

    [TestMethod]
    public async Task MkdirCommand_CreatesDirectory_OnServer()
    {
        Directory.CreateDirectory(_tempDir);
        using var env = new TestEnvironment(opts =>
        {
            opts.ServerOptions = new TestServerOptions { StorageRootPath = _tempDir };
        });
        await env.Cli.Run($"server connect -u {env.ServerUrl}");

        await env.Cli.Run("server mkdir /reports");

        Directory.Exists(System.IO.Path.Combine(_tempDir, "reports")).Should().BeTrue();
        env.Console.Should().ContainText("Created");
    }
}
```

---

## Adding Each Subsequent Command

Follow the same pattern:
1. Write failing unit tests for the command's behaviour and all flags
2. Create the command class in `Server/Commands/`
3. Add `opt.RegisterCommand<T>()` in `AddCommandLineHub()`
4. Write one integration test for the happy path
5. All tests pass ✓

### Recommended order (simplest → most complex)

1. `MkdirCommand` — single IFileSystem call, no glob, no prompt
2. `StatCommand` — read-only, no modification, just format output
3. `LsCommand` — read-only, adds glob + sorting + formatting
4. `MvCommand` — single atomic operation, two paths
5. `CpCommand` — adds recursive copy helper
6. `RmCommand` — adds glob + confirmation prompt
7. `CatCommand` — adds binary detection + large-file prompt + head/tail

---

## Key DI Wiring (Already Done — No Changes Needed)

```
IFileSystem → SandboxedFileSystem (registered in AddCommandLineHub)
IAnsiConsole → SignalRAnsiConsole (created in ServerLogic.Run per request)
[ServerFilePathAutoComplete] → ServerFilePathAutoCompleteHandler
                             → IPathEntryProvider[Server] → LocalPathEntryProvider(IFileSystem)
```

Interactive prompts (`_console.Confirm(...)`) work via:
```
SignalRAnsiConsole → SignalRAnsiInput → IClientProxy.Rpc<ReadKeyResponse>(ReadKeyRequest)
                                     ← client sends key press back
```

---

## Common Gotchas

| Issue | Fix |
|-------|-----|
| `MockFileSystem` doesn't recognise separator | Use forward slashes in test paths; `MockFileSystem` handles them |
| `SandboxedFileSystem` blocks a path in integration test | Ensure path is under `StorageRootPath` |
| Command not visible after `server connect` | Check `opt.RegisterCommand<T>()` is called before `Build(services)` |
| `TestConsole` output is empty | Call `await _command.Execute(ctx)` before checking `_console.Output` |
