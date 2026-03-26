using BitPantry.CommandLine.API;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.PluginModule
{
    /// <summary>
    /// A module that registers grouped commands for testing group functionality.
    /// </summary>
    public class GroupedCommandsModule : ICommandModule
    {
        /// <inheritdoc/>
        public string Name => "GroupedCommandsModule";

        /// <inheritdoc/>
        public void Configure(ICommandModuleContext context)
        {
            // Commands with [InGroup<T>] will auto-register their groups
            context.Commands.RegisterCommand(typeof(PluginMathAddCommand));
            context.Commands.RegisterCommand(typeof(PluginMathSubtractCommand));
        }
    }

    /// <summary>
    /// A test group for plugin math operations.
    /// </summary>
    [Group(Name = "plugin-math")]
    [Description("Plugin mathematical operations")]
    public class PluginMathGroup { }

    /// <summary>
    /// An add command in the plugin-math group.
    /// </summary>
    [InGroup<PluginMathGroup>]
    [Command(Name = "add")]
    [Description("Adds two numbers")]
    public class PluginMathAddCommand : CommandBase
    {
        [Argument(Name = "a", Position = 0, IsRequired = true)]
        public int A { get; set; }

        [Argument(Name = "b", Position = 1, IsRequired = true)]
        public int B { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            Console.MarkupLine($"{A} + {B} = {A + B}");
        }
    }

    /// <summary>
    /// A subtract command in the plugin-math group.
    /// </summary>
    [InGroup<PluginMathGroup>]
    [Command(Name = "subtract")]
    [Description("Subtracts two numbers")]
    public class PluginMathSubtractCommand : CommandBase
    {
        [Argument(Name = "a", Position = 0, IsRequired = true)]
        public int A { get; set; }

        [Argument(Name = "b", Position = 1, IsRequired = true)]
        public int B { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            Console.MarkupLine($"{A} - {B} = {A - B}");
        }
    }
}
