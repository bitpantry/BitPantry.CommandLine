# Quickstart: Upload Command Development

> Phase 1 output - development setup and getting started guide

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- Git

## Clone and Build

```powershell
# Clone repository (if not already cloned)
git clone https://github.com/bitpantry/BitPantry.CommandLine.git
cd BitPantry.CommandLine

# Switch to feature branch
git checkout 006-upload-command

# Restore and build
dotnet restore
dotnet build
```

## Project Structure

```
BitPantry.CommandLine.Remote.SignalR.Client/
├── UploadCommand.cs           # NEW - implement here
├── ServerGroup.cs             # Existing command group
├── FileTransferService.cs     # Existing upload service
└── SignalRServerProxy.cs      # Connection state

BitPantry.CommandLine.Tests.Remote.SignalR/
├── ClientTests/
│   └── UploadCommandTests.cs  # NEW - unit tests here
└── IntegrationTests/
    └── IntegrationTests_UploadCommand.cs  # NEW - integration tests
```

## Run Tests

```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test BitPantry.CommandLine.Tests.Remote.SignalR

# Run with filter
dotnet test --filter "FullyQualifiedName~UploadCommand"
```

## Development Workflow (TDD)

Per Constitution Principle I:

1. **Write test first** in `UploadCommandTests.cs`
2. **Run test** - verify it fails (red)
3. **Implement** in `UploadCommand.cs`
4. **Run test** - verify it passes (green)
5. **Refactor** if needed
6. Repeat

## Key Files to Reference

| File | Purpose |
|------|---------|
| [ConnectCommand.cs](../../BitPantry.CommandLine.Remote.SignalR.Client/ConnectCommand.cs) | Pattern for ServerGroup commands |
| [DisconnectCommand.cs](../../BitPantry.CommandLine.Remote.SignalR.Client/DisconnectCommand.cs) | Simple command pattern |
| [FileTransferService.cs](../../BitPantry.CommandLine.Remote.SignalR.Client/FileTransferService.cs) | Upload API to use |
| [IntegrationTests_FileTransferService.cs](../../BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/IntegrationTests_FileTransferService.cs) | Integration test patterns |
| [TestEnvironment.cs](../../BitPantry.CommandLine.Tests.Remote.SignalR/Environment/TestEnvironment.cs) | Test infrastructure |

## Command Registration Pattern

```csharp
[Command(Group = typeof(ServerGroup), Name = "upload")]
[Description("Uploads files to the remote server")]
public class UploadCommand : CommandBase
{
    // Constructor injection
    public UploadCommand(
        IServerProxy proxy,
        FileTransferService fileTransferService,
        IAnsiConsole console)
    {
        // ...
    }

    // Positional arguments
    [Argument(Position = 0, Name = "source", IsRequired = true)]
    [Description("Local file path or glob pattern")]
    public string Source { get; set; }

    [Argument(Position = 1, Name = "destination", IsRequired = true)]
    [Description("Remote destination path")]
    public string Destination { get; set; }

    // Async execution
    public async Task<int> Execute(CommandExecutionContext ctx)
    {
        // Implementation
    }
}
```

## Testing with Sandbox

Use the existing sandbox environment for manual testing:

```powershell
cd sandbox
.\start.ps1

# In REPL:
server connect -u http://localhost:5000
server upload myfile.txt /remote/
```

## Dependencies

The command uses these existing services (inject via constructor):

| Service | Interface | Purpose |
|---------|-----------|---------|
| `SignalRServerProxy` | `IServerProxy` | Connection state |
| `FileTransferService` | - | File upload with progress |
| Spectre.Console | `IAnsiConsole` | Progress display |

## Common Patterns

### Check Connection State

```csharp
if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
{
    _console.MarkupLine("[red]Not connected to server[/]");
    return 1;
}
```

### Progress Bar for Single File

```csharp
await _console.Progress()
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask(fileName);
        await _fileTransferService.UploadFile(path, dest,
            async p => task.Value = (double)p.TotalRead / fileSize * 100);
    });
```

### Concurrent Multi-File with Semaphore

```csharp
var semaphore = new SemaphoreSlim(4); // Max 4 concurrent
var tasks = files.Select(async file =>
{
    await semaphore.WaitAsync(ct);
    try { /* upload */ }
    finally { semaphore.Release(); }
});
await Task.WhenAll(tasks);
```
