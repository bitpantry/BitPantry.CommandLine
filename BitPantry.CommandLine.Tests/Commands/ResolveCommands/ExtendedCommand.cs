using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{
    [Command]
    public class ExtendedCommand : AbstractedBaseCommand
    {

        protected override int OnExecute(CommandExecutionContext ctx)
        {
            return 42;
        }
    }
}
