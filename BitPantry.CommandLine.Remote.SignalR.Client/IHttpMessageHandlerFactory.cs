namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public interface IHttpMessageHandlerFactory
    {
        HttpMessageHandler CreateHandler(HttpMessageHandler handler);
    }
}
