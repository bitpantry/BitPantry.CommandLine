# Quickstart: Release Workflow Guide

**Branch**: `007-flex-version-mgmt` | **Date**: 2026-01-01

## Overview

This guide explains how to release packages in the BitPantry.CommandLine solution after the flexible version management feature is implemented.

## Prerequisites

1. All changes committed to the repository
2. On a branch that can push tags (typically `main` or feature branch merged to main)
3. NuGet API key configured in GitHub Secrets (`NUGET_API_KEY`)

## Release Process

### Option A: Using the Release Agent (Recommended)

```bash
# In VS Code with Claude, run the release agent command
/speckit.bp.release
```

The agent will:
1. Analyze changes since last release
2. Propose version bumps for affected packages
3. Display a release plan table for your review
4. Upon confirmation: update versions, commit, push trigger tag

### Option B: Manual Release

If the agent is unavailable, you can trigger releases manually:

#### Step 1: Update Versions

Edit version numbers in the appropriate files:

**For package versions** (in .csproj):
```xml
<!-- BitPantry.CommandLine/BitPantry.CommandLine.csproj -->
<Version>5.3.0</Version>
```

**For dependency ranges** (in Directory.Packages.props, only for breaking changes):
```xml
<PackageVersion Include="BitPantry.CommandLine" Version="[6.0.0, 7.0.0)" />
```

#### Step 2: Commit Changes

```bash
git add -A
git commit -m "Release: Core 5.3.0"
```

#### Step 3: Create and Push Trigger Tag

```bash
# Create trigger tag with timestamp
git tag release-v$(date +%Y%m%d-%H%M%S)

# Push commit and tag
git push origin HEAD --tags
```

#### Step 4: Monitor Workflow

1. Go to GitHub Actions: `https://github.com/bitpantry/BitPantry.CommandLine/actions`
2. Find the "Unified Package Release" workflow run
3. Monitor job progress (Core → SignalR → Client/Server)

## Common Scenarios

### Scenario 1: Core-Only Patch Release

**Situation**: Bug fix in Core, no breaking changes

**Steps**:
1. Run `/speckit.bp.release`
2. Agent proposes: Core 5.2.0 → 5.2.1 (patch)
3. Confirm the plan
4. Workflow publishes Core; skips all downstream packages

**Result**: Downstream packages automatically use new Core version (version range `[5.0.0, 6.0.0)` accepts 5.2.1)

### Scenario 2: Core Minor Release

**Situation**: New feature in Core, backward compatible

**Steps**:
1. Run `/speckit.bp.release`
2. Agent proposes: Core 5.2.0 → 5.3.0 (minor)
3. Confirm the plan
4. Workflow publishes Core; skips all downstream packages

**Result**: Same as patch—downstream packages accept the new version

### Scenario 3: Breaking Change Cascade

**Situation**: Breaking change in Core requiring major version bump

**Steps**:
1. Run `/speckit.bp.release`
2. Agent detects breaking change and proposes:
   - Core: 5.2.0 → 6.0.0 (major)
   - SignalR: 1.2.1 → 1.3.0 (minor - cascade)
   - Client: 1.2.1 → 1.3.0 (minor - cascade)
   - Server: 1.2.1 → 1.3.0 (minor - cascade)
3. Agent shows "Cascade Impact" visualization
4. Confirm the plan
5. Workflow publishes all packages in order with NuGet polling

**Result**: All packages released with updated version ranges

### Scenario 4: SignalR-Only Update

**Situation**: Change only in SignalR package

**Steps**:
1. Run `/speckit.bp.release`
2. Agent proposes: SignalR 1.2.1 → 1.2.2 (patch)
3. Confirm the plan
4. Workflow: Core skipped, SignalR published, Client/Server skipped

**Result**: Only SignalR is released

## Verification Procedures

### Verify Core-Only Release (US1)

After a Core minor/patch release, verify downstream packages automatically accept the new version:

```powershell
# 1. Verify Core was published
$coreVersion = "5.3.0"  # Replace with actual version
$url = "https://api.nuget.org/v3-flatcontainer/bitpantry.commandline/$coreVersion/bitpantry.commandline.$coreVersion.nupkg"
Invoke-WebRequest -Uri $url -Method Head

# 2. Verify downstream packages can restore with new Core version
cd BitPantry.CommandLine.Remote.SignalR
dotnet restore --force

# 3. Verify version range accepts new version (should show 5.3.0 in restored packages)
dotnet list package --include-transitive | Select-String "BitPantry.CommandLine"
```

