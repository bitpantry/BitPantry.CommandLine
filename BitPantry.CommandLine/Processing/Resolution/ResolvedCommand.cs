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
        public ParsedInput Input { get; private set; }

        /// <summary>
        /// The CommandInfo resolved by the input
        /// </summary>
        public CommandInfo CommandInfo { get; private set; }

        /// <summary>
        /// An map between the command's argument information and the input
        /// </summary>
        public IReadOnlyDictionary<ArgumentInfo, ParsedInputElement> InputMap { get; private set; }

        /// <summary>
        /// Any resolution errors
        /// </summary>
        public IReadOnlyCollection<ResolveCommandError> Errors { get; private set; }

        /// <summary>
        /// Whether or not the resolution is valid
        /// </summary>
        public bool IsValid => !Errors.Any();

        //internal ResolvedCommand(ParsedInput input, CommandInfo info) : this(input, info, null) { }

        internal ResolvedCommand(ParsedInput input, CommandResolutionErrorType error, string message = null) 
            : this(input, null, null, new List<ResolveCommandError>(new[] { new ResolveCommandError { Type = error, Message = message } })) { }

        internal ResolvedCommand(
            ParsedInput input,
            CommandInfo info,
            IReadOnlyDictionary<ArgumentInfo, ParsedInputElement> inputMap,
            List<ResolveCommandError> errors)
        {
            Input = input;
            CommandInfo = info;
            InputMap = inputMap;
            Errors = errors == null
                ? new List<ResolveCommandError>().AsReadOnly()
                : errors.AsReadOnly();
        }
    }
}
