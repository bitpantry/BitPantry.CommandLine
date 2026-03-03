using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.SignalR;

namespace BitPantry.CommandLine.Remote.SignalR.Server;

/// <summary>
/// Provides ambient access to the current hub invocation's per-request data
/// (<see cref="IClientProxy"/>, <see cref="RpcMessageRegistry"/>, and <see cref="Theme"/>)
/// across DI scope boundaries.
/// 
/// Register as <strong>singleton</strong>. The hub sets <see cref="Current"/> before
/// handler activation and clears it in a <c>finally</c> block; <c>AsyncLocal</c>
/// ensures thread-safety across concurrent requests.
/// 
/// This mirrors the <c>IHttpContextAccessor</c> pattern: the hub writes, downstream
/// classes (e.g., <see cref="ClientFileSystemBrowser"/>) read.
/// </summary>
public sealed class HubInvocationContext
{
    private static readonly AsyncLocal<ContextHolder?> _current = new();

    /// <summary>
    /// Gets or sets the current hub invocation data for the executing async flow.
    /// Set by the hub before handler activation; read by downstream services.
    /// </summary>
    public HubInvocationContextData? Current
    {
        get => _current.Value?.Data;
        set
        {
            // Wrap in a holder to ensure changes don't leak across async forks.
            // (Same pattern as IHttpContextAccessor in ASP.NET Core.)
            var holder = _current.Value;
            if (holder != null)
            {
                // Clear current data stored in the previous holder to prevent leaking.
                holder.Data = null;
            }
            if (value != null)
            {
                _current.Value = new ContextHolder { Data = value };
            }
        }
    }

    private sealed class ContextHolder
    {
        public HubInvocationContextData? Data;
    }
}

/// <summary>
/// Data bag holding the per-request hub invocation state, making it
/// available to downstream services regardless of DI scope.
/// </summary>
public sealed class HubInvocationContextData
{
    public required IClientProxy ClientProxy { get; init; }
    public required RpcMessageRegistry RpcMessageRegistry { get; init; }
    public required Theme Theme { get; init; }
}
