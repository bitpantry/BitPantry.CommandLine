using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class ReturnsZero : CommandBase
    {
        public int Execute(CommandExecutionContext ctx)
        {
            return 0;
        }
    }
}
