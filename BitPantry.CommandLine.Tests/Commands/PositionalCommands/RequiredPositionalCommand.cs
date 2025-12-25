using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// Test command with required positional arguments
    /// </summary>
    class RequiredPositionalCommand : CommandBase
    {
        [Argument(Position = 0, IsRequired = true)]
        public string Source { get; set; }

        [Argument(Position = 1, IsRequired = true)]
        public string Destination { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
