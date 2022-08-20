using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine
{
    public class CommandRunResult
    {
        private List<CommandRunResultError> _errors = new List<CommandRunResultError>();
        public IReadOnlyList<CommandRunResultError> Errors => _errors.AsReadOnly();

        public int ResultCode { get; set; } = (int)CommandRunResultCode.Success;

        public void AddError(CommandRunResultErrorType type, string message)
        { AddError(type, message, null); }  

        public void AddError(CommandRunResultErrorType type, string message, Exception ex)
        { _errors.Add(new CommandRunResultError(type, message, ex)); }

    }
}
