using BitPantry.CommandLine.Remote.SignalR.Rpc;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    /// <summary>
    /// Used to create a scope that can include multiple rpc interactions allowing all of those interactions to be managed together.
    /// For example, if a client connection is closed, all active rpc messages in a scope can be aborted together.
    /// </summary>
    public class SignalRRpcScope : IRpcScope
    {
        private string _currentScopeName = null;

        public string GetIdentifier() => _currentScopeName;

        public void SetScope(string currentScopeName)
        {
            _currentScopeName = currentScopeName;
        }
    }
}
