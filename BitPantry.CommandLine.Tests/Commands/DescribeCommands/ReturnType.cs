using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    public class ReturnType : CommandBase
    {
        public string Execute(CommandExecutionContext ctx)
        {
            return "hello world!";
        }
    }
}
