using BitPantry.CommandLine.API;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.PluginModule
{
    /// <summary>
    /// A test command that greets the user.
    /// </summary>
    [Command(Name = "plugin-greet")]
    [Description("A greeting command from the plugin")]
    public class PluginGreetCommand : CommandBase
    {
        [Argument(Name = "name", Position = 0)]
        [Description("The name to greet")]
        public string Name { get; set; } = "World";

        public void Execute(CommandExecutionContext ctx)
        {
            Console.MarkupLine($"Hello from plugin, {Name}!");
        }
    }
}
