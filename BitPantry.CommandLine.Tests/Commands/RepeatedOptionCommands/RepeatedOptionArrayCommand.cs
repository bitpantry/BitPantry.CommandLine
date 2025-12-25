using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.RepeatedOptionCommands
{
    /// <summary>
    /// Test command with repeated option on an array type
    /// </summary>
    class RepeatedOptionArrayCommand : CommandBase
    {
        [Argument]
        public string[] Items { get; set; }

        [Argument]
        public bool Verbose { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
