namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles
{
    /// <summary>
    /// Represents a saved server connection profile.
    /// </summary>
    public class ServerProfile
    {
        /// <summary>
        /// Unique profile name (alphanumeric, hyphen, underscore only).
        /// Validation: ^[a-zA-Z0-9_-]+$
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Server URI (e.g., "https://api.example.com").
        /// </summary>
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// Whether credentials are stored for this profile.
        /// </summary>
        public bool HasCredentials { get; set; }

        /// <summary>
        /// Timestamp when profile was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when profile was last modified.
        /// </summary>
        public DateTimeOffset ModifiedAt { get; set; }
    }
}
