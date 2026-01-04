# Feature Specification: Flexible Internal Package Version Management

**Feature Branch**: `007-flex-version-mgmt`  
**Created**: January 1, 2026  
**Status**: Draft  
**Input**: User description: "Implement flexible version management for internal package dependencies within the solution"

## Clarifications

### Session 2026-01-01
- Q: Which NuGet feed is used for publishing and querying packages? → A: NuGet.org (public feed) only
- Q: How does the release agent handle NuGet publishing and API keys? → A: Agent does NOT publish directly. Agent manages version numbers in Directory.Packages.props and .csproj files, commits changes, and creates/pushes a single `release-v{timestamp}` tag. This triggers the unified release workflow which handles all NuGet publishing with dependency ordering and availability polling.
- Q: How should the agent classify changes (breaking/feature/fix) without conventional commits? → A: Default to patch bump; user can override classification during plan review.
- Q: What is the recovery approach if a GitHub workflow fails after tags are pushed? → A: Leave tags in place; document manual recovery steps (delete tag, fix issue, re-run agent).
- Q: For breaking change cascades, how should downstream packages be versioned? → A: Downstream packages get minor version bump only (not major), with updated dependency range. The downstream package itself didn't have breaking changes.
- Q: Should the solution adopt Central Package Management (CPM)? → A: Yes. CPM centralizes all package versions in Directory.Packages.props, simplifying version range management for internal dependencies and reducing agent complexity.
- Q: How should multiple packages be published when they have dependencies? → A: A unified release workflow handles this. Each job detects whether its package needs publishing by comparing .csproj version vs NuGet. Jobs are ordered by dependencies and poll NuGet for upstream packages before publishing. This handles variable release plans automatically.
- Q: What happens if the release agent fails mid-execution (after file updates but before commit/tag)? → A: Atomic operations; uncommitted changes left for user to inspect/discard. Agent commits all changes together before tagging. If interrupted, user can inspect local modifications and either discard (`git checkout .`) or manually continue.
- Q: Should there be notifications when releases complete or fail? → A: No additional notification—rely on GitHub Actions UI and GitHub's built-in notification settings. Keeps implementation simple.
- Q: Should GitHub Releases be created alongside NuGet publishing? → A: Yes. Create a single GitHub Release per unified workflow run with auto-generated release notes summarizing all packages published.
- Q: How should the release agent determine what changed since the last release? → A: Compare HEAD to the commit when the .csproj version was last changed. Use `git log -1 --format=%H -S"<Version>X.Y.Z</Version>" -- path/to/project.csproj` to find the commit where the current version was set, then show all commits since.

## User Scenarios & Testing *(mandatory)*

### User Story 0 - Adopt Central Package Management (Priority: P1)

As a solution maintainer, I want to adopt NuGet Central Package Management (CPM) so that all package versions are defined in a single location, making version range management simpler and more consistent across all projects.

**Why this priority**: CPM is a prerequisite that simplifies the entire flexible versioning implementation. Without CPM, version ranges must be updated in multiple .csproj files during breaking change cascades. With CPM, a single file (Directory.Packages.props) controls all versions.

**Independent Test**: Can be tested by verifying that after migration: (1) Directory.Packages.props exists with ManagePackageVersionsCentrally=true, (2) all .csproj files have PackageReference without Version attributes, (3) the solution builds successfully, and (4) packages are generated with correct dependency versions.

**Acceptance Scenarios**:

1. **Given** the solution currently uses per-project package versioning, **When** CPM migration is complete, **Then** a Directory.Packages.props file exists at the solution root with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.

2. **Given** CPM is enabled, **When** any .csproj file is examined, **Then** all `<PackageReference>` elements specify only the package name (Include attribute) without a Version attribute.

3. **Given** CPM is enabled, **When** Directory.Packages.props is examined, **Then** it contains `<PackageVersion>` entries for all external dependencies with exact versions and internal dependencies with version ranges.

4. **Given** CPM is enabled with internal dependencies using version ranges, **When** a package is built for publishing, **Then** the generated .nupkg contains the version range metadata for internal dependencies.

5. **Given** CPM is enabled, **When** a developer runs `dotnet restore` and `dotnet build`, **Then** the solution builds successfully with no version resolution errors.

---

### User Story 1 - Minor/Patch Core Update Without Downstream Releases (Priority: P1)

