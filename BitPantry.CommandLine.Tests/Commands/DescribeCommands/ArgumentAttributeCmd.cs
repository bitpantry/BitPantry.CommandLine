using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{

    /// <summary>
    /// Intended to test describe results for parameter attribute that defines a name
    /// </summary>
    class ArgumentAttributeCmd : CommandBase
    {
        [Argument(Name = "MyName")]
        public int PropertyOne { get; set; }

        public int Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
