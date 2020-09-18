using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    class TestExecute : CommandBase
    {
        public int Execute(CommandExecutionContext ctx)
        {
            return 0;
        }
    }
}
