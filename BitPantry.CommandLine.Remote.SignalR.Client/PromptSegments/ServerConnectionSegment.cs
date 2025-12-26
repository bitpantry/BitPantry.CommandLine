using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;

namespace BitPantry.CommandLine.Remote.SignalR.Client.PromptSegments
{
    /// <summary>
    /// Prompt segment displaying the connected server hostname.
    /// </summary>
    public class ServerConnectionSegment : IPromptSegment
    {
        private readonly IServerProxy _serverProxy;

        public int Order => 100;

        public ServerConnectionSegment(IServerProxy serverProxy)
        {
            _serverProxy = serverProxy;
        }

        public string Render()
        {
            if (_serverProxy.ConnectionState != ServerProxyConnectionState.Connected)
                return null;

            return $"@{_serverProxy.ConnectionUri?.Host}";
        }
    }
}
