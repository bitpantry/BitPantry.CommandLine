using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Commands.ApplicationCommands
{
    [InGroup<TestGroup>]
    [Command(Name = "virt")]
    public class VirtualCommand : CommandBase
    {
        [Argument]
        public string Arg1 { get; set; }

        public virtual string Execute(CommandExecutionContext ctx)
        {
            return "base";
        }
    }


    [InGroup<TestGroup>]
    [Command(Name = "evirt")]
    public class ExtendVirtualCommand : VirtualCommand
    {

        [Argument]
        public string Arg2 { get; set; }
        
        public override string Execute(CommandExecutionContext ctx)
        {
            return $"extend:{base.Execute(ctx)}:{base.Arg1}:{Arg2}";
        }
    }

}
