# Data Model: Server Profile Management

**Branch**: `009-server-profile` | **Date**: 2026-02-02 | **Phase**: 1

## Entity Definitions

### ServerProfile

Core entity representing a saved server connection profile.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Represents a saved server connection profile.
/// </summary>
public class ServerProfile
{
    /// <summary>
    /// Unique profile name (case-insensitive comparison).
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Server URI for the remote connection.
    /// </summary>
    public required string Uri { get; set; }
    
    /// <summary>
    /// Decrypted API key. Populated by IProfileManager.GetProfileAsync(),
    /// not persisted to profiles.json.
    /// </summary>
    [JsonIgnore]
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// Timestamp when the profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when the profile was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}
```

### ProfileConfiguration

Storage model for the profiles.json file.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Root configuration object persisted to profiles.json.
/// </summary>
public class ProfileConfiguration
{
    /// <summary>
    /// Name of the default profile (null if none set).
    /// </summary>
    public string? DefaultProfile { get; set; }
    
    /// <summary>
    /// Dictionary of profile name to profile data.
    /// </summary>
    public Dictionary<string, ServerProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Schema version for future migrations.
    /// </summary>
    public int Version { get; set; } = 1;
}
```

### CredentialEntry (Internal)

Internal model for credential storage (not directly exposed).

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Internal structure for encrypted credential storage.
/// </summary>
internal class CredentialEntry
{
    public required string ProfileName { get; set; }
    public required byte[] EncryptedApiKey { get; set; }
}

/// <summary>
/// Root structure for credentials.enc file.
/// </summary>
internal class CredentialStore
{
    public int Version { get; set; } = 1;
    public List<CredentialEntry> Entries { get; set; } = new();
}
```

## Storage Schema

### profiles.json

```json
{
  "version": 1,
  "defaultProfile": "production",
  "profiles": {
    "production": {
      "name": "production",
      "uri": "https://api.example.com",
      "createdAt": "2026-02-02T10:30:00Z",
      "modifiedAt": null
    },
    "staging": {
      "name": "staging",
      "uri": "https://staging.example.com",
      "createdAt": "2026-02-02T11:00:00Z",
      "modifiedAt": "2026-02-02T12:00:00Z"
    }
  }
}
```

### credentials.enc (Binary Format)

```
[4 bytes]  Version (int32, little-endian) = 1
[4 bytes]  Entry count (int32, little-endian)

For each entry:
  [4 bytes]  Profile name length (int32)
  [n bytes]  Profile name (UTF-8)
  [4 bytes]  Encrypted data length (int32)
  [n bytes]  Encrypted API key data
             Windows: DPAPI-encrypted UTF-8 bytes
             Linux/macOS: [24 bytes nonce] + [n bytes SecretBox cipher]
```

## File Locations

| File | Path | Purpose |
|------|------|---------|
| profiles.json | `~/.bitpantry/commandline/profiles/profiles.json` | Profile metadata |
| credentials.enc | `~/.bitpantry/commandline/profiles/credentials.enc` | Encrypted credentials |

**Path Resolution**:
- Windows: `%USERPROFILE%\.bitpantry\commandline\profiles\`
- Linux/macOS: `$HOME/.bitpantry/commandline/profiles/`

## Interface Contracts

### API Design Decision

> **Q: Will there be a single API for profiles and credentials?**
>
> **Yes.** `IProfileManager` is the primary public API. `ICredentialStore` is injected into the `ProfileManager` implementation as an internal dependency. Commands and consumers only need to inject `IProfileManager`.

**Architecture**:
```
┌──────────────────────────────────────────────────────────────────┐
│                       Command Layer                               │
│  (ProfileAddCommand, ConnectCommand, etc.)                        │
│                            │                                      │
│                   inject IProfileManager                          │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                  IProfileManager (public API)                     │
│                                                                   │
│  GetProfileAsync(name) → ServerProfile (with ApiKey populated)    │
│  SaveProfileAsync(profile) → stores profile + profile.ApiKey      │
│  DeleteProfileAsync(name) → removes profile AND credential        │
└────────────────────────────┬─────────────────────────────────────┘
                             │ (internal)
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                  ICredentialStore (internal)                      │
│  Injected into ProfileManager, not used directly by commands      │
└──────────────────────────────────────────────────────────────────┘
```

### IProfileManager (Primary Public API)

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

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
    /// Save or update a profile. If profile.ApiKey is set, it's encrypted and stored.
    /// If profile.ApiKey is null, existing credential (if any) is preserved.
    /// </summary>
    Task SaveProfileAsync(ServerProfile profile, CancellationToken ct = default);
    
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
```

### ICredentialStore (Internal Implementation Detail)

> **Note**: This interface is **internal** to the `ProfileManager` implementation. Commands should use `IProfileManager.GetProfileWithCredentialAsync()` instead of accessing credentials directly.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

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
    Task<string?> RetrieveAsync(string profileName, CancellationToken ct = default);
    
    /// <summary>
    /// Remove the credential for a profile.
    /// </summary>
    Task RemoveAsync(string profileName, CancellationToken ct = default);
    
    /// <summary>
    /// Check if credentials exist for a profile.
    /// </summary>
    Task<bool> ExistsAsync(string profileName, CancellationToken ct = default);
}
```

## Validation Rules

| Field | Rule | Error Message |
|-------|------|---------------|
| Profile.Name | Non-empty, alphanumeric + hyphen, max 64 chars | "Profile name must be alphanumeric (hyphens allowed), max 64 characters" |
| Profile.Name | Unique (case-insensitive) | "Profile '{name}' already exists" |
| Profile.Uri | Valid URI format | "Invalid server URI format" |
| ApiKey | Non-empty when storing | "API key cannot be empty" |

## Naming Conventions

- Profile names: Case-insensitive storage and lookup
- Profile names: Allowed characters: `a-z`, `A-Z`, `0-9`, `-`
- Profile names: Must start with alphanumeric character
- Profile names: Maximum length: 64 characters

## Error Codes

| Code | Scenario | User Message |
|------|----------|--------------|
| PROFILE_NOT_FOUND | Profile doesn't exist | "Profile '{name}' not found" |
| PROFILE_EXISTS | Duplicate name on add | "Profile '{name}' already exists" |
| CREDENTIAL_UNAVAILABLE | libsodium not installed | "Credential storage requires libsodium. Install via: {instructions}" |
| CREDENTIAL_DECRYPT_FAILED | Wrong machine/corruption | "Failed to decrypt credentials. API key may need to be re-entered." |
| STORAGE_INACCESSIBLE | File permissions | "Cannot access profile storage at {path}" |