As a package maintainer, I want to release a minor or patch update to the core BitPantry.CommandLine package without being forced to release new versions of all downstream packages (Remote.SignalR, Client, Server), so that I can ship bug fixes and non-breaking enhancements faster with less coordination overhead.

**Why this priority**: This is the primary pain point driving this feature. Currently, every core package release requires coordinated releases of 3+ downstream packages even when they have no functional changes. This creates unnecessary release overhead and clutters the package version history.

**Independent Test**: Can be tested by releasing a minor version bump to BitPantry.CommandLine (e.g., 5.2.0 → 5.3.0), then verifying that consuming applications using downstream packages (e.g., BitPantry.CommandLine.Remote.SignalR.Client 1.2.1) automatically resolve and use the new core version without requiring new downstream package releases.

**Acceptance Scenarios**:

1. **Given** BitPantry.CommandLine is at version 5.2.0 and downstream packages reference it with flexible versioning, **When** BitPantry.CommandLine 5.3.0 is published to NuGet, **Then** applications consuming downstream packages automatically receive the updated core package without any downstream package releases.

2. **Given** an application depends on BitPantry.CommandLine.Remote.SignalR.Client 1.2.1 which has a flexible dependency on BitPantry.CommandLine ≥5.0.0 <6.0.0, **When** BitPantry.CommandLine 5.4.0 is available on NuGet, **Then** NuGet restore selects BitPantry.CommandLine 5.4.0 for the application.

3. **Given** a downstream package references core with flexible versioning, **When** the package is built for publishing, **Then** the generated .nupkg contains version range metadata (not a pinned version) for internal dependencies.

---

### User Story 2 - Breaking Change Coordination (Priority: P2)

As a package maintainer, I want breaking changes (major version bumps) to still require coordinated releases across all affected packages, so that consuming applications don't encounter runtime incompatibilities between package versions.

**Why this priority**: Semantic versioning contract integrity is essential for users to trust the package ecosystem. Breaking changes must be explicit and coordinated to prevent runtime failures.

**Independent Test**: Can be tested by attempting to reference BitPantry.CommandLine 6.0.0 from a downstream package that specifies ≥5.0.0 <6.0.0, and verifying that NuGet correctly rejects this version mismatch.

**Acceptance Scenarios**:

1. **Given** downstream packages specify flexible versioning with upper bound at next major version, **When** BitPantry.CommandLine 6.0.0 (major version bump) is published, **Then** existing downstream packages do NOT automatically use the new major version.

2. **Given** a major version bump is planned for the core package, **When** the release is executed, **Then** all downstream packages must be explicitly updated and re-released with updated version ranges before consumers can use the new major version coherently.

---

### User Story 3 - Third-Party Dependencies Remain Pinned (Priority: P2)

As a package maintainer, I want external third-party package dependencies (e.g., Microsoft.Extensions.DependencyInjection, Spectre.Console) to remain pinned to specific versions, so that builds are reproducible and I have explicit control over when to adopt third-party updates.

**Why this priority**: External dependencies carry risk of unexpected breaking changes and should be updated deliberately, not automatically. This is a different concern than internal package coordination.

**Independent Test**: Can be tested by examining the generated .nupkg files and verifying that all external PackageReference dependencies specify exact versions rather than version ranges.

**Acceptance Scenarios**:

1. **Given** a project references Microsoft.Extensions.DependencyInjection at version 9.0.1, **When** the package is published, **Then** the .nupkg dependency metadata shows version="9.0.1" (exact), not a version range.

2. **Given** a new version of an external dependency is available on NuGet, **When** a package referencing it is built, **Then** the build continues to use the explicitly specified version until manually updated.

---

### User Story 4 - Local Development Experience Unchanged (Priority: P2)

As a developer working on the solution locally, I want to continue using project references during development, so that I get immediate feedback on changes across package boundaries without needing to publish intermediate packages.

**Why this priority**: Developer productivity depends on fast feedback loops. The existing project reference mechanism (controlled by UseProjectReferences in Directory.Build.props) should continue to work seamlessly.

**Independent Test**: Can be tested by making a change in BitPantry.CommandLine, then building and debugging BitPantry.CommandLine.Remote.SignalR.Client with breakpoints, verifying that the change is immediately reflected without any manual package restore steps.

**Acceptance Scenarios**:

