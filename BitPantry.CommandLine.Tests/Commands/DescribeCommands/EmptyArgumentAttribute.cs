using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    /// <summary>
    /// Intended to test describe result for parameter with no name defined
    /// </summary>
    class EmptyArgumentAttribute : CommandBase
    {
        [Argument]
        public string TestArg { get; set; }

        public int Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
