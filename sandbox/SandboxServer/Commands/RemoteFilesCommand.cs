using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxServer.Commands;

/// <summary>
/// Handler that returns paths with spaces to test remote auto-quoting.
/// </summary>
public class RemoteFilePathHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        var paths = new[] { "Server Data", "Remote Logs", "Backup Files", "config.json" };
        return Task.FromResult(paths
            .Where(p => p.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .Select(p => new AutoCompleteOption(p))
            .ToList());
    }
}

/// <summary>
/// Tests auto-quoting of values with spaces over SignalR.
/// Features: FR-053, FR-054 over remote
/// </summary>
[Command(Name = "remote-files")]
public class RemoteFilesCommand : CommandBase
{
    [Argument(Name = "path")]
    [AutoComplete<RemoteFilePathHandler>]
    public string Path { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"[blue][[REMOTE]][/] File: {Path}");
}
