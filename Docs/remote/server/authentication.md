# Server Authentication

The server uses JWT token-based authentication. The developer provides implementations for API key validation and refresh token storage.

---

## Authentication Flow

```
Client                            Server
  │                                  │
  ├─── API Key ──────────────────────►  IApiKeyStore.ValidateAsync()
  │                                  │
  ◄─── Access Token + Refresh Token ─┤  TokenRequestEndpointService
  │                                  │
  ├─── SignalR + Bearer Token ───────►  TokenValidationMiddleware
  │                                  │
  ├─── Refresh Token ────────────────►  IRefreshTokenStore.ValidateAsync()
  │                                  │
  ◄─── New Access Token ────────────-┤
```

---

## Required Implementations

The server requires two interface implementations:

### IApiKeyStore

Validates API keys presented by clients during initial authentication:

```csharp
public interface IApiKeyStore
{
    Task<bool> ValidateAsync(string apiKey);
}
```

```csharp
public class MyApiKeyStore : IApiKeyStore
{
    private readonly IConfiguration _config;

    public MyApiKeyStore(IConfiguration config) => _config = config;

    public Task<bool> ValidateAsync(string apiKey)
    {
        var validKeys = _config.GetSection("ApiKeys").Get<string[]>() ?? Array.Empty<string>();
        return Task.FromResult(validKeys.Contains(apiKey));
    }
}
```

### IRefreshTokenStore

Stores and validates refresh tokens for token renewal:

```csharp
public interface IRefreshTokenStore
{
    Task StoreAsync(string tokenId, string refreshToken, DateTime expiry);
    Task<bool> ValidateAsync(string tokenId, string refreshToken);
    Task RevokeAsync(string tokenId);
}
```

---

## Configuration

Register auth implementations and configure JWT options:

```csharp
builder.Services.AddSingleton<IApiKeyStore, MyApiKeyStore>();
builder.Services.AddSingleton<IRefreshTokenStore, MyRefreshTokenStore>();

builder.Services.AddCommandLineHub(opt =>
{
    // JwtAuthOptions are configured automatically
    // Custom configuration is available if needed
});
```

---

## Token Endpoints

The server automatically exposes HTTP endpoints for token operations:

| Endpoint | Method | Description |
|----------|--------|-------------|
| Token request | `POST` | Exchange API key for access + refresh tokens |
| Token refresh | `POST` | Exchange refresh token for new access token |

These endpoint paths are defined in `ServiceEndpointNames`.

---

## See Also

- [Setting Up the Server](index.md)
- [Connecting & Disconnecting](../client/connecting.md)
- [Server Profiles](../client/profiles.md)
