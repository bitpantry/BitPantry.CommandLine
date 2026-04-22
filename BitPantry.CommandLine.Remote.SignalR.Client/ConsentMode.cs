namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Controls how the consent system handles file access requests that are not
    /// covered by any allow-path pattern.
    /// </summary>
    public enum ConsentMode
    {
        /// <summary>
        /// Prompt the user for consent on uncovered paths. Default for interactive sessions.
        /// </summary>
        Prompt,

        /// <summary>
        /// Allow all file access requests without prompting.
        /// Use in trusted development environments.
        /// </summary>
        AllowAll,

        /// <summary>
        /// Deny all file access requests that are not covered by allow-path patterns.
        /// No prompts — uncovered paths are silently denied. Use for CI/scripts.
        /// </summary>
        DenyAll
    }
}
