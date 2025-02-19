namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public interface IHttpClientFactory
    {
        public HttpClient CreateClient();
    }
}
