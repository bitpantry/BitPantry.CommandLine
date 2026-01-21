using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands
{
    /// <summary>
    /// Test enum for autocomplete integration tests.
    /// </summary>
    public enum TestLogLevel { Debug, Info, Warning, Error }

    /// <summary>
    /// Test command with an enum argument for autocomplete testing.
    /// </summary>
    [Command]
    public class CommandWithEnumArg : CommandBase
    {
        [Argument]
        public TestLogLevel Level { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }
}
