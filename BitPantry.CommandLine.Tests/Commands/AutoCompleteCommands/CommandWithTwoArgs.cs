using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    [Command]
    public class CommandWithTwoArgs : CommandBase
    {
        [Argument]
        [Alias('a')]
        public string Arg1 { get; set; }

        [Argument]
        [Alias('x')]
        public string XyzQp { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }
}
