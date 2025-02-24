using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class TestExecuteWithReturnType : CommandBase
    {
        public string Execute(CommandExecutionContext ctx)
        {
            return "hello world!";
        }
    }
}
