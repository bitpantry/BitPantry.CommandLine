using Spectre.Console;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
