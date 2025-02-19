namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class SignalRClientOptions
    {
        public IHttpClientFactory HttpClientFactory { get; set; } = new DefaultHttpClientFactory();
        public IHttpMessageHandlerFactory HttpMessageHandlerFactory { get; set; } = new DefaultHttpMessageHandlerFactory();
        public TimeSpan TokenRefreshMonitorInterval { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan TokenRefreshThreshold { get; set; } = TimeSpan.FromMinutes(5);
    }
}
