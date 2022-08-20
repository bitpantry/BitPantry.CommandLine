using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{
    class MultipleArgumentsAndAliases : CommandBase
    {
        [Argument]
        public int MyProperty { get; set; }

        [Argument]
        [Alias('p')]
        public string PropertyTwo { get; set; }

        [Argument]
        [Alias('X')]
        public string Prop { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
