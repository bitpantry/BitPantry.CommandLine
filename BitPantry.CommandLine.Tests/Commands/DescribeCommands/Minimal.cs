using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    /// <summary>
    /// Intended to test the describe results of a minimal command class
    /// </summary>
    class Minimal : CommandBase
    {
        public int Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
