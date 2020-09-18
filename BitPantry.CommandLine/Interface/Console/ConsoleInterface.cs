using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Interface.Console
{
    public class ConsoleInterface : IInterface
    {
        public IWriterCollection WriterCollection => new ConsoleWriterCollection();

        public string ReadLine(bool maskInput = false)
        {
            if (!maskInput)
                return System.Console.ReadLine();

            return MaskedInput.Get();
        }

        public char ReadKey()
        {
            return System.Console.ReadKey().KeyChar;
        }

        public void Clear() { System.Console.Clear(); }
    }
}
