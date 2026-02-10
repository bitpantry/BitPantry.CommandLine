using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Tests.Commands.ResolveCommands
{
    class MultipleArgumentsAndAliases : CommandBase
    {
        [Argument]
        public int MyProperty { get; set; }

        [Argument]
        [Alias('y')]
        public string PropertyTwo { get; set; }

        [Argument]
        [Alias('X')]
        public string Prop { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
