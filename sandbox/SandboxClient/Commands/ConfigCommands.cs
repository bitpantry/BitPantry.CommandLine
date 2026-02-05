using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

/// <summary>
/// Command group for testing group/command syntax autocomplete.
/// Features: FR-018 to FR-022
/// </summary>
[Group(Name = "config")]
public class ConfigGroup { }

[InGroup<ConfigGroup>]
[Command(Name = "show")]
public class ConfigShowCommand : CommandBase
{
    [Argument(Name = "key")]
    public string Key { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"Config[{Key}] = (value)");
}

[InGroup<ConfigGroup>]
[Command(Name = "set")]
public class ConfigSetCommand : CommandBase
{
    [Argument(Name = "key")]
    public string Key { get; set; } = "";

    [Argument(Name = "value")]
    public string Value { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
        => Console.MarkupLine($"Set Config[{Key}] = {Value}");
}
