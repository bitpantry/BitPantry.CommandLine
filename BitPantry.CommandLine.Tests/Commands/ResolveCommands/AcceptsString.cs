using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{
    public class AcceptsString : CommandBase
    {
        public void Execute(CommandExecutionContext<string> ctx) { }
    }
}
