using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxServer.Commands;

/// <summary>
/// Server-side environment handler with different values than local.
/// This proves the handler runs on the server, not the client.
/// </summary>
public class RemoteEnvironmentHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        // Different values than local to prove server execution
        var envs = new[] { "AWS-Prod", "AWS-Staging", "Azure-Prod", "Azure-Staging", "Local" };
        return Task.FromResult(envs
            .Where(e => e.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e)
            .Select(e => new AutoCompleteOption(e))
            .ToList());
    }
}

/// <summary>
/// Tests remote attribute handler resolution.
/// Features: RMT-007
/// </summary>
[Command(Name = "remote-deploy")]
public class RemoteDeployCommand : CommandBase
{
    [Argument(Name = "environment")]
    [AutoComplete<RemoteEnvironmentHandler>]
    public string Environment { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"[blue][[REMOTE]][/] Deploying to: {Environment}");
}
