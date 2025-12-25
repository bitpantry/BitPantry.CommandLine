using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// INVALID: Gap in position indices (0, 2 - missing 1, should fail validation)
    /// </summary>
    class InvalidGapPositionCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string First { get; set; }

        [Argument(Position = 2)]  // Gap - missing Position = 1
        public string Third { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
