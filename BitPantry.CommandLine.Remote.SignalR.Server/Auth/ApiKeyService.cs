public class ApiKeyService
{
    private readonly IApiKeyStore _keyStore; 

    public ApiKeyService(IApiKeyStore keyStore)
    {
        _keyStore = keyStore;
    }

    public virtual Task<bool> ValidateKey(string apiKey, out string clientId)
        => _keyStore.TryGetUserIdByApiKey(apiKey, out clientId);
}
