using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.ActivateCommands
{
    class MultipleArgs : CommandBase
    {
        [Argument]
        public int ArgOne { get; set; }

        [Argument]
        public string StrArg { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
