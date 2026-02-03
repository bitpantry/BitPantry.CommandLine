# Research: Server Profile Management

**Branch**: `009-server-profile` | **Date**: 2026-02-02 | **Phase**: 0

## Technology Decisions

### Encryption Strategy

#### Windows: DPAPI (Data Protection API)

**Decision**: Use `System.Security.Cryptography.ProtectedData` for Windows credential encryption.

**Rationale**:
- Built into .NET, no additional dependencies required
- Machine/user-bound encryption by design (aligns with FR-011, FR-012)
- Already proven in Windows credential management scenarios
- `DataProtectionScope.CurrentUser` ensures only the current user can decrypt

**Implementation**:
```csharp
using System.Security.Cryptography;

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

**NuGet**: `System.Security.Cryptography.ProtectedData` (already part of .NET 8.0 Windows targets)

#### Linux/macOS: libsodium via Sodium.Core

**Decision**: Use `Sodium.Core` NuGet package for libsodium-based encryption.

**Rationale**:
- Industry-standard cryptographic library with proven security
- SecretBox provides authenticated encryption (XSalsa20-Poly1305)
- Simple API reduces implementation errors
- Spec requires libsodium specifically (FR-014)

**Implementation**:
```csharp
using Sodium;

// Key derivation from machine identifier
byte[] key = GenericHash.Hash(MachineId.GetBytes(), null, 32);

// Encrypt
byte[] nonce = SecretBox.GenerateNonce();
byte[] cipher = SecretBox.Create(Encoding.UTF8.GetBytes(apiKey), nonce, key);
// Store: nonce + cipher

// Decrypt
byte[] cipher = storedData[24..];
byte[] nonce = storedData[..24];
byte[] decrypted = SecretBox.Open(cipher, nonce, key);
string apiKey = Encoding.UTF8.GetString(decrypted);
```

**NuGet**: `Sodium.Core` (version 1.3.7+)

**Native Library Bundling**: Sodium.Core bundles libsodium via its dependency on the `libsodium` NuGet package (~6.19MB). **No separate installation required** - native binaries are included for:

| Platform | Architecture | Native File |
|----------|-------------|-------------|
| Windows | x64, x86, arm64 | `libsodium.dll` |
| Linux | x64, arm, arm64 (glibc + musl) | `libsodium.so` |
| macOS | x64, arm64 (Apple Silicon) | `libsodium.dylib` |

> **Q: What about libsodium on macOS?** The NuGet package includes pre-compiled `libsodium.dylib` for both Intel (x64) and Apple Silicon (arm64). No Homebrew/system installation needed.

**Failure Mode**: If the native library fails to load (unsupported platform or missing VC++ Redistributable on Windows), a `DllNotFoundException` is thrown. This aligns with FR-017a - we catch this and provide a clear error message.

**Machine ID Sources** (for key derivation):
- Linux: `/etc/machine-id` or `/var/lib/dbus/machine-id`
- macOS: `IOPlatformUUID` via IOKit (fallback: `/etc/machine-id` if available)

### JSON Storage

**Decision**: Use `System.Text.Json` for profile metadata storage.

**Rationale**:
- Built-in to .NET 8.0, no additional dependencies
- Already used throughout the codebase (see ConcreteObjectSerializer.cs)
- Excellent performance and source-generator support available
- Native nullable reference type support

**Storage Location**: `~/.bitpantry/commandline/profiles/`
- `profiles.json`: Profile metadata (name, URI, default flag)
- `credentials.enc`: Encrypted credentials (binary)

### File System Abstraction

**Decision**: Use `System.IO.Abstractions` for file system operations.

**Rationale**:
- Already a dependency in the client project (`TestableIO.System.IO.Abstractions.Wrappers`)
- Enables comprehensive unit testing without touching disk
- Test project already has `TestableIO.System.IO.Abstractions.TestingHelpers`

**SandboxedFileSystem vs MockFileSystem Clarification**:

> **Q: Are TestingHelpers constrained to the server's sandbox?**
> 
> **No.** `MockFileSystem` (from TestingHelpers) and `SandboxedFileSystem` are completely separate:
> 
> - **`MockFileSystem`**: An in-memory file system for unit testing. Used by both client and server tests. It's not sandboxed - you can create files anywhere in the mock.
> - **`SandboxedFileSystem`**: A **server-side only** wrapper that confines operations to `StorageRootPath`. It wraps a real `IFileSystem` (or `MockFileSystem` in tests) and validates paths.
> 
> For profile management tests, we inject `MockFileSystem` directly into `ProfileManager` - no sandbox involvement. The profile system operates on the **client's local file system** (user's home directory), not the server's sandboxed storage.

**Test Strategy**: Profile tests use `MockFileSystem` to simulate `~/.bitpantry/commandline/profiles/` without touching disk.

### Autocomplete Handler

**Decision**: Implement `IAutoCompleteHandler` for profile name suggestions.

**Rationale**:
- Existing pattern used throughout codebase (EnvironmentHandler, FilePathHandler, etc.)
- Spec requires autocomplete support (FR-008.1)
- Simple implementation based on `IProfileManager.GetAllProfilesAsync()`

### Platform Detection

**Decision**: Use `RuntimeInformation.IsOSPlatform()` for platform-specific encryption selection.

**Rationale**:
- Built-in to .NET, no dependencies
- Standard pattern for cross-platform .NET applications
- Simple conditional at runtime

```csharp
bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
```

## Alternative Considered

### Alternative: Azure Key Vault / Cloud Storage

**Rejected**: Profiles are intentionally local and machine-bound per clarification. Cloud storage would enable portability which conflicts with security requirements.

### Alternative: ASP.NET Core Data Protection API

**Rejected**: Would add dependency on `Microsoft.AspNetCore.DataProtection`. While cross-platform, it's designed for web applications and adds complexity. Simpler to use DPAPI + libsodium directly.

### Alternative: Keyring/Keychain Integration

**Rejected**: Would require platform-specific native libraries (libsecret on Linux, Keychain on macOS, Windows Credential Manager). Adds complexity and potential deployment issues. DPAPI + libsodium is more portable across deployment scenarios.

## Dependency Matrix

| Component | Windows | Linux | macOS |
|-----------|---------|-------|-------|
| Encryption | ProtectedData (built-in) | Sodium.Core | Sodium.Core |
| JSON | System.Text.Json (built-in) | System.Text.Json (built-in) | System.Text.Json (built-in) |
| File System | System.IO.Abstractions | System.IO.Abstractions | System.IO.Abstractions |

## Package References to Add

```xml
<!-- BitPantry.CommandLine.Remote.SignalR.Client.csproj -->
<ItemGroup>
  <PackageReference Include="Sodium.Core" Version="1.3.7" />
