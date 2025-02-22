using BitPantry.CommandLine.Remote.SignalR.Rpc;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// An implementation of the <see cref="IRpcScope"/> that always returns the same value making a single scope
    /// </summary>
    public class SingletonRpcScope : IRpcScope
    {
        public string GetIdentifier()
        {
            return "SINGLE";
        }
    }
}
