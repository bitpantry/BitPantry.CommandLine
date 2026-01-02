namespace BitPantry.CommandLine.Input
{
    /// <summary>
    /// Configuration options for the command line prompt.
    /// Values can include Spectre.Console markup (e.g., "[bold]myapp[/]").
    /// </summary>
    public class PromptOptions
    {
        /// <summary>
        /// The application name displayed in the prompt.
        /// If null, the name is derived from the entry assembly.
        /// Supports Spectre.Console markup.
        /// </summary>
        /// <example>
        /// <code>
        /// options.Name("myapp");
        /// options.Name("[bold cyan]myapp[/]");
        /// </code>
        /// </example>
        public string? AppName { get; private set; }

        /// <summary>
        /// The suffix appended to the end of the prompt.
        /// Defaults to "&gt; ".
        /// Supports Spectre.Console markup.
        /// </summary>
        /// <example>
        /// <code>
        /// options.Suffix("$ ");
        /// options.Suffix("[green]>[/] ");
        /// </code>
        /// </example>
        public string Suffix { get; private set; } = "> ";

        /// <summary>
        /// Sets the application name for the prompt.
        /// </summary>
        /// <param name="name">The name to display. Supports Spectre.Console markup.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        public PromptOptions Name(string name)
        {
            AppName = name;
            return this;
        }

        /// <summary>
        /// Sets the suffix appended to the prompt (after all segments).
        /// </summary>
        /// <param name="suffix">The suffix to append. Supports Spectre.Console markup.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        public PromptOptions WithSuffix(string suffix)
        {
            Suffix = suffix;
            return this;
        }
    }
}
