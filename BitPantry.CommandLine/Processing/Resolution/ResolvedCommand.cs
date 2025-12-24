using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Processing.Resolution
{
    /// <summary>
    /// The type of resolution result
    /// </summary>
    public enum ResolvedType
    {
        /// <summary>
        /// The input resolved to a command
        /// </summary>
        Command,

        /// <summary>
        /// The input resolved to a group (no command specified, group help should be displayed)
        /// </summary>
        Group
    }

    /// <summary>
    /// Represents a command that has been resolved to an input
    /// </summary>
    public class ResolvedCommand
    {
        /// <summary>
        /// The type of resolution - whether this resolved to a command or a group
        /// </summary>
        public ResolvedType ResolvedType { get; private set; } = ResolvedType.Command;

        /// <summary>
        /// The GroupInfo if the resolution type is Group, or the command's group if type is Command
        /// </summary>
        public GroupInfo GroupInfo { get; private set; }

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
            GroupInfo = info?.Group;
            InputMap = inputMap;
            Errors = errors == null
                ? new List<ResolveCommandError>().AsReadOnly()
                : errors.AsReadOnly();
        }

        /// <summary>
        /// Creates a ResolvedCommand that represents a group resolution (for group help display)
        /// </summary>
        internal static ResolvedCommand ForGroup(ParsedCommand parsedCommand, GroupInfo groupInfo)
        {
            return new ResolvedCommand(parsedCommand, null, null, null)
            {
                ResolvedType = ResolvedType.Group,
                GroupInfo = groupInfo
            };
        }
    }
}
