namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Internal credential storage interface.
/// Injected into ProfileManager, not used directly by commands.
/// </summary>
internal interface ICredentialStore
{
    /// <summary>
    /// Store an encrypted API key for a profile.
    /// </summary>
    Task StoreAsync(string profileName, string apiKey, CancellationToken ct = default);

    /// <summary>
    /// Retrieve a decrypted API key for a profile.
    /// </summary>
    /// <returns>The API key or null if not found.</returns>
    Task<string> RetrieveAsync(string profileName, CancellationToken ct = default);

    /// <summary>
    /// Remove the credential for a profile.
    /// </summary>
    Task RemoveAsync(string profileName, CancellationToken ct = default);

    /// <summary>
    /// Check if credentials exist for a profile.
    /// </summary>
    Task<bool> ExistsAsync(string profileName, CancellationToken ct = default);
}
