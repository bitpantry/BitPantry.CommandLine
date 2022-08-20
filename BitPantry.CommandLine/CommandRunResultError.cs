using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine
{

    public enum CommandRunResultErrorType : int
    {
        None = 0,
        InputValidation = 1,
        Resolution = 2,
        Execution = 3
    }

    public class CommandRunResultError
    {
        public CommandRunResultErrorType ErrorType { get; private set; }
        public string Message { get; private set; }
        public Exception Exception { get; private set; }

        public CommandRunResultError(CommandRunResultErrorType type, string message)
            : this(type, message, null) { }

        public CommandRunResultError(CommandRunResultErrorType type, string message, Exception ex)
        {
            ErrorType = type;
            Message = message;
            Exception = ex;
        }
    }
}
