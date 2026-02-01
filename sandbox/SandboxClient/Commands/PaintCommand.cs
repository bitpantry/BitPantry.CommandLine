using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

public enum Color { Red, Green, Blue, Yellow }
public enum Size { Small, Medium, Large, XLarge }

/// <summary>
/// Tests positional argument tracking and satisfaction.
/// Features: FR-042 to FR-046
/// </summary>
[Command(Name = "paint")]
public class PaintCommand : CommandBase
{
    [Argument(Position = 0)]
    public Color Color { get; set; }

    [Argument(Position = 1)]
    public Size Size { get; set; }

    [Argument(Name = "glossy")]
    public bool Glossy { get; set; }

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"Painting {Size} {Color}, Glossy={Glossy}");
}
