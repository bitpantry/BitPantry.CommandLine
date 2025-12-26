namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles
{
    /// <summary>
    /// Exception thrown when credential store operations fail.
    /// </summary>
    public class CredentialStoreException : Exception
    {
        public CredentialStoreException(string message) : base(message) { }
        public CredentialStoreException(string message, Exception inner) : base(message, inner) { }
    }
}
