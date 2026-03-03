using BitPantry.CommandLine.AutoComplete.Handlers;

namespace BitPantry.CommandLine.Remote.SignalR.AutoComplete;

/// <summary>
/// Binds a command property to directory-only autocomplete on the client's file system.
/// Use on arguments that reference client-side directory paths (e.g., download destination).
/// </summary>
public class ClientDirectoryPathAutoCompleteAttribute : AutoCompleteAttribute<ClientDirectoryPathAutoCompleteHandler>
{
}
