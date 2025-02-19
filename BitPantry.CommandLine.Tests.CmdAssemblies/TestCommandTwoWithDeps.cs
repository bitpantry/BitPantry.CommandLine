using BitPantry.CommandLine.API;

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
