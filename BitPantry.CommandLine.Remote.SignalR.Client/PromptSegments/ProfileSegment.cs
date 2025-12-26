using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;

namespace BitPantry.CommandLine.Remote.SignalR.Client.PromptSegments
{
    /// <summary>
    /// Prompt segment displaying the connected profile name.
    /// </summary>
    public class ProfileSegment : IPromptSegment
    {
        private readonly IServerProxy _serverProxy;
        
        // Track which profile was used for current connection
        private string _currentProfile;
        private readonly object _lock = new();

        public int Order => 110;

        public ProfileSegment(IServerProxy serverProxy)
        {
            _serverProxy = serverProxy;
        }

        /// <summary>
        /// Sets the current profile name for display.
        /// </summary>
        /// <param name="profileName">The profile name, or null to clear.</param>
        public void SetCurrentProfile(string profileName)
        {
            lock (_lock)
            {
                _currentProfile = profileName;
            }
        }

        /// <summary>
        /// Clears the current profile name.
        /// </summary>
        public void ClearCurrentProfile()
        {
            lock (_lock)
            {
                _currentProfile = null;
            }
        }

        public string Render()
        {
            if (_serverProxy.ConnectionState != ServerProxyConnectionState.Connected)
                return null;

            lock (_lock)
            {
                if (string.IsNullOrEmpty(_currentProfile))
                    return null;

                return $"[{_currentProfile}]";
            }
        }
    }
}
