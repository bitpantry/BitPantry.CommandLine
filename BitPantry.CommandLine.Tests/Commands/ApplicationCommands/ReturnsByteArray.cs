using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class ReturnsByteArray : CommandBase
    {
        public async Task<byte[]> Execute(CommandExecutionContext ctx)
        {
            await Task.Delay(1);
            return Encoding.ASCII.GetBytes("hello world!");
        }
    }
}
