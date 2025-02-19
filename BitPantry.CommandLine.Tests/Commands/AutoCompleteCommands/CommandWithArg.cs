using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{

    [Command]
    public class CommandWithArg : CommandBase
    {
        [Argument]
        [Alias('a')]
        public string Arg1 { get; set; }

        public void Execute(CommandExecutionContext context) { }
    }
}
