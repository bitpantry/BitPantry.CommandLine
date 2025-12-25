using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// Test command with multiple positional arguments at positions 0, 1, 2
    /// </summary>
    class MultiplePositionalCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string First { get; set; }

        [Argument(Position = 1)]
        public string Second { get; set; }

        [Argument(Position = 2)]
        public int Third { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
