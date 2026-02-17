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
- [Custom Type Handlers](../autocomplete/type-handlers.md)
- [The IServerProxy Interface](../remote/server-proxy.md)
- [Console Configuration](../building/console-configuration.md)
- [Error Handling](../commands/error-handling.md)
