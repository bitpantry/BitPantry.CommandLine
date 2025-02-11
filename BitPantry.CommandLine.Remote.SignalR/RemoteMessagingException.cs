namespace BitPantry.CommandLine.Remote.SignalR
{
    public class RemoteMessagingException : Exception
    {
        public string CorrelationId { get; private set; }

        public RemoteMessagingException(string correlationId, string message)
            : base(message) { CorrelationId = correlationId; }
    }
}
