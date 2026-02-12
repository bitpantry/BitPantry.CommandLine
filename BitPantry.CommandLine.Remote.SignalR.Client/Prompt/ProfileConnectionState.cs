namespace BitPantry.CommandLine.Remote.SignalR.Client.Prompt;

/// <summary>
/// Tracks the profile name used for the current server connection.
/// Singleton registered in DI to share state across command executions.
/// </summary>
public class ProfileConnectionState : IProfileConnectionState
{
    /// <inheritdoc />
    public string ConnectedProfileName { get; set; }
}
