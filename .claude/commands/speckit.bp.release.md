# BitPantry Release Agent

You are the release agent for BitPantry.CommandLine. Your job is to analyze changes, propose version bumps, and prepare releases for the NuGet packages in this solution.

## Package Dependency Graph

```
                    ┌──────────────────────┐
                    │        Core          │
                    │ BitPantry.CommandLine│
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │       SignalR        │
                    │ ...Remote.SignalR    │
                    └──────────┬───────────┘
                               │
              ┌────────────────┴────────────────┐
              ▼                                 ▼
   ┌──────────────────────┐         ┌──────────────────────┐
   │        Client        │         │        Server        │
   │ ...SignalR.Client    │         │ ...SignalR.Server    │
   └──────────────────────┘         └──────────────────────┘
```

**Publishing Order**: Core → SignalR → Client/Server (parallel)

## Package Manifest

| Package | .csproj Path | NuGet ID | Version Range Used By |
|---------|--------------|----------|----------------------|
| Core | BitPantry.CommandLine/BitPantry.CommandLine.csproj | BitPantry.CommandLine | `[X.0.0, X+1.0.0)` |
| SignalR | BitPantry.CommandLine.Remote.SignalR/BitPantry.CommandLine.Remote.SignalR.csproj | BitPantry.CommandLine.Remote.SignalR | `[X.0.0, X+1.0.0)` |
| Client | BitPantry.CommandLine.Remote.SignalR.Client/BitPantry.CommandLine.Remote.SignalR.Client.csproj | BitPantry.CommandLine.Remote.SignalR.Client | — (leaf) |
| Server | BitPantry.CommandLine.Remote.SignalR.Server/BitPantry.CommandLine.Remote.SignalR.Server.csproj | BitPantry.CommandLine.Remote.SignalR.Server | — (leaf) |

## Execution Steps

### Step 1: Gather Current Versions

Read the `<Version>` element from each .csproj file:

```powershell
# Get versions from all publishable packages
$packages = @(
    @{Name="Core"; Path="BitPantry.CommandLine/BitPantry.CommandLine.csproj"; NuGet="BitPantry.CommandLine"},
    @{Name="SignalR"; Path="BitPantry.CommandLine.Remote.SignalR/BitPantry.CommandLine.Remote.SignalR.csproj"; NuGet="BitPantry.CommandLine.Remote.SignalR"},
    @{Name="Client"; Path="BitPantry.CommandLine.Remote.SignalR.Client/BitPantry.CommandLine.Remote.SignalR.Client.csproj"; NuGet="BitPantry.CommandLine.Remote.SignalR.Client"},
    @{Name="Server"; Path="BitPantry.CommandLine.Remote.SignalR.Server/BitPantry.CommandLine.Remote.SignalR.Server.csproj"; NuGet="BitPantry.CommandLine.Remote.SignalR.Server"}
)

foreach ($pkg in $packages) {
    $content = Get-Content $pkg.Path -Raw
    if ($content -match '<Version>([^<]+)</Version>') {
        Write-Host "$($pkg.Name): $($matches[1])"
    }
}
```

### Step 2: Query NuGet for Published Versions

For each package, query NuGet to find the latest published version:

```powershell
# Query NuGet API for latest version
function Get-NuGetLatestVersion {
    param([string]$PackageId)
    $url = "https://api.nuget.org/v3-flatcontainer/$($PackageId.ToLower())/index.json"
    try {
        $response = Invoke-RestMethod -Uri $url -ErrorAction Stop
        return $response.versions[-1]  # Last is latest
    } catch {
        return $null  # Package not yet published
    }
}

# Example usage
Get-NuGetLatestVersion "BitPantry.CommandLine"
```

### Step 3: Detect Changes Since Last Release

For each package, analyze changes since the version was last set:

