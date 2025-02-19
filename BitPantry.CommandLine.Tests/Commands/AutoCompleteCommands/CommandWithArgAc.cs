using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    [Command]
    public class CommandWithArgAc : CommandBase
    {
        [Argument(AutoCompleteFunctionName = nameof(AutoComplete_Arg1))]
        [Alias('a')]
        public string Arg1 { get; set; }

        public void Execute(CommandExecutionContext context) { }

        public List<AutoCompleteOption> AutoComplete_Arg1(AutoCompleteContext context)
        {
            return new List<AutoCompleteOption>
            {
                new AutoCompleteOption("Opt1"),
                new AutoCompleteOption("Big2"),
                new AutoCompleteOption("obc3"),
            };
        }
    }
}
