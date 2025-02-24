using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    [Command]
    public class CommandWithTwoArgs : CommandBase
    {
        [Argument]
        [Alias('a')]
        public string Arg1 { get; set; }

        [Argument]
        [Alias('x')]
        public string XyzQp { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }
}