```powershell
# Find commits that changed a package directory since last version bump
function Get-ChangeSummary {
    param(
        [string]$PackageDir,
        [string]$CurrentVersion
    )
    
    # Find when the current version was set
    $versionCommit = git log --all --oneline -1 --grep="$CurrentVersion" -- "$PackageDir/*.csproj"
    
    if (-not $versionCommit) {
        # Fall back to checking when <Version> was last changed
        $versionCommit = git log --all --oneline -1 -S"<Version>$CurrentVersion</Version>" -- "$PackageDir/*.csproj"
    }
    
    if (-not $versionCommit) {
        Write-Host "No version commit found, analyzing all recent commits"
        $versionCommit = "HEAD~50"  # Analyze last 50 commits
    }
    
    $commitHash = ($versionCommit -split ' ')[0]
    
    # Get commits since version was set
    $commits = git log --oneline "$commitHash..HEAD" -- "$PackageDir"
    $commitCount = ($commits | Measure-Object -Line).Lines
    
    # Get file changes with stats
    $diffStats = git diff --stat "$commitHash..HEAD" -- "$PackageDir"
    
    return @{
        CommitCount = $commitCount
        Commits = $commits
        DiffStats = $diffStats
    }
}
```

### Step 4: Generate Rich Change Summary

For each changed package, provide:
- **Commit count**: Number of commits since last version
- **Commit messages**: List of commit messages (grouped by type if possible)
- **Files changed**: With insertions/deletions stats
- **Public API impact**: Flag files like `**/API/**`, `**/Commands/**`, interfaces

```powershell
# Identify public API changes
function Get-PublicApiChanges {
    param([string]$PackageDir, [string]$SinceCommit)
    
    $apiPatterns = @(
        "$PackageDir/API/*",
        "$PackageDir/Commands/*",
        "$PackageDir/I*.cs"  # Interfaces
    )
    
    $apiChanges = @()
    foreach ($pattern in $apiPatterns) {
        $changes = git diff --name-only "$SinceCommit..HEAD" -- $pattern
        if ($changes) {
            $apiChanges += $changes
        }
    }
    
    return $apiChanges
}
```

### Step 5: Propose Version Bumps

Based on the analysis, propose versions using semantic versioning:

| Change Type | Version Bump | Trigger |
|-------------|-------------|---------|
| Bug fixes only | Patch (X.Y.Z → X.Y.Z+1) | No API changes, commit messages indicate fixes |
| New features | Minor (X.Y.Z → X.Y+1.0) | New files added, no breaking changes |
| Breaking changes | Major (X.Y.Z → X+1.0.0) | API files changed, removed methods, signature changes |

### Step 6: Display Release Plan

Present the plan in a clear table format:

```
╔══════════════════════════════════════════════════════════════════════════════╗
║                           RELEASE PLAN                                        ║
╠══════════════════════════════════════════════════════════════════════════════╣
║ Package   │ Current │ Proposed │ Bump  │ Changes                              ║
╠═══════════╪═════════╪══════════╪═══════╪══════════════════════════════════════╣
║ Core      │ 5.2.0   │ 5.3.0    │ Minor │ 12 commits, 8 files (+145/-23)       ║
║ SignalR   │ 1.2.1   │ —        │ Skip  │ No changes                           ║
║ Client    │ 1.2.1   │ —        │ Skip  │ No changes                           ║
║ Server    │ 1.2.1   │ —        │ Skip  │ No changes                           ║
╚══════════════════════════════════════════════════════════════════════════════╝
```

For breaking changes, also show cascade impact:

```
⚠️  CASCADE IMPACT
├── Core 5.2.0 → 6.0.0 (BREAKING)
│   ├── SignalR 1.2.1 → 1.3.0 (cascade)
│   │   ├── Client 1.2.1 → 1.3.0 (cascade)
│   │   └── Server 1.2.1 → 1.3.0 (cascade)
│   └── Directory.Packages.props: [5.0.0, 6.0.0) → [6.0.0, 7.0.0)
```

### Step 7: Await User Confirmation

Present options to the user:
1. **Confirm** - Proceed with the proposed plan
2. **Modify** - Adjust version numbers before proceeding
3. **Cancel** - Abort the release

### Step 8: Execute Version Updates

If confirmed, update the files:

**Update .csproj files**:
```powershell
# Update version in .csproj
function Update-CsprojVersion {
    param([string]$Path, [string]$NewVersion)
    
    $content = Get-Content $Path -Raw
    $updated = $content -replace '<Version>[^<]+</Version>', "<Version>$NewVersion</Version>"
    Set-Content -Path $Path -Value $updated -NoNewline
}
```

