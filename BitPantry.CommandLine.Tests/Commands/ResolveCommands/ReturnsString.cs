using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{

    public class ReturnsString : CommandBase
    {
        public string Execute(CommandExecutionContext ctx)
        {
            return "hello world!";
        }
    }
}
