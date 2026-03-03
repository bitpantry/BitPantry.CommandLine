using BitPantry.CommandLine.AutoComplete.Handlers;

namespace BitPantry.CommandLine.Remote.SignalR.AutoComplete;

/// <summary>
/// Binds a command property to directory-only autocomplete on the server's file system.
/// Use on arguments that reference server-side directory paths (e.g., upload destination).
/// </summary>
public class ServerDirectoryPathAutoCompleteAttribute : AutoCompleteAttribute<ServerDirectoryPathAutoCompleteHandler>
{
}
