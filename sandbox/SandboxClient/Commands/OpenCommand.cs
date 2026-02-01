using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

/// <summary>
/// Handler that returns paths with spaces to test auto-quoting.
/// </summary>
public class FilePathHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        var paths = new[] { "My Documents", "Program Files", "AppData", "simple.txt" };
        return Task.FromResult(paths
            .Where(p => p.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .Select(p => new AutoCompleteOption(p))
            .ToList());
    }
}

/// <summary>
/// Tests auto-quoting of values with spaces.
/// Features: FR-053, FR-054
/// </summary>
[Command(Name = "open")]
public class OpenCommand : CommandBase
{
    [Argument(Name = "path")]
    [AutoComplete<FilePathHandler>]
    public string Path { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"Opening: {Path}");
}
