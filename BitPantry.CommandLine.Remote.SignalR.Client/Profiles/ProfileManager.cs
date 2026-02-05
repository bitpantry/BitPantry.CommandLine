using System.IO.Abstractions;
using System.Text.Json;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Manages server connection profiles with encrypted credential storage.
/// </summary>
public class ProfileManager : IProfileManager
{
    private readonly IFileSystem _fileSystem;
    private readonly string _storagePath;
    private readonly string _configFilePath;
    private readonly ICredentialStore _credentialStore;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a ProfileManager with default file system and credential storage.
    /// </summary>
    public ProfileManager(string storagePath)
        : this(new FileSystem(), storagePath, new CredentialStore(new FileSystem(), storagePath))
    {
    }

    /// <summary>
    /// Creates a ProfileManager with custom file system (for testing).
    /// </summary>
    internal ProfileManager(IFileSystem fileSystem, string storagePath, ICredentialStore credentialStore)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _configFilePath = _fileSystem.Path.Combine(_storagePath, "profiles.json");
    }

    public async Task<IReadOnlyList<ServerProfile>> GetAllProfilesAsync(CancellationToken ct = default)
    {
        var config = await LoadConfigurationAsync(ct);
        return config.Profiles.Values.ToList().AsReadOnly();
    }

    public async Task<ServerProfile?> GetProfileAsync(string name, CancellationToken ct = default)
    {
        var config = await LoadConfigurationAsync(ct);
        if (!config.Profiles.TryGetValue(name, out var profile))
            return null;

        // Populate API key from credential store (create a copy with ApiKey set)
        var apiKey = await _credentialStore.RetrieveAsync(name, ct);
        return new ServerProfile
        {
            Name = profile.Name,
            Uri = profile.Uri,
            ApiKey = apiKey,
            CreatedAt = profile.CreatedAt,
            ModifiedAt = profile.ModifiedAt
        };
    }

    public async Task SaveProfileAsync(ServerProfile profile, CancellationToken ct = default)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        if (string.IsNullOrWhiteSpace(profile.Name))
            throw new ArgumentException("Profile name cannot be empty", nameof(profile));

        var config = await LoadConfigurationAsync(ct);
        
        // Store API key separately if provided
        if (!string.IsNullOrEmpty(profile.ApiKey))
        {
            await _credentialStore.StoreAsync(profile.Name, profile.ApiKey, ct);
        }

        // Store profile without API key (API key is stored encrypted separately)
        var isNew = !config.Profiles.ContainsKey(profile.Name);
        var profileToStore = new ServerProfile
        {
            Name = profile.Name,
            Uri = profile.Uri,
            ApiKey = null, // Don't persist API key to JSON
            CreatedAt = isNew ? DateTime.UtcNow : profile.CreatedAt,
            ModifiedAt = DateTime.UtcNow
        };
        
        config.Profiles[profile.Name] = profileToStore;
        await SaveConfigurationAsync(config, ct);
    }

    public async Task SetApiKeyAsync(string profileName, string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be empty", nameof(profileName));

        var config = await LoadConfigurationAsync(ct);
        if (!config.Profiles.ContainsKey(profileName))
            throw new InvalidOperationException($"Profile '{profileName}' does not exist");

        await _credentialStore.StoreAsync(profileName, apiKey, ct);
    }

    public async Task<bool> HasCredentialAsync(string name, CancellationToken ct = default)
    {
        return await _credentialStore.ExistsAsync(name, ct);
    }

    public async Task<bool> DeleteProfileAsync(string name, CancellationToken ct = default)
    {
        var config = await LoadConfigurationAsync(ct);
        if (!config.Profiles.Remove(name))
            return false;

        // Also remove credentials
        await _credentialStore.RemoveAsync(name, ct);
        
        // Clear default if it was the deleted profile
        if (string.Equals(config.DefaultProfile, name, StringComparison.OrdinalIgnoreCase))
        {
            config.DefaultProfile = null;
        }

        await SaveConfigurationAsync(config, ct);
        return true;
    }

    public async Task<string?> GetDefaultProfileNameAsync(CancellationToken ct = default)
    {
        var config = await LoadConfigurationAsync(ct);
        return config.DefaultProfile;
    }

    public async Task SetDefaultProfileAsync(string? name, CancellationToken ct = default)
    {
        var config = await LoadConfigurationAsync(ct);
        
        if (name != null && !config.Profiles.ContainsKey(name))
            throw new InvalidOperationException($"Profile '{name}' does not exist");

        config.DefaultProfile = name;
        await SaveConfigurationAsync(config, ct);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken ct = default)
    {
        var config = await LoadConfigurationAsync(ct);
        return config.Profiles.ContainsKey(name);
    }

    private async Task<ProfileConfiguration> LoadConfigurationAsync(CancellationToken ct)
    {
        if (!_fileSystem.File.Exists(_configFilePath))
            return new ProfileConfiguration();

        var json = await _fileSystem.File.ReadAllTextAsync(_configFilePath, ct);
        var config = JsonSerializer.Deserialize<ProfileConfiguration>(json, _jsonOptions) ?? new ProfileConfiguration();
        
        // Ensure dictionary uses case-insensitive comparison (JSON deserialization loses the custom comparer)
        if (config.Profiles.Count > 0)
        {
            var profiles = new Dictionary<string, ServerProfile>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in config.Profiles)
            {
                profiles[kvp.Key] = kvp.Value;
            }
            config.Profiles = profiles;
        }
        
        return config;
    }

    private async Task SaveConfigurationAsync(ProfileConfiguration config, CancellationToken ct)
    {
        // Ensure directory exists
        if (!_fileSystem.Directory.Exists(_storagePath))
            _fileSystem.Directory.CreateDirectory(_storagePath);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await _fileSystem.File.WriteAllTextAsync(_configFilePath, json, ct);
    }
}
