using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{
    public abstract class AbstractedBaseCommand : CommandBase
    {
        public int Execute(CommandExecutionContext ctx)
        {
            return OnExecute(ctx);
        }

        protected abstract int OnExecute(CommandExecutionContext ctx);
    }
}
