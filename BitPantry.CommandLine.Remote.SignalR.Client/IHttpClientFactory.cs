namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Defines the behaviors of an http client factory class
    /// </summary>
    public interface IHttpClientFactory
    {
        /// <summary>
        /// Returns a new http client
        /// </summary>
        /// <returns>A new http client</returns>
        public HttpClient CreateClient();
    }
}
