using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles
{
    /// <summary>
    /// Manages server connection profiles stored in a JSON file.
    /// </summary>
    public class ProfileManager : IProfileManager
    {
        private static readonly Regex ProfileNamePattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly ILogger<ProfileManager> _logger;
        private readonly ICredentialStore _credentialStore;
        private readonly string _profilesFilePath;
        private readonly object _lock = new();
        private ProfileConfiguration? _cache;

        public ProfileManager(ILogger<ProfileManager> logger, ICredentialStore credentialStore)
        {
            _logger = logger;
            _credentialStore = credentialStore;
            _profilesFilePath = Path.Combine(CredentialStore.GetConfigDirectory(), "profiles.json");
        }

        /// <summary>
        /// Constructor for testing with custom path.
        /// </summary>
        internal ProfileManager(ILogger<ProfileManager> logger, ICredentialStore credentialStore, string profilesFilePath)
        {
            _logger = logger;
            _credentialStore = credentialStore;
            _profilesFilePath = profilesFilePath;
        }

        public async Task<IReadOnlyList<ServerProfile>> GetAllProfilesAsync()
        {
            var config = await LoadConfigurationAsync();
            return config.Profiles.Values.ToList().AsReadOnly();
        }

        public async Task<ServerProfile?> GetProfileAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var config = await LoadConfigurationAsync();
            return config.Profiles.TryGetValue(name, out var profile) ? profile : null;
        }

        public async Task SaveProfileAsync(ServerProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (!IsValidProfileName(profile.Name))
            {
                throw new ArgumentException(
                    $"Invalid profile name '{profile.Name}'. Names may only contain letters, numbers, hyphens, and underscores.",
                    nameof(profile));
            }

            var config = await LoadConfigurationAsync();
            var key = profile.Name.ToLowerInvariant();
            var isNew = !config.Profiles.ContainsKey(key);

            var now = DateTimeOffset.UtcNow;
            if (isNew)
            {
                profile.CreatedAt = now;
            }
            profile.ModifiedAt = now;

            config.Profiles[key] = profile;
            await SaveConfigurationAsync(config);

            _logger.LogDebug("{Action} profile '{ProfileName}'", isNew ? "Created" : "Updated", profile.Name);
        }

        public async Task<bool> DeleteProfileAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var config = await LoadConfigurationAsync();
            var key = name.ToLowerInvariant();

            if (config.Profiles.Remove(key))
            {
                // Clear default if deleted profile was default
                if (string.Equals(config.DefaultProfile, name, StringComparison.OrdinalIgnoreCase))
                {
                    config.DefaultProfile = null;
                }

                await SaveConfigurationAsync(config);
                
                // Also remove credentials
                await _credentialStore.RemoveAsync(name);

                _logger.LogDebug("Deleted profile '{ProfileName}'", name);
                return true;
            }

            return false;
        }

        public async Task<string?> GetDefaultProfileAsync()
        {
            var config = await LoadConfigurationAsync();
            return config.DefaultProfile;
        }

        public async Task SetDefaultProfileAsync(string? name)
        {
            var config = await LoadConfigurationAsync();

            if (name != null)
            {
                if (!config.Profiles.ContainsKey(name))
                {
                    throw new ArgumentException($"Profile '{name}' not found", nameof(name));
                }
            }

            config.DefaultProfile = name;
            await SaveConfigurationAsync(config);

            _logger.LogDebug("Set default profile to '{ProfileName}'", name ?? "(none)");
        }

        public bool IsValidProfileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (name.Length > 64)
                return false;

            return ProfileNamePattern.IsMatch(name);
        }

        private async Task<ProfileConfiguration> LoadConfigurationAsync()
        {
            lock (_lock)
            {
                if (_cache != null)
                    return _cache;
            }

            try
            {
                if (!File.Exists(_profilesFilePath))
                {
                    var empty = new ProfileConfiguration();
                    lock (_lock) { _cache = empty; }
                    return empty;
                }

                string json;
                lock (_lock)
                {
                    json = File.ReadAllText(_profilesFilePath);
                }

                var config = JsonSerializer.Deserialize<ProfileConfiguration>(json, JsonOptions)
                    ?? new ProfileConfiguration();

                lock (_lock) { _cache = config; }
                return config;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Corrupted profiles.json, creating backup and resetting");
                await HandleCorruptedConfigAsync();
                var empty = new ProfileConfiguration();
                lock (_lock) { _cache = empty; }
                return empty;
            }
        }

        private async Task SaveConfigurationAsync(ProfileConfiguration config)
        {
            var directory = Path.GetDirectoryName(_profilesFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            lock (_lock)
            {
                File.WriteAllText(_profilesFilePath, json);
                _cache = config;
            }
        }

        private async Task HandleCorruptedConfigAsync()
        {
            try
            {
                if (File.Exists(_profilesFilePath))
                {
                    var backupPath = _profilesFilePath + ".bak";
                    File.Copy(_profilesFilePath, backupPath, overwrite: true);
                    File.Delete(_profilesFilePath);
                    _logger.LogWarning("Profile configuration was corrupted. A backup was saved to {BackupPath}", backupPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup corrupted profiles.json");
            }
        }
    }
}
