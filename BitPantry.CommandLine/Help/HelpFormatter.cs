using BitPantry.CommandLine.Component;
using Spectre.Console;
using System.Linq;

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
        public void DisplayGroupHelp(IAnsiConsole console, GroupInfo group, CommandRegistry registry)
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

            // === ARGUMENTS SECTION (positional only) ===
            var positionalArgsList = command.Arguments.Where(a => a.IsPositional).OrderBy(a => a.Position).ToList();
            if (positionalArgsList.Any())
            {
                console.WriteLine("Arguments:");
                
                // Calculate column widths for alignment
                var maxPositionWidth = positionalArgsList.Max(a => $"[{a.Position}]".Length);
                var maxNameWidth = positionalArgsList.Max(a => FormatPositionalArgumentName(a).Length);
                var maxRequiredWidth = "(required)".Length;
                
                foreach (var arg in positionalArgsList)
                {
                    var positionCol = $"[{arg.Position}]".PadRight(maxPositionWidth);
                    var nameCol = FormatPositionalArgumentName(arg).PadRight(maxNameWidth);
                    var requiredCol = arg.IsRequired ? "(required)" : "";
                    requiredCol = requiredCol.PadRight(maxRequiredWidth);
                    var restNote = arg.IsRest ? " (variadic)" : "";
                    var namedHint = $"(or --{arg.Name})";
                    var desc = string.IsNullOrEmpty(arg.Description) ? "" : arg.Description;
                    
                    console.WriteLine($"  {positionCol} {nameCol} {requiredCol}{restNote}  {desc} {namedHint}");
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
                    
                    console.WriteLine($"  {optionCol} {requiredCol}{repeatNote}  {desc}");
                }
                console.WriteLine();
            }

            console.WriteLine();
        }

        /// <summary>
        /// Format a positional argument name for display in Arguments section.
        /// Positional args are shown without -- prefix since they're used by position.
        /// </summary>
        private string FormatPositionalArgumentName(ArgumentInfo arg)
        {
            return arg.Name;
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
            if (!arg.IsOption)
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
            if (arg.IsOption)
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
        public void DisplayRootHelp(IAnsiConsole console, CommandRegistry registry)
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
    }
}
