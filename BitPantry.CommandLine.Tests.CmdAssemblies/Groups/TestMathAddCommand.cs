using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.CmdAssemblies.Groups
{
    /// <summary>
    /// Test add command in math group - used in assembly scanning tests
    /// </summary>
    [Command(Group = typeof(TestMathGroup))]
    public class TestMathAddCommand : CommandBase
    {
        [Argument]
        public int Num1 { get; set; }

        [Argument]
        public int Num2 { get; set; }

        public int Execute(CommandExecutionContext ctx)
        {
            return Num1 + Num2;
        }
    }
}
