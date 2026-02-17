# Setting Up the Server

Configure an ASP.NET application to host commands for remote execution.

---

## Installation

```shell
dotnet add package BitPantry.CommandLine.Remote.SignalR.Server
```

---

## Configuration

Register the command-line hub in your ASP.NET `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommandLineHub(opt =>
{
    opt.HubUrlPattern = "/cli";                           // SignalR hub path (default)
    opt.FileTransferOptions.StorageRootPath = "./storage"; // Required for file transfers
    opt.FileTransferOptions.MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB (default)
    opt.RegisterCommands(typeof(MyServerCommand));        // Register commands
});

var app = builder.Build();
app.ConfigureCommandLineHub();
app.Run();
```

---

## CommandLineServerOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HubUrlPattern` | `string` | `"/cli"` | SignalR hub URL path |
| `FileTransferOptions` | `FileTransferOptions` | — | File transfer configuration |
| `Services` | `IServiceCollection` | — | DI container for server commands |

Inherited from `CommandRegistryApplicationBuilder<T>`:

| Method | Description |
|--------|-------------|
| `RegisterCommand<T>()` | Register a single command |
| `RegisterCommands(params Type[])` | Scan assemblies for commands |
| `ConfigureAutoComplete(Action)` | Register server-side autocomplete handlers |

---

## Registering Server Commands

Server commands extend `CommandBase` just like local commands. They are registered with the server options and executed on the server when a client invokes them:

```csharp
[Command(Name = "server-status")]
[Description("Shows server health information")]
public class ServerStatusCommand : CommandBase
{
    private readonly IHealthService _health;

    public ServerStatusCommand(IHealthService health) => _health = health;

    public async Task Execute(CommandExecutionContext ctx)
    {
        var status = await _health.GetStatusAsync();
        Console.MarkupLine($"Server: [green]{status}[/]");
    }
}
```

---

## Server-Side DI

Server commands have access to the full ASP.NET DI container. Services registered with `builder.Services` are available for injection:

```csharp
builder.Services.AddSingleton<IHealthService, HealthService>();
builder.Services.AddCommandLineHub(opt =>
{
    opt.RegisterCommand<ServerStatusCommand>();
});
```

---

## In This Section

| Page | Description |
|------|-------------|
| [Authentication](authentication.md) | JWT tokens, API key stores, token endpoints |
| [File System & Sandboxing](sandboxing.md) | Sandboxed file system, path/size/extension validation |

---

## See Also

- [Remote Execution](../index.md)
- [Authentication](authentication.md)
- [File System & Sandboxing](sandboxing.md)
- [Setting Up the Client](../client/index.md)
