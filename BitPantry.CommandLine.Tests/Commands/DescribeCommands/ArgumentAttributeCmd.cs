using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{

    /// <summary>
    /// Intended to test describe results for parameter attribute that defines a name
    /// </summary>
    class ArgumentAttributeCmd : CommandBase
    {
        [Argument(Name = "MyName")]
        public int PropertyOne { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
