using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

public enum Month
{
    January, February, March, April, May, June,
    July, August, September, October, November, December
}

/// <summary>
/// Tests menu scroll and type-to-filter with >5 options.
/// Features: FR-047 to FR-052
/// </summary>
[Command(Name = "calendar")]
public class CalendarCommand : CommandBase
{
    [Argument(Name = "month")]
    public Month Month { get; set; }

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"Calendar for: {Month}");
}
