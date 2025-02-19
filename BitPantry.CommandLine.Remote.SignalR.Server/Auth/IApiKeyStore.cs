public interface IApiKeyStore
{
    Task<bool> TryGetUserIdByApiKey(string apiKey, out string clientId);
}
