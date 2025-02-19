namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    public class TestApiKeyStore : IApiKeyStore
    {
        private readonly Dictionary<string, string> _apiKeyDict = new Dictionary<string, string>
            {
                { "key1", "1" },
                { "key2", "2" },
                { "key3", "3" }
            };

        public Task<bool> TryGetUserIdByApiKey(string apiKey, out string clientId)
        {
            if (_apiKeyDict.TryGetValue(apiKey, out clientId))
                return Task.FromResult(true);

            clientId = null;
            return Task.FromResult(false);
        }
    }
}
