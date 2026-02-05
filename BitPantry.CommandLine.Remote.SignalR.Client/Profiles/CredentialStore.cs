using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Sodium;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Encryption provider selection for credential storage.
/// </summary>
internal enum EncryptionProvider
{
    /// <summary>Auto-select based on platform (DPAPI on Windows, libsodium on Linux/macOS)</summary>
    Auto,
    /// <summary>Force Windows DPAPI (only works on Windows)</summary>
    Dpapi,
    /// <summary>Force libsodium SecretBox (cross-platform, for testing on Windows)</summary>
    Libsodium
}

/// <summary>
/// Secure credential storage using platform-specific encryption.
/// Windows: DPAPI, Linux/macOS: libsodium SecretBox.
/// </summary>
internal class CredentialStore : ICredentialStore
{
    private readonly IFileSystem _fileSystem;
    private readonly string _storagePath;
    private readonly string _credentialFilePath;
    private readonly EncryptionProvider _encryptionProvider;
    private byte[]? _libsodiumKey;

    // File format: [4 bytes version][4 bytes entry count][entries...]
    // Entry format: [4 bytes name length][name bytes][4 bytes data length][encrypted data bytes]
    private const int CurrentVersion = 1;

    /// <summary>
    /// Creates an exception with helpful install instructions when libsodium is unavailable.
    /// </summary>
    public static InvalidOperationException CreateLibsodiumUnavailableException(Exception innerException)
    {
        var message = "Failed to use libsodium for credential encryption. " +
            "This library is required on non-Windows platforms. " +
            "Please ensure Sodium.Core NuGet package is properly installed and native binaries are available. " +
            "On Linux, you may need to install libsodium: 'apt-get install libsodium-dev' or 'yum install libsodium-devel'.";
        
        return new InvalidOperationException(message, innerException);
    }

    public CredentialStore(IFileSystem fileSystem, string storagePath)
        : this(fileSystem, storagePath, EncryptionProvider.Auto)
    {
    }