**Expected**: SignalR, Client, and Server restore successfully without any version changes because their version range `[5.0.0, 6.0.0)` accepts the new Core version.

### Inspect .nupkg for Version Range Metadata (US1)

To verify version ranges are correctly embedded in published packages:

```powershell
# Download the .nupkg file
$packageName = "BitPantry.CommandLine.Remote.SignalR"
$version = "1.2.1"  # Replace with actual version
$url = "https://api.nuget.org/v3-flatcontainer/$($packageName.ToLower())/$version/$($packageName.ToLower()).$version.nupkg"
Invoke-WebRequest -Uri $url -OutFile "$packageName.$version.nupkg"

# Extract and inspect the .nuspec
Expand-Archive -Path "$packageName.$version.nupkg" -DestinationPath "nupkg-contents" -Force
Get-Content "nupkg-contents\$packageName.nuspec" | Select-String -Pattern "dependency"

# Expected output shows version ranges:
# <dependency id="BitPantry.CommandLine" version="[5.0.0, 6.0.0)" />

# Cleanup
Remove-Item -Path "nupkg-contents" -Recurse -Force
Remove-Item -Path "$packageName.$version.nupkg"
```

### Verify Minor/Patch Release Workflow (US1)

1. **Before Release**: Check current versions
   ```powershell
   # List current package versions from .csproj files
   Get-ChildItem -Recurse -Filter "*.csproj" | ForEach-Object {
       $content = Get-Content $_.FullName -Raw
       if ($content -match '<Version>([^<]+)</Version>') {
           Write-Host "$($_.Name): $($matches[1])"
       }
   }
   ```

2. **After Release**: Verify on NuGet
   ```powershell
   # Check all packages versions via NuGet API
   $packages = @(
       "bitpantry.commandline",
       "bitpantry.commandline.remote.signalr",
       "bitpantry.commandline.remote.signalr.client",
       "bitpantry.commandline.remote.signalr.server"
   )
   
   foreach ($pkg in $packages) {
       $versions = Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$pkg/index.json"
       Write-Host "$pkg : $($versions.versions[-1])"  # Latest version
   }
   ```

3. **Monitor GitHub Actions**: View the workflow run at:
   ```
   https://github.com/bitpantry/BitPantry.CommandLine/actions/workflows/release-unified.yml
   ```

### Verify Full Cascade Release (US7)

When a breaking change cascade occurs (e.g., Core major bump):

```powershell
# 1. Verify all packages were published with correct versions
$expectedVersions = @{
    "bitpantry.commandline" = "6.0.0"
    "bitpantry.commandline.remote.signalr" = "1.3.0"
    "bitpantry.commandline.remote.signalr.client" = "1.3.0"
    "bitpantry.commandline.remote.signalr.server" = "1.3.0"
}

foreach ($pkg in $expectedVersions.Keys) {
    $url = "https://api.nuget.org/v3-flatcontainer/$pkg/$($expectedVersions[$pkg])/$pkg.$($expectedVersions[$pkg]).nupkg"
    try {
        Invoke-WebRequest -Uri $url -Method Head
        Write-Host "✓ $pkg $($expectedVersions[$pkg]) - Published" -ForegroundColor Green
    } catch {
        Write-Host "✗ $pkg $($expectedVersions[$pkg]) - NOT FOUND" -ForegroundColor Red
    }
}

# 2. Verify version ranges were updated in Directory.Packages.props
Get-Content "Directory.Packages.props" | Select-String "PackageVersion"

# 3. Verify GitHub Release was created
# Navigate to: https://github.com/bitpantry/BitPantry.CommandLine/releases/latest
```

### Cascade Detection Logic

The unified workflow determines cascade requirements based on:

1. **Version Detection**: Each job reads its package's `<Version>` from .csproj
2. **NuGet Comparison**: Checks if that version exists on NuGet.org
3. **Skip Logic**: If version already published → skip the job
4. **Cascade Trigger**: Jobs only run if their dependencies completed successfully

**Cascade Matrix**:
| If Major Bump In | Cascade To |
|------------------|------------|
| Core (6.0.0) | SignalR, Client, Server |
| SignalR (2.0.0) | Client, Server |
| Client or Server | *(none - leaf packages)* |

See [data-model.md](data-model.md) for the complete dependency graph.

### Verify Major Version Bounds (US2)

Version ranges prevent automatic adoption of breaking changes. To verify:

