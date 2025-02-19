using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    public class ReturnType : CommandBase
    {
        public string Execute(CommandExecutionContext ctx)
        {
            return "hello world!";
        }
    }
}
