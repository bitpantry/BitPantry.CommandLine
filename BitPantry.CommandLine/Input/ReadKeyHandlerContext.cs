using System;

namespace BitPantry.CommandLine.Input
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
