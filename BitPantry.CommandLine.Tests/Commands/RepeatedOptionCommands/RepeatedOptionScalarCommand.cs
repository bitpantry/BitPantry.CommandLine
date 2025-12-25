using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.RepeatedOptionCommands
{
    /// <summary>
    /// Test command with scalar (non-collection) argument - repeated use should error
    /// </summary>
    class RepeatedOptionScalarCommand : CommandBase
    {
        [Argument]
        public string Value { get; set; }

        [Argument]
        public int Count { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
