# Interfaces

Key interfaces across the BitPantry.CommandLine framework.

---

## Core

### ICommandRegistry

Access to all registered commands and groups.

```csharp
public interface ICommandRegistry
{
    IReadOnlyCollection<CommandInfo> Commands { get; }
    IReadOnlyList<GroupInfo> Groups { get; }
    IReadOnlyList<CommandInfo> RootCommands { get; }
    IReadOnlyList<GroupInfo> RootGroups { get; }
    bool CaseSensitive { get; }

    GroupInfo FindGroup(string nameOrPath);
    CommandInfo FindCommand(string name, GroupInfo group = null);
    CommandInfo Find(string fullyQualifiedCommandName);
    void RegisterCommandsAsRemote(IReadOnlyList<CommandInfo> infos);
    void DropRemoteCommands();
}
```

### IConsoleService

Low-level console operations not covered by Spectre.Console.

```csharp
public interface IConsoleService
{
    CursorPosition GetCursorPosition();
}
```

### IHelpFormatter

Renders help output at root, group, and command levels.

```csharp
public interface IHelpFormatter
{
    void DisplayRootHelp(IAnsiConsole console, ICommandRegistry registry);
    void DisplayGroupHelp(IAnsiConsole console, GroupInfo group, ICommandRegistry registry);
    void DisplayCommandHelp(IAnsiConsole console, CommandInfo command);
}
```

### IUserFacingException

Marker interface — exceptions implementing this have their messages displayed to users.

```csharp
public interface IUserFacingException { }
```

---

## Command Modules

### ICommandModule

Defines a self-contained module that registers commands, DI services, and autocomplete handlers. Implement this interface to create plugins that can be loaded from external assemblies.

```csharp
public interface ICommandModule
{
    /// <summary>
    /// Gets the display name of this module.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Configures the module by registering commands, services, and autocomplete handlers.
    /// </summary>
    void Configure(ICommandModuleContext context);
}
```

### ICommandModuleContext

Provides access to registration surfaces for command modules. Passed to `ICommandModule.Configure()`.

```csharp
public interface ICommandModuleContext
{
    /// <summary>
    /// Gets the command registry builder for registering commands.
    /// </summary>
    ICommandRegistryBuilder Commands { get; }

    /// <summary>
    /// Gets the service collection for registering DI services.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the autocomplete handler registry builder.
    /// </summary>
    IAutoCompleteHandlerRegistryBuilder AutoComplete { get; }
}
```

**Example Module Implementation:**

```csharp
public class MyModule : ICommandModule
{
    public string Name => "MyModule";

    public void Configure(ICommandModuleContext context)
    {
        // Register commands
        context.Commands.RegisterCommand(typeof(MyCommand));
        
        // Register DI services
        context.Services.AddSingleton<IMyService, MyService>();
        
        // Register autocomplete handlers
        context.AutoComplete.Register<MyAutoCompleteHandler>();
    }
}
```

---

## Autocomplete

### IAutoCompleteHandler

Provides autocomplete suggestions for a specific argument.

```csharp
public interface IAutoCompleteHandler
{
    Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default);
}
```

### ITypeAutoCompleteHandler

Provides autocomplete for all arguments of a matching type.

```csharp
public interface ITypeAutoCompleteHandler : IAutoCompleteHandler
{
    bool CanHandle(Type argumentType);
}
```

### IAutoCompleteHandlerRegistryBuilder

Builder for registering type handlers.

```csharp
public interface IAutoCompleteHandlerRegistryBuilder
{
    void Register<THandler>() where THandler : ITypeAutoCompleteHandler;
    IAutoCompleteHandlerRegistry Build(IServiceCollection services);
    IAutoCompleteHandlerRegistry Build();
}
```

---

## Prompt

### IPromptSegment

A composable prompt segment for multi-part prompts.

```csharp
public interface IPromptSegment
{
    int Order { get; }
    string? GetSegmentText();
}
```

---

## Remote

### IServerProxy

Core abstraction for remote server operations.

```csharp
public interface IServerProxy
{
    ServerProxyConnectionState ConnectionState { get; }
    ServerCapabilities Server { get; }

    Task Connect(string uri, CancellationToken token = default);
    Task<bool> EnsureConnectedAsync(CancellationToken token = default);
    Task Disconnect(CancellationToken token = default);

    Task<RunResponse> Run(RunRequest request, CancellationToken token = default);
    Task<AutoCompleteResponse> AutoComplete(AutoCompleteRequest request, CancellationToken token = default);
    Task<TResponse> SendRpcRequest<TResponse>(object request, CancellationToken token = default);
}
```

### IAutoConnectHandler

Handles automatic connection for `RunOnce()` mode.

```csharp
public interface IAutoConnectHandler
{
    string RequestedProfileName { get; set; }
    bool AutoConnectEnabled { get; set; }
    Task<bool> EnsureConnectedAsync(IServerProxy proxy, CancellationToken token = default);
}
```

---

## Server Authentication

### IApiKeyStore

Validates API keys on the server.

```csharp
public interface IApiKeyStore
{
    Task<bool> ValidateAsync(string apiKey);
}
```

### IRefreshTokenStore

Stores and validates refresh tokens on the server.

```csharp
public interface IRefreshTokenStore
{
    Task StoreAsync(string tokenId, string refreshToken, DateTime expiry);
    Task<bool> ValidateAsync(string tokenId, string refreshToken);
    Task RevokeAsync(string tokenId);
}
```

---

## See Also

- [API Reference](index.md)
- [Plugins Guide](../plugins/index.md)
- [Custom Type Handlers](../autocomplete/type-handlers.md)
- [The IServerProxy Interface](../remote/server-proxy.md)
- [Console Configuration](../building/console-configuration.md)
- [Error Handling](../commands/error-handling.md)