</ItemGroup>
```

Note: `System.Security.Cryptography.ProtectedData` is included in `net8.0-windows` target. For cross-platform builds, it will need conditional inclusion or a runtime check.

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| libsodium native library not installed | FR-017a: Fail with clear error message including installation instructions |
| DPAPI unavailable on non-Windows | Platform detection prevents DPAPI calls on non-Windows |
| File corruption | Use atomic writes (write to temp, then rename) |
| Concurrent access | Single-user CLI, file locking not required initially |

## Bootstrapping Strategy

**Decision**: Register profile services in `ConfigureSignalRClient()` extension method.

**Rationale**: Follows existing SignalR client bootstrapping pattern.

**Implementation Location**: `CommandLineApplicationBuilderExtensions.cs` (existing file)

```csharp
public static CommandLineApplicationBuilder ConfigureSignalRClient(
    this CommandLineApplicationBuilder builder, 
    Action<SignalRClientOptions> optsAct = null)
{
    // ... existing registrations ...

    // Profile management services (NEW)
    builder.Services.AddSingleton<ICredentialStore, CredentialStore>();
    builder.Services.AddSingleton<IProfileManager, ProfileManager>();
    builder.Services.AddTransient<ProfileNameProvider>();

    // Profile commands (NEW - added alongside existing server commands)
    builder.RegisterCommand<ProfileAddCommand>();
    builder.RegisterCommand<ProfileListCommand>();
    builder.RegisterCommand<ProfileShowCommand>();
    builder.RegisterCommand<ProfileRemoveCommand>();
    builder.RegisterCommand<ProfileSetDefaultCommand>();
    builder.RegisterCommand<ProfileSetKeyCommand>();

    // ... existing command registrations ...
    builder.RegisterCommand<ConnectCommand>();  // existing
    // ...
}
```

**Key Points**:
- Services registered as **Singleton** (profile data is user-global, not scoped)
- Commands registered via `RegisterCommand<T>()` like existing `ConnectCommand`, `DisconnectCommand`, etc.
- `IFileSystem` already registered by the same method - profile services depend on it
- Registration order: services first (for DI resolution), then commands

## References

- [DPAPI Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)
- [libsodium Documentation](https://doc.libsodium.org/)
- [Sodium.Core NuGet](https://www.nuget.org/packages/Sodium.Core)
- [System.IO.Abstractions](https://github.com/TestableIO/System.IO.Abstractions)
