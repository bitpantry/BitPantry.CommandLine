# Research: Flexible Internal Package Version Management

**Branch**: `007-flex-version-mgmt` | **Date**: 2026-01-01

## Research Topics

### 1. NuGet Central Package Management (CPM)

**Decision**: Adopt NuGet CPM with Directory.Packages.props at solution root.

**Rationale**: 
- Centralizes all package versions in one file
- Simplifies breaking change cascades (update one file vs multiple .csproj)
- Built-in NuGet feature since .NET 6, well-supported
- Reduces merge conflicts when multiple developers update dependencies

**Alternatives Considered**:
| Alternative | Rejected Because |
|-------------|------------------|
| Keep per-.csproj versioning | Breaking change cascades require editing multiple files |
| Custom MSBuild props file | Non-standard, CPM is the official solution |
| Package lock files only | Doesn't solve version range requirement |

**Implementation**:
```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Internal dependencies with version ranges -->
    <PackageVersion Include="BitPantry.CommandLine" Version="[5.0.0, 6.0.0)" />
    <PackageVersion Include="BitPantry.CommandLine.Remote.SignalR" Version="[1.0.0, 2.0.0)" />
    <!-- External dependencies with exact versions -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.1" />
  </ItemGroup>
</Project>
```

---

### 2. NuGet Version Range Syntax

**Decision**: Use `[min, max)` notation (inclusive min, exclusive max).

**Rationale**:
- Standard NuGet syntax, universally supported
- Aligns with semantic versioning: `[5.0.0, 6.0.0)` = "any 5.x version"
- Exclusive upper bound prevents automatic adoption of breaking changes

**Version Range Patterns**:
| Pattern | Meaning | Use Case |
|---------|---------|----------|
| `[5.0.0, 6.0.0)` | ≥5.0.0 and <6.0.0 | Internal dependencies |
| `5.0.0` | Exactly 5.0.0 | External dependencies |
| `[5.2.0,)` | ≥5.2.0 (no upper bound) | NOT recommended for internal deps |

---

### 3. GitHub Actions Workflow Orchestration

**Decision**: Single unified workflow with `needs:` job dependencies and version detection.

**Rationale**:
- GitHub Actions `needs:` ensures dependency ordering
- Each job self-detects if publishing needed (idempotent)
- Single trigger tag simplifies agent logic
- Handles variable release plans automatically

**Alternatives Considered**:
| Alternative | Rejected Because |
|-------------|------------------|
| Workflow dispatch with matrix | Cannot express dependency ordering between matrix jobs |
| Multiple workflows with repository_dispatch | Complex cross-workflow coordination |
| Manual sequential triggering | Error-prone, not automatable |

**Workflow Structure**:
```yaml
on:
  push:
    tags: ['release-v*']

jobs:
  publish-core:
    # Detect version, publish if needed
  
  publish-signalr:
    needs: [publish-core]
    # Wait for core on NuGet, publish if needed
  
  publish-client:
    needs: [publish-signalr]
    # Wait for signalr on NuGet, publish if needed
  
  publish-server:
    needs: [publish-signalr]
    # Wait for signalr on NuGet, publish if needed
```

---

### 4. NuGet API for Version Detection and Availability Polling

**Decision**: Use NuGet V3 flat container API for version queries.

**Rationale**:
- Fast, cacheable JSON responses
- No authentication required for public packages
- Returns all versions in single request

**API Endpoint**:
```
https://api.nuget.org/v3-flatcontainer/{package-id-lowercase}/index.json
```

**Response Format**:
```json
{
  "versions": ["1.0.0", "1.1.0", "2.0.0", "5.2.0"]
}
```

**Polling Strategy**:
- Interval: 30 seconds
- Timeout: 15 minutes (30 attempts)
- Check: Is required version in `versions` array?

**PowerShell Implementation**:
```powershell
$packageId = "bitpantry.commandline"
$requiredVersion = "5.3.0"
$maxAttempts = 30
$attempt = 0

while ($attempt -lt $maxAttempts) {
    $response = Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$packageId/index.json"
    if ($response.versions -contains $requiredVersion) {
        Write-Host "✓ Version $requiredVersion is available"
        exit 0
    }
    Start-Sleep -Seconds 30
    $attempt++
}
exit 1  # Timeout
```

---

### 5. Git Tag Conventions

**Decision**: Use only `release-v{timestamp}` for unified trigger. Remove package-specific tags.

**Rationale**:
- Unified trigger tag is package-agnostic (agent creates one tag)
- Package-specific tags add complexity with no benefit after unified workflow adoption
- Version history is tracked in .csproj files and git commits
- Timestamp format: YYYYMMDD-HHMMSS (sortable, unique)

**Tag Pattern**:
| Tag Pattern | Purpose | Created By |
|-------------|---------|------------|
| `release-v20260101-143052` | Trigger unified workflow | Release agent |

