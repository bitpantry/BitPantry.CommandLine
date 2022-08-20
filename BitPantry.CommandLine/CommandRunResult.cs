using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine
{
    public class CommandRunResult
    {
        public Exception RunError { get; internal set; }

        public int ResultCode { get; internal set; } = (int)CommandRunResultCode.Success;

        //public void AddError(CommandRunResultErrorType type, string message)
        //{ AddError(type, message, null); }  

        //public void AddError(CommandRunResultErrorType type, string message, Exception ex)
        //{ _errors.Add(new CommandRunResultError(type, message, ex)); }

        public object Result { get; internal set; }
    }
}
