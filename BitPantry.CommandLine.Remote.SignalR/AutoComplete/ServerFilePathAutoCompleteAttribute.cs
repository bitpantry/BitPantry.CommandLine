using BitPantry.CommandLine.AutoComplete.Handlers;

namespace BitPantry.CommandLine.Remote.SignalR.AutoComplete;

/// <summary>
/// Binds a command property to file path autocomplete on the server's file system.
/// Use on arguments that reference server-side file paths (e.g., download source).
/// </summary>
public class ServerFilePathAutoCompleteAttribute : AutoCompleteAttribute<ServerFilePathAutoCompleteHandler>
{
}
