using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// Test command with positional arguments before IsRest
    /// </summary>
    class IsRestWithPrecedingCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string Target { get; set; }

        [Argument(Position = 1, IsRest = true)]
        public string[] Sources { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