```powershell
# 1. Check that a downstream package with [5.0.0, 6.0.0) does NOT accept Core 6.x
# Create a test scenario (do not commit):

# Temporarily modify Directory.Packages.props to simulate Core 6.0.0
$props = Get-Content "Directory.Packages.props" -Raw
$testProps = $props -replace '\[5\.0\.0, 6\.0\.0\)', '[6.0.0, 7.0.0)'
Set-Content "Directory.Packages.props" -Value $testProps

# Try to restore SignalR (should fail if Core 6.0.0 doesn't exist on NuGet)
cd BitPantry.CommandLine.Remote.SignalR
dotnet restore 2>&1 | Select-String "error"

# Restore original
cd ..
git checkout Directory.Packages.props
```

**Expected**: Restore fails because Core 6.0.0 doesn't exist, proving the version range blocks it.

### NuGet Version Resolution Behavior (US2)

NuGet uses the following resolution rules for version ranges:

| Range Notation | Meaning | Example |
|----------------|---------|---------|
| `[5.0.0, 6.0.0)` | >= 5.0.0 AND < 6.0.0 | Accepts 5.0.0, 5.2.1, 5.99.99 |
| `[5.0.0]` | Exactly 5.0.0 | Only 5.0.0 |
| `5.0.0` | >= 5.0.0 (no upper bound) | Accepts any 5.x, 6.x, 7.x... |

**Key behavior**:
- NuGet selects the **lowest applicable version** that satisfies all constraints
- With `[5.0.0, 6.0.0)`, if 5.0.0 and 5.2.1 are available, NuGet picks 5.2.1 (latest in range)
- If a consumer has `[5.0.0, 6.0.0)` and the producer publishes 6.0.0, restore succeeds but uses 5.2.1 (latest 5.x)

### Verify External Dependencies Use Exact Versions (US3)

```powershell
# 1. Check Directory.Packages.props for external dependencies
Get-Content "Directory.Packages.props" | Select-String "External Dependencies" -Context 0,20

# 2. Verify no ranges in external deps (should see exact versions like "9.0.1")
Get-Content "Directory.Packages.props" | Select-String '\[.*\)' | Where-Object {
    $_ -notmatch "BitPantry"  # Exclude internal deps
}

# Expected: No matches (external deps should not have ranges)
```

### Verify .nupkg Shows Exact External Versions (US3)

```powershell
# Download and inspect a package
$pkg = "bitpantry.commandline"
$version = "5.2.0"  # Replace with actual version
$url = "https://api.nuget.org/v3-flatcontainer/$pkg/$version/$pkg.$version.nupkg"

Invoke-WebRequest -Uri $url -OutFile "$pkg.$version.nupkg"
Expand-Archive -Path "$pkg.$version.nupkg" -DestinationPath "nupkg-extract" -Force

# Check dependencies in nuspec
Get-Content "nupkg-extract/$pkg.nuspec" | Select-String "dependency"

# Expected: External deps show exact versions (e.g., version="9.0.1")
# NOT ranges like version="[9.0.0, 10.0.0)"

# Cleanup
Remove-Item "nupkg-extract" -Recurse -Force
Remove-Item "$pkg.$version.nupkg"
```

### Verify Project References Work (US4)

With `UseProjectReferences=true` (default for local development):

```powershell
# 1. Verify UseProjectReferences setting
Get-Content "Directory.Build.props" | Select-String "UseProjectReferences"

# 2. Check that .csproj files use ProjectReference (not PackageReference) for internal deps
Get-Content "BitPantry.CommandLine.Remote.SignalR\BitPantry.CommandLine.Remote.SignalR.csproj" | Select-String "ProjectReference|PackageReference.*BitPantry"

# Expected: See ProjectReference to ..\BitPantry.CommandLine\BitPantry.CommandLine.csproj
```

### Verify Package References with Ranges (US4)

With `UseProjectReferences=false` (for package testing):

```powershell
# 1. Temporarily set UseProjectReferences to false
$content = Get-Content "Directory.Build.props" -Raw
$modified = $content -replace 'true</UseProjectReferences>', 'false</UseProjectReferences>'
Set-Content "Directory.Build.props" -Value $modified

# 2. Restore and check references
dotnet restore --force
dotnet list package

# Expected: See PackageReference with versions from NuGet

# 3. Restore original
git checkout Directory.Build.props
dotnet restore --force
```

### Verify Local Development Workflow (US4)

Changes to Core should immediately reflect in downstream packages during local development:

