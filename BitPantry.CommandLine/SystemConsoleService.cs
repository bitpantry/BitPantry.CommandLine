using System;
using System.Text;

namespace BitPantry.CommandLine
{
    public class SystemConsoleService : IConsoleService
    {
        public CursorPosition GetCursorPosition()
            => new(Console.CursorTop, Console.CursorLeft);

    }
}
