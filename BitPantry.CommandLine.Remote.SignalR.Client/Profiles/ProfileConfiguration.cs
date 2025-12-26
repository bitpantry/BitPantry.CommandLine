namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles
{
    /// <summary>
    /// Root configuration object stored in profiles.json.
    /// </summary>
    public class ProfileConfiguration
    {
        /// <summary>
        /// Name of the default profile, or null if none set.
        /// </summary>
        public string? DefaultProfile { get; set; }

        /// <summary>
        /// Dictionary of profiles keyed by name (case-insensitive).
        /// </summary>
        public Dictionary<string, ServerProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
