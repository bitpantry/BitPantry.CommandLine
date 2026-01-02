using System.Collections.Concurrent;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    public class TestApiKeyStore : IApiKeyStore
    {
        // Static concurrent dictionary for thread-safe API key registration across parallel tests
        // Each test environment can register its own unique API key -> client ID mapping
        private static readonly ConcurrentDictionary<string, string> _apiKeyDict = new ConcurrentDictionary<string, string>();

        // Default test keys for backwards compatibility
        static TestApiKeyStore()
        {
            _apiKeyDict["key1"] = "1";
            _apiKeyDict["key2"] = "2"; 
            _apiKeyDict["key3"] = "3";
        }

        /// <summary>
        /// Registers a unique API key with its client ID. 
        /// Used for test isolation during parallel execution.
        /// </summary>
        public static void RegisterApiKey(string apiKey, string clientId)
        {
            _apiKeyDict[apiKey] = clientId;
        }

        /// <summary>
        /// Removes an API key registration.
        /// </summary>
        public static void UnregisterApiKey(string apiKey)
        {
            _apiKeyDict.TryRemove(apiKey, out _);
        }

        public Task<bool> TryGetClientIdByApiKey(string apiKey, out string clientId)
        {
            if (_apiKeyDict.TryGetValue(apiKey, out clientId))
                return Task.FromResult(true);

            clientId = null;
            return Task.FromResult(false);
        }
    }
}
