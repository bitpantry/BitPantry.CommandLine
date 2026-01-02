# Implementation Plan: Flexible Internal Package Version Management

**Branch**: `007-flex-version-mgmt` | **Date**: 2026-01-01 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-flex-version-mgmt/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement flexible version management for internal package dependencies using NuGet Central Package Management (CPM) and version ranges. Create a unified release workflow that publishes packages in dependency order with NuGet availability polling. Build a release agent (speckit.bp.release.md) that automates version analysis, proposes releases, and triggers the unified workflow via a single `release-v{timestamp}` tag.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: MSBuild/NuGet (CPM), GitHub Actions, Git  
**Storage**: N/A (file-based: .csproj, Directory.Packages.props, YAML workflows)  
**Testing**: MSTest with FluentAssertions (existing test framework)  
**Target Platform**: Windows/Linux (GitHub Actions runners, local dev)
**Project Type**: Multi-project NuGet package solution (5 publishable packages)  
**Performance Goals**: NuGet polling timeout: 15 minutes max, 30-second intervals  
**Constraints**: Must maintain backward compatibility for existing consumers  
**Scale/Scope**: 5 packages, 1 unified workflow, 1 release agent command

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Test-Driven Development** | ✅ PASS | Integration tests for CPM migration (build validation), workflow tests via manual trigger verification |
| **II. Dependency Injection** | ✅ N/A | Feature is build/workflow configuration, no runtime C# code requiring DI |
| **III. Security by Design** | ✅ PASS | NuGet API key handled by GitHub Secrets (existing pattern); no new secrets exposed |
| **IV. Follow Existing Patterns** | ✅ PASS | Follows existing UseProjectReferences pattern; workflows follow existing build-*.yml patterns |
| **V. Integration Testing** | ✅ PASS | End-to-end validation via workflow dry-runs and actual package publish to NuGet |
| **Documentation** | ✅ PASS | Release agent serves as comprehensive documentation per FR-038 to FR-043 |

**Gate Result**: PASS - No violations. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/007-flex-version-mgmt/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (package dependency graph)
├── quickstart.md        # Phase 1 output (release workflow guide)
├── contracts/           # Phase 1 output (workflow YAML schemas)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
# Files to CREATE
Directory.Packages.props              # NEW: Central Package Management file
.github/workflows/release-unified.yml # NEW: Unified release workflow
.claude/commands/speckit.bp.release.md # NEW: Release agent command

# Files to MODIFY
BitPantry.CommandLine/
  BitPantry.CommandLine.csproj        # UPDATE: Remove Version from PackageReferences
BitPantry.CommandLine.Remote.SignalR/
  BitPantry.CommandLine.Remote.SignalR.csproj # UPDATE: Remove Version, update internal dep refs
BitPantry.CommandLine.Remote.SignalR.Client/
  ...csproj                           # UPDATE: Same pattern
BitPantry.CommandLine.Remote.SignalR.Server/
  ...csproj                           # UPDATE: Same pattern

# Files to DEPRECATE (leave in place but document as superseded)
.github/workflows/build-core.yml      # DEPRECATED: Replaced by release-unified.yml
.github/workflows/build-client.yml    # DEPRECATED: Replaced by release-unified.yml
.github/workflows/build-server.yml    # DEPRECATED: Replaced by release-unified.yml
.github/workflows/build-remote-signalr.yml # DEPRECATED: Replaced by release-unified.yml
```

**Structure Decision**: This feature modifies existing solution structure (no new C# projects). Primary artifacts are configuration files (Directory.Packages.props, workflows) and documentation (release agent).

## Complexity Tracking

> No Constitution Check violations requiring justification.

| Aspect | Complexity | Justification |
|--------|------------|---------------|
| Unified workflow vs per-package | Medium | Single workflow with version detection is simpler than coordinating 4 separate workflows with timing delays |
| CPM adoption | Low | Standard NuGet feature, well-documented migration path |
| Release agent | Medium | Claude command file with git operations; no compiled code needed |

---

## Phase Completion Status

### Phase 0: Research ✅ COMPLETE

**Output**: [research.md](research.md)

Topics Resolved:
- NuGet Central Package Management (CPM) adoption
- Version range syntax (`[min, max)` notation)
- GitHub Actions workflow orchestration
- NuGet API for version detection and polling
- Git tag conventions
- UseProjectReferences pattern preservation
- Breaking change detection approach

### Phase 1: Design & Contracts ✅ COMPLETE

**Outputs**:
- [data-model.md](data-model.md) - Package dependency graph, publishing order, cascade matrix
- [contracts/release-unified-workflow.md](contracts/release-unified-workflow.md) - Unified workflow YAML schema
- [quickstart.md](quickstart.md) - Release workflow guide for developers

**Agent Context Updated**: CLAUDE.md updated with new technology references

### Phase 2: Tasks (Next)

Run `/speckit.tasks` to generate implementation tasks from this plan.
