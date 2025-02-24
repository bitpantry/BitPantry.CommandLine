using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class TestExecuteError : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            var x = 1;
            var y = 0;
            var z = x / y;
        }
    }
}
