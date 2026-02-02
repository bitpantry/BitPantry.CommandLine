using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;

namespace BitPantry.CommandLine.Tests.Commands.PositionalCommands
{
    public enum TestColor { Red, Green, Blue, Cyan }
    public enum TestSize { Small, Medium, Large, Huge }

    /// <summary>
    /// Test command with enum positional arguments and named boolean argument.
    /// Used to test mixed positional enum + named argument parsing scenarios.
    /// </summary>
    [Command(Name = "enumPositionalWithNamed")]
    class EnumPositionalWithNamedCommand : CommandBase
    {
        public static TestColor? LastColor { get; set; }
        public static TestSize? LastSize { get; set; }
        public static bool? LastFlag { get; set; }

        [Argument(Position = 0)]
        public TestColor Color { get; set; }

        [Argument(Position = 1)]
        public TestSize Size { get; set; }

        [Argument(Name = "flag")]
        public bool Flag { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            LastColor = Color;
            LastSize = Size;
            LastFlag = Flag;
        }

        public static void Reset()
        {
            LastColor = null;
            LastSize = null;
            LastFlag = null;
        }
    }
}
