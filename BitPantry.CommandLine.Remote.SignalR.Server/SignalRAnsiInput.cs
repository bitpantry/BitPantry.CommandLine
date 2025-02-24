using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.SignalR;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    /// <summary>
    /// Implements <see cref="IAnsiConsoleInput"/> to broker all input requests with the client terminal
    /// </summary>
    public class SignalRAnsiInput : IAnsiConsoleInput
    {
        // the signalR client proxy and rpc message registry used to make all input requests rpc interactions with the client

        private IClientProxy _proxy;
        private RpcMessageRegistry _rpcMsgReg;

        public bool IsKeyAvailable()
            => _proxy.Rpc<IsKeyAvailableResponse>(_rpcMsgReg, new ClientRequest(ClientRequestType.IsKeyAvailable)).GetAwaiter().GetResult().IsKeyAvailable;

        public ConsoleKeyInfo? ReadKey(bool intercept)
            => ReadKeyAsync(intercept, CancellationToken.None).GetAwaiter().GetResult();

        public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
        {
            var resp = await _proxy.Rpc<ReadKeyResponse>(_rpcMsgReg, new ReadKeyRequest(intercept), cancellationToken);
            return resp.KeyInfo.ToKeyInfo();
        }

        public SignalRAnsiInput(IClientProxy proxy, RpcMessageRegistry rpcMsgReg)
        {
            _proxy = proxy;
            _rpcMsgReg = rpcMsgReg;
        }

    }
}
