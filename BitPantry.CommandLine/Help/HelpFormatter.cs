using BitPantry.CommandLine.Component;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitPantry.CommandLine.Help
{
    /// <summary>
    /// Default implementation of IHelpFormatter using Spectre.Console markup for enhanced readability.
    /// </summary>
    public class HelpFormatter : IHelpFormatter
    {
        /// <summary>
        /// Display help for a specific group, showing its subgroups and commands.
        /// </summary>
        public void DisplayGroupHelp(IAnsiConsole console, GroupInfo group, ICommandRegistry registry)
        {
            console.WriteLine();
            console.WriteLine($"Group: {group.FullPath}");

            if (!string.IsNullOrEmpty(group.Description))
            {
                console.WriteLine($"  {group.Description}");
            }

            console.WriteLine();

            // Show subgroups
            var subgroups = registry.Groups.Where(g => g.Parent?.Name == group.Name).ToList();
            if (subgroups.Any())
            {
                console.WriteLine("Subgroups:");

                // Calculate max width for alignment
                var maxSubgroupWidth = subgroups.Max(g => g.Name.Length);

                foreach (var subgroup in subgroups.OrderBy(g => g.Name))
                {
                    var nameCol = subgroup.Name.PadRight(maxSubgroupWidth);
                    var desc = string.IsNullOrEmpty(subgroup.Description) ? "" : $"  {subgroup.Description}";
                    console.WriteLine($"  {nameCol}{desc}");
                }
                console.WriteLine();
            }

            // Show commands in this group
            var commands = registry.Commands.Where(c => c.Group?.Name == group.Name).ToList();
            if (commands.Any())
            {
                console.WriteLine("Commands:");

                // Calculate max width for alignment
                var maxCommandWidth = commands.Max(c => c.Name.Length);

                foreach (var cmd in commands.OrderBy(c => c.Name))
                {
                    var nameCol = cmd.Name.PadRight(maxCommandWidth);
                    var desc = string.IsNullOrEmpty(cmd.Description) ? "" : $"  {cmd.Description}";
                    console.WriteLine($"  {nameCol}{desc}");
                }
                console.WriteLine();
            }
            else if (!subgroups.Any())
            {
                console.WriteLine("  No commands available in this group.");
                console.WriteLine();
            }

            // Show usage hint
            console.WriteLine($"Usage: {group.FullPath} <command> [options]");
            console.WriteLine();
            console.WriteLine($"Run '{group.FullPath} <command> --help' for more information on a command.");
            console.WriteLine();
        }

        /// <summary>
        /// Display help for a specific command, showing its usage and arguments.
        /// </summary>
        public void DisplayCommandHelp(IAnsiConsole console, CommandInfo command)
        {
            console.WriteLine();

            // Build the full command path including the complete group hierarchy
            var groupPath = command.Group != null ? $"{command.Group.FullPath} " : "";
            var fullPath = $"{groupPath}{command.Name}";

            // === DESCRIPTION SECTION ===
            console.WriteLine("Description:");
            if (!string.IsNullOrEmpty(command.Description))
            {
                console.WriteLine($"  {command.Description}");
            }
            else
            {
                console.WriteLine("  (no description)");
            }
            console.WriteLine();

            // === USAGE SECTION ===
            console.WriteLine("Usage:");
            var positionalUsage = command.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .Select(a => FormatPositionalUsage(a));

            var namedUsage = command.Arguments
                .Where(a => !a.IsPositional)
                .OrderBy(a => a.Name)
                .Select(a => FormatNamedUsage(a));

            var usageArgs = string.Join(" ", positionalUsage.Concat(namedUsage));
            console.WriteLine($"  {fullPath} {usageArgs}");
            console.WriteLine();

            // Get console width for text wrapping (default to 120 if not available)
            var consoleWidth = console.Profile.Width > 0 ? console.Profile.Width : 120;

            // === ARGUMENTS SECTION (positional only) ===
            var positionalArgsList = command.Arguments.Where(a => a.IsPositional).OrderBy(a => a.Position).ToList();
            if (positionalArgsList.Any())
            {
                console.WriteLine("Arguments:");

                // Calculate column widths for alignment
                var maxNameWidth = positionalArgsList.Max(a => FormatPositionalArgumentName(a).Length);
                var maxRequiredWidth = "(required)".Length;

                foreach (var arg in positionalArgsList)
                {
                    var nameCol = FormatPositionalArgumentName(arg).PadRight(maxNameWidth);
                    var requiredCol = arg.IsRequired ? "(required)" : "";
                    requiredCol = requiredCol.PadRight(maxRequiredWidth);
                    var restNote = arg.IsRest ? " (variadic)" : "";
                    var namedHint = FormatPositionalNamedHint(arg);
                    var desc = string.IsNullOrEmpty(arg.Description) ? "" : $"{arg.Description} ";

                    // Build the prefix (everything before the description)
                    var prefix = $"  {nameCol} {requiredCol}{restNote}  ";
                    var descWithHint = $"{desc}{namedHint}";

                    WriteWithWrappedDescription(console, prefix, descWithHint, consoleWidth);
                }
                console.WriteLine();
            }

            // === OPTIONS SECTION (named arguments only) ===
            // Sort required arguments first, then alphabetically within each group
            var namedArgsList = command.Arguments
                .Where(a => !a.IsPositional)
                .OrderByDescending(a => a.IsRequired)
                .ThenBy(a => a.Name)
                .ToList();
            if (namedArgsList.Any())
            {
                console.WriteLine("Options:");

                // Calculate column widths for alignment
                var maxOptionWidth = namedArgsList.Max(a => FormatOptionName(a).Length);
                var maxRequiredOptWidth = namedArgsList.Any(a => a.IsRequired) ? "(required)".Length : 0;

                foreach (var arg in namedArgsList)
                {
                    var optionCol = FormatOptionName(arg).PadRight(maxOptionWidth);
                    var requiredCol = arg.IsRequired ? "(required)" : "";
                    requiredCol = requiredCol.PadRight(maxRequiredOptWidth);
                    var repeatNote = arg.IsCollection ? " (repeatable)" : "";
                    var desc = string.IsNullOrEmpty(arg.Description) ? "" : arg.Description;

                    // Build the prefix (everything before the description)
                    var prefix = $"  {optionCol} {requiredCol}{repeatNote}  ";

                    WriteWithWrappedDescription(console, prefix, desc, consoleWidth);
                }
                console.WriteLine();
            }
        }

        /// <summary>
        /// Format a positional argument name for display in Arguments section.
        /// Positional args are shown without -- prefix since they're used by position.
        /// Includes alias if available.
        /// </summary>
        private string FormatPositionalArgumentName(ArgumentInfo arg)
        {
            if (arg.Alias != default(char))
            {
                return $"{arg.Name}, -{arg.Alias}";
            }
            return arg.Name;
        }

        /// <summary>
        /// Format the "(or --Name)" hint for positional arguments, including alias if present.
        /// </summary>
        private string FormatPositionalNamedHint(ArgumentInfo arg)
        {
            if (arg.Alias != default(char))
            {
                return $"(or --{arg.Name}, -{arg.Alias})";
            }
            return $"(or --{arg.Name})";
        }

        /// <summary>
        /// Checks if an argument is a boolean flag (doesn't take a value).
        /// This includes bool, bool?, and the Option type.
        /// </summary>
        private bool IsFlag(ArgumentInfo arg)
        {
            if (arg.PropertyInfo?.PropertyTypeName == null)
                return false;

            var type = Type.GetType(arg.PropertyInfo.PropertyTypeName);
            if (type == null)
                return false;
                
            return type == typeof(bool) || type == typeof(bool?) || type == typeof(API.Option);
        }

        /// <summary>
        /// Format an option name with alias for display in Options section.
        /// Shows <value> placeholder for options that take values (non-flags).
        /// </summary>
        private string FormatOptionName(ArgumentInfo arg)
        {
            var name = $"--{arg.Name}";
            if (arg.Alias != default(char))
            {
                name += $", -{arg.Alias}";
            }
            // Add <value> indicator for non-flag options
            if (!IsFlag(arg))
            {
                name += " <value>";
            }
            return name;
        }

        /// <summary>
        /// Format a named argument for the usage synopsis.
        /// Required options use angle brackets, optional use square brackets.
        /// Option types (flags) don't show &lt;value&gt;.
        /// </summary>
        private string FormatNamedUsage(ArgumentInfo arg)
        {
            if (IsFlag(arg))
            {
                // Flags: [--name] or --name (if required)
                return arg.IsRequired ? $"--{arg.Name}" : $"[--{arg.Name}]";
            }
            else
            {
                // Value args: [--name <value>] or --name <value> (if required)
                return arg.IsRequired ? $"--{arg.Name} <value>" : $"[--{arg.Name} <value>]";
            }
        }

        /// <summary>
        /// Format a positional argument for the usage synopsis.
        /// Required: &lt;name&gt;, Optional: [name], Variadic: &lt;name&gt;...
        /// </summary>
        private string FormatPositionalUsage(ArgumentInfo arg)
        {
            var name = arg.Name;
            var suffix = arg.IsRest ? "..." : "";

            if (arg.IsRequired)
            {
                return $"<{name}>{suffix}";
            }
            else
            {
                return $"[{name}]{suffix}";
            }
        }

        /// <summary>
        /// Display root-level help, showing all top-level groups and commands.
        /// </summary>
        public void DisplayRootHelp(IAnsiConsole console, ICommandRegistry registry)
        {
            console.WriteLine();
            console.WriteLine("Available commands and groups:");
            console.WriteLine();

            // Show root-level groups
            var rootGroups = registry.RootGroups.ToList();
            if (rootGroups.Any())
            {
                console.WriteLine("Groups:");
                foreach (var group in rootGroups.OrderBy(g => g.Name))
                {
                    var desc = string.IsNullOrEmpty(group.Description) ? "" : $"  {group.Description}";
                    console.WriteLine($"  {group.Name}{desc}");
                }
                console.WriteLine();
            }

            // Show root-level commands
            var rootCommands = registry.RootCommands.ToList();
            if (rootCommands.Any())
            {
                console.WriteLine("Commands:");
                foreach (var cmd in rootCommands.OrderBy(c => c.Name))
                {
                    var desc = string.IsNullOrEmpty(cmd.Description) ? "" : $"  {cmd.Description}";
                    console.WriteLine($"  {cmd.Name}{desc}");
                }
                console.WriteLine();
            }

            if (!rootGroups.Any() && !rootCommands.Any())
            {
                console.WriteLine("  No commands or groups registered.");
                console.WriteLine();
            }

            console.WriteLine("Run '<command> --help' for more information on a command.");
            console.WriteLine("Run '<group>' to see commands in a group.");
            console.WriteLine();
        }

        /// <summary>
        /// Wraps text to fit within a maximum width, preserving word boundaries.
        /// Returns a list of lines.
        /// </summary>
        private List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
            {
                lines.Add(text ?? "");
                return lines;
            }

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length == 0)
                {
                    currentLine.Append(word);
                }
                else if (currentLine.Length + 1 + word.Length <= maxWidth)
                {
                    currentLine.Append(' ');
                    currentLine.Append(word);
                }
                else
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    currentLine.Append(word);
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }

            return lines.Count > 0 ? lines : new List<string> { "" };
        }

        /// <summary>
        /// Writes a line with a description that may wrap, maintaining indentation for wrapped lines.
        /// </summary>
        private void WriteWithWrappedDescription(IAnsiConsole console, string prefix, string description, int consoleWidth)
        {
            // Calculate available width for description
            var prefixLength = prefix.Length;
            var availableWidth = consoleWidth - prefixLength - 1; // -1 for safety margin

            if (availableWidth < 20)
            {
                // If not enough room, just print without wrapping
                console.WriteLine($"{prefix}{description}");
                return;
            }

            var wrappedLines = WrapText(description, availableWidth);

            // Print first line with the prefix
            console.WriteLine($"{prefix}{wrappedLines[0]}");

            // Print continuation lines with indentation matching the prefix
            var indent = new string(' ', prefixLength);
            for (int i = 1; i < wrappedLines.Count; i++)
            {
                console.WriteLine($"{indent}{wrappedLines[i]}");
            }
        }
    }
}
