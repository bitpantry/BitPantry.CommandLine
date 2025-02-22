using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.SignalR;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    /// <summary>
    /// Extension functions fo the <see cref="IClientProxy"/>
    /// </summary>
    public static class IClientProxyExtensions
    {
        /// <summary>
        /// Executes an Rpc request (request / response) with the client
        /// </summary>
        /// <typeparam name="T">The expected return type</typeparam>
        /// <param name="proxy">The <see cref="IClientProxy"/> of the client to send the request to</param>
        /// <param name="msgReg">The <see cref="RpcMessageRegistry"/> that will track the interaction</param>
        /// <param name="req">The request to send to the client</param>
        /// <returns>The response from the client</returns>
        public static async Task<T> Rpc<T>(this IClientProxy proxy, RpcMessageRegistry msgReg, ClientRequest req, CancellationToken token = default)
        {
            var ctx = msgReg.Register();
            req.CorrelationId = ctx.CorrelationId;

            await proxy.SendAsync(SignalRMethodNames.ReceiveRequest, req, token);

            return await ctx.WaitForCompletion<T>();
        }
    }
}
