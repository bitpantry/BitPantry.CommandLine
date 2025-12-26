namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles
{
    /// <summary>
    /// Provides secure storage for profile credentials (API keys).
    /// Uses OS credential store when available, falls back to encrypted file.
    /// </summary>
    public interface ICredentialStore
    {
        /// <summary>
        /// Stores credentials for a profile.
        /// </summary>
        /// <param name="profileName">The profile name (used as key).</param>
        /// <param name="apiKey">The API key to store securely.</param>
        /// <exception cref="CredentialStoreException">If storage fails.</exception>
        Task StoreAsync(string profileName, string apiKey);

        /// <summary>
        /// Retrieves credentials for a profile.
        /// </summary>
        /// <param name="profileName">The profile name.</param>
        /// <returns>The stored API key, or null if not found.</returns>
        Task<string?> RetrieveAsync(string profileName);

        /// <summary>
        /// Removes credentials for a profile.
        /// </summary>
        /// <param name="profileName">The profile name.</param>
        /// <returns>True if removed, false if not found.</returns>
        Task<bool> RemoveAsync(string profileName);

        /// <summary>
        /// Checks if credentials exist for a profile.
        /// </summary>
        /// <param name="profileName">The profile name.</param>
        Task<bool> ExistsAsync(string profileName);
    }
}