**Removed Tag Patterns** (deprecated):
- `core-v*` - No longer used
- `client-v*` - No longer used
- `server-v*` - No longer used
- `remote-signalr-v*` - No longer used

---

### 8. GitHub Releases

**Decision**: Create a single GitHub Release per unified workflow run with auto-generated notes.

**Rationale**:
- Maintains consistency with existing releases in the repo
- Provides visibility for users watching the repository
- Auto-generated notes reduce maintenance burden
- Single release per workflow (not per-package) aligns with unified approach

**Implementation**:
```yaml
# Final job in unified workflow
create-release:
  needs: [publish-core, publish-signalr, publish-client, publish-server]
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Create GitHub Release
      uses: softprops/action-gh-release@v1
      with:
        generate_release_notes: true
        name: "Release ${{ github.ref_name }}"
        body: |
          ## Packages Published
          - Core: ${{ needs.publish-core.outputs.version }} (${{ needs.publish-core.outputs.published == 'true' && 'published' || 'skipped' }})
          - SignalR: ${{ needs.publish-signalr.outputs.version }} (${{ needs.publish-signalr.outputs.published == 'true' && 'published' || 'skipped' }})
          - Client: (see workflow)
          - Server: (see workflow)
```

---

### 6. Existing UseProjectReferences Pattern

**Decision**: Preserve existing `UseProjectReferences` pattern unchanged.

**Rationale**:
- Already works correctly for local development
- .csproj files already have conditional ItemGroups
- CPM integrates seamlessly with existing pattern

**Current Pattern** (already in .csproj files):
```xml
<ItemGroup Condition="'$(UseProjectReferences)' == 'true'">
  <ProjectReference Include="..\BitPantry.CommandLine\BitPantry.CommandLine.csproj" />
</ItemGroup>

<ItemGroup Condition="'$(UseProjectReferences)' != 'true'">
  <PackageReference Include="BitPantry.CommandLine" />  <!-- Version from CPM -->
</ItemGroup>
```

**No Changes Needed**: Local development continues to use project references when `UseProjectReferences=true` (the default in Directory.Build.props).

---

### 7. Breaking Change Detection

**Decision**: Default to patch bump; user override during plan review. Provide rich change summary to inform decision.

**Rationale**:
- Automated breaking change detection (API diffing) is complex and error-prone
- Manual classification is simple and accurate
- User has final say during release plan confirmation
- Rich change summary gives user the information needed to make informed decisions

**Change Summary Approach**:

1. **Find last release commit**: Use git to find when current version was set in .csproj
   ```bash
   git log -1 --format=%H -S"<Version>5.2.0</Version>" -- BitPantry.CommandLine/BitPantry.CommandLine.csproj
   ```

2. **Get commits since then**:
   ```bash
   git log --oneline {last-release-commit}..HEAD -- BitPantry.CommandLine/
   ```

3. **Get file changes with stats**:
   ```bash
   git diff --stat {last-release-commit}..HEAD -- BitPantry.CommandLine/
   ```

4. **Flag public API files**: Scan changed files for `public class`, `public interface`, or common extension patterns

**Example Output**:
```
┌─────────────────────────────────────────────────────────────────────────────┐
│ BitPantry.CommandLine                                                       │
│ Current: 5.2.0 → Proposed: 5.2.1 (patch)                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│ 3 commits since 5.2.0:                                                      │
│   • abc1234 - Fix null reference in CommandRegistry                         │
│   • def5678 - Add logging to ParsedInput                                    │
│   • ghi9012 - Update Spectre.Console to 0.49.1                             │
│                                                                             │
│ Files changed: 4 files (+45 / -12 lines)                                   │
│   • CommandRegistry.cs                                                      │
│   • Input/ParsedInput.cs                                                    │
│   • BitPantry.CommandLine.csproj                                           │
│   • ServiceCollectionExtensions.cs                                          │
│                                                                             │
│ ⚠️  Public API files modified: ServiceCollectionExtensions.cs              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Detection Flow**:
1. Agent detects any changes since last release (git diff from version commit)
2. Agent presents rich change summary
3. Agent proposes patch bump as default
4. User reviews summary and can upgrade to minor/major
5. If user selects major, agent automatically triggers cascade analysis

---

## Summary

All research topics resolved. No NEEDS CLARIFICATION items remain.

| Topic | Decision |
|-------|----------|
| Package version management | NuGet CPM with Directory.Packages.props |
| Version range syntax | `[min, max)` notation |
| Workflow orchestration | Single unified workflow with `needs:` dependencies |
| NuGet API | V3 flat container for version detection/polling |
| Git tags | `release-v{timestamp}` for triggers |
| Local development | Preserve existing UseProjectReferences |
| Breaking change detection | Manual classification with rich change summary |
| GitHub Releases | Single release per workflow with auto-generated notes |
