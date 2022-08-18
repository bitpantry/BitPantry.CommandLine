using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{
    [Command(Namespace = "BitPantry", Name = "Command")]
    public class DupNameDifferentNamespace : CommandBase
    {
        public int Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
