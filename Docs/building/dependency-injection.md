# Dependency Injection

BitPantry.CommandLine uses `Microsoft.Extensions.DependencyInjection` throughout. Commands are resolved from the DI container, so constructor injection works naturally.

---

## Registering Services

Access the `IServiceCollection` via the builder's `Services` property:

```csharp
var builder = new CommandLineApplicationBuilder();

builder.Services.AddSingleton<IConfigStore, JsonConfigStore>();
builder.Services.AddTransient<IEmailService, SmtpEmailService>();

var app = builder
    .RegisterCommand<NotifyCommand>()
    .Build();
```

---

## Constructor Injection in Commands

Commands are instantiated by the DI container during the activation stage of the [processing pipeline](../running/processing-pipeline.md). Any registered service can be injected via the constructor:

```csharp
[Command(Name = "notify")]
public class NotifyCommand : CommandBase
{
    private readonly IEmailService _email;

    public NotifyCommand(IEmailService email)
    {
        _email = email;
    }

    [Argument(Name = "to", IsRequired = true)]
    public string To { get; set; } = "";

    [Argument(Name = "message", IsRequired = true)]
    public string Message { get; set; } = "";

    public async Task Execute(CommandExecutionContext ctx)
    {
        await _email.SendAsync(To, Message);
        Console.MarkupLine($"[green]Sent to {To}[/]");
    }
}
```

---

## Command Lifetime

Commands are resolved as **scoped** instances — one instance per command execution. This means:

- A new command instance is created for each invocation
- Scoped services injected into commands follow the same per-execution lifecycle
- Singleton services are shared across all executions
- Transient services are created fresh each time they are requested

---

## Built-in Registrations

The builder automatically registers several services that can be injected:

| Service | Lifetime | Description |
|---------|----------|-------------|
| `ICommandRegistry` | Singleton | Access to all registered commands and groups |
| `IServerProxy` | Singleton | Remote server proxy (`NoopServerProxy` by default) |
| `IFileSystem` | Singleton | File system abstraction |
| `IAnsiConsole` | Singleton | Spectre.Console instance |
| `IHelpFormatter` | Singleton | Help output formatter |

---

## See Also

- [Building the Application](index.md)
- [Registering Commands](registering-commands.md)
- [Unit Testing Commands](../testing/unit-testing.md)
- [Builder API Reference](../api-reference/builder-api.md)
