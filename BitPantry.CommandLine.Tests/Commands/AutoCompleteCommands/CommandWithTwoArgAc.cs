using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    [Command]
    public class CommandWithTwoArgAc : CommandBase
    {
        [Argument(AutoCompleteFunctionName = nameof(AutoComplete_Arg1))]
        [Alias('a')]
        public string Arg1 { get; set; }

        [Argument(AutoCompleteFunctionName = nameof(AutoComplete_Arg1))]
        [Alias('b')]
        public string Arg2 { get; set; }

        public void Execute(CommandExecutionContext context) { }

        public List<string> AutoComplete_Arg1(AutoCompleteContext context)
        {
            return new List<string> { "Opt1", "Big2", "obc3" };
        }
    }
}
