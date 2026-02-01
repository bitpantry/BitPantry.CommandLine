using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

public enum Priority { Low, Medium, High, Critical }
public enum Status { Pending, Active, Completed, Cancelled }

/// <summary>
/// Tests built-in enum and boolean autocomplete handlers.
/// Features: FR-015, FR-016, FR-017
/// </summary>
[Command(Name = "task")]
public class TaskCommand : CommandBase
{
    [Argument(Name = "priority")]
    public Priority Priority { get; set; }

    [Argument(Name = "status")]
    public Status Status { get; set; }

    [Argument(Name = "urgent")]
    public bool Urgent { get; set; }

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"Task: {Priority}/{Status}, Urgent={Urgent}");
}
