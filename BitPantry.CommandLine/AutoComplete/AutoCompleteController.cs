using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Prompt;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete
{
    public class AutoCompleteController : IDisposable
    {
        private AutoCompleteOptionSetBuilder _optionsBldr;

        private readonly string defaultOptionMarkup = "black on silver";

        private int _activeStartingBufferPosition;
        private ParsedInput _activeParsedInput;
        private ParsedCommandElement _activeParsedElement;
        private AutoCompleteOptionSet _activeOptionsSet;
        private int _activeAutoCompleteStartPosition;

        public bool IsEngaged => _activeOptionsSet != null;

        public AutoCompleteController(AutoCompleteOptionSetBuilder optionsBldr)
        {
            _optionsBldr = optionsBldr;
        }

        public async Task Begin(ConsoleLineMirror inputLine)
        {
            _activeStartingBufferPosition = inputLine.BufferPosition;
            _activeParsedInput = new ParsedInput(inputLine.Buffer);
            _activeParsedElement = _activeParsedInput.GetElementAtPosition(_activeStartingBufferPosition);

            if (_activeParsedElement == null) return;

            _activeOptionsSet = await _optionsBldr.BuildOptions(_activeParsedElement);

            if (_activeOptionsSet == null) // no options, end auto complete
                _activeOptionsSet = null;
            else // preview the current option to the console
                PreviewCurrentOption(inputLine, defaultOptionMarkup);
        }

        private void PreviewCurrentOption(ConsoleLineMirror inputLine, string markup)
        {
            // if no option available, return

            if (_activeOptionsSet == null || _activeOptionsSet.CurrentOption == null) return;

            // initialize option value parameters

            var formattedOptionValue = _activeOptionsSet.CurrentOption.GetFormattedValue(markup);
            var padStart = string.Empty;
            var padEnd = string.Empty;

            // if the active parsed element is of type empty, then prepare to insert the preview into the white space at the relative cursor position

            if (_activeParsedElement.ElementType == CommandElementType.Empty)
            {
                var relativeCursorPosition = _activeParsedInput.GetCursorPositionRelativeToCommandString(_activeStartingBufferPosition);
                padStart = _activeParsedElement.Raw.Substring(0, relativeCursorPosition - (_activeParsedElement.StartPosition - 1));
                padEnd = _activeParsedElement.Raw.Substring(relativeCursorPosition - (_activeParsedElement.StartPosition - 1));
            }

            // initialize the string builders used to rebuild the input string around the option preview

            var preSb = new StringBuilder();
            var postSb = new StringBuilder();
            var sb = preSb;

            for (int i = 0; i < _activeParsedInput.ParsedCommands.Count; i++)
            {
                var cmd = _activeParsedInput.ParsedCommands[i];
                sb.Append(string.Empty.PadLeft(cmd.LeadingWhiteSpaceCount));

                foreach (var elem in cmd.Elements)
                {
                    if (elem == _activeParsedElement) // when reaching the active parsed element, swtich to the new string builder making the preview value the beginning of the new string
                    {
                        sb.Append(padStart);
                        _activeAutoCompleteStartPosition = preSb.GetTerminalDisplayLength();
                        sb = postSb;
                        sb.Append(formattedOptionValue);
                        sb.Append(padEnd);
                    }
                    else
                    {
                        sb.Append(elem.ToString().EscapeMarkup());
                    }
                }

                if (i < _activeParsedInput.ParsedCommands.Count - 1)
                    sb.Append('|');
            }

            // update the console with the new preview

            SetOverwrite(inputLine, (line) =>
            {
                line.MoveToPosition(_activeAutoCompleteStartPosition); // move to start of auto complete
                line.Markup(sb.ToString().TrimEnd([' ', '|'])); // write the line
                line.Clear(line.BufferPosition); // clear out the older input line
                line.MoveToPosition(_activeAutoCompleteStartPosition + formattedOptionValue.Unmarkup().Length); // move cursor to end of auto complete position
            });
        }

        public void PreviousOption(ConsoleLineMirror input)
        {
            if (_activeOptionsSet.PreviousOption())
                PreviewCurrentOption(input, defaultOptionMarkup);
        }

        public void NextOption(ConsoleLineMirror input)
        {
            if (_activeOptionsSet.NextOption())
                PreviewCurrentOption(input, defaultOptionMarkup);
        }

        public void Cancel(ConsoleLineMirror inputLine)
        {
            SetOverwrite(inputLine, (line) =>
            {
                line.MoveToPosition(_activeAutoCompleteStartPosition);
                line.Write(_activeParsedInput.ToString().Substring(_activeAutoCompleteStartPosition));
                line.Clear(line.BufferPosition);
                line.MoveToPosition(_activeStartingBufferPosition);
            });
            _activeOptionsSet = null;
        }

        public void Accept(ConsoleLineMirror input)
        {
            PreviewCurrentOption(input, null);
            _activeOptionsSet = null;
        }

        public void End(ConsoleLineMirror input)
        {
            PreviewCurrentOption(input, null);
            _activeOptionsSet = null;
        }

        private void SetOverwrite(ConsoleLineMirror line, Action<ConsoleLineMirror> action)
        {
            var originalOverwrite = line.Overwrite;
            line.Overwrite = true;
            action(line);
            line.Overwrite = originalOverwrite;
        }

        public void Dispose()
        {
            _optionsBldr.Dispose();
        }

    }
}
