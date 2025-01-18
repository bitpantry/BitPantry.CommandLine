using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{

    [Command]
    public class CommandWithArg : CommandBase
    {
        [Argument]
        [Alias('a')]
        public string Arg1 { get; set; }

        public void Execute(CommandExecutionContext context) { }
    }
}
