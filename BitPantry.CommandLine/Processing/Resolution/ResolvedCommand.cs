using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Processing.Resolution
{
    /// <summary>
    /// Represents a command that has been resolved to an input
    /// </summary>
    public class ResolvedCommand
    {
        /// <summary>
        /// The input resolved to the command
        /// </summary>
        public ParsedCommand ParsedCommand { get; private set; }

        /// <summary>
        /// The CommandInfo resolved by the input
        /// </summary>
        public CommandInfo CommandInfo { get; private set; }

        /// <summary>
        /// An map between the command's argument information and the input
        /// </summary>
        public IReadOnlyDictionary<ArgumentInfo, ParsedCommandElement> InputMap { get; private set; }

        /// <summary>
        /// Any resolution errors
        /// </summary>
        public IReadOnlyCollection<ResolveCommandError> Errors { get; private set; }

        /// <summary>
        /// Whether or not the resolution is valid
        /// </summary>
        public bool IsValid => !Errors.Any();

        internal ResolvedCommand(ParsedCommand input, CommandResolutionErrorType error, string message = null) 
            : this(input, null, null, new List<ResolveCommandError>(new[] { new ResolveCommandError { Type = error, Message = message } })) { }

        internal ResolvedCommand(
            ParsedCommand parsedCommand,
            CommandInfo info,
            IReadOnlyDictionary<ArgumentInfo, ParsedCommandElement> inputMap,
            List<ResolveCommandError> errors)
        {
            ParsedCommand = parsedCommand;
            CommandInfo = info;
            InputMap = inputMap;
            Errors = errors == null
                ? new List<ResolveCommandError>().AsReadOnly()
                : errors.AsReadOnly();
        }
    }
}
