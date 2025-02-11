using BitPantry.CommandLine.Remote.SignalR.Rpc;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
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
