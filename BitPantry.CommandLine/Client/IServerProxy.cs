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
        /// <summary>
        /// The current state of the connection.
        /// </summary>
        public ServerProxyConnectionState ConnectionState { get; }

        /// <summary>
        /// The server capabilities and connection information, or null if not connected.
        /// This property consolidates all server-provided state received during connection.
        /// </summary>
        ServerCapabilities Server { get; }

        Task<List<AutoCompleteOption>> AutoComplete(string groupPath, string cmdName, string functionName, bool isFunctionAsync, AutoCompleteContext ctx, CancellationToken token);
        Task Connect(string uri, CancellationToken token = default);
        Task Disconnect(CancellationToken token = default);
        Task<RunResult> Run(string commandLineInput, object data, CancellationToken token);
    }
}