**Update Directory.Packages.props** (for breaking changes only):
```powershell
# Update version range for internal dependency
function Update-VersionRange {
    param([string]$PackageId, [string]$NewMajor)
    
    $propsPath = "Directory.Packages.props"
    $content = Get-Content $propsPath -Raw
    
    $oldRange = "[$($NewMajor - 1).0.0, $NewMajor.0.0)"
    $newRange = "[$NewMajor.0.0, $($NewMajor + 1).0.0)"
    
    $updated = $content -replace [regex]::Escape("<PackageVersion Include=`"$PackageId`" Version=`"$oldRange`""), "<PackageVersion Include=`"$PackageId`" Version=`"$newRange`""
    Set-Content -Path $propsPath -Value $updated -NoNewline
}
```

### Step 9: Commit and Tag

Create atomic commit with descriptive message:

```powershell
# Stage all changes
git add -A

# Build commit message
$message = "Release: "
$versions = @()
# Add each changed package version
# Example: "Release: Core 5.3.0, SignalR 1.3.0"
$message += ($versions -join ", ")

# Commit
git commit -m $message

# Create trigger tag
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$tag = "release-v$timestamp"
git tag $tag
```

### Step 10: Push and Monitor

Push the commit and tag to trigger the workflow:

```powershell
# Push commit and tag together
git push origin HEAD --tags

# Output the Actions URL
$repoUrl = "https://github.com/bitpantry/BitPantry.CommandLine"
Write-Host ""
Write-Host "✓ Release triggered!" -ForegroundColor Green
Write-Host ""
Write-Host "Monitor the workflow at:"
Write-Host "$repoUrl/actions/workflows/release-unified.yml" -ForegroundColor Cyan
```

## Cascade Detection Logic

When a package has a **major version bump**, determine cascade requirements:

```powershell
function Get-CascadePackages {
    param([string]$PackageName, [string]$BumpType)
    
    if ($BumpType -ne "Major") {
        return @()  # No cascade for minor/patch
    }
    
    $cascadeMap = @{
        "Core" = @("SignalR", "Client", "Server")
        "SignalR" = @("Client", "Server")
        "Client" = @()
        "Server" = @()
    }
    
    return $cascadeMap[$PackageName]
}
```

**Cascade Rules**:
1. **Core major bump** → SignalR, Client, Server all need minor bump (cascade)
2. **SignalR major bump** → Client, Server need minor bump (cascade)
3. **Client/Server major bump** → No cascade (leaf packages)

**Version Range Updates**:
When a major bump triggers cascade, update Directory.Packages.props:
- `[5.0.0, 6.0.0)` → `[6.0.0, 7.0.0)` (Core)
- `[1.0.0, 2.0.0)` → `[2.0.0, 3.0.0)` (SignalR)

## NuGet Polling Mechanism

The unified workflow polls NuGet to wait for dependency availability:

| Parameter | Value |
|-----------|-------|
| Endpoint | `https://api.nuget.org/v3-flatcontainer/{package-id}/{version}/{package-id}.{version}.nupkg` |
| Method | HEAD request (checks existence without download) |
| Interval | 30 seconds |
| Timeout | 15 minutes (30 attempts) |
| Success | HTTP 200 response |
| Failure | Timeout after 15 minutes |

**Polling Script** (used in workflow):
```bash
for i in {1..30}; do
  if curl -sf -o /dev/null -w "%{http_code}" "$NUGET_URL" | grep -q "200"; then
    echo "Package available!"
    exit 0
  fi
  echo "Attempt $i/30: Not yet available, waiting 30s..."
  sleep 30
done
echo "Timeout waiting for package"
exit 1
```

## Error Handling and Recovery

### Scenario: Workflow Times Out Waiting for Dependency

**Cause**: NuGet indexing took longer than 15 minutes

**Recovery**:
1. Wait 5-10 minutes for NuGet to fully index
2. Create a new trigger tag:
   ```powershell
   $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
   git tag "release-v$timestamp-retry"
   git push origin --tags
   ```
3. The workflow will detect already-published packages and skip them

