using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.ActivateCommands
{
    class WithIntArg : CommandBase
    {
        [Argument]
        public int IntArg { get; set; }
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
