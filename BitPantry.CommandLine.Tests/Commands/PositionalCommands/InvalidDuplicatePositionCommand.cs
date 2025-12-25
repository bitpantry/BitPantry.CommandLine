using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// INVALID: Duplicate position indices (should fail validation)
    /// </summary>
    class InvalidDuplicatePositionCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string First { get; set; }

        [Argument(Position = 0)]  // Duplicate position
        public string AlsoFirst { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
