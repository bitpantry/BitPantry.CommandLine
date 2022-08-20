using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    class InvalidExecuteParametersAsync : CommandBase
    {
        public async Task Execute(string str) { await Task.Factory.StartNew(() => { }); }
    }
}
