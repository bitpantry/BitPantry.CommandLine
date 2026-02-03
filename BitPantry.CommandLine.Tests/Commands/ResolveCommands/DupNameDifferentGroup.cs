using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
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
