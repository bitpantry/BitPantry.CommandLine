# Server Authentication

The server uses JWT token-based authentication. The developer provides implementations for API key validation and refresh token storage.

---

## Authentication Flow

```
Client                            Server
  │                                  │
  ├─── API Key ──────────────────────►  IApiKeyStore.TryGetClientIdByApiKey()
  │                                  │     returns clientId → becomes JWT "sub" claim
  ◄─── Access Token + Refresh Token ─┤  TokenRequestEndpointService
  │                                  │
  ├─── SignalR + Bearer Token ───────►  TokenValidationMiddleware
  │                                  │     sets HttpContext.User from JWT claims
  ├─── Refresh Token ────────────────►  IRefreshTokenStore
  │                                  │
  ◄─── New Access Token ────────────-┤
```

---

## Required Implementations

The server requires two interface implementations:

### IApiKeyStore

Validates API keys and returns the client identity. The `clientId` you return becomes the `sub` (subject) claim in the JWT — this is the authenticated user's identity throughout the system.

```csharp
public interface IApiKeyStore
{
    Task<bool> TryGetClientIdByApiKey(string apiKey, out string clientId);
}
```

> **Important:** The `clientId` output parameter is the user's identity. Return a meaningful, stable identifier (e.g., username, email, or user ID). This value appears as the `sub` claim in the JWT and is accessible via `HttpContext.User`, `HubCallerContext.User`, and `HubInvocationContextData.User` during command execution.

```csharp
public class MyApiKeyStore : IApiKeyStore
{
    private readonly IUserRepository _users;

    public MyApiKeyStore(IUserRepository users) => _users = users;

    public async Task<bool> TryGetClientIdByApiKey(string apiKey, out string clientId)
    {
        var user = await _users.FindByApiKeyAsync(apiKey);
        if (user == null)
        {
            clientId = null;
            return false;
        }

        clientId = user.Id;  // This becomes the JWT "sub" claim
        return true;
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

## User Identity

When a client authenticates, the JWT contains the following claims:

| Claim | JWT Name | .NET Type | Value |
|-------|----------|-----------|-------|
| Subject (user ID) | `sub` | `ClaimTypes.NameIdentifier` | The `clientId` returned by your `IApiKeyStore` |
| Role | `role` | `ClaimTypes.Role` | `"cli"` (hardcoded) |

The authenticated `ClaimsPrincipal` is available in three ways:

### In Command Handlers

Inject `HubInvocationContext` to access the user during command execution:

```csharp
public class AuditedCommand : CommandBase
{
    private readonly HubInvocationContext _ctx;
    private readonly ILogger<AuditedCommand> _logger;

    public AuditedCommand(HubInvocationContext ctx, ILogger<AuditedCommand> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public override Task<int> Execute()
    {
        var user = _ctx.Current?.User;
        var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Command executed by {UserId}", userId);

        return Task.FromResult(0);
    }
}
```

### In Middleware

`HttpContext.User` is set by `UseCommandLineTokenValidation()`, so standard ASP.NET middleware and logger enrichers work:

```csharp
app.UseCommandLineTokenValidation();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Log, enrich, or authorize based on userId
    }
    await next();
});

app.MapCommandLineHub();
```

### Via HttpContext.Items (legacy)

The `ClaimsPrincipal` is also stored in `HttpContext.Items["User"]` for backward compatibility.

---

## See Also

- [Setting Up the Server](index.md)
- [Connecting & Disconnecting](../client/connecting.md)
- [Server Profiles](../client/profiles.md)
