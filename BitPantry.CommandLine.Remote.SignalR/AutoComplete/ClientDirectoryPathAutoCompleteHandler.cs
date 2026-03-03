using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.AutoComplete;

/// <summary>
/// Autocomplete handler that enumerates only directories on the client's file system.
/// When applied to a server-side command, this triggers an RPC call to the client.
/// When applied to a client-side command, this uses the local file system directly.
/// </summary>
public class ClientDirectoryPathAutoCompleteHandler : PathAutoCompleteHandlerBase
{
    public ClientDirectoryPathAutoCompleteHandler(
        [FromKeyedServices(PathEntryProviderKeys.Client)] IPathEntryProvider provider,
        Theme theme)
        : base(provider, theme, includeFiles: false)
    {
    }
}
