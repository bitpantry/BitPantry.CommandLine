# Implementation Plan: Server Profile Management

**Branch**: `009-server-profile` | **Date**: 2026-02-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-server-profile/spec.md`

## Summary

Implement a server profile management system that allows users to save, manage, and reuse remote server connection settings with secure credential storage. The system will use OS-native encryption (DPAPI on Windows, libsodium on Linux/macOS) for credential protection and integrate with the existing command infrastructure via nested command groups.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: System.Security.Cryptography.ProtectedData (DPAPI), Sodium.Core (libsodium), System.Text.Json  
**Storage**: JSON files for profile metadata, encrypted binary for credentials  
**Testing**: MSTest with FluentAssertions and Moq, System.IO.Abstractions.TestingHelpers  
**Target Platform**: Windows, Linux, macOS (cross-platform CLI)  
**Project Type**: Multi-project solution (extends BitPantry.CommandLine.Remote.SignalR.Client)  
**Performance Goals**: < 100ms for profile operations, < 100ms for autocomplete  
**Constraints**: Credentials machine-bound, no plaintext storage, Debug-only logging  
**Scale/Scope**: 100+ profiles supported, single-user local storage

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| **TDD (NON-NEGOTIABLE)** | ✅ PASS | Test cases defined for all commands, credential store, profile manager |
| **Dependency Injection** | ✅ PASS | IProfileManager, ICredentialStore interfaces with constructor injection |
| **Security by Design** | ✅ PASS | DPAPI/libsodium encryption, no plaintext, masked input, Debug-only logging |
| **Follow Existing Patterns** | ✅ PASS | Uses [Group]/[Command] attributes, IAutoCompleteHandler, existing project structure |
| **Integration Testing** | ✅ PASS | Integration tests for profile + connect flow across platforms |

## Project Structure

### Documentation (this feature)

```text
specs/009-server-profile/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── test-cases.md        # Phase 2 output
```

### Source Code (repository root)

```text
BitPantry.CommandLine.Remote.SignalR.Client/
├── Commands/
│   └── Server/
│       ├── ServerGroup.cs                    # EXISTING - parent group
│       ├── ConnectCommand.cs                 # MODIFY - add --profile support
│       └── Profile/                          # NEW - nested group
│           ├── ProfileGroup.cs               # NEW - [Group(Name = "profile")]
│           ├── ProfileAddCommand.cs          # NEW
│           ├── ProfileListCommand.cs         # NEW
│           ├── ProfileShowCommand.cs         # NEW
│           ├── ProfileRemoveCommand.cs       # NEW
│           ├── ProfileSetDefaultCommand.cs   # NEW
│           └── ProfileSetKeyCommand.cs       # NEW
├── Profiles/                                 # NEW - profile management
│   ├── ServerProfile.cs                      # NEW - entity
│   ├── ProfileConfiguration.cs               # NEW - storage model
│   ├── IProfileManager.cs                    # NEW - interface
│   ├── ProfileManager.cs                     # NEW - JSON storage impl
│   ├── ICredentialStore.cs                   # NEW - interface
│   └── CredentialStore.cs                    # NEW - DPAPI/libsodium impl
├── AutoComplete/                             # NEW
│   └── ProfileNameProvider.cs                # NEW - IAutoCompleteHandler impl
└── PromptSegments/                           # NEW
    └── ProfileSegment.cs                     # NEW - IPromptSegment impl

