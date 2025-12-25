using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment.Commands
{
    /// <summary>
    /// Test command with completion support for autocomplete integration tests.
    /// </summary>
    [Command(Group = typeof(RemoteTestGroup), Name = "complete")]
    [API.Description("Test command with completion support")]
    public class CompletionTestCommand : CommandBase
    {
        /// <summary>
        /// Environment parameter with static values completion.
        /// </summary>
        [Argument(Name = "environment")]
        [API.Description("Target environment")]
        [Completion("dev", "staging", "production")]
        public string Environment { get; set; }

        /// <summary>
        /// Region parameter with static values completion.
        /// </summary>
        [Argument(Name = "region")]
        [API.Description("Target region")]
        [Completion("us-east", "us-west", "eu-west", "ap-south")]
        public string Region { get; set; }

        /// <summary>
        /// Format parameter with enum completion.
        /// </summary>
        [Argument(Name = "format")]
        [API.Description("Output format")]
        public OutputFormat Format { get; set; } = OutputFormat.Text;

        public void Execute(CommandExecutionContext ctx)
        {
            // No output needed for testing
        }
    }

    /// <summary>
    /// Output format options for completion testing.
    /// </summary>
    public enum OutputFormat
    {
        Text,
        Json,
        Xml,
        Csv
    }
}
