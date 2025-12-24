using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    [Command(Group = typeof(BitPantryGroup))]
    class CommandWithGroupNoName : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
