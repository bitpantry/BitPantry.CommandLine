﻿using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    class InvalidExecuteReturn : CommandBase
    {
        public int Execute(CommandExecutionContext ctx) { return 0; }
    }
}
