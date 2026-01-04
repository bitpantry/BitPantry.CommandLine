# Implementation Plan: Spectre Visual Rendering Refactor

**Branch**: `009-spectre-visual-refactor` | **Date**: January 3, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-spectre-visual-refactor/spec.md`

## Summary

Refactor the autocomplete menu and ghost text rendering to use Spectre.Console's Renderable and LiveRenderable patterns, replacing fragile manual ANSI cursor math with battle-tested visual update mechanisms. This includes consolidating test infrastructure on Spectre.Console.Testing, switching the menu to vertical layout (one item per line), and adding snapshot testing for visual regression detection.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: Spectre.Console 0.49.1 (production), Spectre.Console.Testing 0.54.0 (test), Verify.MSTest (test)  
**Storage**: N/A  
**Testing**: MSTest with FluentAssertions 6.12.0, Moq 4.20.72, Spectre.Console.Testing 0.54.0, Verify.MSTest  
**Target Platform**: Cross-platform CLI (.NET 8.0 runtime)  
**Project Type**: Library with test project  
**Performance Goals**: Isolated renderable tests execute in <50ms total; no visible flicker during menu navigation  
**Constraints**: Must maintain StepwiseTestRunner pattern for step-by-step debugging; must preserve existing ~130 visual test behaviors  
**Scale/Scope**: ~130 existing visual tests; 5 new renderables/wrappers; 5+ snapshot baselines

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Test-Driven Development** | ✅ PASS | Spec mandates tests first; existing ~130 visual tests preserved; new snapshot tests added before controller refactoring |
| **II. Dependency Injection** | ✅ PASS | New renderables are stateless value types; LiveRenderer uses constructor-injected IAnsiConsole |
| **III. Security by Design** | ✅ N/A | No security-sensitive functionality in visual rendering |
| **IV. Follow Existing Patterns** | ✅ PASS | Adopts Spectre.Console patterns (Renderable, LiveRenderable, TestConsole) which are already used in the solution |
| **V. Integration Testing** | ✅ PASS | StepwiseTestRunner retained for integration; Spectre-style completion tests added as complement |

**Gate Result**: ✅ PASS - All constitution principles satisfied. No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/009-spectre-visual-refactor/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output - Spectre pattern research
├── data-model.md        # Phase 1 output - Renderable entity definitions
├── quickstart.md        # Phase 1 output - Developer onboarding guide
├── contracts/           # Phase 1 output - Renderable interfaces/protocols
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
BitPantry.CommandLine/
├── AutoComplete/
│   ├── AutoCompleteController.cs       # REFACTOR: Use LiveRenderer instead of manual ANSI
│   ├── GhostTextRenderer.cs            # REFACTOR: Use GhostTextRenderable
│   ├── MenuState.cs                    # Existing: Menu items, selection, viewport
│   └── GhostState.cs                   # Existing: Ghost text state
│   └── Rendering/                      # NEW: Spectre rendering components
│       ├── AutoCompleteMenuRenderable.cs  # Vertical menu as Spectre Renderable
│       ├── GhostTextRenderable.cs         # Ghost text as Spectre Renderable
│       ├── MenuLiveRenderer.cs            # LiveRenderable pattern wrapper
│       └── AnsiCodes.cs                   # ANSI constant helpers

BitPantry.CommandLine.Tests/
├── VirtualConsole/                     # Test infrastructure
│   ├── ConsolidatedTestConsole.cs     # NEW: Wraps Spectre TestConsole + cursor tracking
│   ├── StepwiseTestRunner.cs          # REFACTOR: Use ConsolidatedTestConsole
│   ├── StepwiseTestRunnerAssertions.cs
│   └── [VirtualAnsiConsole.cs]        # DELETE after migration
├── Snapshots/                          # NEW: Snapshot test baselines
│   ├── ModuleInitializer.cs           # Verifier path configuration
│   └── *.verified.txt                 # ANSI snapshot files
└── AutoComplete/
    └── Visual/                         # Existing visual tests (~130)
```

**Structure Decision**: Single library project with test project. New rendering components added to `AutoComplete/Rendering/` subfolder. Test infrastructure consolidated in `VirtualConsole/` with new snapshot folder.

## Complexity Tracking

> No violations requiring justification. All constitution principles satisfied.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | — | — |
