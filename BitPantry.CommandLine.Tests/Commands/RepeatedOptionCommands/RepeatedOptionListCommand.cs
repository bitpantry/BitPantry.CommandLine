using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Commands.RepeatedOptionCommands
{
    /// <summary>
    /// Test command with repeated option on a List type
    /// </summary>
    class RepeatedOptionListCommand : CommandBase
    {
        [Argument]
        public List<string> Tags { get; set; }

        [Argument]
        public string Name { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
