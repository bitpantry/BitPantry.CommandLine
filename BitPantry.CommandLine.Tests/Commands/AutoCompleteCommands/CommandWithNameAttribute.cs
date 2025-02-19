using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    [Command(Name = "myCommand")]
    class CommandWithNameAttribute : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
