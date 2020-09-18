using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    class InvalidExecuteReturnAsync : CommandBase
    {
        public async Task<string> Execute(CommandExecutionContext ctx) { return await Task.Factory.StartNew(() => "hello world"); }
    }
}
