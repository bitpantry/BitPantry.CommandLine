using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Parsing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete
{

    /// <summary>
    /// Builds auto-complete options and results for command line inputs.
    /// </summary>
    public class AutoCompleteOptionsBuilder
    {
        private readonly CommandRegistry _registry;
        private readonly IServiceProvider _serviceProvider;

        private readonly ParsedCommandElement _parsedElementUnderCursor;
        private int _previousStringLength = 0;

        /// <summary>
        /// Gets the parsed input.
        /// </summary>
        public ParsedInput ParsedInput { get; private set; }

        /// <summary>
        /// Gets the cursor position in the input string.
        /// </summary>
        public int InputStringCursorPosition { get; private set; }

        /// <summary>
        /// Gets the list of auto-complete options.
        /// </summary>
        public IReadOnlyList<AutoCompleteOption> Options { get; private set; } = new List<AutoCompleteOption>().AsReadOnly();

        /// <summary>
        /// Gets the current auto-complete result.
        /// </summary>
        public AutoCompleteResult CurrentResult { get; private set; } = new AutoCompleteResult();

        /// <summary>
        /// Gets the auto-complete context for unit testing.
        /// </summary>
        public AutoCompleteContext AutoCompleteContext { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoCompleteOptionsBuilder"/> class.
        /// </summary>
        /// <param name="registry">The command registry.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="input">The input string.</param>
        /// <param name="inputStringCursorPosition">The cursor position in the input string.</param>
        public AutoCompleteOptionsBuilder(CommandRegistry registry, IServiceProvider serviceProvider, string input, int inputStringCursorPosition)
        {
            _registry = registry;
            _serviceProvider = serviceProvider;
            _previousStringLength = input.Length;
            ParsedInput = new ParsedInput(input);
            InputStringCursorPosition = inputStringCursorPosition;
            _parsedElementUnderCursor = ParsedInput.GetElementAtCursorPosition(inputStringCursorPosition);

            if (_parsedElementUnderCursor != null)
            {
                BuildOptions().Wait();

                if (Options.Any())
                    BuildResult(Options[0]);
            }
        }

        /// <summary>
        /// Builds the auto-complete result for the specified option.
        /// </summary>
        /// <param name="forOption">The auto-complete option.</param>
        private void BuildResult(AutoCompleteOption forOption)
        {
            var formattedOptionValue = forOption.GetFormattedValue();
            var padStart = string.Empty;
            var padEnd = string.Empty;

            if (_parsedElementUnderCursor.ElementType == CommandElementType.Empty)
            {
                var relativeCursorPosition = ParsedInput.GetRelativeCursorPosition(InputStringCursorPosition);
                padStart = _parsedElementUnderCursor.Raw.Substring(0, ParsedInput.GetRelativeCursorPosition(InputStringCursorPosition) - _parsedElementUnderCursor.StartPosition);
                padEnd = _parsedElementUnderCursor.Raw.Substring(ParsedInput.GetRelativeCursorPosition(InputStringCursorPosition) - _parsedElementUnderCursor.StartPosition);
            }

            var autoCompleteStartPosition = 0;
            var preSb = new StringBuilder();
            var postSb = new StringBuilder();
            var sb = preSb;

            for (int i = 0; i < ParsedInput.ParsedCommands.Count; i++)
            {
                var cmd = ParsedInput.ParsedCommands[i];
                sb.Append(string.Empty.PadLeft(cmd.LeadingWhiteSpaceCount));

                foreach (var elem in cmd.Elements)
                {
                    if (elem == _parsedElementUnderCursor)
                    {
                        sb.Append(padStart);
                        autoCompleteStartPosition = preSb.Length + 1;
                        sb = postSb;
                        sb.Append(formattedOptionValue);
                        sb.Append(padEnd);
                    }
                    else
                    {
                        sb.Append(elem.ToString());
                    }
                }

                if (i < ParsedInput.ParsedCommands.Count - 1)
                    sb.Append('|');
            }

            var newStringLength = preSb.Length + postSb.Length;
            var spaceToClearAtEnd = _previousStringLength - newStringLength;
            if (spaceToClearAtEnd < 0) spaceToClearAtEnd = 0;
            _previousStringLength = newStringLength;

            CurrentResult = new AutoCompleteResult(
                forOption,
                sb.ToString().TrimEnd('|', ' '),
                autoCompleteStartPosition,
                autoCompleteStartPosition + formattedOptionValue.Length,
                spaceToClearAtEnd);
        }

        /// <summary>
        /// Builds the auto-complete options based on the parsed element under the cursor.
        /// </summary>
        private async Task BuildOptions()
        {
            switch (_parsedElementUnderCursor.ElementType)
            {
                case CommandElementType.Command:
                    BuildOptions_Command();
                    break;
                case CommandElementType.ArgumentName:
                    BuildOptions_ArgumentName();
                    break;
                case CommandElementType.ArgumentAlias:
                    BuildOptions_Alias();
                    break;
                case CommandElementType.ArgumentValue:
                    await BuildOptions_ArgumentValue();
                    break;
                case CommandElementType.Unexpected:
                    BuildOptions_Unexpected();
                    break;
                case CommandElementType.Empty:
                    await BuildOptions_ArgumentValue();
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"No case defined for {typeof(CommandElementType).FullName}");
            }
        }

        /// <summary>
        /// Builds the auto-complete options for unexpected elements.
        /// </summary>
        private void BuildOptions_Unexpected()
        {
            if (_parsedElementUnderCursor.Raw.Equals(CommandInputParsingConstants.ElementPrefixArgument))
                BuildOptions_ArgumentName();

            if (_parsedElementUnderCursor.Raw.Equals(CommandInputParsingConstants.ElementPrefixAlias))
                BuildOptions_Alias();
        }

        /// <summary>
        /// Builds the auto-complete options for argument names.
        /// </summary>
        private void BuildOptions_ArgumentName()
        {
            var cmdElem = _parsedElementUnderCursor.ParentCommand.GetCommandElement();
            if (cmdElem == null) return;

            var cmdInfo = _registry.Find(cmdElem.Value);
            if (cmdInfo == null || cmdInfo.Arguments.Count == 0) return;

            var usedArgNames = _parsedElementUnderCursor.ParentCommand.Elements
                .Where(e => e.ElementType == CommandElementType.ArgumentName && e != _parsedElementUnderCursor)
                .Select(e => e.Value.ToUpper())
                .ToList();

            var argNames = cmdInfo.Arguments
                .Where(a =>
                    a.Name.StartsWith(_parsedElementUnderCursor.Value, StringComparison.InvariantCultureIgnoreCase)
                    && !usedArgNames.Contains(a.Name.ToUpper()))
                .Select(a => a.Name)
                .Order()
                .ToList();

            var options = new List<AutoCompleteOption>();
            foreach (var name in argNames)
                options.Add(new AutoCompleteOption(name, $"--{{0}}"));

            Options = options.AsReadOnly();
        }

        /// <summary>
        /// Builds the auto-complete options for argument aliases.
        /// </summary>
        private void BuildOptions_Alias()
        {
            if (_parsedElementUnderCursor.Value.Length > 1) return;

            var cmdElem = _parsedElementUnderCursor.ParentCommand.GetCommandElement();
            if (cmdElem == null) return;

            var cmdInfo = _registry.Find(cmdElem.Value);
            if (cmdInfo == null || cmdInfo.Arguments.Count == 0) return;

            var usedAliases = _parsedElementUnderCursor.ParentCommand.Elements
                .Where(e => e.ElementType == CommandElementType.ArgumentAlias && e.Value.Length == 1 && e != _parsedElementUnderCursor)
                .OrderBy(e => e.Value)
                .Select(e => e.Value.ToUpper()[0])
                .ToList();

            var aliases = cmdInfo.Arguments
                .Where(a => !usedAliases.Contains(a.Alias.ToString().ToUpper()[0]))
                .Select(a => a.Alias)
                .Order()
                .ToList();

            var options = new List<AutoCompleteOption>();
            foreach (var alias in aliases)
                options.Add(new AutoCompleteOption(alias.ToString(), $"-{{0}}"));

            Options = options.AsReadOnly();
        }

        /// <summary>
        /// Builds the auto-complete options for argument values.
        /// </summary>
        private async Task BuildOptions_ArgumentValue()
        {
            var argumentElement = _parsedElementUnderCursor.IsPairedWith;

            if (_parsedElementUnderCursor.ElementType == CommandElementType.Empty)
            {
                var index = _parsedElementUnderCursor.ParentCommand.Elements.ToList().IndexOf(_parsedElementUnderCursor);
                if (_parsedElementUnderCursor.ParentCommand.Elements.Count > 1 && index > 0)
                {
                    var previousElement = _parsedElementUnderCursor.ParentCommand.Elements[index - 1];
                    if (previousElement.ElementType == CommandElementType.ArgumentName || previousElement.ElementType == CommandElementType.ArgumentAlias)
                        argumentElement = previousElement;
                }
            }

            var parsedCmd = _parsedElementUnderCursor.ParentCommand;
            var cmdInfo = _registry.Find(parsedCmd.GetCommandElement().Value);

            if (cmdInfo == null) return;

            var argInfo = argumentElement.ElementType == CommandElementType.ArgumentName
                ? cmdInfo.Arguments.Where(a => a.Name.Equals(argumentElement.Value, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault()
                : cmdInfo.Arguments.Where(a => a.Alias.ToString().Equals(argumentElement.Value, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (argInfo == null || string.IsNullOrEmpty(argInfo.AutoCompleteFunctionName)) return;

            var cmd = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService(cmdInfo.Type);

            AutoCompleteContext = BuildAutoCompleteContext();
            var method = cmdInfo.Type.GetMethod(argInfo.AutoCompleteFunctionName);
            var args = new[] { AutoCompleteContext };

            var result = await (argInfo.IsAutoCompleteFunctionAsync
                    ? (Task<List<string>>)method.Invoke(cmd, args)
                    : Task.Factory.StartNew(() => (List<string>)method.Invoke(cmd, args)));

            result = result.Where(v => v.StartsWith(_parsedElementUnderCursor.Value, StringComparison.InvariantCultureIgnoreCase)).ToList();

            var options = new List<AutoCompleteOption>();
            foreach (var str in result)
                options.Add(new AutoCompleteOption(str));

            Options = options.AsReadOnly();
        }

        /// <summary>
        /// Builds the auto-complete context.
        /// </summary>
        /// <returns>The auto-complete context.</returns>
        private AutoCompleteContext BuildAutoCompleteContext()
        {
            var cmdInfo = _registry.Find(_parsedElementUnderCursor.ParentCommand.GetCommandElement().Value);

            var values = new Dictionary<ArgumentInfo, string>();

            foreach (var val in _parsedElementUnderCursor.ParentCommand.Elements.Where(e => e.ElementType == CommandElementType.ArgumentValue))
            {
                var argInfo = val.IsPairedWith.ElementType == CommandElementType.ArgumentName
                    ? cmdInfo.Arguments.FirstOrDefault(a => a.Name.Equals(val.IsPairedWith.Value, StringComparison.InvariantCultureIgnoreCase))
                    : cmdInfo.Arguments.FirstOrDefault(a => a.Alias.ToString().Equals(val.IsPairedWith.Value, StringComparison.InvariantCultureIgnoreCase));

                if (argInfo != null)
                    values.Add(argInfo, val.Raw);
            }

            return new AutoCompleteContext(values);
        }

        /// <summary>
        /// Builds the auto-complete options for commands.
        /// </summary>
        private void BuildOptions_Command()
        {
            var currentValue = _parsedElementUnderCursor.Value;

            var ns = currentValue.Contains('.') ? currentValue.Split('.').First() : null;
            var name = currentValue.Contains('.') ? currentValue.Split('.')[1] : currentValue;

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(ns))
            {
                var options = _registry.Commands
                    .Where(c =>
                        !string.IsNullOrEmpty(c.Namespace)
                        && c.Namespace.Equals(ns, StringComparison.InvariantCultureIgnoreCase)
                        && c.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    .DistinctBy(c => c.Name)
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Namespace, c.Name })
                    .ToList();

                Options = options.Select(o => new AutoCompleteOption(o.Name, $"{o.Namespace}.{{0}}")).ToList().AsReadOnly();
            }
            else if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(ns))
            {
                var nsOptions = _registry.Commands
                    .Where(c => !string.IsNullOrEmpty(c.Namespace) && c.Namespace.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    .Select(c => c.Namespace)
                    .Distinct()
                    .Order()
                    .ToList();

                var optList = new List<AutoCompleteOption>();
                optList.AddRange(nsOptions.Select(o => new AutoCompleteOption(o, $"{{0}}.")));

                var nameOptions = _registry.Commands
                    .Where(c => string.IsNullOrEmpty(c.Namespace) && c.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    .Select(c => c.Name)
                    .Distinct()
                    .Order()
                    .ToList();

                optList.AddRange(nameOptions.Select(o => new AutoCompleteOption(o)));

                Options = optList.AsReadOnly();
            }
        }

        /// <summary>
        /// Moves to the next auto-complete option.
        /// </summary>
        /// <returns>True if the next option is available; otherwise, false.</returns>
        public bool NextOption()
        {
            if (Options.Count < 2)
                return false;

            var index = new List<AutoCompleteOption>(Options).IndexOf(CurrentResult.Option) + 1;

            if (index > Options.Count - 1)
                index = 0;

            BuildResult(Options[index]);
            return true;
        }

        /// <summary>
        /// Moves to the previous auto-complete option.
        /// </summary>
        /// <returns>True if the previous option is available; otherwise, false.</returns>
        public bool PreviousOption()
        {
            if (Options.Count < 2)
                return false;

            var index = new List<AutoCompleteOption>(Options).IndexOf(CurrentResult.Option) - 1;

            if (index < 0)
                index = Options.Count - 1;

            BuildResult(Options[index]);
            return true;
        }
    }
}
