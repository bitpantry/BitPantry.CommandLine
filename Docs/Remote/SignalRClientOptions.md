# SignalRClientOptions

`BitPantry.CommandLine.Remote.SignalR.Client.SignalRClientOptions`

[← Back to Client Configuration](Client.md)

Configuration options for the SignalR remote CLI client.

## Table of Contents

- [Overview](#overview)
- [Configuration](#configuration)
- [Properties](#properties)
- [Token Refresh Configuration](#token-refresh-configuration)
- [Custom HTTP Factories](#custom-http-factories)
- [Registered Services](#registered-services)
- [See Also](#see-also)

## Overview

`SignalRClientOptions` provides settings for configuring the client connection to a remote CommandLine server. These options are passed when calling `ConfigureSignalRClient()` on the application builder.

## Configuration

```csharp
using BitPantry.CommandLine;
using BitPantry.CommandLine.Remote.SignalR.Client;

var builder = new CommandLineApplicationBuilder();

builder.ConfigureSignalRClient(options =>
{
    options.TokenRefreshMonitorInterval = TimeSpan.FromMinutes(2);
    options.TokenRefreshThreshold = TimeSpan.FromMinutes(10);
});

builder.RegisterCommands(typeof(Program));
var app = builder.Build();
```

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HttpClientFactory` | `IHttpClientFactory` | `DefaultHttpClientFactory` | Factory for creating HttpClient instances |
| `HttpMessageHandlerFactory` | `IHttpMessageHandlerFactory` | `DefaultHttpMessageHandlerFactory` | Factory for creating HttpMessageHandler instances |
| `TokenRefreshMonitorInterval` | `TimeSpan` | 1 minute | How often the AccessTokenManager checks if the token needs refresh |
| `TokenRefreshThreshold` | `TimeSpan` | 5 minutes | How long before token expiry the client begins refresh attempts |

## Token Refresh Configuration

The client automatically manages access token refresh to maintain connectivity. Configure these settings based on your server's token lifetime:

```csharp
builder.ConfigureSignalRClient(options =>
{
    // Check every 30 seconds
    options.TokenRefreshMonitorInterval = TimeSpan.FromSeconds(30);
    
    // Start refreshing 10 minutes before expiry
    options.TokenRefreshThreshold = TimeSpan.FromMinutes(10);
});
```

> **Tip**: Set `TokenRefreshThreshold` to be longer than `TokenRefreshMonitorInterval` to ensure the token is refreshed before expiry.

## Custom HTTP Factories

For advanced scenarios, implement custom factories to control HTTP behavior:

### Custom HttpClient Factory

```csharp
public class CustomHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("X-Custom-Header", "value");
        return client;
    }
}

builder.ConfigureSignalRClient(options =>
{
    options.HttpClientFactory = new CustomHttpClientFactory();
});
```

### Custom Message Handler Factory

```csharp
public class CustomHttpMessageHandlerFactory : IHttpMessageHandlerFactory
{
    public HttpMessageHandler CreateHandler(string name)
    {
        return new HttpClientHandler
        {
            UseProxy = true,
            Proxy = new WebProxy("http://proxy:8080")
        };
    }
}

builder.ConfigureSignalRClient(options =>
{
    options.HttpMessageHandlerFactory = new CustomHttpMessageHandlerFactory();
});
```

## Registered Services

When `ConfigureSignalRClient()` is called, the following services are registered:

| Service | Lifetime | Description |
|---------|----------|-------------|
| `IServerProxy` | Singleton | Manages connection to remote server |
| `AccessTokenManager` | Singleton | Handles JWT token lifecycle |
| `FileTransferService` | Singleton | Manages file upload/download |
| `RpcMessageRegistry` | Singleton | RPC message tracking |
| `ConnectCommand` | Transient | Built-in connect command |
| `DisconnectCommand` | Transient | Built-in disconnect command |

## See Also

- [Client](Client.md) - Client configuration guide
- [CommandLineServer](CommandLineServer.md) - Server setup
- [JwtAuthOptions](JwtAuthOptions.md) - JWT authentication configuration
- [Troubleshooting](Troubleshooting.md) - Common issues and solutions
