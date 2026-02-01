using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxServer.Commands;

public enum RemotePriority { Low, Medium, High, Critical }
public enum RemoteStatus { Pending, Active, Completed, Cancelled }

/// <summary>
/// Tests remote enum and boolean autocomplete over SignalR.
/// Features: RMT-004, RMT-005, RMT-UX-001, RMT-UX-002
/// </summary>
[Command(Name = "remote-task")]
public class RemoteTaskCommand : CommandBase
{
    [Argument(Name = "priority")]
    public RemotePriority Priority { get; set; }

    [Argument(Name = "status")]
    public RemoteStatus Status { get; set; }

    [Argument(Name = "urgent")]
    public bool Urgent { get; set; }

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"[blue][[REMOTE]][/] Task: {Priority}/{Status}, Urgent={Urgent}");
}
