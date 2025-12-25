using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// Test command with IsRest positional argument to capture remaining values
    /// </summary>
    class IsRestCommand : CommandBase
    {
        [Argument(Position = 0, IsRest = true)]
        public string[] Files { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
