namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Options used to configure the CommandLine SignalR client
    /// </summary>
    public class SignalRClientOptions
    {
        /// <summary>
        /// The HttpClient factory used by the client
        /// </summary>
        public IHttpClientFactory HttpClientFactory { get; set; } = new DefaultHttpClientFactory();

        /// <summary>
        /// The HttpMessageHandler used by the cient
        /// </summary>
        public IHttpMessageHandlerFactory HttpMessageHandlerFactory { get; set; } = new DefaultHttpMessageHandlerFactory();

        /// <summary>
        /// How often the <see cref="AccessTokenManager"/> should see if the current access token needs to be refreshed and attempt to refresh it
        /// </summary>
        public TimeSpan TokenRefreshMonitorInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// How long before the access token expires can the <see cref="AccessTokenManager"/> begin attempting to refresh it
        /// </summary>
        public TimeSpan TokenRefreshThreshold { get; set; } = TimeSpan.FromMinutes(5);
    }
}
