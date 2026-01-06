# Implementation Plan: Menu Filtering While Typing

**Branch**: `010-menu-filter` | **Date**: 2026-01-03 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/010-menu-filter/spec.md`

## Summary

Implement real-time menu filtering for autocomplete: when the menu is open, typing filters items via case-insensitive substring matching with match highlighting. Backspace expands filter; Space closes menu (unless inside quotes). Remove trailing space on acceptance for consistency with ghost text.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: Spectre.Console (rendering, markup)  
**Storage**: N/A (in-memory state only)  
**Testing**: MSTest with FluentAssertions, StepwiseTestRunner for visual/UX tests  
**Target Platform**: Windows/Linux/macOS console applications  
**Project Type**: Single library project with test project  
**Performance Goals**: <50ms filter response for 100 items (imperceptible)  
**Constraints**: Non-modal menu integrated with live input editing; must not break existing 377+ tests  
**Scale/Scope**: Filtering lists of 1-100 items typically

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. TDD (NON-NEGOTIABLE)** | ✅ PASS | Tests written FIRST using StepwiseTestRunner; existing visual test infrastructure |
| **II. Dependency Injection** | ✅ PASS | No new DI required; uses existing AutoCompleteController/Orchestrator |
| **III. Security by Design** | ✅ PASS | No security concerns (local input filtering only) |
| **IV. Follow Existing Patterns** | ✅ PASS | Uses existing MenuState, CompletionMatcher, AutoCompleteMenuRenderable |
| **V. Integration Testing** | ✅ PASS | StepwiseTestRunner provides end-to-end visual testing |

**All gates PASS. Proceeding to Phase 0.**

## Project Structure

### Documentation (this feature)

```text
specs/010-menu-filter/
├── plan.md              # This file
├── research.md          # Phase 0 output - existing infrastructure analysis
├── data-model.md        # Phase 1 output - entity updates
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (existing repository structure)

```text
BitPantry.CommandLine/
├── AutoComplete/
│   ├── MenuState.cs                      # UPDATE: Use FilterText property
│   ├── CompletionMatcher.cs              # EXISTING: Has ContainsCaseInsensitive mode
│   ├── CompletionItem.cs                 # EXISTING: Has MatchRanges for highlighting
│   ├── CompletionOrchestrator.cs         # UPDATE: Fix HandleCharacterAsync to use substring match
│   └── Rendering/
│       └── AutoCompleteMenuRenderable.cs # UPDATE: Add match highlighting
├── Input/
│   └── InputBuilder.cs                   # UPDATE: Wire filtering in default handler
└── StringExtensions.cs                   # ADD: IsInsideQuotes() helper

BitPantry.CommandLine.Tests/
└── AutoComplete/
    └── Visual/
        ├── MenuBehaviorTests.cs          # UPDATE: Add filtering tests
        └── MenuFilteringTests.cs         # NEW: Dedicated filtering test class
```

**Structure Decision**: Existing structure; minimal new files needed. Most changes are wiring existing infrastructure.

## Complexity Tracking

> No Constitution Check violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
