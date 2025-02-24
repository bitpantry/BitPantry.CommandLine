using Spectre.Console;

namespace BitPantry.CommandLine.API
{
    public abstract class CommandBase
    {
        protected IAnsiConsole Console { get; private set; }

        internal void SetConsole(IAnsiConsole console)
        {
            Console = console;
        }
    }
}
