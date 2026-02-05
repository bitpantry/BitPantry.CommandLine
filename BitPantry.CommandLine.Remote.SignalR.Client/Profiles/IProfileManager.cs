namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Primary public API for profile management.
/// </summary>
public interface IProfileManager
{
    /// <summary>
    /// Get all saved profiles (without credentials - use GetProfileAsync for full profile).
    /// </summary>
    Task<IReadOnlyList<ServerProfile>> GetAllProfilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a complete profile by name, including decrypted API key.
    /// The returned ServerProfile.ApiKey is populated from the credential store.
    /// </summary>
    Task<ServerProfile?> GetProfileAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Create a new profile. If profile.ApiKey is set, it's encrypted and stored.
    /// Throws InvalidOperationException if a profile with the same name already exists.
    /// </summary>
    Task CreateProfileAsync(ServerProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Update an existing profile. If profile.ApiKey is set, it's encrypted and stored.
    /// If profile.ApiKey is null, existing credential (if any) is preserved.
    /// Throws InvalidOperationException if the profile does not exist.
    /// </summary>
    Task UpdateProfileAsync(ServerProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Update the API key for an existing profile.
    /// </summary>
    Task SetApiKeyAsync(string profileName, string apiKey, CancellationToken ct = default);

    /// <summary>
    /// Check if a profile has stored credentials.
    /// </summary>
    Task<bool> HasCredentialAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Delete a profile and its associated credential.
    /// </summary>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteProfileAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Get the name of the default profile.
    /// </summary>
    Task<string?> GetDefaultProfileNameAsync(CancellationToken ct = default);

    /// <summary>
    /// Set the default profile name (null to clear).
    /// </summary>
    Task SetDefaultProfileAsync(string? name, CancellationToken ct = default);

    /// <summary>
    /// Check if a profile exists.
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken ct = default);
}
