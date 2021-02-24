using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class TestExecuteCancel : CommandBase
    {
        public async Task<int> Execute(CommandExecutionContext ctx)
        {
            DateTime start = DateTime.Now;

            do
            {
                await Task.Delay(100);
            } while (!ctx.CancellationToken.IsCancellationRequested && (DateTime.Now - start).TotalMilliseconds < TimeSpan.FromMilliseconds(500).TotalMilliseconds);

            return (int)(DateTime.Now - start).TotalMilliseconds;
        }
    }
}
