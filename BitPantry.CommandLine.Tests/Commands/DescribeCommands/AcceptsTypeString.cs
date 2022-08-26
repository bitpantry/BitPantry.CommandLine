using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    class AcceptsTypeString : CommandBase
    {
        public void Execute(CommandExecutionContext<string> ctx) { }
    }
}
