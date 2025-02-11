using BitPantry.CommandLine.AutoComplete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Task<List<AutoCompleteOption>> AutoComplete(string cmdNamespace, string cmdName, string functionName, bool isFunctionAsync, AutoCompleteContext ctx, CancellationToken token)
        {
            throw new InvalidOperationException(_err);
        }

        public Task Connect(string uri)
        {
            throw new InvalidOperationException(_err);
        }

        public Task Disconnect()
        {
            throw new InvalidOperationException(_err);
        }

        public void Dispose()
        {
            throw new InvalidOperationException(_err);
        }

        public Task<RunResult> Run(string commandLineInput, object data, CancellationToken token)
        {
            throw new InvalidOperationException(_err);
        }
    }
}
