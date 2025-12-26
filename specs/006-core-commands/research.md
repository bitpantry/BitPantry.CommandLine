# Research: Core CLI Commands & Prompt Redesign

**Feature**: 006-core-commands  
**Date**: 2025-12-26

## Research Topics

### 1. Cross-Platform Credential Storage

**Decision**: Use DPAPI on Windows, libsodium on Linux/macOS with encrypted file fallback

**Rationale**:
- DPAPI (`System.Security.Cryptography.ProtectedData`) is the standard Windows credential protection used by Chrome, Azure CLI, and other major tools
- Encrypts to current user scope - no password required
- libsodium via `Sodium.Core` NuGet package provides robust cross-platform encryption
- Fallback to encrypted file when OS credential store unavailable

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Windows Credential Manager directly | More complex API, DPAPI achieves same security |
| User-provided password | Poor UX - requires password on every operation |
| Plaintext with warning | Security risk for API keys |
| Azure Key Vault / AWS Secrets Manager | Overkill for local CLI tool, requires cloud dependency |

**Implementation Notes**:
```csharp
// Windows - DPAPI
byte[] encrypted = ProtectedData.Protect(
    Encoding.UTF8.GetBytes(apiKey),
    null,  // optional entropy
    DataProtectionScope.CurrentUser);

// Linux/macOS - libsodium
// Use machine ID + user ID as key derivation input
```

**NuGet Packages**:
- `System.Security.Cryptography.ProtectedData` (already in .NET, Windows-only)
- `Sodium.Core` (cross-platform libsodium bindings)

---

### 2. Profile Storage Location

**Decision**: Use `Environment.SpecialFolder.ApplicationData` with BitPantry subdirectory

**Rationale**:
- Cross-platform standard for user configuration
- Follows XDG Base Directory Specification on Linux
- Consistent with how other CLI tools store configuration

**Platform-Specific Paths**:
| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\BitPantry\CommandLine\` |
| Linux | `~/.config/bitpantry-commandline/` |
| macOS | `~/.config/bitpantry-commandline/` |

**Implementation**:
```csharp
public static string GetConfigDirectory()
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
```

---

### 3. Assembly Version Extraction

**Decision**: Use `AssemblyInformationalVersionAttribute` → `AssemblyVersion` → `0.0.0` fallback chain

**Rationale**:
- `AssemblyInformationalVersionAttribute` contains the full semantic version including prerelease tags (e.g., `1.2.3-beta.1`)
- `AssemblyVersion` is always present but may not include prerelease info
- Fallback to `0.0.0` handles edge cases where neither is set

**Implementation**:
```csharp
public static string GetVersion(Assembly assembly)
{
    // Try informational version first (includes prerelease)
    var infoVersion = assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion;
    
    if (!string.IsNullOrEmpty(infoVersion))
    {
        // Strip SourceLink metadata (everything after +)
        var plusIndex = infoVersion.IndexOf('+');
        return plusIndex > 0 ? infoVersion[..plusIndex] : infoVersion;
    }
    
    // Fall back to assembly version
    var version = assembly.GetName().Version;
    return version?.ToString() ?? "0.0.0";
}
```

**Framework Assembly Discovery**:
```csharp
var frameworkAssemblies = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => a.GetName().Name?.StartsWith("BitPantry.CommandLine") == true)
    .OrderBy(a => a.GetName().Name);
```

---

### 4. Prompt Segment Architecture

**Decision**: Composite pattern with `IPromptSegment` interface and `CompositePrompt` aggregator

**Rationale**:
- Allows packages to contribute prompt content without coupling
- Segments pull state from injected dependencies at render time
- Null return from segment = skip (no visual artifact)
- Order property controls segment positioning

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Event-based (segments push updates) | Complex, state synchronization issues |
| Single prompt class with optional fields | Tight coupling, doesn't scale to extensions |
| String template with tokens | Current approach - requires manual updates |

**Design**:
```csharp
public interface IPromptSegment
{
    int Order { get; }
    string? Render();
}

public interface IPrompt
{
    string Render();
}

public class CompositePrompt : IPrompt
{
    private readonly IEnumerable<IPromptSegment> _segments;
    
    public CompositePrompt(IEnumerable<IPromptSegment> segments)
    {
        _segments = segments.OrderBy(s => s.Order);
    }
    
