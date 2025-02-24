using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class ReturnsInputPlusOne : CommandBase
    {
        public int Execute(CommandExecutionContext<int> ctx)
        {
            return ctx.Input + 1;
        }
    }
}
