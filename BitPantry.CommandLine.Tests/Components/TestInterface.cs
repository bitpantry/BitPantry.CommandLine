using BitPantry.CommandLine.Interface;
using BitPantry.CommandLine.Interface.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Components
{
    public class TestInterface : IInterface
    {
        public IWriterCollection WriterCollection => new ConsoleWriterCollection();

        public event ConsoleEvents.CancelExecutionEventHandler CancelExecutionEvent;

        public void Clear()
        {
            /* do nothing */
        }

        public char ReadKey()
        {
            return 'a';
        }

        public string ReadLine(bool maskInput = false)
        {
            return "abcdefg";
        }

        public void CancelExecution()
        {
            CancelExecutionEvent?.Invoke();
        }
    }
}
