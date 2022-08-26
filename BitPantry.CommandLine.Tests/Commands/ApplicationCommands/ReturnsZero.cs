using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class ReturnsZero : CommandBase
    {
        public int Execute(CommandExecutionContext ctx)
        {
            return 0;
        }
    }
}
