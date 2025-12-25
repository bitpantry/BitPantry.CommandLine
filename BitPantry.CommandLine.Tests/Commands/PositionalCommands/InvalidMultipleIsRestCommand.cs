using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// INVALID: Multiple IsRest arguments (should fail validation)
    /// </summary>
    class InvalidMultipleIsRestCommand : CommandBase
    {
        [Argument(Position = 0, IsRest = true)]
        public string[] FirstRest { get; set; }

        [Argument(Position = 1, IsRest = true)]
        public string[] SecondRest { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
