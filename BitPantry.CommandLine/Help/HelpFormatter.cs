using BitPantry.CommandLine.Component;
using System.IO;
using System.Linq;

namespace BitPantry.CommandLine.Help
{
    /// <summary>
    /// Default implementation of IHelpFormatter using plain text output.
    /// </summary>
    public class HelpFormatter : IHelpFormatter
    {
        /// <summary>
        /// Display help for a specific group, showing its subgroups and commands.
        /// </summary>
        public void DisplayGroupHelp(TextWriter writer, GroupInfo group, CommandRegistry registry)
        {
            writer.WriteLine();
            writer.WriteLine($"Group: {group.Name}");
            
            if (!string.IsNullOrEmpty(group.Description))
            {
                writer.WriteLine($"  {group.Description}");
            }
            
            writer.WriteLine();

            // Show subgroups
            var subgroups = registry.Groups.Where(g => g.Parent?.Name == group.Name).ToList();
            if (subgroups.Any())
            {
                writer.WriteLine("Subgroups:");
                foreach (var subgroup in subgroups.OrderBy(g => g.Name))
                {
                    var desc = string.IsNullOrEmpty(subgroup.Description) ? "" : $"  {subgroup.Description}";
                    writer.WriteLine($"  {subgroup.Name}{desc}");
                }
                writer.WriteLine();
            }

            // Show commands in this group
            var commands = registry.Commands.Where(c => c.Group?.Name == group.Name).ToList();
            if (commands.Any())
            {
                writer.WriteLine("Commands:");
                foreach (var cmd in commands.OrderBy(c => c.Name))
                {
                    var desc = string.IsNullOrEmpty(cmd.Description) ? "" : $"  {cmd.Description}";
                    writer.WriteLine($"  {cmd.Name}{desc}");
                }
                writer.WriteLine();
            }
            else if (!subgroups.Any())
            {
                writer.WriteLine("  No commands available in this group.");
                writer.WriteLine();
            }

            // Show usage hint
            writer.WriteLine($"Usage: {group.Name} <command> [options]");
            writer.WriteLine();
            writer.WriteLine($"Run '{group.Name} <command> --help' for more information on a command.");
        }

        /// <summary>
        /// Display help for a specific command, showing its usage and arguments.
        /// </summary>
        public void DisplayCommandHelp(TextWriter writer, CommandInfo command)
        {
            // Build the full command path
            var groupPath = command.Group != null ? $"{command.Group.Name} " : "";
            var fullPath = $"{groupPath}{command.Name}";

            writer.WriteLine();
            writer.WriteLine($"Command: {fullPath}");
            
            if (!string.IsNullOrEmpty(command.Description))
            {
                writer.WriteLine($"  {command.Description}");
            }
            
            writer.WriteLine();

            // Build usage string - positional args first (ordered by position), then named args
            var positionalArgs = command.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .Select(a => FormatPositionalUsage(a));
            
            var namedArgs = command.Arguments
                .Where(a => !a.IsPositional)
                .OrderBy(a => a.Name)
                .Select(a => $"[--{a.Name} <value>]");
            
            var usageArgs = string.Join(" ", positionalArgs.Concat(namedArgs));
            writer.WriteLine($"Usage: {fullPath} {usageArgs}".TrimEnd());
            writer.WriteLine();

            // Show positional arguments first
            var positionalArgsList = command.Arguments.Where(a => a.IsPositional).OrderBy(a => a.Position).ToList();
            if (positionalArgsList.Any())
            {
                writer.WriteLine("Positional Arguments:");
                foreach (var arg in positionalArgsList)
                {
                    var restNote = arg.IsRest ? " (variadic)" : "";
                    var optNote = !arg.IsRequired ? " (optional)" : "";
                    var desc = string.IsNullOrEmpty(arg.Description) ? "" : $"  {arg.Description}";
                    writer.WriteLine($"  {arg.Name}{restNote}{optNote}{desc}");
                }
                writer.WriteLine();
            }

            // Show named arguments
            var namedArgsList = command.Arguments.Where(a => !a.IsPositional).OrderBy(a => a.Name).ToList();
            if (namedArgsList.Any())
            {
                writer.WriteLine("Options:");
                foreach (var arg in namedArgsList)
                {
                    var alias = arg.Alias != default(char) ? $", -{arg.Alias}" : "";
                    var repeatNote = arg.IsCollection ? " (can be repeated)" : "";
                    var desc = string.IsNullOrEmpty(arg.Description) ? "" : $"  {arg.Description}";
                    writer.WriteLine($"  --{arg.Name}{alias}{repeatNote}{desc}");
                }
                writer.WriteLine();
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
        public void DisplayRootHelp(TextWriter writer, CommandRegistry registry)
        {
            writer.WriteLine();
            writer.WriteLine("Available commands and groups:");
            writer.WriteLine();

            // Show root-level groups
            var rootGroups = registry.RootGroups.ToList();
            if (rootGroups.Any())
            {
                writer.WriteLine("Groups:");
                foreach (var group in rootGroups.OrderBy(g => g.Name))
                {
                    var desc = string.IsNullOrEmpty(group.Description) ? "" : $"  {group.Description}";
                    writer.WriteLine($"  {group.Name}{desc}");
                }
                writer.WriteLine();
            }

            // Show root-level commands
            var rootCommands = registry.RootCommands.ToList();
            if (rootCommands.Any())
            {
                writer.WriteLine("Commands:");
                foreach (var cmd in rootCommands.OrderBy(c => c.Name))
                {
                    var desc = string.IsNullOrEmpty(cmd.Description) ? "" : $"  {cmd.Description}";
                    writer.WriteLine($"  {cmd.Name}{desc}");
                }
                writer.WriteLine();
            }

            if (!rootGroups.Any() && !rootCommands.Any())
            {
                writer.WriteLine("  No commands or groups registered.");
                writer.WriteLine();
            }

            writer.WriteLine("Run '<command> --help' for more information on a command.");
            writer.WriteLine("Run '<group>' to see commands in a group.");
        }
    }
}
