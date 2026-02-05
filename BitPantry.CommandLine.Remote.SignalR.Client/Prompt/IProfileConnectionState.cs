namespace BitPantry.CommandLine.Remote.SignalR.Client.Prompt;

/// <summary>
/// Tracks the profile name used for the current server connection.
/// </summary>
public interface IProfileConnectionState
{
    /// <summary>
    /// Gets or sets the name of the profile used for the current connection.
    /// Null when not connected or when connected via direct URI (not a profile).
    /// </summary>
    string? ConnectedProfileName { get; set; }
}
