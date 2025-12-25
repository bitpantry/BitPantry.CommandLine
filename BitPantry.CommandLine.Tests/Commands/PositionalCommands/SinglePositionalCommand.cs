using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// Test command with a single positional argument at position 0
    /// </summary>
    class SinglePositionalCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string Source { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
