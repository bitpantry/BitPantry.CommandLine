using BitPantry.CommandLine.API;

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
