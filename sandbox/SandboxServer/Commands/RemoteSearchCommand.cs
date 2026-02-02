using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxServer.Commands;

/// <summary>
/// Handler that returns empty list for most queries.
/// Only returns results if query starts with "valid".
/// </summary>
public class EmptySearchHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        // Only return results if query starts with "valid"
        if (context.QueryString?.StartsWith("valid", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Task.FromResult(new List<AutoCompleteOption>
            {
                new("valid-result-1"),
                new("valid-result-2")
            });
        }
        return Task.FromResult(new List<AutoCompleteOption>()); // Empty list
    }
}

/// <summary>
/// Tests remote empty results handling.
/// Features: RMT-009
/// </summary>
[Command(Name = "remotesearch")]
public class RemoteSearchCommand : CommandBase
{
    [Argument(Name = "query")]
    [AutoComplete<EmptySearchHandler>]
    public string Query { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"[blue][[REMOTE]][/] Search: {Query}");
}
