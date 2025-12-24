using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{
    [Command(Group = typeof(BitPantryGroup))]
    public class CommandWithGroup : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
