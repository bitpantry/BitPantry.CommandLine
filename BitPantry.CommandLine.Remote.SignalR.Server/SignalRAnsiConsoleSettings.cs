using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public class SignalRAnsiConsoleSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether or
        /// not ANSI escape sequences are supported.
        /// </summary>
        public AnsiSupport Ansi { get; set; }

        /// <summary>
        /// Gets or sets the color system to use.
        /// </summary>
        public ColorSystemSupport ColorSystem { get; set; } = ColorSystemSupport.Detect;

        /// <summary>
        /// Gets or sets a value indicating whether or not the
        /// terminal is interactive or not.
        /// </summary>
        public InteractionSupport Interactive { get; set; }

        /// <summary>
        /// Gets or sets the exclusivity mode.
        /// </summary>
        public IExclusivityMode ExclusivityMode { get; set; }

        /// <summary>
        /// Gets or sets the profile enrichments settings.
        /// </summary>
        public ProfileEnrichment Enrichment { get; set; }

        /// <summary>
        /// Gets or sets the environment variables.
        /// If not value is provided the default environment variables will be used.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnsiConsoleSettings"/> class.
        /// </summary>
        public SignalRAnsiConsoleSettings()
        {
            Enrichment = new ProfileEnrichment();
        }
    }
}
