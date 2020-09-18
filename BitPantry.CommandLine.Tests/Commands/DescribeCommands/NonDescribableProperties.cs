using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    /// <summary>
    /// Intended to test the describe results of a command with non-describably properties (properties without attributes)
    /// </summary>
    class NonDescribableProperties : CommandBase
    {
        public int MyProperty { get; set; }

        public int Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
