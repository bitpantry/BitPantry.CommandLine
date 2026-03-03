using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.AutoComplete;

/// <summary>
/// Autocomplete handler that enumerates only directories on the server's file system.
/// When applied to a client-side command, this triggers an RPC call to the server.
/// When applied to a server-side command, this uses the local file system directly.
/// </summary>
public class ServerDirectoryPathAutoCompleteHandler : PathAutoCompleteHandlerBase
{
    public ServerDirectoryPathAutoCompleteHandler(
        [FromKeyedServices(PathEntryProviderKeys.Server)] IPathEntryProvider provider,
        Theme theme)
        : base(provider, theme, includeFiles: false)
    {
    }
}
