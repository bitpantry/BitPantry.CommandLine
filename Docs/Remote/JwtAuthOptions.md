# JwotAuthOptions

```BitPantry.CommandLine.Remote.SignalR.Server.Authentication.JwtAuthOptions```

When adding authentication an option action can be used to configure the authentication features.

```
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
    /// A <see cref="TimeSpan"/> representing the lifetime of an access token - new tokens will be generated using this
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// A <see cref="TimeSpan"/> representing the lifetime of a refresh token - new tokens will be generated using this
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// How much grace time is given to an expired token before the server will consider it expired
    /// </summary>
    public TimeSpan TokenValidationClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// A function used to return the <see cref="IApiKeyStore"/> implementation
    /// </summary>
    public Func<IServiceProvider, TApiKeyStore> ApiKeyStoreImplementationFactory { get; set; }

    /// <summary>
    /// A function used to return the <see cref="IRefreshTokenStore"/> implementation
    /// </summary>
    public Func<IServiceProvider, TRefreshTokenStore> RefreshTokenStoreImplementationFactory { get; set; }
}
```
