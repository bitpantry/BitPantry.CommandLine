using BitPantry.CommandLine.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Interface.Console
{
    public class ConsoleWriterCollection : IWriterCollection
    {
        public Writer Standard { get; private set; }
        public Writer Warning { get; private set; }
        public Writer Error { get; private set; }
        public Writer Debug { get; private set; }
        public Writer Verbose { get; private set; }

        public ConsoleWriterCollection()
        {
            var standardBackgroundColor = System.Console.BackgroundColor;
            var standardForecolor = System.Console.ForegroundColor;

            Standard = new ConsoleWriter(standardBackgroundColor, standardForecolor);
            Warning = new ConsoleWriter(standardBackgroundColor, ConsoleColor.Yellow);
            Error = new ConsoleWriter(standardBackgroundColor, ConsoleColor.Red);
            Debug = new ConsoleWriter(standardBackgroundColor, standardForecolor);
            Verbose = new ConsoleWriter(standardBackgroundColor, ConsoleColor.DarkGray);
        }
    }
}
