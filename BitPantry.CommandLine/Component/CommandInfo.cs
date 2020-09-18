using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Component
{
    public class CommandInfo
    {
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public Type Type { get; internal set; }
        public IReadOnlyCollection<ArgumentInfo> Arguments { get; internal set; }
        public bool IsExecuteAsync { get; internal set; }
    }
}
