using BitPantry.CommandLine.Interface.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Interface
{
    public interface IInterface
    {
        /// <summary>
        /// Returns an IWriterCollection belonging to the interface
        /// </summary>
        IWriterCollection WriterCollection { get; }  

        /// <summary>
        /// Raising this event attempts to cancel the currently focused process execution
        /// </summary>
        event ConsoleEvents.CancelExecutionEventHandler CancelExecutionEvent;

        /// <summary>
        /// Reads a line of input from the interface
        /// </summary>
        /// <param name="maskInput">Whether or not the interface should mask the input as entered</param>
        /// <returns>The line read from the interface</returns>
        string ReadLine(bool maskInput = false);

        /// <summary>
        /// Reads a character from the interface
        /// </summary>
        /// <returns>The character read from the interface</returns>
        char ReadKey();

        /// <summary>
        /// Clears the interface
        /// </summary>
        void Clear();
    }
}
