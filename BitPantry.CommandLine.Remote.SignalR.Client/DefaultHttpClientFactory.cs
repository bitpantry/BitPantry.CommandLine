namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Implementation of the <see cref="IHttpClientFactory"/> that returns a new <see cref="HttpClient"/> when CreateClient is invoked.
    /// </summary>
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient()
            => new();
    }
}