1. **Given** UseProjectReferences is set to true in Directory.Build.props, **When** a developer builds any project in the solution, **Then** all internal dependencies use project references with immediate reflection of source changes.

2. **Given** UseProjectReferences is set to false (simulating publish mode), **When** a developer builds any project, **Then** internal dependencies use NuGet package references with flexible version ranges for internal packages.

---

### User Story 5 - CI/CD Version Validation (Priority: P3)

As a release engineer, I want the CI/CD pipeline to validate that all referenced internal package versions exist on NuGet before publishing downstream packages, so that we don't publish packages with unresolvable dependencies.

**Why this priority**: This is a safety net that prevents publishing broken packages. It's lower priority because manual processes could catch this, but automation reduces risk.

**Independent Test**: Can be tested by attempting to publish a downstream package that references an internal package version not yet on NuGet, and verifying the CI/CD pipeline fails with a clear error message before the publish step.

**Acceptance Scenarios**:

1. **Given** a downstream package specifies a flexible version range for an internal dependency, **When** the CI/CD pipeline runs before publishing, **Then** it validates that at least one version within the specified range exists on NuGet.

2. **Given** a downstream package references BitPantry.CommandLine ≥5.0.0 <6.0.0 but no version in that range exists on NuGet, **When** the publish pipeline runs, **Then** the pipeline fails with an error message identifying the missing dependency before attempting to publish.

3. **Given** all referenced internal package versions exist on NuGet, **When** the publish pipeline runs, **Then** validation passes and publishing proceeds.

---

### Edge Cases

- What happens when a downstream package is published before its core dependency? CI/CD validation should catch and block this.
- How does version resolution work when multiple downstream packages reference different (but overlapping) version ranges of the same core package? NuGet's standard version unification handles this—highest compatible version is selected.
- What happens when a developer has local package cache with an older core version? Standard NuGet cache behavior applies; `dotnet restore --force` clears cache if needed.
- How are prerelease versions handled in version ranges? Prerelease versions (e.g., 5.3.0-beta1) are excluded from ranges by default per NuGet conventions unless explicitly requested by the consumer.
- What happens if a GitHub workflow fails after tags are pushed? Tags remain in place for investigation. Manual recovery: delete the failed tag (`git tag -d <tag> && git push origin :refs/tags/<tag>`), fix the issue, and re-run the release agent.
- What happens if the release agent fails mid-execution? Agent uses atomic operations—all file changes are staged and committed together before tag creation. If interrupted before commit, uncommitted changes remain as local modifications for user inspection. User can discard with `git checkout .` or manually complete the release.

---

### User Story 6 - Automated Release Planning Tool (Priority: P2)

As a package maintainer, I want an automated release planning agent that performs all analysis and proposes a complete release plan, so that I only need to review and confirm before the agent executes the entire release.

**Why this priority**: The flexible versioning reduces release frequency, but when releases are needed, having an intelligent agent that handles all analysis, version determination, and execution significantly reduces manual effort and human error.

**Agent Responsibilities**:
1. **Query current releases**: Query NuGet.org API to determine the latest published version of each package
2. **Analyze dependencies**: Build the dependency graph between all solution packages
3. **Detect changes**: Compare current .csproj versions against NuGet versions to identify packages needing release
4. **Classify changes**: Determine if changes are breaking (major), feature additions (minor), or fixes (patch)
5. **Propose versions**: Calculate the appropriate new version for each package based on semantic versioning
6. **Generate release plan**: Present a table with current version, proposed version, change summary, and rationale
7. **Await confirmation**: Pause for user to confirm, modify, or reject the plan
8. **Execute release**: Upon confirmation:
   - Update version numbers in .csproj files (for packages being released)
   - Update dependency version ranges in Directory.Packages.props (for breaking change cascades)
   - Commit all changes with descriptive commit message
   - Create and push a single `release-v{timestamp}` tag to trigger the unified release workflow
   - Report that the unified workflow has been triggered and provide link to monitor

**Agent as Documentation**:
The release agent command file (speckit.bp.release.md) serves as the comprehensive documentation for the entire release system. It must explain:
- The complete dependency graph and publishing order
- How version detection works (comparing .csproj vs NuGet versions)
- How NuGet availability polling works (endpoint, timing, error handling)
- The unified workflow structure and job dependencies
- Error recovery procedures for common failure scenarios
- Examples of variable release plans and how they're handled

