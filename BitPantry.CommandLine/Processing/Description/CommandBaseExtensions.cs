using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;

namespace BitPantry.CommandLine.Processing.Description
{
    /// <summary>
    /// Extends the CommandBase class 
    /// </summary>
    public static class CommandBaseExtensions
    {
        /// <summary>
        /// Describes a command object
        /// </summary>
        /// <param name="command">The object to describe</param>
        /// <returns>The corresponding CommandInfo</returns>
        public static CommandInfo DescribeCommand(this CommandBase command)
        {
            return CommandReflection.Describe(command.GetType());
        }
    }
}
