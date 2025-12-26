# Data Model: Core CLI Commands & Prompt Redesign

**Feature**: 006-core-commands  
**Date**: 2025-12-26

## Entities

### 1. ServerProfile

Represents a saved server connection profile.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Represents a saved server connection profile.
/// </summary>
public class ServerProfile
{
    /// <summary>
    /// Unique profile name (alphanumeric, hyphen, underscore only).
    /// Validation: ^[a-zA-Z0-9_-]+$
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Server URI (e.g., "https://api.example.com").
    /// </summary>
    public string Uri { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether credentials are stored for this profile.
    /// </summary>
    public bool HasCredentials { get; set; }
    
    /// <summary>
    /// Timestamp when profile was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Timestamp when profile was last modified.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; set; }
}
```

**Validation Rules**:
- `Name`: Required, must match `^[a-zA-Z0-9_-]+$`
- `Uri`: Required, must be valid URI format

---

### 2. ProfileConfiguration

Root object for profiles.json storage.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Root configuration object stored in profiles.json.
/// </summary>
public class ProfileConfiguration
{
    /// <summary>
    /// Name of the default profile, or null if none set.
    /// </summary>
    public string? DefaultProfile { get; set; }
    
    /// <summary>
    /// Dictionary of profiles keyed by name.
    /// </summary>
    public Dictionary<string, ServerProfile> Profiles { get; set; } = new();
}
```

**JSON Schema**:
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "defaultProfile": { "type": ["string", "null"] },
    "profiles": {
      "type": "object",
      "additionalProperties": {
        "type": "object",
        "properties": {
          "name": { "type": "string", "pattern": "^[a-zA-Z0-9_-]+$" },
          "uri": { "type": "string", "format": "uri" },
          "hasCredentials": { "type": "boolean" },
          "createdAt": { "type": "string", "format": "date-time" },
          "modifiedAt": { "type": "string", "format": "date-time" }
        },
        "required": ["name", "uri"]
      }
    }
  }
}
```

---

### 3. ConnectionStatus

Represents the current server connection status.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client;

/// <summary>
/// Represents the current server connection status.
/// </summary>
public class ConnectionStatus
{
    /// <summary>
    /// Whether currently connected to a server.
    /// </summary>
    public bool Connected { get; set; }
    
    /// <summary>
    /// The server URI if connected, null otherwise.
    /// </summary>
    public string? Server { get; set; }
    
    /// <summary>
    /// The profile name used for connection, if any.
    /// </summary>
    public string? Profile { get; set; }
    
    /// <summary>
    /// Timestamp when connection was established.
    /// </summary>
    public DateTimeOffset? ConnectedAt { get; set; }
}
```

---

## Interfaces

### 4. IPromptSegment

Interface for prompt segment contributions.

```csharp
namespace BitPantry.CommandLine.Input;

/// <summary>
/// Contributes a segment to the composite prompt.
/// </summary>
public interface IPromptSegment
{
    /// <summary>
    /// Order in which this segment appears. Lower values appear first.
    /// </summary>
    /// <remarks>
    /// Convention:
    /// - 0-99: Core (app name)
    /// - 100-199: Connection state
    /// - 200-299: Session state
    /// - 300+: Custom/user
    /// </remarks>
    int Order { get; }
    
    /// <summary>
    /// Renders this segment's content.
    /// </summary>
    /// <returns>
    /// The segment text, or null to skip this segment.
    /// Segment owns its complete output including decorators (e.g., "@", "[]").
    /// </returns>
    string? Render();
}
```

---

### 5. IPrompt

Interface for complete prompt rendering.

```csharp
namespace BitPantry.CommandLine.Input;

/// <summary>
/// Renders the complete REPL prompt.
/// </summary>
public interface IPrompt
{
    /// <summary>
    /// Renders the complete prompt string.
    /// </summary>
    /// <returns>The prompt string including suffix (e.g., "> ").</returns>
    string Render();
    
    /// <summary>
    /// Gets the length of the rendered prompt (for cursor positioning).
    /// </summary>
    int GetPromptLength();
    
    /// <summary>
    /// Writes the prompt to the console.
    /// </summary>
    void Write(IAnsiConsole console);
}
```

---

### 6. ICredentialStore

Interface for secure credential storage.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Provides secure storage for profile credentials.
/// </summary>
public interface ICredentialStore
{
    /// <summary>
    /// Stores credentials for a profile.
    /// </summary>
    /// <param name="profileName">The profile name.</param>
    /// <param name="apiKey">The API key to store.</param>
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
    Task RemoveAsync(string profileName);
    
    /// <summary>
    /// Checks if credentials exist for a profile.
    /// </summary>
    /// <param name="profileName">The profile name.</param>
    Task<bool> ExistsAsync(string profileName);
}
```

---

### 7. IProfileManager

Interface for profile CRUD operations.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

/// <summary>
/// Manages server connection profiles.
/// </summary>
public interface IProfileManager
{
    /// <summary>
    /// Gets all saved profiles.
    /// </summary>
    Task<IReadOnlyList<ServerProfile>> GetAllProfilesAsync();
    
    /// <summary>
    /// Gets a profile by name.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <returns>The profile, or null if not found.</returns>
    Task<ServerProfile?> GetProfileAsync(string name);
    
    /// <summary>
    /// Creates or updates a profile.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    Task SaveProfileAsync(ServerProfile profile);
    
    /// <summary>
    /// Deletes a profile by name.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteProfileAsync(string name);
    
    /// <summary>
    /// Gets the default profile name.
    /// </summary>
    Task<string?> GetDefaultProfileAsync();
    
    /// <summary>
    /// Sets the default profile.
    /// </summary>
    /// <param name="name">The profile name to set as default, or null to clear.</param>
    Task SetDefaultProfileAsync(string? name);
    
    /// <summary>
    /// Validates a profile name.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool IsValidProfileName(string name);
}
```

