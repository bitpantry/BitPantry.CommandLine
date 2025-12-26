namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles
{
    /// <summary>
    /// Manages server connection profiles.
    /// Profiles are stored in a cross-platform configuration directory.
    /// </summary>
    public interface IProfileManager
    {
        /// <summary>
        /// Gets all saved profiles.
        /// </summary>
        /// <returns>Read-only list of all profiles, may be empty.</returns>
        Task<IReadOnlyList<ServerProfile>> GetAllProfilesAsync();

        /// <summary>
        /// Gets a profile by name.
        /// </summary>
        /// <param name="name">The profile name (case-insensitive).</param>
        /// <returns>The profile, or null if not found.</returns>
        Task<ServerProfile?> GetProfileAsync(string name);

        /// <summary>
        /// Creates or updates a profile.
        /// </summary>
        /// <param name="profile">The profile to save.</param>
        /// <exception cref="ArgumentException">If profile name is invalid.</exception>
        Task SaveProfileAsync(ServerProfile profile);

        /// <summary>
        /// Deletes a profile by name.
        /// </summary>
        /// <param name="name">The profile name (case-insensitive).</param>
        /// <returns>True if deleted, false if not found.</returns>
        /// <remarks>Also removes associated credentials from credential store.</remarks>
        Task<bool> DeleteProfileAsync(string name);

        /// <summary>
        /// Gets the default profile name.
        /// </summary>
        /// <returns>Default profile name, or null if none set.</returns>
        Task<string?> GetDefaultProfileAsync();

        /// <summary>
        /// Sets the default profile.
        /// </summary>
        /// <param name="name">Profile name to set as default, or null to clear.</param>
        /// <exception cref="ArgumentException">If profile does not exist.</exception>
        Task SetDefaultProfileAsync(string? name);

        /// <summary>
        /// Validates a profile name against naming rules.
        /// </summary>
        /// <param name="name">The name to validate.</param>
        /// <returns>True if valid (alphanumeric, hyphen, underscore only).</returns>
        bool IsValidProfileName(string name);
    }
}
