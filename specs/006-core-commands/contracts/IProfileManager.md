# Contract: IProfileManager

**Package**: `BitPantry.CommandLine.Remote.SignalR.Client`  
**Namespace**: `BitPantry.CommandLine.Remote.SignalR.Client.Profiles`

## Interface Definition

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

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
```

## Storage Location

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\BitPantry\CommandLine\profiles.json` |
| Linux | `~/.config/bitpantry-commandline/profiles.json` |
| macOS | `~/.config/bitpantry-commandline/profiles.json` |

## File Format

```json
{
  "defaultProfile": "prod",
  "profiles": {
    "prod": {
      "name": "prod",
      "uri": "https://api.example.com",
      "hasCredentials": true,
      "createdAt": "2025-12-26T10:00:00Z",
      "modifiedAt": "2025-12-26T10:00:00Z"
    },
    "staging": {
      "name": "staging",
      "uri": "https://staging.example.com",
      "hasCredentials": true,
      "createdAt": "2025-12-26T11:00:00Z",
      "modifiedAt": "2025-12-26T11:00:00Z"
    }
  }
}
```

## Validation Rules

### Profile Name
- Pattern: `^[a-zA-Z0-9_-]+$`
- Min length: 1 character
- Max length: 64 characters
- Case-insensitive for lookups

### URI
- Must be valid URI format
- Must include scheme (http:// or https://)

## Error Handling

### Invalid Profile Name
```csharp
throw new ArgumentException(
    $"Invalid profile name '{name}'. Names may only contain letters, numbers, hyphens, and underscores.");
```

### Corrupted Configuration File
```csharp
// 1. Rename corrupted file to profiles.json.bak
// 2. Create fresh empty configuration
// 3. Log warning to ILogger
// 4. Return empty profile list (not an exception)
```

## Implementation Notes

- Thread-safe for concurrent access
- Uses file locking for writes
- Lazy-loads configuration on first access
- Caches configuration in memory, invalidates on file change
