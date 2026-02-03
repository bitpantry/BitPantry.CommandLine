using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
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
