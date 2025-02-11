using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.SignalR;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public static class IClientProxyExtensions
    {
        public static async Task<T> Rpc<T>(this IClientProxy proxy, RpcMessageRegistry msgReg, ClientRequest req, CancellationToken token = default)
        {
            var ctx = msgReg.Register();
            req.CorrelationId = ctx.CorrelationId;

            await proxy.SendAsync(SignalRMethodNames.ReceiveRequest, req, token);

            return await ctx.WaitForCompletion<T>();
        }
    }
}