A developer reading only the agent's instructions should fully understand how the release system works.

**Independent Test**: Can be tested by making changes to multiple packages, then invoking the release agent and verifying it correctly analyzes all packages, proposes appropriate version bumps, and summarizes the changes without any manual input required before the confirmation step.

**Acceptance Scenarios**:

1. **Given** changes have been made to BitPantry.CommandLine since its last NuGet release, **When** the release agent is invoked, **Then** it automatically determines the current published version, analyzes changes, classifies them as breaking/feature/fix, proposes the new version, and displays a table with all this information.

2. **Given** no changes have been made to a package since its last release, **When** the release agent runs, **Then** that package is shown in the table with a "No Release Needed" status and rationale explaining no changes detected.

3. **Given** breaking changes are detected (e.g., removed public API, changed method signatures), **When** the agent analyzes the changes, **Then** it proposes a major version bump and triggers cascade analysis for downstream packages.

4. **Given** the release plan is displayed, **When** the user confirms the plan, **Then** the agent updates version numbers, commits changes, and pushes a single `release-v{timestamp}` tag to trigger the unified release workflow which handles dependency ordering and NuGet availability polling automatically.

5. **Given** the release plan is displayed, **When** the user declines or requests modifications, **Then** the agent halts and awaits further instructions without publishing any packages.

6. **Given** multiple packages need release with interdependencies, **When** the release is executed, **Then** the unified workflow publishes packages in dependency order (core packages before downstream packages), with NuGet availability polling ensuring each package waits for its dependencies to be indexed.

7. **Given** the user wants to understand how releases work, **When** they read the release agent command file, **Then** they find complete documentation of the entire release system including dependency ordering, version detection, and error recovery.

---

### User Story 7 - Breaking Change Cascade Management (Priority: P1)

As a package maintainer, when the agent detects a breaking change to a core package (major version bump), I want it to automatically identify all downstream packages that need updated version ranges and include them in the release plan, so that I don't have to manually track dependencies or risk publishing incompatible packages.

**Why this priority**: Breaking changes are the highest-risk scenario. Without automated cascade management, maintainers can easily forget to update downstream packages, leading to broken dependency chains where consumers can't resolve compatible versions. This is elevated to P1 because the flexible versioning feature actively makes this scenario more likely (since maintainers are no longer in the habit of releasing all packages together).

**Independent Test**: Can be tested by making a breaking change to BitPantry.CommandLine and verifying the release agent: (1) detects this as a breaking change, (2) proposes a major version bump, (3) identifies all downstream packages, (4) proposes version range updates in their .csproj files, (5) includes all affected downstream packages in the release plan, and (6) executes the coordinated release in correct dependency order upon confirmation.

**Acceptance Scenarios**:

1. **Given** breaking changes are detected in BitPantry.CommandLine, **When** the release agent analyzes the release, **Then** it proposes a major version bump for Core (e.g., 5.2.0 → 6.0.0) and minor version bumps for all downstream packages (e.g., Remote.SignalR 1.2.1 → 1.3.0) with updated version ranges.

2. **Given** a major version bump is proposed for a core package, **When** the release plan is displayed, **Then** downstream packages are marked as "Required - Breaking Change Cascade" with minor version bumps, even if they have no code changes, with clear explanation that their version ranges must be updated.

3. **Given** a breaking change cascade is detected, **When** the release plan is confirmed, **Then** the agent automatically updates the version range in each downstream .csproj (e.g., `[5.0.0, 6.0.0)` becomes `[6.0.0, 7.0.0)`) before building and publishing.

4. **Given** a user attempts to release only a core package with a major version bump without the downstream packages, **When** they confirm the plan, **Then** the agent warns that this will break the dependency chain and requires explicit override to proceed.

5. **Given** a multi-level dependency chain (Core → SignalR → Client), **When** Core has a breaking change, **Then** the cascade analysis correctly identifies that both SignalR AND Client need updates, even though Client doesn't directly depend on Core's version range (it depends on SignalR which depends on Core).

6. **Given** the release agent detects a breaking change, **When** displaying the release plan, **Then** it shows a "Cascade Impact" section that visualizes which packages are affected and why.

---

### User Story 8 - Selective Breaking Change Scope (Priority: P3)

