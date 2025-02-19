namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient()
            => new();
    }
}
