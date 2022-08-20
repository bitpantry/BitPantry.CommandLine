using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    /// <summary>
    /// Intended to test the description results of a Command attribute that defines the command name
    /// </summary>
    [Command(Name = "NewName")]
    class CommandAttributeCmd : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
