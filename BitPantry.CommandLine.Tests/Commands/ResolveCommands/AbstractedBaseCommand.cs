using BitPantry.CommandLine.API;

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
