using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    [InGroup<BitPantryGroup>]
    [Command(Name = "NewName")]
    class CommandWithGroup : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
