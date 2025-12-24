using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    [Command(Group = typeof(BitPantryGroup), Name = "Command")]
    public class DupNameDifferentGroup : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