    public string Render()
    {
        var parts = _segments
            .Select(s => s.Render())
            .Where(s => !string.IsNullOrEmpty(s));
        return string.Join(" ", parts) + "> ";
    }
}
```

**Segment Order Convention**:
| Order Range | Purpose | Package |
|-------------|---------|---------|
| 0-99 | Core (app name) | Core |
| 100-199 | Connection state | SignalR Client |
| 200-299 | Session state | Future |
| 300+ | User/custom | Extension |

---

### 5. Profile Name Autocomplete

**Decision**: Create `ProfileNameProvider : ICompletionProvider` in SignalR Client package

**Rationale**:
- Follows existing `ICompletionProvider` pattern
- Profile names read from `ProfileManager` at completion time
- Filters suggestions based on typed prefix

**Implementation**:
```csharp
public class ProfileNameProvider : ICompletionProvider
{
    private readonly ProfileManager _profileManager;
    
    public int Priority => 80; // Higher than static values
    
    public bool CanHandle(CompletionContext context)
    {
        // Handle arguments with [Completion(Provider = typeof(ProfileNameProvider))]
        return context.ElementType == CompletionElementType.ArgumentValue
            && context.CompletionAttribute?.Provider == typeof(ProfileNameProvider);
    }
    
    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, CancellationToken ct)
    {
        var profiles = await _profileManager.GetAllProfilesAsync();
        var prefix = context.CurrentWord ?? "";
        
        var items = profiles
            .Where(p => p.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(p => new CompletionItem
            {
                DisplayText = p.Name,
                InsertText = p.Name,
                Kind = CompletionItemKind.ArgumentValue
            })
            .ToList();
            
        return new CompletionResult(items);
    }
}
```

---

### 6. Existing Command Migration

**Decision**: Clean break - delete existing `ConnectCommand`/`DisconnectCommand` and replace

**Rationale**:
- Existing `--confirmDisconnect` flag has confusing semantics (skips confirmation)
- New design follows industry CLI best practices
- Pre-1.0 framework - breaking changes expected
- Document in release notes

**Files to Delete**:
- `BitPantry.CommandLine.Remote.SignalR.Client/ConnectCommand.cs` (existing)
- `BitPantry.CommandLine.Remote.SignalR.Client/DisconnectCommand.cs` (existing)

**Files to Create**:
- `BitPantry.CommandLine.Remote.SignalR.Client/ConnectCommand.cs` (new implementation)
- `BitPantry.CommandLine.Remote.SignalR.Client/DisconnectCommand.cs` (new implementation)
- `BitPantry.CommandLine.Remote.SignalR.Client/StatusCommand.cs` (new)

---

### 7. Testing Strategy

**Decision**: Follow existing test patterns with unit tests for services, integration tests for commands

**Unit Tests** (MSTest + FluentAssertions + Moq):
- `ProfileManagerTests` - CRUD operations, file I/O mocking
- `CredentialStoreTests` - Encryption/decryption, cross-platform
- `VersionCommandTests` - Version extraction, output format
- `CompositePromptTests` - Segment ordering, null handling
- `ProfileNameProviderTests` - Autocomplete filtering

**Integration Tests** (using existing `TestEnvironment`):
- `IntegrationTests_Connect` - Connection flows, error cases
- `IntegrationTests_Disconnect` - Disconnection, state cleanup
- `IntegrationTests_Status` - Status output, JSON format
- `IntegrationTests_Profiles` - Profile CRUD via commands

**Mocking Strategy**:
- Use `System.IO.Abstractions` for file operations (already in codebase)
- Mock `ICredentialStore` for profile tests
- Use existing `TestServer` / `TestHttpClientFactory` for server tests

---

### 8. Documentation Updates

**Decision**: Update existing docs, create new Remote section

**Files to Modify**:
- `Docs/CommandLine/BuiltInCommands.md` - Remove `lc`, add `version`

**Files to Create**:
- `Docs/Remote/ServerCommands.md` - Document `server connect/disconnect/status`
- `Docs/Remote/ProfileManagement.md` - Document `server profile` subcommands

---

## Summary of Dependencies

| Package | Purpose | Platform |
|---------|---------|----------|
| `System.Security.Cryptography.ProtectedData` | DPAPI encryption | Windows |
| `Sodium.Core` | libsodium encryption | All |
| `System.IO.Abstractions` | File system mocking | All (existing) |
| `Spectre.Console` | CLI output | All (existing) |

All other dependencies already exist in the solution.
