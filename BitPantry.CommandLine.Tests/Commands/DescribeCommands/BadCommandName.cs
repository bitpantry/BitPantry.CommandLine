using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    [Command(Name = "bad.name")]
    public class BadCommandName : CommandBase
    {
        public int Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
