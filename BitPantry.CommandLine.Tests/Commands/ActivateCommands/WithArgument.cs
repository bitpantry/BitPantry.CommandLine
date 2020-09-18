using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ActivateCommands
{
    class WithArgument : CommandBase
    {
        [Argument]
        public int ArgOne { get; set; }

        public int Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
