using BitPantry.CommandLine.AutoComplete;
using HandlerContext = BitPantry.CommandLine.AutoComplete.Handlers.AutoCompleteContext;
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

        Task<List<AutoCompleteOption>> AutoComplete(string groupPath, string cmdName, HandlerContext ctx, CancellationToken token);
        Task Connect(string uri, CancellationToken token = default);
        Task Disconnect(CancellationToken token = default);
        Task<RunResult> Run(string commandLineInput, object data, CancellationToken token);

        /// <summary>
        /// Ensures a connection is established. If auto-connect is enabled and a handler is
        /// registered, will attempt to connect using the configured profile resolution strategy.
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if connected (already or newly); false if connection could not be established.</returns>
        Task<bool> EnsureConnectedAsync(CancellationToken token = default);

        /// <summary>
        /// Sends an RPC request to the server and waits for the response.
        /// </summary>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="request">The request object (must be a ServerRequest-derived type)</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>The response from the server</returns>
        Task<TResponse> SendRpcRequest<TResponse>(object request, CancellationToken token = default);
    }
}
