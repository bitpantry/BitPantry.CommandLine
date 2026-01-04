# Data Model: Package Dependency Graph

**Branch**: `007-flex-version-mgmt` | **Date**: 2026-01-01

## Package Inventory

| Package ID | NuGet Name | Current Version | Type |
|------------|------------|-----------------|------|
| Core | BitPantry.CommandLine | 5.2.0 | Root |
| SignalR | BitPantry.CommandLine.Remote.SignalR | 1.2.1 | Intermediate |
| Client | BitPantry.CommandLine.Remote.SignalR.Client | 1.2.1 | Leaf |
| Server | BitPantry.CommandLine.Remote.SignalR.Server | 1.2.1 | Leaf |

## Dependency Graph

```
                    ┌──────────────────────┐
                    │        Core          │
                    │ BitPantry.CommandLine│
                    │      (5.2.0)         │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │       SignalR        │
                    │ ...Remote.SignalR    │
                    │      (1.2.1)         │
                    └──────────┬───────────┘
                               │
              ┌────────────────┴────────────────┐
              ▼                                 ▼
   ┌──────────────────────┐         ┌──────────────────────┐
   │        Client        │         │        Server        │
   │ ...SignalR.Client    │         │ ...SignalR.Server    │
   │      (1.2.1)         │         │      (1.2.1)         │
   └──────────────────────┘         └──────────────────────┘
```

## Publishing Order

Packages MUST be published in this order to satisfy dependencies:

1. **Core** (BitPantry.CommandLine) - No internal dependencies
2. **SignalR** (BitPantry.CommandLine.Remote.SignalR) - Depends on Core
3. **Client** (BitPantry.CommandLine.Remote.SignalR.Client) - Depends on SignalR (and transitively Core)
4. **Server** (BitPantry.CommandLine.Remote.SignalR.Server) - Depends on SignalR (and transitively Core)

**Note**: Client and Server can publish in parallel after SignalR is complete.

## Internal Dependencies (Version Ranges)

| Dependent Package | Depends On | Version Range |
|-------------------|------------|---------------|
| SignalR | Core | `[5.0.0, 6.0.0)` |
| Client | Core | `[5.0.0, 6.0.0)` |
| Client | SignalR | `[1.0.0, 2.0.0)` |
| Server | Core | `[5.0.0, 6.0.0)` |
| Server | SignalR | `[1.0.0, 2.0.0)` |

## External Dependencies (Pinned Versions)

These remain at exact versions in Directory.Packages.props:

| Package | Version | Used By |
|---------|---------|---------|
| BitPantry.Parsing.Strings | 2.0.1.6 | Core, SignalR |
| Microsoft.Extensions.DependencyInjection | 9.0.1 | Core |
| Microsoft.Extensions.DependencyInjection.Abstractions | 9.0.1 | Core, Client |
| Microsoft.Extensions.Logging | 9.0.1 | Core |
| Spectre.Console | 0.49.1 | Core |
| System.CodeDom | 6.0.0 | Core |
| System.Linq.Dynamic.Core | 1.6.0.1 | Core |
| TestableIO.System.IO.Abstractions.Wrappers | 21.2.1 | Core, Client |
| Sodium.Core | 1.4.0 | Client |
| System.IdentityModel.Tokens.Jwt | 8.4.0 | Client |
| Microsoft.AspNetCore.SignalR.Client | 9.0.1 | Client |
| System.Security.Cryptography.ProtectedData | 10.0.1 | Client |

## Breaking Change Cascade Matrix

When a package has a **major version bump**, all downstream packages require release:

| If This Has Major Bump | These Need Release |
|------------------------|-------------------|
| Core → 6.0.0 | SignalR, Client, Server |
| SignalR → 2.0.0 | Client, Server |
| Client → 2.0.0 | *(none - leaf package)* |
| Server → 2.0.0 | *(none - leaf package)* |

## Version Range Update Rules

When a breaking change cascade occurs:

| Original Range | After Major Bump | Example |
|----------------|------------------|---------|
| `[5.0.0, 6.0.0)` | `[6.0.0, 7.0.0)` | Core 5.x → 6.x |
| `[1.0.0, 2.0.0)` | `[2.0.0, 3.0.0)` | SignalR 1.x → 2.x |

## Directory.Packages.props Structure

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup Label="Internal Dependencies - Version Ranges">
    <PackageVersion Include="BitPantry.CommandLine" Version="[5.0.0, 6.0.0)" />
    <PackageVersion Include="BitPantry.CommandLine.Remote.SignalR" Version="[1.0.0, 2.0.0)" />
  </ItemGroup>
  
  <ItemGroup Label="External Dependencies - Pinned Versions">
    <PackageVersion Include="BitPantry.Parsing.Strings" Version="2.0.1.6" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.1" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.1" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="9.0.1" />
    <PackageVersion Include="Spectre.Console" Version="0.49.1" />
    <PackageVersion Include="System.CodeDom" Version="6.0.0" />
    <PackageVersion Include="System.Linq.Dynamic.Core" Version="1.6.0.1" />
    <PackageVersion Include="TestableIO.System.IO.Abstractions.Wrappers" Version="21.2.1" />
    <PackageVersion Include="Sodium.Core" Version="1.4.0" />
    <PackageVersion Include="System.IdentityModel.Tokens.Jwt" Version="8.4.0" />
    <PackageVersion Include="Microsoft.AspNetCore.SignalR.Client" Version="9.0.1" />
    <PackageVersion Include="System.Security.Cryptography.ProtectedData" Version="10.0.1" />
  </ItemGroup>
</Project>
```

## Git Tag Pattern

| Tag Type | Pattern | Example |
|----------|---------|---------|
| Release Trigger | `release-v{timestamp}` | `release-v20260101-143052` |

**Deprecated Tags** (no longer created):
- `core-v*`, `client-v*`, `server-v*`, `remote-signalr-v*`

Version history is tracked via .csproj `<Version>` elements and git commit history.
