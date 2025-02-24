using BitPantry.CommandLine.API;
using Microsoft.Extensions.Logging;


namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment.Commands
{
    [Command(Namespace = "test", Name = "basic")]
    public class BasicCommand : CommandBase
    {
        private ILogger<BasicCommand> _logger;

        public BasicCommand(ILogger<BasicCommand> logger) { _logger = logger; }
        public void Execute(CommandExecutionContext ctx)
        {
            _logger.LogDebug("Basic command executed");
        }
    }
}
