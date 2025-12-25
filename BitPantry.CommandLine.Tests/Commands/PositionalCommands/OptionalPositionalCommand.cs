using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    /// <summary>
    /// Test command with optional positional arguments
    /// </summary>
    class OptionalPositionalCommand : CommandBase
    {
        [Argument(Position = 0, IsRequired = true)]
        public string Name { get; set; }

        [Argument(Position = 1)]
        public string Title { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
