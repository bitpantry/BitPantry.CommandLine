using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    public class ReturnTypeAsyncGeneric : CommandBase
    {
        public async Task<string> Execute(CommandExecutionContext ctx)
        {
            await Task.Delay(1);
            return "hello world!";
        }
    }
}
