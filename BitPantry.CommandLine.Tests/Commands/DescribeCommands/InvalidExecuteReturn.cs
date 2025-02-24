using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    class InvalidExecuteReturn : CommandBase
    {
        public int Execute(CommandExecutionContext ctx) { return 0; }
    }
}
