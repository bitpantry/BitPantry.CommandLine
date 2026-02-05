# Quickstart: Server Profile Management

**Branch**: `009-server-profile` | **Date**: 2026-02-02 | **Phase**: 1

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- Git

## Development Setup

### 1. Clone and Checkout

```powershell
git checkout 009-server-profile
```

### 2. Build Solution

```powershell
dotnet build BitPantry.CommandLine.sln
```

### 3. Run Tests

```powershell
dotnet test BitPantry.CommandLine.Tests.Remote.SignalR
```

## Project Structure Overview

### Primary Implementation Files

| File | Purpose |
|------|---------|
| `Profiles/IProfileManager.cs` | Profile management interface |
| `Profiles/ProfileManager.cs` | JSON-based profile storage |
| `Profiles/ICredentialStore.cs` | Credential storage interface |
| `Profiles/CredentialStore.cs` | DPAPI/libsodium encryption |
| `Profiles/ServerProfile.cs` | Profile entity |
| `Commands/Server/Profile/ProfileGroup.cs` | CLI command group |
| `Commands/Server/Profile/*Command.cs` | Individual CLI commands |

### Test Files

| File | Purpose |
|------|---------|
| `ProfileTests/ProfileManagerTests.cs` | Unit tests for ProfileManager |
| `ProfileTests/CredentialStoreTests.cs` | Unit tests for CredentialStore |
| `ProfileTests/ProfileCommandTests.cs` | Command integration tests |

## Quick Implementation Guide

### Step 1: Create Core Entities

Create `Profiles/ServerProfile.cs`:

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

public class ServerProfile
{
    public required string Name { get; set; }
    public required string Uri { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}
```

### Step 2: Create Interfaces

Create `Profiles/IProfileManager.cs`:

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.Profiles;

public interface IProfileManager
{
    Task<IReadOnlyList<ServerProfile>> GetAllProfilesAsync(CancellationToken ct = default);
    Task<ServerProfile?> GetProfileAsync(string name, CancellationToken ct = default);
    Task CreateProfileAsync(ServerProfile profile, CancellationToken ct = default);  // Throws if exists
    Task UpdateProfileAsync(ServerProfile profile, CancellationToken ct = default);  // Throws if doesn't exist
    Task<bool> DeleteProfileAsync(string name, CancellationToken ct = default);
    Task<string?> GetDefaultProfileNameAsync(CancellationToken ct = default);
    Task SetDefaultProfileAsync(string? name, CancellationToken ct = default);
}
```

### Step 3: Write Tests First (TDD)

Create test file `ProfileTests/ProfileManagerTests.cs`:

```csharp
using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ProfileTests;

[TestClass]
public class ProfileManagerTests
{
    private MockFileSystem _fileSystem;
    private ProfileManager _sut;

    [TestInitialize]
    public void Setup()
    {
        _fileSystem = new MockFileSystem();
        _sut = new ProfileManager(_fileSystem);
    }

    [TestMethod]
    public async Task GetAllProfiles_EmptyStore_ReturnsEmptyList()
    {
        var profiles = await _sut.GetAllProfilesAsync();
        profiles.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CreateProfile_NewProfile_CanBeRetrieved()
    {
        var profile = new ServerProfile { Name = "test", Uri = "https://example.com" };
        
        await _sut.CreateProfileAsync(profile);
        var retrieved = await _sut.GetProfileAsync("test");
        
        retrieved.Should().NotBeNull();
        retrieved!.Uri.Should().Be("https://example.com");
    }
}
```

### Step 4: Implement ProfileManager

Implement to make tests pass.

### Step 5: Add DI Registration

In `CommandLineApplicationBuilderExtensions.cs` or similar:

```csharp
services.AddSingleton<IProfileManager, ProfileManager>();
services.AddSingleton<ICredentialStore, CredentialStore>();
```

## Testing Strategy

### Unit Tests

- Mock `IFileSystem` for file operations
- Mock `ICredentialStore` when testing `ProfileManager`
- Test encryption/decryption with known test vectors

### Integration Tests

- End-to-end command execution tests
- Profile + connect workflow tests
- Cross-platform credential tests (CI matrix)

## Common Tasks

### Adding a New Command

1. Create command class in `Commands/Server/Profile/`
2. Use `[Command]` attribute with proper name
3. Inject required services via constructor
4. Write tests first (TDD)

### Testing Encryption

Windows tests use real DPAPI (current user scope).

Linux/macOS tests require libsodium:

```bash
# Ubuntu/Debian
sudo apt-get install libsodium-dev

# macOS
brew install libsodium
```

## Debugging Tips

### View Profile Storage

```powershell
# Windows
cat $env:USERPROFILE\.bitpantry\commandline\profiles\profiles.json | ConvertFrom-Json

# Linux/macOS
cat ~/.bitpantry/commandline/profiles/profiles.json | jq
```

### Clear All Profiles

```powershell
# Windows
Remove-Item -Recurse $env:USERPROFILE\.bitpantry\commandline\profiles

# Linux/macOS
rm -rf ~/.bitpantry/commandline/profiles
```

## Related Documentation

- [spec.md](spec.md) - Feature specification
- [research.md](research.md) - Technology decisions
- [data-model.md](data-model.md) - Entity definitions
- [test-cases.md](test-cases.md) - Comprehensive test cases
