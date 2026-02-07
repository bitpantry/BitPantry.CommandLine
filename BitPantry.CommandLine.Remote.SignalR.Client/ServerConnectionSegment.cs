using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Remote.SignalR.Client.Prompt;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Prompt segment displaying the connected server hostname.
    /// Shows only when connected via direct URI (no profile).
    /// When connected via profile, ProfilePromptSegment handles display instead.
    /// </summary>
    public class ServerConnectionSegment : IPromptSegment
    {
        private readonly IServerProxy _serverProxy;
        private readonly IProfileConnectionState _profileConnectionState;

        public int Order => 100;

        public ServerConnectionSegment(IServerProxy serverProxy, IProfileConnectionState profileConnectionState)
        {
            _serverProxy = serverProxy;
            _profileConnectionState = profileConnectionState;
        }

        public string Render()
        {
            if (_serverProxy.ConnectionState != ServerProxyConnectionState.Connected)
                return null;

            // When connected via profile, the profile segment handles display
            if (!string.IsNullOrEmpty(_profileConnectionState.ConnectedProfileName))
                return null;

            return $"@{_serverProxy.Server?.ConnectionUri?.Host}";
        }
    }
}
