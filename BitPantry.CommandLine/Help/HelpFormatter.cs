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
            console.MarkupLine($"[bold cyan]Group:[/] [yellow]{Markup.Escape(group.FullPath)}[/]");
            
            if (!string.IsNullOrEmpty(group.Description))
            {
                console.MarkupLine($"  [dim]{Markup.Escape(group.Description)}[/]");
            }
            
            console.WriteLine();

            // Show subgroups
            var subgroups = registry.Groups.Where(g => g.Parent?.Name == group.Name).ToList();
            if (subgroups.Any())
            {
                console.MarkupLine("[bold]Subgroups:[/]");
                
                // Calculate max width for alignment
                var maxSubgroupWidth = subgroups.Max(g => g.Name.Length);
                
                foreach (var subgroup in subgroups.OrderBy(g => g.Name))
                {
                    var nameCol = subgroup.Name.PadRight(maxSubgroupWidth);
                    var desc = string.IsNullOrEmpty(subgroup.Description) ? "" : $"  [dim]{Markup.Escape(subgroup.Description)}[/]";
                    console.MarkupLine($"  [green]{Markup.Escape(nameCol)}[/]{desc}");
                }
                console.WriteLine();
            }

            // Show commands in this group
            var commands = registry.Commands.Where(c => c.Group?.Name == group.Name).ToList();
            if (commands.Any())
            {
                console.MarkupLine("[bold]Commands:[/]");
                
                // Calculate max width for alignment
                var maxCommandWidth = commands.Max(c => c.Name.Length);
                
                foreach (var cmd in commands.OrderBy(c => c.Name))
                {
                    var nameCol = cmd.Name.PadRight(maxCommandWidth);
                    var desc = string.IsNullOrEmpty(cmd.Description) ? "" : $"  [dim]{Markup.Escape(cmd.Description)}[/]";
                    console.MarkupLine($"  [green]{Markup.Escape(nameCol)}[/]{desc}");
                }
                console.WriteLine();
            }
            else if (!subgroups.Any())
            {
                console.MarkupLine("  [dim]No commands available in this group.[/]");
                console.WriteLine();
            }

            // Show usage hint
            console.MarkupLine($"[bold]Usage:[/] [yellow]{Markup.Escape(group.FullPath)}[/] [grey]<command> [[options]][/]");
            console.WriteLine();
            console.MarkupLine($"[dim]Run '[/][yellow]{Markup.Escape(group.FullPath)} <command> --help[/][dim]' for more information on a command.[/]");
        }

        /// <summary>
        /// Display help for a specific command, showing its usage and arguments.
        /// </summary>
        public void DisplayCommandHelp(IAnsiConsole console, CommandInfo command)
        {
            // Build the full command path including the complete group hierarchy
            var groupPath = command.Group != null ? $"{command.Group.FullPath} " : "";
            var fullPath = $"{groupPath}{command.Name}";

            // === DESCRIPTION SECTION ===
            console.MarkupLine("[bold]Description:[/]");
            if (!string.IsNullOrEmpty(command.Description))
            {
                console.MarkupLine($"  [dim]{Markup.Escape(command.Description)}[/]");
            }
            else
            {
                console.MarkupLine("  [dim](no description)[/]");
            }
            console.WriteLine();

            // === USAGE SECTION ===
            console.MarkupLine("[bold]Usage:[/]");
            var positionalUsage = command.Arguments
                .Where(a => a.IsPositional)
                .OrderBy(a => a.Position)
                .Select(a => FormatPositionalUsage(a));
            
            var namedUsage = command.Arguments
                .Where(a => !a.IsPositional)
                .OrderBy(a => a.Name)
                .Select(a => FormatNamedUsage(a));
            
            var usageArgs = string.Join(" ", positionalUsage.Concat(namedUsage));
            console.MarkupLine($"  [yellow]{Markup.Escape(fullPath)}[/] [grey]{usageArgs}[/]");
            console.WriteLine();

            // === ARGUMENTS SECTION (positional only) ===
            var positionalArgsList = command.Arguments.Where(a => a.IsPositional).OrderBy(a => a.Position).ToList();
            if (positionalArgsList.Any())
            {
                console.MarkupLine("[bold]Arguments:[/]");
                
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
                    
                    // Note: positionCol contains [n] which must be escaped for Spectre.Console
                    if (arg.IsRequired)
                    {
                        console.MarkupLine($"  [dim]{Markup.Escape(positionCol)}[/] [green]{Markup.Escape(nameCol)}[/] [red]{requiredCol}[/]{restNote}  [dim]{Markup.Escape(desc)}[/] [dim]{namedHint}[/]");
                    }
                    else
                    {
                        console.MarkupLine($"  [dim]{Markup.Escape(positionCol)}[/] [green]{Markup.Escape(nameCol)}[/] {requiredCol}{restNote}  [dim]{Markup.Escape(desc)}[/] [dim]{namedHint}[/]");
                    }
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
                console.MarkupLine("[bold]Options:[/]");
                
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
                    
                    if (arg.IsRequired)
                    {
                        console.MarkupLine($"  [green]{Markup.Escape(optionCol)}[/] [red]{requiredCol}[/]{repeatNote}  [dim]{Markup.Escape(desc)}[/]");
                    }
                    else
                    {
                        console.MarkupLine($"  [green]{Markup.Escape(optionCol)}[/] {requiredCol}{repeatNote}  [dim]{Markup.Escape(desc)}[/]");
                    }
                }
                console.WriteLine();
            }
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
                return arg.IsRequired ? $"--{arg.Name}" : $"[[--{arg.Name}]]";
            }
            else
            {
                // Value args: [--name <value>] or --name <value> (if required)
                return arg.IsRequired ? $"--{arg.Name} <value>" : $"[[--{arg.Name} <value>]]";
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
                return $"[[{name}]]{suffix}";
            }
        }

        /// <summary>
        /// Display root-level help, showing all top-level groups and commands.
        /// </summary>
        public void DisplayRootHelp(IAnsiConsole console, CommandRegistry registry)
        {
            console.MarkupLine("[bold cyan]Available commands and groups:[/]");
            console.WriteLine();

            // Show root-level groups
            var rootGroups = registry.RootGroups.ToList();
            if (rootGroups.Any())
            {
                console.MarkupLine("[bold]Groups:[/]");
                foreach (var group in rootGroups.OrderBy(g => g.Name))
                {
                    var desc = string.IsNullOrEmpty(group.Description) ? "" : $"  [dim]{Markup.Escape(group.Description)}[/]";
                    console.MarkupLine($"  [yellow]{Markup.Escape(group.Name)}[/]{desc}");
                }
                console.WriteLine();
            }

            // Show root-level commands
            var rootCommands = registry.RootCommands.ToList();
            if (rootCommands.Any())
            {
                console.MarkupLine("[bold]Commands:[/]");
                foreach (var cmd in rootCommands.OrderBy(c => c.Name))
                {
                    var desc = string.IsNullOrEmpty(cmd.Description) ? "" : $"  [dim]{Markup.Escape(cmd.Description)}[/]";
                    console.MarkupLine($"  [green]{Markup.Escape(cmd.Name)}[/]{desc}");
                }
                console.WriteLine();
            }

            if (!rootGroups.Any() && !rootCommands.Any())
            {
                console.MarkupLine("  [dim]No commands or groups registered.[/]");
                console.WriteLine();
            }

            console.MarkupLine("[dim]Run '[/][yellow]<command> --help[/][dim]' for more information on a command.[/]");
            console.MarkupLine("[dim]Run '[/][yellow]<group>[/][dim]' to see commands in a group.[/]");
        }
    }
}