```powershell
# 1. Make a change to Core
$testFile = "BitPantry.CommandLine\TestChange.cs"
'namespace BitPantry.CommandLine { public class TestChange { public static string Value => "test"; } }' | Out-File $testFile

# 2. Build Core
dotnet build BitPantry.CommandLine\BitPantry.CommandLine.csproj

# 3. Use the new class from SignalR (add temporary usage)
# This verifies project reference provides immediate access

# 4. Build SignalR (should see the new class)
dotnet build BitPantry.CommandLine.Remote.SignalR\BitPantry.CommandLine.Remote.SignalR.csproj

# 5. Cleanup
Remove-Item $testFile
```

**Expected**: No need to publish Core to NuGet - local project reference provides immediate access.

### Verify Validation Failure Scenario (US5)

The unified workflow validates internal dependencies before publishing. To verify this behavior:

```powershell
# Simulate a missing dependency scenario (DO NOT COMMIT)

# 1. Temporarily set an invalid version range
$props = Get-Content "Directory.Packages.props" -Raw
$testProps = $props -replace '\[5\.0\.0, 6\.0\.0\)', '[99.0.0, 100.0.0)'
Set-Content "Directory.Packages.props" -Value $testProps

# 2. Run a local dotnet restore to see the failure message
cd BitPantry.CommandLine.Remote.SignalR
dotnet restore 2>&1

# Expected error message:
# "error NU1102: Unable to find package BitPantry.CommandLine with version (>= 99.0.0)"

# 3. Restore original
cd ..
git checkout Directory.Packages.props
```

**Workflow behavior**: The unified workflow includes a "Validate internal dependencies exist on NuGet" step that checks if packages satisfying the version ranges exist before attempting to build. If validation fails, you'll see:
```
❌ VALIDATION FAILED: No BitPantry.CommandLine version satisfying [99.0.0, 100.0.0) found on NuGet
```

## Troubleshooting

### Workflow Fails: "Timeout waiting for dependency"

**Cause**: NuGet indexing took longer than 15 minutes

**Resolution**:
1. Wait 5-10 minutes for NuGet to fully index
2. Re-trigger the workflow:
   ```bash
   git tag release-v$(date +%Y%m%d-%H%M%S)-retry
   git push origin --tags
   ```
3. Workflow will detect already-published packages and skip them

### Workflow Fails: Build Error

**Cause**: Code doesn't compile in Release configuration

**Resolution**:
1. Fix the build error locally
2. Commit the fix
3. Re-trigger:
   ```bash
   git tag release-v$(date +%Y%m%d-%H%M%S)-fix
   git push origin HEAD --tags
   ```

### Agent Fails Mid-Execution

**Cause**: Agent interrupted after updating files but before commit

**Resolution**:
1. Check local changes: `git status`
2. Review the changes: `git diff`
3. If changes look correct:
   ```bash
   git add -A
   git commit -m "Release: [describe versions]"
   git tag release-v$(date +%Y%m%d-%H%M%S)
   git push origin HEAD --tags
   ```
4. If changes look wrong:
   ```bash
   git checkout .
   ```

### Package Published but Not Showing on NuGet.org

**Cause**: NuGet UI cache delay

**Resolution**:
- API reflects changes in 1-5 minutes
- NuGet.org website UI may take up to 30 minutes
- Verify via API: `curl https://api.nuget.org/v3-flatcontainer/bitpantry.commandline/index.json | jq`

## Version Numbering Guidelines

| Change Type | Version Bump | Example | Downstream Impact |
|-------------|-------------|---------|-------------------|
| Bug fix | Patch | 5.2.0 → 5.2.1 | None - auto-accepted |
| New feature (backward compatible) | Minor | 5.2.0 → 5.3.0 | None - auto-accepted |
| Breaking change | Major | 5.2.0 → 6.0.0 | Cascade required |

## Directory.Packages.props Reference

After CPM migration, all versions are centralized:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <!-- Internal: Version Ranges -->
  <ItemGroup Label="Internal Dependencies">
    <PackageVersion Include="BitPantry.CommandLine" Version="[5.0.0, 6.0.0)" />
    <PackageVersion Include="BitPantry.CommandLine.Remote.SignalR" Version="[1.0.0, 2.0.0)" />
  </ItemGroup>
  
  <!-- External: Pinned Versions -->
  <ItemGroup Label="External Dependencies">
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.1" />
    <!-- ... other external packages ... -->
  </ItemGroup>
</Project>
```

## See Also

- [spec.md](spec.md) - Full feature specification
- [data-model.md](data-model.md) - Package dependency graph
- [contracts/release-unified-workflow.md](contracts/release-unified-workflow.md) - Workflow YAML reference
- `.claude/commands/speckit.bp.release.md` - Release agent command (after implementation)
