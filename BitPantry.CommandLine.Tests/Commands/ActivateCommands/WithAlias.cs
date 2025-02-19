using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.ActivateCommands
{
    class WithAlias : CommandBase
    {
        [Argument]
        [Alias('a')]
        public int ArgOne { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
