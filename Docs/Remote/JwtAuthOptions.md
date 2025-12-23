# JwtAuthOptions

`BitPantry.CommandLine.Remote.SignalR.Server.Authentication.JwtAuthOptions`

[← Back to CommandLine Server](CommandLineServer.md)

When adding authentication an option action can be used to configure the authentication features.

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Issuer` | `string` | `"cli-server"` | The token issuer name |
| `Audience` | `string` | `"cli-clients"` | The token audience name |
| `TokenRequestRoute` | `string` | `"/cli-auth/token-request"` | URL route for requesting new access tokens |
| `TokenRefreshRoute` | `string` | `"/cli-auth/token-refresh"` | URL route for refreshing access tokens |
| `AccessTokenLifetime` | `TimeSpan` | 1 hour | Lifetime of access tokens |
| `RefreshTokenLifetime` | `TimeSpan` | 30 days | Lifetime of refresh tokens |
| `TokenValidationClockSkew` | `TimeSpan` | 5 minutes | Grace time for expired tokens |
| `ApiKeyStoreImplementationFactory` | `Func<IServiceProvider, TApiKeyStore>` | `null` | Factory for API key store |
| `RefreshTokenStoreImplementationFactory` | `Func<IServiceProvider, TRefreshTokenStore>` | `null` | Factory for refresh token store |

## Example Configuration

```csharp
builder.Services.AddCommandLineHub(opt =>
{
    opt.RegisterCommands(typeof(Program));
    opt.AddJwtAuthentication<MyApiKeyStore, MyRefreshTokenStore>(
        "atLeast128BitSecretForSigningTokens",
        authOpts =>
        {
            authOpts.Issuer = "my-server";
            authOpts.Audience = "my-clients";
            authOpts.AccessTokenLifetime = TimeSpan.FromMinutes(30);
            authOpts.RefreshTokenLifetime = TimeSpan.FromDays(7);
            authOpts.TokenValidationClockSkew = TimeSpan.FromMinutes(2);
        });
});
```

## Full Type Definition

```csharp
public class JwtAuthOptions<TApiKeyStore, TRefreshTokenStore>
{
    /// <summary>
    /// The token issuer name
    /// </summary>
    public string Issuer { get; set; } = "cli-server";

    /// <summary>
    /// The token audience name
    /// </summary>
    public string Audience { get; set; } = "cli-clients";

    /// <summary>
    /// The URL route where a request for a new access token can be made
    /// </summary>
    public string TokenRequestRoute { get; set; } = "/cli-auth/token-request";

    /// <summary>
    /// The URL route where a request for an access token can be refreshed
    /// </summary>
    public string TokenRefreshRoute { get; set; } = "/cli-auth/token-refresh";

    /// <summary>
    /// A <see cref="TimeSpan"/> representing the lifetime of an access token
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// A <see cref="TimeSpan"/> representing the lifetime of a refresh token
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// How much grace time is given to an expired token before the server considers it expired
    /// </summary>
    public TimeSpan TokenValidationClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// A function used to return the IApiKeyStore implementation
    /// </summary>
    public Func<IServiceProvider, TApiKeyStore> ApiKeyStoreImplementationFactory { get; set; }

    /// <summary>
    /// A function used to return the IRefreshTokenStore implementation
    /// </summary>
    public Func<IServiceProvider, TRefreshTokenStore> RefreshTokenStoreImplementationFactory { get; set; }
}
```

---

## See Also

- [CommandLineServer](CommandLineServer.md) - Server configuration
- [IApiKeyStore](IApiKeyStore.md) - API key validation interface
- [IRefreshTokenStore](IRefreshTokenStore.md) - Refresh token storage interface
- [Client](Client.md) - Client configuration
- [Troubleshooting](Troubleshooting.md) - Authentication troubleshooting
