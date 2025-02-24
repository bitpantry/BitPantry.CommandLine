using BitPantry.CommandLine.API;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    class InvalidExecuteParametersAsync : CommandBase
    {
        public async Task Execute(string str) { await Task.Factory.StartNew(() => { }); }
    }
}
