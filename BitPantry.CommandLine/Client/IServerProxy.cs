using BitPantry.CommandLine.AutoComplete;
using System;
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
        public string ConnectionId { get; }
        Task<CompletionResult> GetCompletionsAsync(CompletionContext context, CancellationToken token);
        Task Connect(string uri, CancellationToken token = default);
        Task Disconnect(CancellationToken token = default);
        Task<RunResult> Run(string commandLineInput, object data, CancellationToken token);
    }
}
