namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Defines the behaviors of an http message handler factory class
    /// </summary>
    public interface IHttpMessageHandlerFactory
    {
        /// <summary>
        /// Creates a new <see cref="HttpMessageHandler"/>
        /// </summary>
        /// <param name="handler">The handler used to create the new handler</param>
        /// <returns>A new message handler</returns>
        HttpMessageHandler CreateHandler(HttpMessageHandler handler);
    }
}
