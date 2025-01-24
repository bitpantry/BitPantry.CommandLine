using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine
{
    public class SystemConsoleServices : IConsoleService
    {
        public CursorPosition GetCursorPosition()
            => new(System.Console.CursorTop, System.Console.CursorLeft);
    }
}
