using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.SignalR.Client;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Helper extensions for the <see cref="HubConnection"/>
    /// </summary>
    public static class HubConnectionExtensions
    {
        /// <summary>
        /// Executes an Rpc request (request / response) with the server
        /// </summary>
        /// <typeparam name="T">The expected return type</typeparam>
        /// <param name="connection">The <see cref="HubConnection"/> to the server</param>
        /// <param name="msgReg">The <see cref="RpcMessageRegistry"/> that will track the interaction</param>
        /// <param name="req">The request to send to the server</param>
        /// <returns>The response from the server</returns>
        public static async Task<T> Rpc<T>(this HubConnection connection, RpcMessageRegistry msgReg, ServerRequest req, CancellationToken token = default)
        {
            var ctx = msgReg.Register();
            req.CorrelationId = ctx.CorrelationId;
            await connection.InvokeAsync(SignalRMethodNames.ReceiveRequest, req, token);
            return await ctx.WaitForCompletion<T>();
        }
    }
}
