using BitPantry.CommandLine.AutoComplete;
using HandlerContext = BitPantry.CommandLine.AutoComplete.Handlers.AutoCompleteContext;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Client
{
    /// <summary>
    /// Throws a more descripted error (than null) if an attempt to use the remote logic is made
    /// </summary>
    public class NoopServerProxy : IServerProxy
    {
        private readonly string _err = "The server proxy is disabled";

        public ServerProxyConnectionState ConnectionState => throw new InvalidOperationException(_err);

        public ServerCapabilities Server => throw new InvalidOperationException(_err);

        public Task<List<AutoCompleteOption>> AutoComplete(string groupPath, string cmdName, HandlerContext ctx, CancellationToken token)
        {
            throw new InvalidOperationException(_err);
        }

        public Task Connect(string uri, CancellationToken token = default)
        {
            throw new InvalidOperationException(_err);
        }

        public Task Disconnect(CancellationToken token = default)
        {
            throw new InvalidOperationException(_err);
        }

        public void Dispose()
        {
            // do nothing
        }

        public Task<RunResult> Run(string commandLineInput, object data, CancellationToken token)
        {
            throw new InvalidOperationException(_err);
        }

        public Task<TResponse> SendRpcRequest<TResponse>(object request, CancellationToken token = default)
        {
            throw new InvalidOperationException(_err);
        }
    }
}
