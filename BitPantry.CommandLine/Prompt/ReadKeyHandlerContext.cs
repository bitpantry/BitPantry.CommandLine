using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Prompt
{
    public class ReadKeyHandlerContext
    {
        public ConsoleLineMirror InputLine { get; }
        public ConsoleKeyInfo KeyInfo { get; }

        public ReadKeyHandlerContext(ConsoleLineMirror inputLine, ConsoleKeyInfo keyInfo)
        {
            InputLine = inputLine;
            KeyInfo = keyInfo;
        }

    }
}
