using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class ReturnsInputPlusOne : CommandBase
    {
        public int Execute(CommandExecutionContext<int> ctx)
        {
            return ctx.Input + 1;
        }
    }
}
