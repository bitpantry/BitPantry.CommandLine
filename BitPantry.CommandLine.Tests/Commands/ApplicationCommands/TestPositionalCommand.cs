using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    /// <summary>
    /// Test command for positional argument integration testing
    /// </summary>
    public class TestPositionalCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string Source { get; set; }

        [Argument(Position = 1)]
        public string Destination { get; set; }

        /// <summary>
        /// Returns a concatenation of positional arguments
        /// </summary>
        public string Execute(CommandExecutionContext ctx)
        {
            return $"{Source}|{Destination}";
        }
    }
}
