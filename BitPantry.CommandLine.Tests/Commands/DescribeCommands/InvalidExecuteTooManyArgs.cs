using BitPantry.CommandLine.API;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    [Command]
    public class InvalidExecuteTooManyArgs : CommandBase
    {
        public async Task Execute(CommandExecutionContext ctx, string oneTooMany) { await Task.CompletedTask; }
    }
}
