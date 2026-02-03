using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{
    [InGroup<BitPantryGroup>]
    [Command]
    public class CommandWithGroup : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
