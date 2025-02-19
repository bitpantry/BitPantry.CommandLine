using BitPantry.CommandLine.API;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class TestExecuteWithReturnTypeAsync : CommandBase
    {
        public async Task Execute(CommandExecutionContext ctx)
        {
            await Task.Delay(1);
        }
    }
}
