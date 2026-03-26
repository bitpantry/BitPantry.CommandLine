using BitPantry.CommandLine.API;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.PluginModule
{
    /// <summary>
    /// A test command that echoes input and demonstrates DI injection.
    /// </summary>
    [Command(Name = "plugin-echo")]
    [Description("Echoes input and shows the configured service value")]
    public class PluginEchoCommand : CommandBase
    {
        private readonly IPluginService _service;

        [Argument(Name = "message", Position = 0)]
        [Description("The message to echo")]
        public string Message { get; set; } = "";

        public PluginEchoCommand(IPluginService service)
        {
            _service = service;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            Console.MarkupLine($"Echo: {Message}");
            Console.MarkupLine($"Service value: {_service.GetValue()}");
        }
    }
}
