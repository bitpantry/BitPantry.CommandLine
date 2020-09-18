using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    class ArgumentWithAlias : CommandBase
    {
        [Argument]
        [Alias('p')]
        public int MyProperty { get; set; }

        public int Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
