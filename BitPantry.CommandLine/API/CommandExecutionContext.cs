using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BitPantry.CommandLine.API
{
    public class CommandExecutionContext
    {
        public CancellationToken CancellationToken { get; internal set; }
        public CommandRegistry CommandRegistry { get; internal set; }
    }
}
