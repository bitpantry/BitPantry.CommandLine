using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Interface
{
    public class ConsoleEvents
    {
        public delegate void CancelExecutionEventHandler(object sender, ConsoleCancelEventArgs e);
    }
}
