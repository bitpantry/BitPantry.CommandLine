using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    public class TestExecuteWithReturnType : CommandBase
    {
        public string Execute(CommandExecutionContext ctx)
        {
            return "hello world!";
        }
    }
}
