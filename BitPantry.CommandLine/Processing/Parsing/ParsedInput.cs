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

        /// <summary>
        /// Returns the element at the given position in the input string - where the given position is greater-than-or-equal-to the element start position
        /// and less-than-or-equal to the element end position 
        /// </summary>
        /// <param name="position">The position (one-based index)</param>
        /// <returns>The element at the given position, or null if there is no element at the position</returns>
        public ParsedCommandElement GetElementAtPosition(int position)
        {
            var currentCmdStartPos = 0;

            foreach (var cmd in ParsedCommands)
            {
                if (position > currentCmdStartPos && position <= currentCmdStartPos + cmd.StringLength)
                    return cmd.GetElementAtPosition(position - currentCmdStartPos);
                else
                    currentCmdStartPos += cmd.StringLength + 1; // +1 to account for the pipe
            }

            return null;
        }

        /// <summary>
        /// Returns the element at the given cursor position in the input string. This function differs from GetElementAtPosition - if the cursor position is at the 
        /// start position of an empty element the previous element is returned (if any). If the cursor position is at the end position + 1 of the last element, the 
        /// last element is returned (if any). If neither of these conditions are met, the element is returned the same way as GetElementAtPosition.
        /// </summary>
        /// <param name="position">The position (one-based index)</param>
        /// <returns>The element at the given position, or null if there is no element at the position</returns>
        public ParsedCommandElement GetElementAtCursorPosition(int position)
        {
            var currentCmdStartPos = 0;

            foreach (var cmd in ParsedCommands)
            {
                if (position > currentCmdStartPos && position <= currentCmdStartPos + cmd.StringLength + 1) 
                    return cmd.GetElementAtCursorPosition(position - currentCmdStartPos);
                else
                    currentCmdStartPos += cmd.StringLength + 1; // +1 to account for the pipe
            }

            return null;
        }

        /// <summary>
        /// Returns the cursor position relative to the command at the given cursor position
        /// </summary>
        /// <param name="position">The input string cursor relative position</param>
        /// <returns>The relative cursor position, or -1 if the cursor position is not on a command string (either before, after, or on a pipe)</returns>
        public int GetCursorPositionRelativeToCommandString(int position)
        {
            var currentCmdStartPos = 0;

            foreach (var cmd in ParsedCommands)
            {
                if (position > currentCmdStartPos && position <= currentCmdStartPos + cmd.StringLength + 1)
                    return position - currentCmdStartPos;
                else
                    currentCmdStartPos += cmd.StringLength + 1; // +1 to account for the pipe
            }

            return -1;
        }

        public override string ToString()
            => string.Join('|', ParsedCommands.Select(c => c.ToString()));
    }
}