BitPantry.CommandLine.Tests.Remote.SignalR/
├── ProfileTests/                             # NEW
│   ├── ProfileManagerTests.cs                # NEW - unit tests
│   ├── CredentialStoreTests.cs               # NEW - unit tests
│   ├── ProfileCommandTests.cs                # NEW - command tests
│   └── ProfileIntegrationTests.cs            # NEW - E2E tests
```

**Structure Decision**: Extends existing `BitPantry.CommandLine.Remote.SignalR.Client` project with new `Profiles/` folder for core logic and nested `Commands/Server/Profile/` for command implementations. Follows established patterns from existing server commands.

## Technical Design

### Component Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Command Layer                               │
├─────────────────────────────────────────────────────────────────┤
│  ProfileAddCommand  │  ProfileListCommand  │  ProfileShowCommand │
│  ProfileRemoveCommand │ ProfileSetDefaultCommand │ ProfileSetKeyCommand │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Service Layer                               │
├─────────────────────────────────────────────────────────────────┤
│                      IProfileManager (public)                    │
│                              │                                   │
│                       ProfileManager                             │
│                      ┌───────┴───────┐                           │
│              JSON file ops    ICredentialStore (internal)        │
│                                      │                           │
│                               CredentialStore                    │
│                          (DPAPI/libsodium ops)                   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Storage Layer                               │
├─────────────────────────────────────────────────────────────────┤
│     profiles.json              │         credentials.enc         │
│   (profile metadata)           │       (encrypted API keys)      │
└─────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

**Single Public API**: Commands only inject `IProfileManager`. The credential store is an internal implementation detail.

```csharp
public interface IProfileManager
{
    Task<IReadOnlyList<ServerProfile>> GetAllProfilesAsync();
    Task<ServerProfile?> GetProfileAsync(string name);  // Returns profile with ApiKey populated
    Task CreateProfileAsync(ServerProfile profile);     // Throws if exists; stores profile.ApiKey if set
    Task UpdateProfileAsync(ServerProfile profile);     // Throws if doesn't exist; stores profile.ApiKey if set
    Task SetApiKeyAsync(string profileName, string apiKey);
    Task<bool> HasCredentialAsync(string name);
    Task<bool> DeleteProfileAsync(string name);         // Also removes credential
    Task<string?> GetDefaultProfileNameAsync();
    Task SetDefaultProfileAsync(string? name);
    Task<bool> ExistsAsync(string name);
}

// ServerProfile includes [JsonIgnore] ApiKey property - populated on retrieval, not persisted

// Internal - used only by ProfileManager
internal interface ICredentialStore
{
    Task StoreAsync(string profileName, string apiKey);
    Task<string?> RetrieveAsync(string profileName);
    Task RemoveAsync(string profileName);
    Task<bool> ExistsAsync(string profileName);
}
```

### DI Registration

Profile services are registered in `ConfigureSignalRClient()` alongside existing SignalR client services:

```csharp
// In CommandLineApplicationBuilderExtensions.cs (existing file)
// Added alongside existing registrations in ConfigureSignalRClient()

// Profile management services
services.AddSingleton<ICredentialStore, CredentialStore>();
services.AddSingleton<IProfileManager, ProfileManager>();
services.AddTransient<ProfileNameProvider>();

// Profile commands (after existing ConnectCommand, etc.)
builder.RegisterCommand<ProfileAddCommand>();
builder.RegisterCommand<ProfileListCommand>();
builder.RegisterCommand<ProfileShowCommand>();
builder.RegisterCommand<ProfileRemoveCommand>();
builder.RegisterCommand<ProfileSetDefaultCommand>();
builder.RegisterCommand<ProfileSetKeyCommand>();
```

### Platform Detection for Credential Store

```csharp
internal class CredentialStore : ICredentialStore
{
    private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    // Windows: System.Security.Cryptography.ProtectedData (DPAPI)
    // Linux/macOS: Sodium.SecretBox with machine-derived key
}
```

### ConnectCommand Integration

The existing `ConnectCommand` will be modified to accept `--profile` argument:

```csharp
[Argument]
[Alias('p')]
[AutoComplete<ProfileNameProvider>]
[Description("Profile name to use for connection")]
public string Profile { get; set; }

// In Execute():
// 1. If --profile specified, load from IProfileManager
// 2. If --uri also specified, override profile's URI
// 3. Retrieve API key from ICredentialStore
// 4. Connect using resolved settings
```

## Complexity Tracking

No constitution violations requiring justification.

## Phase 0 Deliverables

- [x] research.md with technology decisions

## Phase 1 Deliverables

- [x] data-model.md with entity definitions
- [x] quickstart.md with development setup

## Phase 2 Deliverables

- [x] test-cases.md with comprehensive test coverage
