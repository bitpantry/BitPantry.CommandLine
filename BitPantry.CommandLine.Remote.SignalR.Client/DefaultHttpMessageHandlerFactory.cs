namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class DefaultHttpMessageHandlerFactory : IHttpMessageHandlerFactory
    {
        public HttpMessageHandler CreateHandler(HttpMessageHandler handler)
            => new DefaultHttpMessageHandler();
    }
}