    public CredentialStore(IFileSystem fileSystem, string storagePath, EncryptionProvider encryptionProvider)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
        _credentialFilePath = _fileSystem.Path.Combine(_storagePath, "credentials.enc");
        _encryptionProvider = encryptionProvider;
    }

    public async Task StoreAsync(string profileName, string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name cannot be empty", nameof(profileName));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be empty", nameof(apiKey));

        var credentials = await LoadCredentialsAsync(ct);
        var encryptedApiKey = Encrypt(apiKey);
        credentials[profileName.ToLowerInvariant()] = encryptedApiKey;
        await SaveCredentialsAsync(credentials, ct);
    }

    public async Task<string?> RetrieveAsync(string profileName, CancellationToken ct = default)
    {
        var credentials = await LoadCredentialsAsync(ct);
        if (!credentials.TryGetValue(profileName.ToLowerInvariant(), out var encryptedApiKey))
            return null;
        
        return Decrypt(encryptedApiKey);
    }

    public async Task RemoveAsync(string profileName, CancellationToken ct = default)
    {
        var credentials = await LoadCredentialsAsync(ct);
        if (credentials.Remove(profileName.ToLowerInvariant()))
        {
            await SaveCredentialsAsync(credentials, ct);
        }
    }

    public async Task<bool> ExistsAsync(string profileName, CancellationToken ct = default)
    {
        var credentials = await LoadCredentialsAsync(ct);
        return credentials.ContainsKey(profileName.ToLowerInvariant());
    }

    private bool ShouldUseDpapi()
    {
        return _encryptionProvider switch
        {
            EncryptionProvider.Dpapi => true,
            EncryptionProvider.Libsodium => false,
            _ => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        };
    }

    private byte[] Encrypt(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        
        if (ShouldUseDpapi())
        {
            // Windows: Use DPAPI
            return ProtectedData.Protect(plainBytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
        }
        else
        {
            // Linux/macOS: Use libsodium SecretBox
            var key = GetLibsodiumKey();
            var nonce = SecretBox.GenerateNonce();
            var cipher = SecretBox.Create(plainBytes, nonce, key);
            
            // Prepend nonce to cipher (nonce is 24 bytes)
            var result = new byte[nonce.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(cipher, 0, result, nonce.Length, cipher.Length);
            return result;
        }
    }

    private string Decrypt(byte[] encryptedBytes)
    {
        if (ShouldUseDpapi())
        {
            // Windows: Use DPAPI
            var decrypted = ProtectedData.Unprotect(encryptedBytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        else
        {
            // Linux/macOS: Use libsodium SecretBox
            var key = GetLibsodiumKey();
            
            // Extract nonce (first 24 bytes) and cipher
            var nonce = new byte[24];
            var cipher = new byte[encryptedBytes.Length - 24];
            Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, 24);
            Buffer.BlockCopy(encryptedBytes, 24, cipher, 0, cipher.Length);
            
            var decrypted = SecretBox.Open(cipher, nonce, key);
            return Encoding.UTF8.GetString(decrypted);
        }
    }

    private byte[] GetLibsodiumKey()
    {
        if (_libsodiumKey != null)
            return _libsodiumKey;

        // Derive key from machine identifier + username
        var machineId = GetMachineId();
        var username = Environment.UserName;
        var keyMaterial = Encoding.UTF8.GetBytes($"{machineId}:{username}");
        
        // Use GenericHash to derive a 32-byte key
        _libsodiumKey = GenericHash.Hash(keyMaterial, null, 32);
        return _libsodiumKey;
    }

    private string GetMachineId()
    {
        // Try to get machine ID from various sources
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: /etc/machine-id
            var machineIdPath = "/etc/machine-id";
            if (File.Exists(machineIdPath))
                return File.ReadAllText(machineIdPath).Trim();
            
            // Fallback: /var/lib/dbus/machine-id
            var dbusPath = "/var/lib/dbus/machine-id";
            if (File.Exists(dbusPath))
                return File.ReadAllText(dbusPath).Trim();
        }
        
        // Fallback: Machine name
        return Environment.MachineName;
    }

    private async Task<Dictionary<string, byte[]>> LoadCredentialsAsync(CancellationToken ct)
    {
        if (!_fileSystem.File.Exists(_credentialFilePath))
            return new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        var fileBytes = await _fileSystem.File.ReadAllBytesAsync(_credentialFilePath, ct);
        return DeserializeCredentials(fileBytes);
    }

    private async Task SaveCredentialsAsync(Dictionary<string, byte[]> credentials, CancellationToken ct)
    {
        EnsureDirectoryExists();
        var fileBytes = SerializeCredentials(credentials);
        await _fileSystem.File.WriteAllBytesAsync(_credentialFilePath, fileBytes, ct);
    }

    private byte[] SerializeCredentials(Dictionary<string, byte[]> credentials)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(CurrentVersion);
        writer.Write(credentials.Count);

        foreach (var (name, data) in credentials)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            writer.Write(nameBytes.Length);
            writer.Write(nameBytes);
            writer.Write(data.Length);
            writer.Write(data);
        }

        return ms.ToArray();
    }

    private Dictionary<string, byte[]> DeserializeCredentials(byte[] fileBytes)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        using var ms = new MemoryStream(fileBytes);
        using var reader = new BinaryReader(ms);

        var version = reader.ReadInt32();
        if (version != CurrentVersion)
            return result; // Unsupported version, return empty

        var count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            var nameLength = reader.ReadInt32();
            var nameBytes = reader.ReadBytes(nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            var dataLength = reader.ReadInt32();
            var data = reader.ReadBytes(dataLength);

            result[name] = data;
        }

        return result;
    }

    private void EnsureDirectoryExists()
    {
        if (!_fileSystem.Directory.Exists(_storagePath))
            _fileSystem.Directory.CreateDirectory(_storagePath);
    }
}
