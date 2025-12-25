using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// Test command with positional argument that has auto-complete
    /// </summary>
    [Command]
    class PositionalWithAutoCompleteCommand : CommandBase
    {
        [Argument(Position = 0, AutoCompleteFunctionName = nameof(GetFileCompletions))]
        public string FileName { get; set; }

        [Argument(Position = 1, AutoCompleteFunctionName = nameof(GetModeCompletions))]
        public string Mode { get; set; }

        [Argument]
        [Alias('v')]
        public Option Verbose { get; set; }

        public List<AutoCompleteOption> GetFileCompletions(AutoCompleteContext context)
        {
            return new List<AutoCompleteOption>
            {
                new AutoCompleteOption("file1.txt"),
                new AutoCompleteOption("file2.txt"),
                new AutoCompleteOption("data.csv")
            };
        }

        public List<AutoCompleteOption> GetModeCompletions(AutoCompleteContext context)
        {
            var options = new List<AutoCompleteOption>
            {
                new AutoCompleteOption("read"),
                new AutoCompleteOption("write"),
                new AutoCompleteOption("append")
            };
            
            // If FileName was provided in context, add a marker option
            if (context.Values != null && context.Values.Count > 0)
            {
                options.Add(new AutoCompleteOption("has-context"));
            }
            
            return options;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
