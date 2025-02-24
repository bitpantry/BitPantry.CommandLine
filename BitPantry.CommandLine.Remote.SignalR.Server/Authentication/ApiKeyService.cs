/// <summary>
/// Handles all API key logic for the server
/// </summary>
public class ApiKeyService
{
    private readonly IApiKeyStore _keyStore; 

    /// <summary>
    /// Creates a new <see cref="ApiKeyService"/>
    /// </summary>
    /// <param name="keyStore">The <see cref="IApiKeyStore"/> implementation to use</param>
    public ApiKeyService(IApiKeyStore keyStore)
    {
        _keyStore = keyStore;
    }

    /// <summary>
    /// Validates an API key
    /// </summary>
    /// <param name="apiKey">The key to validate</param>
    /// <param name="clientId">The client ID the key should be for</param>
    /// <returns>True if the key is valid, otherwise false</returns>
    public virtual Task<bool> ValidateKey(string apiKey, out string clientId)
        => _keyStore.TryGetClientIdByApiKey(apiKey, out clientId);
}
