using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using System.Collections.Concurrent;

namespace BitPantry.CommandLine.Remote.SignalR.Rpc
{
    public class RpcMessageRegistry
    {
        private static readonly ConcurrentDictionary<string, RpcMessageContext> _taskCompletionSourceDictionary = new();
        private IRpcScope _rpcScope;

        public RpcMessageRegistry(IRpcScope rpcScope)
        {
            _rpcScope = rpcScope;
        }

        public RpcMessageContext Register()
        {
            var ctx = new RpcMessageContext(_rpcScope.GetIdentifier());
            _taskCompletionSourceDictionary.TryAdd(ctx.CorrelationId, ctx);
            return ctx;
        }

        public void SetResponse(MessageBase msg)
        {
            if (_taskCompletionSourceDictionary.TryRemove(msg.CorrelationId, out var ctx))
                ctx?.SetResponse(msg);
        }

        public void AbortWithError(string correlationId, Exception ex)
        {
            if (_taskCompletionSourceDictionary.TryRemove(correlationId, out var ctx))
                ctx?.AbortWithError(ex);
        }

        public void AbortWithRemoteError(string correlationId, string message)
        {
            if (_taskCompletionSourceDictionary.TryRemove(correlationId, out var ctx))
                ctx?.AbortWithRemoteError(message);
        }

        public void AbortScopeWithRemoteError(string message)
        {
            foreach (var ctx in _taskCompletionSourceDictionary.Values.Where(c => c.Scope == _rpcScope.GetIdentifier()).ToList())
            {
                if (_taskCompletionSourceDictionary.TryRemove(ctx.CorrelationId, out var outCtx))
                    outCtx?.AbortWithRemoteError(message);
            }
        }
    }
}

