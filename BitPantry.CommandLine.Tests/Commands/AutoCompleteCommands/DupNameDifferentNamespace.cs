using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
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
