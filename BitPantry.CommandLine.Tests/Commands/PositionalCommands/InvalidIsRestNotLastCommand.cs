using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// INVALID: IsRest is not the last positional argument (should fail validation)
    /// </summary>
    class InvalidIsRestNotLastCommand : CommandBase
    {
        [Argument(Position = 0, IsRest = true)]
        public string[] Files { get; set; }

        [Argument(Position = 1)]
        public string OutputDir { get; set; }  // After IsRest is invalid

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
