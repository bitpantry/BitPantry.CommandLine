# Contract: ICredentialStore

**Package**: `BitPantry.CommandLine.Remote.SignalR.Client`  
**Namespace**: `BitPantry.CommandLine.Remote.SignalR.Client.Profiles`

## Interface Definition

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

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
```

## Implementation Strategy

### Windows (Primary)

Uses DPAPI via `System.Security.Cryptography.ProtectedData`:

```csharp
// Encrypt
byte[] encrypted = ProtectedData.Protect(
    Encoding.UTF8.GetBytes(apiKey),
    entropy: null,
    scope: DataProtectionScope.CurrentUser);

// Decrypt
byte[] decrypted = ProtectedData.Unprotect(
    encrypted,
    entropy: null,
    scope: DataProtectionScope.CurrentUser);
string apiKey = Encoding.UTF8.GetString(decrypted);
```

### Linux/macOS (Primary)

Uses libsodium via `Sodium.Core` NuGet package:

```csharp
// Key derivation from machine + user identity
var machineId = GetMachineId();
var userId = Environment.UserName;
var key = GenericHash.Hash(
    Encoding.UTF8.GetBytes($"{machineId}:{userId}"),
    null,
    32);

// Encrypt
var nonce = SecretBox.GenerateNonce();
var encrypted = SecretBox.Create(
    Encoding.UTF8.GetBytes(apiKey),
    nonce,
    key);

// Decrypt
var decrypted = SecretBox.Open(encrypted, nonce, key);
string apiKey = Encoding.UTF8.GetString(decrypted);
```

## Storage Location

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\BitPantry\CommandLine\credentials.enc` |
| Linux | `~/.config/bitpantry-commandline/credentials.enc` |
| macOS | `~/.config/bitpantry-commandline/credentials.enc` |

## File Format (Encrypted)

The file contains a JSON structure encrypted with the platform-specific method:

```json
{
  "prod": "<base64-encoded-encrypted-key>",
  "staging": "<base64-encoded-encrypted-key>"
}
```

Each credential is individually encrypted, so removing one doesn't require re-encrypting others.

## Security Considerations

### Key Derivation (Non-Windows)
- Machine ID: `/etc/machine-id` (Linux) or `IOPlatformSerialNumber` (macOS)
- Combined with username for user-specific encryption
- Not stored anywhere - derived at runtime

### Threat Model
| Threat | Mitigation |
|--------|------------|
| File stolen | Encrypted with user-specific key |
| Memory dump | Keys not stored in memory longer than needed |
| Process injection | Out of scope (OS-level protection) |
| Network interception | Not applicable (local storage) |

### Limitations
- Credentials accessible to any process running as the same user
- Same security model as Chrome password storage, Azure CLI, etc.

## Error Handling

```csharp
/// <summary>
/// Exception thrown when credential store operations fail.
/// </summary>
public class CredentialStoreException : Exception
{
    public CredentialStoreException(string message) : base(message) { }
    public CredentialStoreException(string message, Exception inner) : base(message, inner) { }
}
```

### Error Cases

| Scenario | Behavior |
|----------|----------|
| File permissions denied | Throw `CredentialStoreException` with guidance |
| Corrupted file | Log warning, treat as empty, allow overwrite |
| DPAPI unavailable | Fall back to encrypted file |
| libsodium unavailable | Throw with installation guidance |

## Dependencies

| Package | Platform | Purpose |
|---------|----------|---------|
| `System.Security.Cryptography.ProtectedData` | Windows | DPAPI encryption |
| `Sodium.Core` | All | libsodium bindings |