As a package maintainer, when a breaking change only affects certain downstream packages (not all), I want to be able to specify which packages are actually impacted, so that I don't unnecessarily release packages that don't use the changed APIs.

**Why this priority**: This is an optimization for advanced scenarios. The default behavior (cascade to all downstream packages) is safe, but sometimes a breaking change in Core only affects Server, not Client. This allows maintainers to reduce unnecessary releases when they understand the impact.

**Independent Test**: Can be tested by making a breaking change that only affects Server-side APIs, then using the release tool with scope override to exclude Client from the cascade.

**Acceptance Scenarios**:

1. **Given** a major version bump is detected, **When** the user specifies `--cascade-scope SignalR,Server`, **Then** only the specified packages are included in the breaking change cascade (Client is excluded).

2. **Given** a user excludes a package from the cascade, **When** the release plan is displayed, **Then** a warning is shown that the excluded package may have compatibility issues and the user must confirm they understand the risk.

---

### User Story 9 - Unified Release Workflow (Priority: P1)

As a package maintainer, I want a single unified GitHub workflow that publishes all changed packages in the correct dependency order with automatic NuGet availability polling, so that I can trigger one workflow and have all releases complete automatically without worrying about timing or dependencies.

**Why this priority**: Multiple packages often need release together (especially during breaking change cascades). Independent workflows per package cannot coordinate dependency order or wait for NuGet indexing. A unified workflow ensures packages are published in the correct order with the orchestration happening server-side in GitHub Actions.

**How It Works**:

1. **Release Agent Execution**: The agent analyzes changes, proposes versions, and upon confirmation:
   - Updates version numbers in .csproj files and Directory.Packages.props
   - Commits changes to the branch
   - Creates a single trigger tag: `release-v{timestamp}` (e.g., `release-v20260101-143052`)
   - Pushes the commit and tag together

2. **Unified Workflow Triggered**: The `release-v*` tag triggers `.github/workflows/release-unified.yml`

3. **Version Detection**: Each package job in the workflow:
   - Reads the package version from its .csproj file
   - Queries NuGet.org for the latest published version
   - Compares versions: if .csproj version > NuGet version, package needs release
   - If versions match or NuGet has higher, skip publishing (job exits early)

4. **Dependency-Ordered Publishing**: Jobs are structured with `needs:` dependencies:
   - `publish-core` runs first (no dependencies)
   - `publish-remote-signalr` needs `publish-core`
   - `publish-server` needs `publish-remote-signalr`
   - `publish-client` needs `publish-remote-signalr`

5. **NuGet Availability Polling**: Before a dependent package publishes, it polls NuGet API until the dependency is indexed:
   - Query: `https://api.nuget.org/v3-flatcontainer/{package-id}/index.json`
   - Poll every 30 seconds, timeout after 15 minutes
   - Only required when upstream package was actually published (not skipped)

6. **Variable Release Plans**: Because each job independently detects whether to publish, the same workflow handles:
   - Full cascade: All packages have version changes → all publish in order
   - Core-only: Only core has version change → core publishes, others skip
   - Partial: Core + Client change → core publishes, SignalR skips, Client waits for SignalR then publishes

**Independent Test**: Can be tested by making changes only to Core, running the release agent, then verifying: (1) unified workflow triggers, (2) Core job publishes successfully, (3) downstream jobs detect no version changes and skip gracefully, (4) workflow completes as success.

**Acceptance Scenarios**:

1. **Given** changes only to BitPantry.CommandLine (Core), **When** the release agent executes and pushes `release-v*` tag, **Then** the unified workflow runs, publishes Core, and skips all downstream packages (version detection shows no changes).

2. **Given** a breaking change cascade affecting Core, SignalR, Client, and Server, **When** the unified workflow runs, **Then** packages are published in order: Core → SignalR (waits for Core on NuGet) → Client + Server (wait for SignalR on NuGet).

3. **Given** SignalR job needs Core to be on NuGet, **When** Core was just published, **Then** SignalR job polls NuGet API until Core version appears (up to 15 minutes), then proceeds with publishing.

4. **Given** the unified workflow is triggered, **When** a package job detects its .csproj version equals the latest NuGet version, **Then** that job logs "No publish needed - version X.Y.Z already on NuGet" and exits successfully without publishing.

5. **Given** any job in the workflow fails, **When** examining the workflow run, **Then** clear error messages indicate which package failed and why, with downstream jobs not attempted (dependency chain broken).

