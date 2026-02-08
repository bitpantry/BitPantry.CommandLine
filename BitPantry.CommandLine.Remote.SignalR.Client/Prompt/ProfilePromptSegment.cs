using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Prompt;

/// <summary>
/// Prompt segment displaying the connected profile name in brackets.
/// Shows only when connected via a profile, hidden otherwise.
/// </summary>
public class ProfilePromptSegment : IPromptSegment
{
    private readonly IServerProxy _serverProxy;
    private readonly IProfileConnectionState _profileConnectionState;

    /// <summary>
    /// Order is 110 (after ServerConnectionSegment at 100, in the connection state range 100-199).
    /// </summary>
    public int Order => 110;

    public ProfilePromptSegment(IServerProxy serverProxy, IProfileConnectionState profileConnectionState)
    {
        _serverProxy = serverProxy;
        _profileConnectionState = profileConnectionState;
    }

    /// <summary>
    /// Renders the profile segment as [profile-name] when connected via profile.
    /// Returns null to hide segment when not connected or connected via direct URI.
    /// </summary>
    public string? Render()
    {
        // Hide if not connected
        if (_serverProxy.ConnectionState != ServerProxyConnectionState.Connected)
            return null;

        // Hide if connected via direct URI (no profile)
        if (string.IsNullOrEmpty(_profileConnectionState.ConnectedProfileName))
            return null;

        return $"[{_profileConnectionState.ConnectedProfileName}]";
    }
}