---

## Implementations

### 8. CompositePrompt

Default `IPrompt` implementation aggregating segments.

```csharp
namespace BitPantry.CommandLine.Input;

/// <summary>
/// Default prompt implementation that aggregates registered segments.
/// </summary>
public class CompositePrompt : IPrompt
{
    private readonly IEnumerable<IPromptSegment> _segments;
    private const string Suffix = "> ";
    
    public CompositePrompt(IEnumerable<IPromptSegment> segments)
    {
        _segments = segments.OrderBy(s => s.Order);
    }
    
    public string Render()
    {
        var parts = _segments
            .Select(s => s.Render())
            .Where(s => !string.IsNullOrEmpty(s));
        
        var content = string.Join(" ", parts);
        return string.IsNullOrEmpty(content) ? Suffix : content + Suffix;
    }
    
    public int GetPromptLength() => new Spectre.Console.Text(Render()).Length;
    
    public void Write(IAnsiConsole console) => console.Markup(Render());
}
```

---

### 9. AppNameSegment

Core segment for application name.

```csharp
namespace BitPantry.CommandLine.Input;

/// <summary>
/// Prompt segment displaying the application name.
/// </summary>
public class AppNameSegment : IPromptSegment
{
    private readonly string _appName;
    
    public int Order => 0;
    
    public AppNameSegment(string? appName = null)
    {
        _appName = appName ?? GetDefaultAppName();
    }
    
    public string? Render() => _appName;
    
    private static string GetDefaultAppName()
    {
        var assembly = Assembly.GetEntryAssembly();
        return assembly?.GetName().Name?.ToLowerInvariant() ?? "cli";
    }
}
```

---

### 10. ServerConnectionSegment

SignalR Client segment for server hostname.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Prompt;

/// <summary>
/// Prompt segment displaying the connected server hostname.
/// </summary>
public class ServerConnectionSegment : IPromptSegment
{
    private readonly IServerProxy _serverProxy;
    
    public int Order => 100;
    
    public ServerConnectionSegment(IServerProxy serverProxy)
    {
        _serverProxy = serverProxy;
    }
    
    public string? Render()
    {
        if (_serverProxy.ConnectionState != ServerProxyConnectionState.Connected)
            return null;
        
        return $"@{_serverProxy.ConnectionUri?.Host}";
    }
}
```

---

### 11. ProfileSegment

SignalR Client segment for profile name.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Prompt;

/// <summary>
/// Prompt segment displaying the connected profile name.
/// </summary>
public class ProfileSegment : IPromptSegment
{
    private readonly IProfileManager _profileManager;
    private readonly IServerProxy _serverProxy;
    
    // Track which profile was used for current connection
    private string? _currentProfile;
    
    public int Order => 110;
    
    public ProfileSegment(IProfileManager profileManager, IServerProxy serverProxy)
    {
        _profileManager = profileManager;
        _serverProxy = serverProxy;
    }
    
    public void SetCurrentProfile(string? profileName)
    {
        _currentProfile = profileName;
    }
    
    public string? Render()
    {
        if (_serverProxy.ConnectionState != ServerProxyConnectionState.Connected)
            return null;
        
        if (string.IsNullOrEmpty(_currentProfile))
            return null;
        
        return $"[{_currentProfile}]";
    }
}
```

---

## Command Classes

### 12. VersionCommand

```csharp
namespace BitPantry.CommandLine.Commands;

[Command(Name = "version")]
[Description("Displays the application version")]
public class VersionCommand : CommandBase
{
    [Argument]
    [Alias('f')]
    [Description("Include framework assembly versions")]
    public Option Full { get; set; }
    
    public void Execute(CommandExecutionContext ctx)
    {
        var version = GetExecutingAssemblyVersion();
        Console.WriteLine(version);
        
        if (Full.IsPresent)
        {
            foreach (var assembly in GetFrameworkAssemblies())
            {
                Console.WriteLine($"{assembly.Name} {assembly.Version}");
            }
        }
    }
}
```

---

## Relationships

```
ProfileConfiguration
    └── profiles: Dictionary<string, ServerProfile>
           └── ServerProfile
                  ├── name ←→ ICredentialStore (key)
                  └── uri

IPrompt
    └── CompositePrompt
           └── segments: IEnumerable<IPromptSegment>
                  ├── AppNameSegment (Order: 0)
                  ├── ServerConnectionSegment (Order: 100)
                  └── ProfileSegment (Order: 110)

IServerProxy
    ├──→ ServerConnectionSegment (dependency)
    └──→ ProfileSegment (dependency)

IProfileManager
    ├──→ ProfileSegment (dependency)
    └──→ ProfileNameProvider (dependency for autocomplete)
```

---

## State Transitions

### Connection State

```
Disconnected ─── server connect ──→ Connecting ──→ Connected
     ↑                                                   │
     │                                                   │
     └─────────── server disconnect ─────────────────────┘
                  (or connection lost)
```

### Profile Lifecycle

```
[Not Exists] ─── profile add ──→ [Exists]
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
              profile show     profile set-key   profile set-default
                    │                │                │
                    └────────────────┴────────────────┘
                                     │
                             profile remove
                                     │
                                     ↓
                              [Not Exists]
```