6. **Given** a developer wants to understand the release system, **When** they read the release agent documentation (speckit.bp.release.md), **Then** they find comprehensive documentation of the entire workflow including: version detection logic, dependency ordering, NuGet polling mechanism, error handling, and recovery procedures.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Central Package Management (CPM) Adoption
- **FR-CPM-001**: The solution MUST adopt NuGet Central Package Management with a Directory.Packages.props file at the solution root.
- **FR-CPM-002**: Directory.Packages.props MUST contain `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.
- **FR-CPM-003**: All `<PackageReference>` elements in .csproj files MUST specify only the Include attribute (package name), with no Version attribute.
- **FR-CPM-004**: Directory.Packages.props MUST contain `<PackageVersion>` entries for all package dependencies.
- **FR-CPM-005**: Internal package dependencies in Directory.Packages.props MUST use version ranges (e.g., `[5.0.0, 6.0.0)`).
- **FR-CPM-006**: External third-party dependencies in Directory.Packages.props MUST use exact versions (e.g., `9.0.1`).

#### Flexible Version Management
- **FR-001**: Solution MUST allow internal package dependencies to be specified as version ranges following semantic versioning (e.g., [5.0.0, 6.0.0) for "any version 5.x").
- **FR-002**: Version ranges for internal dependencies MUST be bounded by major version (upper bound exclusive at next major version).
- **FR-003**: External third-party package dependencies MUST remain pinned to exact versions.
- **FR-004**: The UseProjectReferences build property MUST continue to control whether local builds use project references or package references.
- **FR-005**: Generated NuGet packages MUST contain version range metadata for internal dependencies when UseProjectReferences is false.
- **FR-006**: CI/CD pipeline MUST include a validation step that checks referenced internal package versions exist on NuGet before publishing.
- **FR-007**: CI/CD validation failure MUST produce a clear error message identifying which internal dependency versions are missing.
- **FR-008**: All package versions MUST be managed centrally in Directory.Packages.props (not scattered across individual .csproj files).
- **FR-009**: The solution MUST provide clear documentation for maintainers on how to specify version ranges and when to use exact versions vs. ranges.

#### Release Agent - Analysis & Version Determination
- **FR-010**: A release agent command (speckit.bp.release.md) MUST be created to automate release analysis and execution.
- **FR-011**: The release agent MUST query NuGet.org API to identify the latest published version of each package (not git tags).
- **FR-012**: The release agent MUST build a dependency graph of all packages in the solution.
- **FR-013**: The release agent MUST detect changes since the last release by comparing current HEAD against the commit referenced by the last release tag for each package.
- **FR-014**: The release agent MUST classify changes as breaking (major), feature (minor), or fix (patch). If conventional commit messages are present, use them; otherwise, default to patch bump with user override available during plan review.
- **FR-015**: The release agent MUST automatically calculate the appropriate new version for each package based on semantic versioning rules and detected change classification.
- **FR-016**: The release agent MUST present a release plan table with: package name, current published version, proposed new version, change summary, and release recommendation with rationale.
- **FR-016a**: The change summary MUST include: commit count, list of commits with messages, files changed with diff stats (+/- lines), and highlight any public API files modified.
- **FR-016b**: The release agent MUST determine "last release commit" by finding the commit where the current .csproj version was set using git history.
- **FR-016c**: The release agent MUST flag files likely to contain public API changes (e.g., files with `public class`, `public interface`, extension methods) to help the user assess breaking change risk.

#### Release Agent - Confirmation & Execution
- **FR-017**: The release agent MUST pause for user confirmation before executing any version updates or tag creation.
- **FR-018**: The release agent MUST allow the user to modify the proposed plan (accept, reject, or adjust individual packages) before confirmation.
- **FR-019**: Upon confirmation, the release agent MUST update version numbers in .csproj files (for package versions) and Directory.Packages.props (for dependency version ranges), commit all changes, and create/push a single `release-v{timestamp}` tag to trigger the unified release workflow.
- **FR-019a**: The release agent MUST use atomic operations: all file changes staged and committed together BEFORE creating the tag. If interrupted before commit, uncommitted changes remain as local modifications for user inspection.
- **FR-019b**: The release agent MUST NOT attempt auto-recovery or auto-rollback on failure; user decides whether to discard changes (`git checkout .`) or manually continue.
- **FR-020**: The release agent MUST NOT create individual package tags (e.g., `core-v6.0.0`); all publishing coordination is handled by the unified workflow triggered by the `release-v*` tag.

#### Release Agent - Breaking Change Cascade
- **FR-021**: The release agent MUST detect when a proposed version bump is a major version change (breaking change).
- **FR-022**: When a breaking change is detected, the agent MUST identify ALL downstream packages in the dependency tree that reference the changed package.
- **FR-023**: The release agent MUST automatically include cascade-affected downstream packages in the release plan with a minor version bump (not major), since the downstream package itself did not have breaking changes.
- **FR-024**: The release agent MUST automatically update internal dependency version ranges in Directory.Packages.props when a breaking change cascade is confirmed (single file update, not multiple .csproj files).
- **FR-025**: The release agent MUST prevent partial releases during breaking change cascades unless explicitly overridden.
- **FR-026**: The release agent MUST display a "Cascade Impact" visualization showing the dependency chain affected by breaking changes.

#### Unified Release Workflow
- **FR-027**: A unified GitHub workflow (`.github/workflows/release-unified.yml`) MUST be created to handle all package publishing in a single coordinated run.
- **FR-028**: The unified workflow MUST be triggered by a single tag pattern: `release-v*` (e.g., `release-v20260101-143052`).
- **FR-029**: The release agent MUST create and push a single `release-v{timestamp}` tag to trigger the unified workflow (instead of individual package tags).
- **FR-030**: Each package job in the unified workflow MUST independently detect whether publishing is needed by comparing .csproj version against NuGet.org's latest version.
- **FR-031**: If a package job detects its version already exists on NuGet (or NuGet has higher), it MUST skip publishing and exit successfully.
- **FR-032**: Package jobs MUST be ordered via `needs:` dependencies reflecting the solution's dependency graph (Core → SignalR → Client/Server).
- **FR-033**: Before publishing, a dependent package job MUST poll NuGet API to confirm the upstream dependency version is indexed and available.
- **FR-034**: NuGet availability polling MUST retry every 30 seconds with a maximum timeout of 15 minutes.
- **FR-035**: If NuGet availability polling times out, the job MUST fail with a clear error message indicating which dependency is not yet available.
- **FR-036**: The workflow MUST handle variable release plans automatically—publishing only packages with version changes while skipping unchanged packages.
- **FR-037**: Each package job MUST log its version detection result clearly ("Publishing X.Y.Z" or "Skipping - version X.Y.Z already on NuGet").
- **FR-037a**: The unified workflow MUST create a single GitHub Release per workflow run with auto-generated release notes.
- **FR-037b**: The GitHub Release title MUST include the trigger tag (e.g., "Release release-v20260101-143052").
- **FR-037c**: The GitHub Release body MUST list all packages published in this run with their versions.

#### Release Agent Documentation
- **FR-038**: The release agent command file (speckit.bp.release.md) MUST serve as comprehensive documentation for the entire release system.
- **FR-039**: The agent documentation MUST explain version detection logic (how .csproj versions are compared against NuGet).
- **FR-040**: The agent documentation MUST include the complete dependency graph of solution packages with publishing order.
- **FR-041**: The agent documentation MUST document NuGet availability polling mechanism (endpoint, interval, timeout).
- **FR-042**: The agent documentation MUST provide error handling and manual recovery procedures for common failure scenarios.
- **FR-043**: The agent documentation MUST be written such that a new developer can understand the complete release system by reading only this file.

### Key Entities

- **Central Package Management (CPM)**: NuGet feature that centralizes all package version definitions in a single Directory.Packages.props file at the solution root.
- **Directory.Packages.props**: The central file containing all `<PackageVersion>` entries for the solution, with version ranges for internal dependencies and exact versions for external dependencies.
- **Internal Dependency**: A package reference where the referenced package is also part of this solution (e.g., BitPantry.CommandLine referenced by BitPantry.CommandLine.Remote.SignalR).
- **External Dependency**: A package reference to a third-party package not maintained as part of this solution (e.g., Microsoft.Extensions.DependencyInjection).
- **Version Range**: A NuGet version specification that allows multiple versions to satisfy the dependency (e.g., [5.0.0, 6.0.0) means ≥5.0.0 and <6.0.0).
- **Pinned Version**: An exact version specification that requires a specific version (e.g., 9.0.1).
- **Release Plan**: A summary table showing all packages, their current and proposed versions, change summaries, and release recommendations generated by the release agent.
- **Release Agent**: The speckit.bp.release.md command that performs all analysis, proposes versions, and executes releases upon confirmation.
- **Breaking Change Cascade**: The set of downstream packages that must be updated and released when an upstream package has a major version bump, ensuring version ranges are updated throughout the dependency tree.
- **Cascade Impact**: A visualization showing which packages are affected by a breaking change and the dependency path that connects them.
- **Unified Release Workflow**: A single GitHub Actions workflow (`.github/workflows/release-unified.yml`) that orchestrates publishing all solution packages in dependency order, triggered by a single `release-v*` tag.
- **Release Trigger Tag**: A tag created by the release agent in the format `release-v{timestamp}` (e.g., `release-v20260101-143052`) that triggers the unified release workflow.
- **Version Detection**: The process by which each workflow job compares the .csproj package version against NuGet.org to determine if publishing is needed.
- **NuGet Availability Polling**: The mechanism by which downstream package jobs wait for upstream package versions to be indexed on NuGet.org before proceeding with their publish (query endpoint, 30-second intervals, 15-minute timeout).
- **Variable Release Plan**: A release where not all packages need publishing (e.g., only Core changed)—the unified workflow automatically handles this by version detection in each job.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-000**: Solution successfully migrated to Central Package Management with all versions defined in Directory.Packages.props.
- **SC-001**: Core package minor/patch releases can be published independently—no downstream package releases required for consumers to receive the update.
- **SC-002**: Time to release a non-breaking core package update is reduced by eliminating the need to update and release 3+ downstream packages.
- **SC-003**: 100% of external third-party dependencies remain pinned to exact versions after implementation.
- **SC-004**: Local development workflow (project references) continues to function identically to current behavior.
- **SC-005**: CI/CD pipeline catches 100% of attempts to publish packages with missing internal dependency versions before they reach NuGet.
- **SC-006**: Existing consuming applications continue to resolve dependencies correctly after switching to flexible versioning (no breaking changes to consumers).
- **SC-007**: Release planning tool accurately identifies 100% of packages with changes since last release.
- **SC-008**: Release execution completes successfully with packages published in correct dependency order.
- **SC-009**: Breaking change cascades correctly identify 100% of downstream packages requiring version range updates.
- **SC-010**: Version ranges in Directory.Packages.props are automatically updated during breaking change releases, requiring only a single file edit.
- **SC-011**: Unified release workflow successfully publishes multi-package releases in correct dependency order with NuGet availability polling.
- **SC-012**: Variable release plans (e.g., Core-only release) complete successfully with unchanged packages correctly skipped.
- **SC-013**: A new developer can understand the complete release system by reading the release agent documentation (speckit.bp.release.md).

## Assumptions

- Central Package Management (CPM) will be adopted as part of this feature, centralizing all package versions in Directory.Packages.props.
- All packages are published to NuGet.org (public feed); no private/enterprise feed is used.
- NuGet publishing is handled by the unified GitHub workflow (release-unified.yml) triggered by `release-v*` tags.
- The release agent does NOT publish directly to NuGet; it manages versions and creates a single trigger tag that starts the unified workflow.
- The unified workflow uses version detection to determine which packages need publishing, enabling variable release plans.
- Legacy per-package workflows (build-core.yml, build-client.yml, etc.) and their tag patterns (core-v*, client-v*) will be removed.
- NuGet's native version range syntax ([min, max) notation) is the appropriate mechanism for specifying flexible versions.
- The existing UseProjectReferences mechanism in Directory.Build.props is sufficient for controlling local vs. publish build modes.
- Semantic versioning is already practiced for this solution—major versions indicate breaking changes, minor/patch versions are backward compatible.
- The release agent can determine the commit associated with the last release by reading git tag references.
- GitHub Actions `needs:` dependencies will cause downstream jobs to wait for upstream jobs to complete before starting.
- NuGet.org package indexing typically completes within 1-5 minutes, but may occasionally take longer; 15-minute timeout provides sufficient margin.
- The existing package-specific workflows (build-core.yml, build-client.yml, etc.) will be deprecated in favor of the unified release workflow.
- The release agent command file will be maintained as living documentation, updated whenever the release process changes.
