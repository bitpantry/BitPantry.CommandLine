﻿using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.DescribeCommands
{
    class ArgumentWithAlias : CommandBase
    {
        [Argument]
        [Alias('p')]
        public int MyProperty { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
