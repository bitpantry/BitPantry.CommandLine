using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// INVALID: IsRest on a named argument (Position not set, should fail validation)
    /// </summary>
    class InvalidIsRestNotPositionalCommand : CommandBase
    {
        [Argument(IsRest = true)]  // Missing Position >= 0
        public string[] Files { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
