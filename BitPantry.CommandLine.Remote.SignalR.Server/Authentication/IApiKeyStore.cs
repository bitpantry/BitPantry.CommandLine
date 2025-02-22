/// <summary>
/// Defines an API key store - to be implemented by the developer
/// </summary>
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
