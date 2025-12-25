using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// INVALID: IsRest on a scalar type (should fail validation)
    /// </summary>
    class InvalidIsRestScalarCommand : CommandBase
    {
        [Argument(Position = 0, IsRest = true)]
        public string SingleFile { get; set; }  // Should be array/collection

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
