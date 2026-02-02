using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxServer.Commands;

public enum RemoteColor { Cyan, Magenta, Yellow, Black }
public enum RemoteSize { Tiny, Small, Medium, Large, Huge }

/// <summary>
/// Tests remote positional arguments and cursor position accuracy.
/// Features: RMT-008
/// </summary>
[Command(Name = "remotepaint")]
public class RemotePaintCommand : CommandBase
{
    [Argument(Position = 0)]
    public RemoteColor Color { get; set; }

    [Argument(Position = 1)]
    public RemoteSize Size { get; set; }

    [Argument(Name = "matte")]
    public bool Matte { get; set; }

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"[blue][[REMOTE]][/] Painting {Size} {Color}, Matte={Matte}");
}
