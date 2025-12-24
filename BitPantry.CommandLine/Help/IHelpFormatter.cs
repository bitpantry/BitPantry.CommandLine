using BitPantry.CommandLine.Component;
using System.IO;

namespace BitPantry.CommandLine.Help
{
    /// <summary>
    /// Interface for formatting and displaying help information.
    /// </summary>
    public interface IHelpFormatter
    {
        /// <summary>
        /// Display help for a specific group, showing its subgroups and commands.
        /// </summary>
        /// <param name="writer">The text writer to output help text to.</param>
        /// <param name="group">The group to display help for.</param>
        /// <param name="registry">The command registry containing all groups and commands.</param>
        void DisplayGroupHelp(TextWriter writer, GroupInfo group, CommandRegistry registry);

        /// <summary>
        /// Display help for a specific command, showing its usage and arguments.
        /// </summary>
        /// <param name="writer">The text writer to output help text to.</param>
        /// <param name="command">The command to display help for.</param>
        void DisplayCommandHelp(TextWriter writer, CommandInfo command);

        /// <summary>
        /// Display root-level help, showing all top-level groups and commands.
        /// </summary>
        /// <param name="writer">The text writer to output help text to.</param>
        /// <param name="registry">The command registry containing all groups and commands.</param>
        void DisplayRootHelp(TextWriter writer, CommandRegistry registry);
    }
}
