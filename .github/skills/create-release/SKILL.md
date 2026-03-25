---
name: create-release
description: 'Plan and execute a NuGet package release. Use when: releasing packages, bumping versions, creating release tags, publishing to NuGet, version increment decisions, preparing a release, shipping changes.'
---

# Create Release

Plan and execute a versioned NuGet package release for the BitPantry.CommandLine solution.

## Publishable Packages

These are the NuGet packages in this solution, listed in dependency order (upstream first):

| Package | Short Name | csproj Path | Dependency |
|---------|-----------|-------------|------------|
| BitPantry.CommandLine | Core | `BitPantry.CommandLine/BitPantry.CommandLine.csproj` | None |
| BitPantry.CommandLine.Remote.SignalR | SignalR | `BitPantry.CommandLine.Remote.SignalR/BitPantry.CommandLine.Remote.SignalR.csproj` | Core |
| BitPantry.CommandLine.Remote.SignalR.Client | Client | `BitPantry.CommandLine.Remote.SignalR.Client/BitPantry.CommandLine.Remote.SignalR.Client.csproj` | Core, SignalR |
| BitPantry.CommandLine.Remote.SignalR.Server | Server | `BitPantry.CommandLine.Remote.SignalR.Server/BitPantry.CommandLine.Remote.SignalR.Server.csproj` | Core, SignalR |

**Not published** (no `GeneratePackageOnBuild`): Client, Server. They are still packed and published by the GitHub Actions workflow (`release-unified.yml`), which packs them explicitly. All four are part of the release pipeline.

**Excluded**: `BitPantry.VirtualConsole` and `BitPantry.VirtualConsole.Testing` live in this solution but are separate projects with independent release cycles. Do **not** include them in the release plan.

## Procedure

### Step 1: Identify Changed Packages

Determine which packages have changes since the last unified release.

Find the most recent `release-v*` tag:

```
git tag --list 'release-v*' --sort=-creatordate | head -1
```

Then check each package directory for changes since that tag:

```
git log <last-release-tag>..HEAD --oneline -- <project-directory>/
```

If no `release-v*` tag exists yet, compare against the full history or ask the user for a baseline.

If a package has no changes since the last release tag, it does **not** need a release.

**Dependency version ranges**: `Directory.Packages.props` defines version ranges for internal dependencies (e.g., `[5.0.0, 6.0.0)` for Core). Patch and minor bumps on an upstream package are automatically satisfied by these ranges — downstream packages do **not** need a coordinated release. Only a **major** version bump (which breaks the range ceiling) requires bumping downstream packages and updating the version ranges in `Directory.Packages.props`.

### Step 2: Determine Version Increments

For each package with changes, classify the increment using semantic versioning:

| Increment | When to use |
|-----------|-------------|
| **Major** (X.0.0) | Breaking API changes: removed public types/methods, changed method signatures, renamed public interfaces, behavioral changes that break existing consumers |
| **Minor** (x.Y.0) | New features: new commands, new public APIs, new attributes, new capabilities that are backward-compatible |
| **Patch** (x.y.Z) | Bug fixes, internal refactors, performance improvements, documentation changes, adding attributes to existing properties, no new public API surface |
| **None** | No changes since last release |

**Guidelines**:
- Adding an attribute (e.g., `[ServerFilePathAutoComplete]`) to an existing command property = **Patch** (no API surface change)
- Adding a new command class = **Minor**
- Fixing a bug in input handling = **Patch**
- Changing a public interface = **Major**
- New optional parameters on existing methods = **Minor**

Read the current version from the `<Version>` element in each package's `.csproj` file.

### Step 3: Present the Release Plan

Present a table to the user for confirmation. Include the proposed `<PackageReleaseNotes>` that will ship with each package on NuGet:

| Package | Current Version | New Version | Release Notes |
|---------|----------------|-------------|---------------|
| BitPantry.CommandLine | 5.2.0 | 5.2.1 | Fixed Tab flicker in autocomplete idle mode |
| BitPantry.CommandLine.Remote.SignalR | 1.2.1 | — | _(no release)_ |
| BitPantry.CommandLine.Remote.SignalR.Client | 1.2.1 | — | _(no release)_ |
| BitPantry.CommandLine.Remote.SignalR.Server | 1.2.1 | 1.2.2 | Added path autocomplete to all server file system commands |

Include ALL four packages in the table. Packages with no changes should show "—" for New Version.

The Release Notes column serves double duty: it is both the rationale for the version bump **and** the value that will be written to `<PackageReleaseNotes>` in the `.csproj`. Keep it concise (one or two sentences) and user-facing — this text appears on the NuGet package page.

**Ask the user to confirm or adjust the plan before proceeding.**

### Step 4: Execute the Release

Once the user confirms:

1. **Bump versions and release notes** — For each changed package's `.csproj`, update:
   - `<Version>` — the new version number
   - `<AssemblyVersion>` — must stay in sync with `<Version>`
   - `<PackageReleaseNotes>` — the release notes from the confirmed plan

2. **Build and test** — Run `dotnet build` and `dotnet test` to verify nothing is broken:
   ```
   dotnet build --configuration Release
   dotnet test --configuration Release --no-build
   ```

3. **Cross-platform validation** — Run the test suite on Linux via WSL to catch path-related failures before pushing. See the `cross-platform-testing` skill for full details and commands:
   ```
   wsl -d Ubuntu -- bash -c 'cd /mnt/c/src/bitpantry/BitPantry.CommandLine && /usr/share/dotnet/dotnet test --configuration Release'
   ```
   Both Windows and Linux must pass before proceeding.

4. **Commit** — Stage and commit the version bumps. The commit message must have a descriptive summary of what this release contains (not just "bump versions"). Format:
   ```
   git add -A
   git commit -m "Release: <descriptive summary of the release>

   - <Package> <old> -> <new>
   - <Package> <old> -> <new>"
   ```
   Example: `"Release: Autocomplete system, syntax highlighting, server file commands, and profile management"`. The first line should read naturally as a release headline. The body lists each bumped package with its version change.

5. **Tag** — Create a `release-v*` tag that triggers the GitHub Actions workflow:
   ```
   git tag release-v<YYYYMMDD-HHmmss>
   ```
   Use the current UTC date/time for the tag suffix (e.g., `release-v20260322-143000`).

6. **Push** — Push the commit and tag together:
   ```
   git push
   git push origin release-v<YYYYMMDD-HHmmss>
   ```

7. **Confirm** — Tell the user the tag has been pushed and the GitHub Actions workflow will handle NuGet publishing. Link to the Actions page: `https://github.com/bitpantry/BitPantry.CommandLine/actions`

## Important Notes

- The `release-unified.yml` workflow automatically **skips** packages whose version already exists on NuGet — only packages with bumped versions get published.
- The workflow publishes in dependency order: Core → SignalR → Client + Server (parallel). It polls NuGet to wait for upstream packages to be indexed before publishing downstream ones.
- A **major** version bump requires updating the version ranges in `Directory.Packages.props` (e.g., changing `[5.0.0, 6.0.0)` to `[6.0.0, 7.0.0)`). Patch and minor bumps do not require any changes to `Directory.Packages.props`.
- All three `.csproj` fields — `<Version>`, `<AssemblyVersion>`, and `<PackageReleaseNotes>` — must be updated for each released package.
