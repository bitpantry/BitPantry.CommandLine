using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    [InGroup<BitPantryGroup>]
    [Command(Name = "Command")]
    public class DupNameDifferentGroup : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
