using BitPantry.CommandLine.API;
using Microsoft.Extensions.Logging;


namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment.Commands
{
    [Command(Namespace = "test", Name = "lrc")]
    public class LongRunningCommand : CommandBase
    {
        private ILogger<LongRunningCommand> _logger;

        public LongRunningCommand(ILogger<LongRunningCommand> logger)
        {
            _logger = logger;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            await Task.Delay(500);
            _logger.LogDebug("Long running command finished");
        }
    }
}
