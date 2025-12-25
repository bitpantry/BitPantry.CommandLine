using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Processing.Resolution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BitPantry.CommandLine.Help
{
    /// <summary>
    /// Handles help flag detection and validation.
    /// </summary>
    public class HelpHandler
    {
        private readonly IHelpFormatter _formatter;
        private readonly CommandRegistry _registry;

        public HelpHandler(IHelpFormatter formatter, CommandRegistry registry)
        {
            _formatter = formatter;
            _registry = registry;
        }

        /// <summary>
        /// Checks if the input is a root-level help request (--help or -h with no command).
        /// </summary>
        public bool IsRootHelpRequest(ParsedInput input)
        {
            if (!input.IsValid && input.ParsedCommands.Count == 1)
            {
                // Check if the only content is --help or -h
                var cmd = input.ParsedCommands[0];
                var elements = cmd.Elements.ToList();
                
                if (elements.Count == 1)
                {
                    var elem = elements[0];
                    if (elem.ElementType == CommandElementType.ArgumentName && 
                        elem.Value.Equals("help", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (elem.ElementType == CommandElementType.ArgumentAlias && 
                        elem.Value.Equals("h", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the input is a help request for a group (e.g., "math --help" or "math").
        /// Returns the group info if found, null otherwise.
        /// </summary>
        public GroupInfo GetGroupHelpRequest(ParsedInput input)
        {
            if (!input.IsValid) return null;
            if (input.ParsedCommands.Count != 1) return null;

            var cmd = input.ParsedCommands[0];
            var elements = cmd.Elements.ToList();
            
            if (elements.Count == 0) return null;

            // Get all command path elements (Command + consecutive PositionalValue before arguments)
            var commandElements = GetCommandPathElements(elements);
            if (commandElements.Count == 0) return null;

            // Check for help flag
            var hasHelpFlag = elements.Any(e => 
                (e.ElementType == CommandElementType.ArgumentName && e.Value.Equals("help", StringComparison.OrdinalIgnoreCase)) ||
                (e.ElementType == CommandElementType.ArgumentAlias && e.Value.Equals("h", StringComparison.OrdinalIgnoreCase)));

            // Build the group path
            var groupPath = string.Join(" ", commandElements.Select(e => e.Value));
            
            // Try to find the group
            var group = _registry.FindGroup(groupPath);
            
            if (group != null)
            {
                // If group exists and either has --help flag OR no significant non-command elements
                // (Empty elements from spaces between tokens should be ignored)
                var nonCommandElements = elements.Where(e => 
                    e.ElementType != CommandElementType.Command && 
                    e.ElementType != CommandElementType.PositionalValue &&
                    e.ElementType != CommandElementType.Empty).ToList();
                if (hasHelpFlag || nonCommandElements.Count == 0)
                {
                    return group;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets elements that form the command path (Command + consecutive PositionalValue before any arguments).
        /// </summary>
        private List<ParsedCommandElement> GetCommandPathElements(List<ParsedCommandElement> elements)
        {
            var pathElements = new List<ParsedCommandElement>();
            foreach (var elem in elements.Where(e => e.ElementType != CommandElementType.Empty))
            {
                if (elem.ElementType == CommandElementType.Command || elem.ElementType == CommandElementType.PositionalValue)
                {
                    pathElements.Add(elem);
                }
                else if (elem.ElementType == CommandElementType.ArgumentName ||
                         elem.ElementType == CommandElementType.ArgumentAlias ||
                         elem.ElementType == CommandElementType.EndOfOptions)
                {
                    // Arguments stop the command path
                    break;
                }
            }
            return pathElements;
        }

        /// <summary>
        /// Checks if the input is a help request for a command (e.g., "math add --help").
        /// Returns the command info if found, null otherwise.
        /// Also returns whether help is combined with other args (invalid - FR-018a).
        /// </summary>
        public (CommandInfo Command, bool IsCombinedWithOtherArgs)? GetCommandHelpRequest(ParsedInput input)
        {
            if (!input.IsValid) return null;
            if (input.ParsedCommands.Count != 1) return null;

            var cmd = input.ParsedCommands[0];
            var elements = cmd.Elements.ToList();
            
            if (elements.Count == 0) return null;

            // Check for help flag
            var hasHelpFlag = elements.Any(e => 
                (e.ElementType == CommandElementType.ArgumentName && e.Value.Equals("help", StringComparison.OrdinalIgnoreCase)) ||
                (e.ElementType == CommandElementType.ArgumentAlias && e.Value.Equals("h", StringComparison.OrdinalIgnoreCase)));

            if (!hasHelpFlag) return null;

            // Build the command path (same as GetFullCommandPath would)
            var commandPath = cmd.GetFullCommandPath();
            
            // Try to find the command
            var commandInfo = _registry.Find(commandPath);
            
            if (commandInfo != null)
            {
                // Check if combined with other args
                var otherArgs = elements.Where(e => 
                    (e.ElementType == CommandElementType.ArgumentName && !e.Value.Equals("help", StringComparison.OrdinalIgnoreCase)) ||
                    (e.ElementType == CommandElementType.ArgumentAlias && !e.Value.Equals("h", StringComparison.OrdinalIgnoreCase)) ||
                    e.ElementType == CommandElementType.ArgumentValue).ToList();

                return (commandInfo, otherArgs.Any());
            }

            return null;
        }

        /// <summary>
        /// Checks if the resolved command has a help flag (--help or -h).
        /// </summary>
        public bool HasHelpFlag(ResolvedCommand resolved)
        {
            var elements = resolved.ParsedCommand.Elements.ToList();
            return elements.Any(e =>
                (e.ElementType == CommandElementType.ArgumentName && e.Value.Equals("help", StringComparison.OrdinalIgnoreCase)) ||
                (e.ElementType == CommandElementType.ArgumentAlias && e.Value.Equals("h", StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Checks if help flag is combined with other arguments (invalid - FR-018a).
        /// Returns true if invalid combination detected.
        /// </summary>
        public bool IsHelpCombinedWithOtherArgs(ResolvedCommand resolved)
        {
            var elements = resolved.ParsedCommand.Elements.ToList();
            
            var hasHelpFlag = HasHelpFlag(resolved);
            if (!hasHelpFlag) return false;

            // Check for other argument elements
            var otherArgs = elements.Where(e => 
                (e.ElementType == CommandElementType.ArgumentName && !e.Value.Equals("help", StringComparison.OrdinalIgnoreCase)) ||
                (e.ElementType == CommandElementType.ArgumentAlias && !e.Value.Equals("h", StringComparison.OrdinalIgnoreCase)) ||
                e.ElementType == CommandElementType.ArgumentValue).ToList();

            return otherArgs.Any();
        }

        /// <summary>
        /// Displays root help.
        /// </summary>
        public void DisplayRootHelp(TextWriter writer)
        {
            _formatter.DisplayRootHelp(writer, _registry);
        }

        /// <summary>
        /// Displays group help.
        /// </summary>
        public void DisplayGroupHelp(TextWriter writer, GroupInfo group)
        {
            _formatter.DisplayGroupHelp(writer, group, _registry);
        }

        /// <summary>
        /// Displays command help.
        /// </summary>
        public void DisplayCommandHelp(TextWriter writer, CommandInfo command)
        {
            _formatter.DisplayCommandHelp(writer, command);
        }
    }
}
