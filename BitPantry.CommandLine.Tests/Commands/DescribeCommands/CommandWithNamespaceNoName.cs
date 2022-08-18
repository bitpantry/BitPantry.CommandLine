using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    [Command(Namespace = "BitPantry")]
    class CommandWithNamespaceNoName : CommandBase
    {
        public int Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
