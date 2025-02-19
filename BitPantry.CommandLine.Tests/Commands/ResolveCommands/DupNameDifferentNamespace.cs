using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{
    [Command(Namespace = "BitPantry", Name = "Command")]
    public class DupNameDifferentNamespace : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
