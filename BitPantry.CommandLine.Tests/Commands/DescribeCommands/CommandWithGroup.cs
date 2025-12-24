using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    [Command(Group = typeof(BitPantryGroup), Name = "NewName")]
    class CommandWithGroup : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
