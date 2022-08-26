using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitPantry.CommandLine.Processing.Parsing
{
    /// <summary>
    /// Parses a raw input string into parsed commands and validation errors
    /// </summary>
    public class ParsedInput
    {
        private List<ParsedCommand> _parsedCommands;

        public IReadOnlyList<ParsedCommand> ParsedCommands => _parsedCommands.AsReadOnly();

        public bool IsValid => !_parsedCommands.Any(c => !c.IsValid);

        public ParsedInput(string input)
        {
            _parsedCommands = StringParsing.SplitInputString(input)
                .Select(cs => new ParsedCommand(cs))
                .ToList();
        }
    }
}
