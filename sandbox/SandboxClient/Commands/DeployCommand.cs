using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

/// <summary>
/// Custom handler that provides environment suggestions.
/// </summary>
public class EnvironmentHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        var envs = new[] { "Development", "Staging", "Production", "QA" };
        return Task.FromResult(envs
            .Where(e => e.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e)
            .Select(e => new AutoCompleteOption(e))
            .ToList());
    }
}

/// <summary>
/// Tests explicit attribute handler override.
/// Features: FR-006 to FR-010
/// </summary>
[Command(Name = "deploy")]
public class DeployCommand : CommandBase
{
    [Argument(Name = "environment")]
    [AutoComplete<EnvironmentHandler>]
    public string Environment { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"Deploying to: {Environment}");
}
