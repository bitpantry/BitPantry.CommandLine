namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// An implementation of the <see cref="IHttpMessageHandlerFactory"/> that returns a new <see cref="DefaultHttpMessageHandler"/> when CreateHandler is called
    /// </summary>
    public class DefaultHttpMessageHandlerFactory : IHttpMessageHandlerFactory
    {
        public HttpMessageHandler CreateHandler(HttpMessageHandler handler)
            => new DefaultHttpMessageHandler();
    }
}
