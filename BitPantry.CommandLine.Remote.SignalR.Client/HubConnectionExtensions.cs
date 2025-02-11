using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.SignalR.Client;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public static class HubConnectionExtensions
    {
        public static async Task<T> Rpc<T>(this HubConnection connection, RpcMessageRegistry msgReg, MessageBase msg, CancellationToken token = default)
        {
            var ctx = msgReg.Register();
            msg.CorrelationId = ctx.CorrelationId;
            await connection.InvokeAsync(SignalRMethodNames.ReceiveRequest, msg, token);
            return await ctx.WaitForCompletion<T>();
        }
    }
}
