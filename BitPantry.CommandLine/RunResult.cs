using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine
{
    public class RunResult
    {
        public int ResultCode { get; internal set; }
        public object Result { get; internal set; }
        public Exception RunError { get; internal set; }
    }
}
