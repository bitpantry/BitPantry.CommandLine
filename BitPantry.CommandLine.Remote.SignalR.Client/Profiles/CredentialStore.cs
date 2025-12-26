using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles
{
    /// <summary>
    /// Provides secure storage for profile credentials (API keys).
    /// Uses DPAPI on Windows, libsodium on Linux/macOS with encrypted file storage.
    /// </summary>
    public class CredentialStore : ICredentialStore
    {
        private readonly ILogger<CredentialStore> _logger;
        private readonly string _credentialsFilePath;
        private readonly object _lock = new();

        public CredentialStore(ILogger<CredentialStore> logger)
        {
            _logger = logger;
            _credentialsFilePath = Path.Combine(GetConfigDirectory(), "credentials.enc");
        }

        /// <summary>
        /// Constructor for testing with custom path.
        /// </summary>
        internal CredentialStore(ILogger<CredentialStore> logger, string credentialsFilePath)
        {
            _logger = logger;
            _credentialsFilePath = credentialsFilePath;
        }

        public async Task StoreAsync(string profileName, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                throw new ArgumentException("Profile name cannot be empty", nameof(profileName));
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be empty", nameof(apiKey));

            try
            {
                var credentials = await LoadCredentialsAsync();
                var encryptedKey = EncryptApiKey(apiKey);
                credentials[profileName.ToLowerInvariant()] = encryptedKey;
                await SaveCredentialsAsync(credentials);
                _logger.LogDebug("Stored credentials for profile '{ProfileName}'", profileName);
            }
            catch (Exception ex) when (ex is not CredentialStoreException)
            {
                throw new CredentialStoreException($"Failed to store credentials for profile '{profileName}'", ex);
            }
        }

        public async Task<string?> RetrieveAsync(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return null;

            try
            {
                var credentials = await LoadCredentialsAsync();
                if (credentials.TryGetValue(profileName.ToLowerInvariant(), out var encryptedKey))
                {
                    return DecryptApiKey(encryptedKey);
                }
                return null;
            }
            catch (Exception ex) when (ex is not CredentialStoreException)
            {
                _logger.LogWarning(ex, "Failed to retrieve credentials for profile '{ProfileName}'", profileName);
                return null;
            }
        }

        public async Task<bool> RemoveAsync(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return false;

            try
            {
                var credentials = await LoadCredentialsAsync();
                var key = profileName.ToLowerInvariant();
                if (credentials.Remove(key))
                {
                    await SaveCredentialsAsync(credentials);
                    _logger.LogDebug("Removed credentials for profile '{ProfileName}'", profileName);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove credentials for profile '{ProfileName}'", profileName);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return false;

            try
            {
                var credentials = await LoadCredentialsAsync();
                return credentials.ContainsKey(profileName.ToLowerInvariant());
            }
            catch
            {
                return false;
            }
        }

        private async Task<Dictionary<string, string>> LoadCredentialsAsync()
        {
            lock (_lock)
            {
                if (!File.Exists(_credentialsFilePath))
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                string json;
                lock (_lock)
                {
                    json = File.ReadAllText(_credentialsFilePath);
                }
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Corrupted credentials file, treating as empty");
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private async Task SaveCredentialsAsync(Dictionary<string, string> credentials)
        {
            var directory = Path.GetDirectoryName(_credentialsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true });
            lock (_lock)
            {
                File.WriteAllText(_credentialsFilePath, json);
            }
        }

        private string EncryptApiKey(string apiKey)
        {
            var plainBytes = Encoding.UTF8.GetBytes(apiKey);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use DPAPI on Windows
                var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            else
            {
                // Use libsodium on Linux/macOS
                return EncryptWithSodium(plainBytes);
            }
        }

        private string DecryptApiKey(string encryptedKey)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedKey);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use DPAPI on Windows
                var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            else
            {
                // Use libsodium on Linux/macOS
                return DecryptWithSodium(encryptedBytes);
            }
        }

        private string EncryptWithSodium(byte[] plainBytes)
        {
            var key = DeriveKey();
            var nonce = Sodium.SecretBox.GenerateNonce();
            var encrypted = Sodium.SecretBox.Create(plainBytes, nonce, key);
            
            // Combine nonce + encrypted data
            var combined = new byte[nonce.Length + encrypted.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(encrypted, 0, combined, nonce.Length, encrypted.Length);
            
            return Convert.ToBase64String(combined);
        }

        private string DecryptWithSodium(byte[] combined)
        {
            var key = DeriveKey();
            
            // Extract nonce (24 bytes for SecretBox)
            var nonce = new byte[24];
            var encrypted = new byte[combined.Length - 24];
            Buffer.BlockCopy(combined, 0, nonce, 0, 24);
            Buffer.BlockCopy(combined, 24, encrypted, 0, encrypted.Length);
            
            var plainBytes = Sodium.SecretBox.Open(encrypted, nonce, key);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private byte[] DeriveKey()
        {
            // Derive key from machine ID + username
            var machineId = GetMachineId();
            var userId = Environment.UserName;
            var combined = $"{machineId}:{userId}";
            
            return Sodium.GenericHash.Hash(Encoding.UTF8.GetBytes(combined), null, 32);
        }

        private string GetMachineId()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Try /etc/machine-id
                if (File.Exists("/etc/machine-id"))
                    return File.ReadAllText("/etc/machine-id").Trim();
            }
            
            // Fallback to machine name
            return Environment.MachineName;
        }

        internal static string GetConfigDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BitPantry", "CommandLine");
            }
            else
            {
                // Linux/macOS - use XDG_CONFIG_HOME or fallback
                var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                return Path.Combine(configHome, "bitpantry-commandline");
            }
        }
    }
}
