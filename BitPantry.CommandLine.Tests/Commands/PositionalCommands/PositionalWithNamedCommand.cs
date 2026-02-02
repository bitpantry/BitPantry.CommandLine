using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// Test command with both positional and named arguments
    /// </summary>
    class PositionalWithNamedCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string Source { get; set; }

        [Argument(Position = 1)]
        public string Destination { get; set; }

        [Argument]
        [Flag]
        public bool Force { get; set; }

        [Argument]
        public string Mode { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
