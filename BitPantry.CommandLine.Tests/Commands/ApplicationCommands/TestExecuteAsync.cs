using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    class TestExecuteAsync : CommandBase
    {
        public async Task<int> Execute(CommandExecutionContext ctx)
        {
            return await Task.Factory.StartNew(() => 10);
        }
    }
}