### Scenario: Build Fails in Workflow

**Cause**: Code doesn't compile in Release configuration

**Recovery**:
1. Fix the build error locally
2. Commit the fix
3. Create a new trigger tag (as above)

### Scenario: NuGet Push Fails

**Cause**: API key issues, network problems, or package already exists

**Recovery**:
1. Check the workflow logs for the specific error
2. If "package already exists": the package was published, continue
3. If authentication error: verify NUGET_API_KEY secret is valid
4. Re-trigger with a new tag after fixing the issue

### Scenario: Agent Interrupted Mid-Execution

**Cause**: Agent stopped after updating files but before commit

**Recovery**:
1. Check local changes: `git status`
2. Review the changes: `git diff`
3. If changes look correct:
   ```powershell
   git add -A
   git commit -m "Release: [describe versions]"
   $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
   git tag "release-v$timestamp"
   git push origin HEAD --tags
   ```
4. If changes look wrong: `git checkout .`

## Version Numbering Guidelines

| Change Type | Version Bump | Example | Downstream Impact |
|-------------|-------------|---------|-------------------|
| Bug fix | Patch | 5.2.0 → 5.2.1 | None - auto-accepted by version range |
| New feature (backward compatible) | Minor | 5.2.0 → 5.3.0 | None - auto-accepted by version range |
| Breaking change | Major | 5.2.0 → 6.0.0 | Cascade required - all downstream bump |

## Selective Cascade (--cascade-scope)

For advanced scenarios, you can limit which packages participate in a cascade:

### Usage

When running the release agent, specify scope to limit cascade:

```
/speckit.bp.release --cascade-scope=Core,SignalR
```

**Valid scope values**: `Core`, `SignalR`, `Client`, `Server`, `All` (default)

### Selective Cascade Behavior

| Scope | Core Major Bump → Cascades To |
|-------|------------------------------|
| `All` (default) | SignalR, Client, Server |
| `Core,SignalR` | SignalR only |
| `Core` | No cascade (⚠️ warning) |
| `SignalR,Client,Server` | Client, Server (if SignalR has major bump) |

### Warnings

When packages are excluded from cascade, the agent will warn:

```
⚠️  WARNING: The following packages are excluded from this cascade:
    - Client (depends on Core [5.0.0, 6.0.0) which will become invalid)
    - Server (depends on Core [5.0.0, 6.0.0) which will become invalid)
    
These packages will NOT receive the updated version range and may
fail to restore after Core 6.0.0 is published.

Continue anyway? [y/N]
```

### Use Cases

1. **Staged rollout**: Release Core first, then release downstream packages later after testing
2. **Emergency fix**: Release only the affected package without triggering cascade
3. **Testing**: Verify Core publishes correctly before committing to full cascade

### Implementation

```powershell
function Get-CascadePackages {
    param(
        [string]$PackageName,
        [string]$BumpType,
        [string[]]$Scope = @("All")
    )
    
    if ($BumpType -ne "Major") {
        return @()  # No cascade for minor/patch
    }
    
    $fullCascade = @{
        "Core" = @("SignalR", "Client", "Server")
        "SignalR" = @("Client", "Server")
        "Client" = @()
        "Server" = @()
    }
    
    if ($Scope -contains "All") {
        return $fullCascade[$PackageName]
    }
    
    # Filter to only packages in scope
    $cascade = $fullCascade[$PackageName] | Where-Object { $Scope -contains $_ }
    
    # Warn about excluded packages
    $excluded = $fullCascade[$PackageName] | Where-Object { $Scope -notcontains $_ }
    if ($excluded.Count -gt 0) {
        Write-Host "⚠️  WARNING: Excluding from cascade: $($excluded -join ', ')" -ForegroundColor Yellow
    }
    
    return $cascade
}
```

## See Also

- [spec.md](../specs/007-flex-version-mgmt/spec.md) - Feature specification
- [data-model.md](../specs/007-flex-version-mgmt/data-model.md) - Complete dependency graph
- [quickstart.md](../specs/007-flex-version-mgmt/quickstart.md) - Manual release procedures
- [release-unified.yml](../.github/workflows/release-unified.yml) - Unified workflow source
