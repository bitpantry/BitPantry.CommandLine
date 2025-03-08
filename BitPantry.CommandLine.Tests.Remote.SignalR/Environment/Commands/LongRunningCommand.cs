using BitPantry.CommandLine.API;
using Microsoft.Extensions.Logging;


namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment.Commands
{
    [Command(Namespace = "test", Name = "lrc")]
    public class LongRunningCommand : CommandBase
    {
        public static TaskCompletionSource<bool> Tcs = new TaskCompletionSource<bool>();

        private ILogger<LongRunningCommand> _logger;

        public LongRunningCommand(ILogger<LongRunningCommand> logger)
        {
            _logger = logger;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            Tcs.SetResult(true);

            await Task.Delay(500);
            _logger.LogDebug("Long running command finished");
        }
    }
}
