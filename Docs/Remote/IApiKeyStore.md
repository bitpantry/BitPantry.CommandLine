# IApiKeyStore

`BitPantry.CommandLine.Remote.SignalR.Server.Authentication.IApiKeyStore`

[← Back to CommandLine Server](CommandLineServer.md)

The JWT authentication implementation used by the command line server uses API keys to request access tokens.

Once the server is [configured to use authentication](CommandLineServer.md#configuring-authentication), the `IApiKeyStore` implementation will be used to validate API keys before issuing access tokens. The `IApiKeyStore` defines the expected behaviors for the API key storage mechanism. 

```
public interface IApiKeyStore
{
    /// <summary>
    /// Try and get a clientId by a given api key
    /// </summary>
    /// <param name="apiKey">The api key to get the client id for</param>
    /// <param name="clientId">The client id</param>
    /// <returns>True if <paramref name="clientId"/> was successfully retrieved using the given api key, false otherwise - if false,
    /// the value of <paramref name="clientId"/> should be considered invalid</returns>
    Task<bool> TryGetClientIdByApiKey(string apiKey, out string clientId);
}
```

---

## See Also

- [CommandLineServer](CommandLineServer.md) - Server configuration
- [JwtAuthOptions](JwtAuthOptions.md) - JWT authentication settings
- [IRefreshTokenStore](IRefreshTokenStore.md) - Refresh token storage