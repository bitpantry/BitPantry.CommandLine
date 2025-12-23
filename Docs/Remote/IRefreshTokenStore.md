# IRefreshTokenStore

`BitPantry.CommandLine.Remote.SignalR.Server.Authentication.IRefreshTokenStore`

[← Back to CommandLine Server](CommandLineServer.md)

Persisting refresh tokens enhances security by preventing users from using revoked tokens or holding on to multiple refresh tokens.

Once the server is [configured to use authentication](CommandLineServer.md#configuring-authentication), the `IRefreshTokenStore` implementation will be used to manage issued refresh tokens. The `IRefreshTokenStore` defines the expected behaviors for the refresh token storage mechanism. 

```
/// <summary>
/// Stores a refresh token
/// </summary>
/// <param name="clientId">The client id the token should be associated to</param>
/// <param name="refreshToken">The refresh token to store</param>
Task StoreRefreshTokenAsync(string clientId, string refreshToken);

/// <summary>
/// Tries to get a refresh token by the given client id
/// </summary>
/// <param name="clientId">The client id to attempt to get a refresh token for</param>
/// <param name="refreshToken">The refresh token retrieved for the given client id</param>
/// <returns>True if a refresh token was found for the given <paramref name="clientId"/>, otherwise false - if false
/// the value of <paramref name="refreshToken"/> should be considered invalid.</returns>
Task<bool> TryGetRefreshTokenAsync(string clientId, out string refreshToken);

/// <summary>
/// Revokes the refresh token for the given client id
/// </summary>
/// <param name="clientId">The id of the client to revoke the refresh token for. Once a token has been revoked
/// any calls to <see cref="TryGetRefreshTokenAsync(string, out string)"/> should return false"/></param>
/// <returns></returns>
Task RevokeRefreshTokenAsync(string clientId);
```

---

## See Also

- [CommandLineServer](CommandLineServer.md) - Server configuration
- [JwtAuthOptions](JwtAuthOptions.md) - JWT authentication settings
- [IApiKeyStore](IApiKeyStore.md) - API key validation