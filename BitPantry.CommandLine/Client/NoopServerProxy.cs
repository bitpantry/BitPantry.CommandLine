using BitPantry.CommandLine.AutoComplete;
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

        public Uri ConnectionUri => throw new InvalidOperationException(_err);

        public string ConnectionId => throw new InvalidOperationException(_err);

        public Task<List<AutoCompleteOption>> AutoComplete(string cmdNamespace, string cmdName, string functionName, bool isFunctionAsync, AutoCompleteContext ctx, CancellationToken token)
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
    }
}
