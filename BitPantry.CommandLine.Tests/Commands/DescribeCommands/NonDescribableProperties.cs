using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    /// <summary>
    /// Intended to test the describe results of a command with non-describably properties (properties without attributes)
    /// </summary>
    class NonDescribableProperties : CommandBase
    {
        public int MyProperty { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
