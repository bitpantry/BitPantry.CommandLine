using BitPantry.CommandLine.AutoComplete.Handlers;

namespace BitPantry.CommandLine.Remote.SignalR.AutoComplete;

/// <summary>
/// Binds a command property to file path autocomplete on the client's file system.
/// Use on arguments that reference client-side file paths (e.g., upload source).
/// </summary>
public class ClientFilePathAutoCompleteAttribute : AutoCompleteAttribute<ClientFilePathAutoCompleteHandler>
{
}
