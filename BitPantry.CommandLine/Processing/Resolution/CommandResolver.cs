using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BitPantry.CommandLine.Processing.Resolution
{
    /// <summary>
    /// Resolves a parsed input command to a registered command
    /// </summary>
    public class CommandResolver
    {
        private CommandRegistry _registry;

        /// <summary>
        /// Creates a new command resolver using the given registry of commands
        /// </summary>
        /// <param name="registry">The command registry for commands that can be resolved</param>
        public CommandResolver(CommandRegistry registry)
        {
            _registry = registry;
        }

        /// <summary>
        /// Attempts to resolve a valid parsed input to a registered command
        /// </summary>
        /// <param name="input">The valid parsed input</param>
        /// <returns>A resolved command</returns>
        public ResolvedCommand Resolve(ParsedInput input)
        {
            // is input valid

            if (!input.IsValid)
                throw new CommandResolutionException(input, "The provided input is invalid and cannot be resolved");

            var cmdInfo = _registry.Find(input.GetCommandElement().Value);

            if (cmdInfo == null)
                return new ResolvedCommand(input, CommandResolutionErrorType.CommandNotFound);

            // begin to accumulate errors

            var errors = new List<ResolveCommandError>();

            // capture argument errors - unknown arguments, options with values

            var inputMap = new Dictionary<ArgumentInfo, ParsedInputElement>();

            foreach (var node in input.Elements.Where(n => n.ElementType == InputElementType.ArgumentName || n.ElementType == InputElementType.ArgumentAlias))
            {
                var argInfo = node.ElementType == InputElementType.ArgumentName
                        ? cmdInfo.Arguments.SingleOrDefault(p => p.Name.Equals(node.Value, StringComparison.OrdinalIgnoreCase))
                        : cmdInfo.Arguments.SingleOrDefault(p => p.Alias.Equals(node.Value.Single()));

                CaptureArgumentErrors(
                    errors,
                    argInfo,
                    node);

                if(argInfo != null)
                {
                    if (!inputMap.ContainsKey(argInfo))
                        inputMap.Add(argInfo, node);
                    else
                        errors.Add(new ResolveCommandError
                        {
                            Type = CommandResolutionErrorType.DuplicateArgument,
                            Element = node,
                            Message = $"Arguments \"{node.Raw}\" and \"{inputMap[argInfo].Raw}\" resolve the same argument property."
                        });
                }
            }

            return new ResolvedCommand(input, cmdInfo, new ReadOnlyDictionary<ArgumentInfo, ParsedInputElement>(inputMap), errors);
        }

        private void CaptureArgumentErrors(
            List<ResolveCommandError> errors,
            ArgumentInfo argInfo,
            ParsedInputElement node)
        {
            var qualifier = node.ElementType == InputElementType.ArgumentName ? "name" : "alias";

            if (argInfo == null) // argument not found
                errors.Add(new ResolveCommandError
                {
                    Type = CommandResolutionErrorType.ArgumentNotFound,
                    Element = node,
                    Message = $"No argument property matching {qualifier} \"{node.Raw}\" could be found."
                });
            else if (argInfo.DataType == typeof(Option) && node.IsPairedWith != null) // option found with a value
                errors.Add(new ResolveCommandError
                {
                    Type = CommandResolutionErrorType.UnexpectedValue,
                    Element = node,
                    Message = $"Argument \"{node.Raw}\" has an associated value, but resolves to a property of type {typeof(Option).FullName} (options can not have associated values)"
                });
        }
    }
}
