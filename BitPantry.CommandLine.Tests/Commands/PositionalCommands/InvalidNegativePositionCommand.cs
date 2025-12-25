using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// INVALID: Explicit negative position (should fail validation)
    /// </summary>
    class InvalidNegativePositionCommand : CommandBase
    {
        [Argument(Position = -2)]  // Explicit negative position (not -1 which is default for named)
        public string First { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
