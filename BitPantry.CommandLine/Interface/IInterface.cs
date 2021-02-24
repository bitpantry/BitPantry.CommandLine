using BitPantry.CommandLine.Interface.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Interface
{
    public interface IInterface
    {
        IWriterCollection WriterCollection { get; }

        event ConsoleEvents.CancelExecutionEventHandler CancelExecutionEvent;

        string ReadLine(bool maskInput = false);
        char ReadKey();
        void Clear();
    }
}
