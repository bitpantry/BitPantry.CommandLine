namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    public class SingletonHttpClientFactory : CommandLine.Remote.SignalR.Client.IHttpClientFactory
    {
        private HttpClient _httpClient;

        public SingletonHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient()
            => _httpClient;
    }
}
