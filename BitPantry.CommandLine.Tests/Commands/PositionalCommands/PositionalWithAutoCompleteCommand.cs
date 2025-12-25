using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
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
        [Argument(Position = 0)]
        [Completion(nameof(GetFileCompletions))]
        public string FileName { get; set; }

        [Argument(Position = 1)]
        [Completion(nameof(GetModeCompletions))]
        public string Mode { get; set; }

        [Argument]
        [Alias('v')]
        public Option Verbose { get; set; }

        public IEnumerable<CompletionItem> GetFileCompletions(CompletionContext context)
        {
            return new List<CompletionItem>
            {
                new CompletionItem { InsertText = "file1.txt" },
                new CompletionItem { InsertText = "file2.txt" },
                new CompletionItem { InsertText = "data.csv" }
            };
        }

        public IEnumerable<CompletionItem> GetModeCompletions(CompletionContext context)
        {
            var options = new List<CompletionItem>
            {
                new CompletionItem { InsertText = "read" },
                new CompletionItem { InsertText = "write" },
                new CompletionItem { InsertText = "append" }
            };
            
            // If FileName was provided in context, add a marker option
            if (context.ParsedArguments != null && context.ParsedArguments.Count > 0)
            {
                options.Add(new CompletionItem { InsertText = "has-context" });
            }
            
            return options;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
