using BitPantry.CommandLine.Component;
using Spectre.Console;

namespace BitPantry.CommandLine.Help
{
    /// <summary>
    /// Interface for formatting and displaying help information.
    /// Output supports Spectre.Console markup.
    /// </summary>
    public interface IHelpFormatter
    {
        /// <summary>
        /// Display help for a specific group, showing its subgroups and commands.
        /// </summary>
        /// <param name="console">The Spectre.Console instance for rich output.</param>
        /// <param name="group">The group to display help for.</param>
        /// <param name="registry">The command registry containing all groups and commands.</param>
        void DisplayGroupHelp(IAnsiConsole console, GroupInfo group, CommandRegistry registry);

        /// <summary>
        /// Display help for a specific command, showing its usage and arguments.
        /// </summary>
        /// <param name="console">The Spectre.Console instance for rich output.</param>
        /// <param name="command">The command to display help for.</param>
        void DisplayCommandHelp(IAnsiConsole console, CommandInfo command);

        /// <summary>
        /// Display root-level help, showing all top-level groups and commands.
        /// </summary>
        /// <param name="console">The Spectre.Console instance for rich output.</param>
        /// <param name="registry">The command registry containing all groups and commands.</param>
        void DisplayRootHelp(IAnsiConsole console, CommandRegistry registry);
    }
}
