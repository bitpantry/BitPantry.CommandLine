using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine
{
    public class CommandExecutionException : Exception
    {
        public CommandExecutionException(string message) : base(message) { }
        public CommandExecutionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
