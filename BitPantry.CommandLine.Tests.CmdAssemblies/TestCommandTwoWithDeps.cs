using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BitPantry.CommandLine.Tests.CmdAssemblies
{
    public class TestCommandTwoWithDeps : CommandBase
    {
        private Dep1 _dep;

        public TestCommandTwoWithDeps(Dep1 dep)
        {
            _dep = dep;
        }

        public void Execute(CommandExecutionContext ctx) { var num = _dep.GetNumber(); }
    }
}
