using BitPantry.CommandLine.Remote.SignalR.Rpc;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class SingletonRpcScope : IRpcScope
    {
        public string GetIdentifier()
        {
            return "SINGLE";
        }
    }
}
