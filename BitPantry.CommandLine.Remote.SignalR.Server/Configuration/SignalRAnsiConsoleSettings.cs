using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Configuration
{
    /// <summary>
    /// Settings used to configure new <see cref="SignalRAnsiConsole"/>s
    /// </summary>
    public class SignalRAnsiConsoleSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether or
        /// not ANSI escape sequences are supported.
        /// </summary>
        public bool Ansi { get; set; }

        /// <summary>
        /// Gets or sets the color system to use.
        /// </summary>
        public ColorSystem ColorSystem { get; set; } = ColorSystem.Standard;

        /// <summary>
        /// Gets or sets a value indicating whether or not the
        /// terminal is interactive or not.
        /// </summary>
        public bool Interactive { get; set; }

    }
}
