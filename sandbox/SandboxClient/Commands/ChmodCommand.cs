using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

[Flags]
public enum Permissions { None = 0, Read = 1, Write = 2, Execute = 4, Delete = 8 }

/// <summary>
/// Tests flags enum autocomplete.
/// Features: FR-015 (Enum handler with [Flags])
/// </summary>
[Command(Name = "chmod")]
public class ChmodCommand : CommandBase
{
    [Argument(Name = "perms")]
    public Permissions Perms { get; set; }

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"Permissions: {Perms}");
}
