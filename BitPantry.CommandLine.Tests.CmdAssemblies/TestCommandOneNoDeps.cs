using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.CmdAssemblies
{
    public class TestCommandOneNoDeps : CommandBase
    {
        public int Execute(CommandExecutionContext ctx) { return 0; }
    }
}
