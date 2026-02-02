namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Marker interface indicating that an exception's message is appropriate
    /// for display to end users. When a remote command throws an exception
    /// implementing this interface, the full exception details (message, stack trace)
    /// are serialized and sent to the client for proper rendering.
    /// </summary>
    public interface IUserFacingException
    {
    }
}
