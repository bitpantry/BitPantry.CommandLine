using BitPantry.CommandLine.AutoComplete;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Client
{
    public enum ServerProxyConnectionState : int
    {
        Disconnected = 0,
        Connected = 1,
        Connecting = 2,
        Reconnecting = 3
    }

    public interface IServerProxy : IDisposable
    {
        public ServerProxyConnectionState ConnectionState { get; }
        Uri ConnectionUri { get; }

        Task<List<AutoCompleteOption>> AutoComplete(string cmdNamespace, string cmdName, string functionName, bool isFunctionAsync, AutoCompleteContext ctx, CancellationToken token);
        Task Connect(string uri, CancellationToken token = default);
        Task Disconnect(CancellationToken token = default);
        Task<RunResult> Run(string commandLineInput, object data, CancellationToken token);
    }
}
