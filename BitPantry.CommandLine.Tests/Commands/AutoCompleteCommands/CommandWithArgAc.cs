using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    [Command]
    public class CommandWithArgAc : CommandBase
    {
        [Argument]
        [Completion(nameof(AutoComplete_Arg1))]
        [Alias('a')]
        public string Arg1 { get; set; }

        public void Execute(CommandExecutionContext context) { }

        public IEnumerable<CompletionItem> AutoComplete_Arg1(CompletionContext context)
        {
            return new List<CompletionItem>
            {
                new CompletionItem { InsertText = "Opt1" },
                new CompletionItem { InsertText = "Big2" },
                new CompletionItem { InsertText = "obc3" },
            };
        }
    }
}
