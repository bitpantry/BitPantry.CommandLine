using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class TestExecuteWithReturnTypeAsyncGeneric : CommandBase
    {
        public async Task<int> Execute(CommandExecutionContext ctx)
        {
            await Task.Delay(1);
            return 42;
        }
    }
}
