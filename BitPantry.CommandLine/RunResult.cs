using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine
{
    /// <summary>
    /// Result of a command execution
    /// </summary>
    public class RunResult
    {
        /// <summary>
        /// A result code enum representing the outcome of the command execution
        /// </summary>
        public RunResultCode ResultCode { get; internal set; }

        /// <summary>
        /// Any data returned as a result of the execution of the command
        /// </summary>
        public object Result { get; internal set; }

        /// <summary>
        /// Any unhandled error originating from the command and intercepted by the command line application
        /// </summary>
        public Exception RunError { get; internal set; }
    }
}
