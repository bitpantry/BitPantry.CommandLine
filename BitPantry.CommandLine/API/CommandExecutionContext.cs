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

    public class CommandExecutionContext<T> : CommandExecutionContext
    {
        public T Input { get; internal set; }

        public CommandExecutionContext(object input)
        {
            Input = Input == null
                ? default(T)
                : (T)input;
        }
    }
}
