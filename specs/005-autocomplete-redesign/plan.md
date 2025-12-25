# Implementation Plan: Autocomplete Redesign

**Branch**: `005-autocomplete-redesign` | **Date**: 2024-12-24 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-autocomplete-redesign/spec.md`

## Summary

Complete redesign of the autocomplete system to provide a modern, Fish/PowerShell-like experience with:
- **Completion menu**: Uses Spectre.Console `SelectionPrompt` for scrollable, searchable menu
- **Ghost suggestions**: Inline muted text showing best match from history/commands, accepted with Right Arrow
- **Async-aware remote support**: Loading indicators, cancellation, debouncing, caching
- **Uniform provider interface**: Built-in (file path, directory) and custom providers use same `ICompletionProvider` pattern
- **Attribute-based hints**: `[FilePath]`, `[DirectoryPath]`, `[CompletionValues(...)]` for declarative completion
- **Full replacement**: Removes all existing autocomplete code; this is the definitive system

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: Spectre.Console 0.49.1 (terminal rendering), Microsoft.Extensions.DependencyInjection 9.0.1, System.IO.Abstractions 21.0.29  
**Storage**: In-memory session cache only (100-item limit, 5-minute TTL)  
**Testing**: MSTest 3.6.1 with FluentAssertions 6.12.0, Moq 4.20.72, System.IO.Abstractions.TestingHelpers  
**Target Platform**: Cross-platform console applications (Windows, Linux, macOS)  
**Project Type**: Library (BitPantry.CommandLine) + Tests (BitPantry.CommandLine.Tests)  
**Performance Goals**: Local completions <50ms, cached <10ms, remote timeout 3s  
**Constraints**: ANSI terminal support required, 80-column minimum width  
**Scale/Scope**: 100-item cache limit, 10-row viewport, 5-minute cache TTL  

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-Driven Development | ✅ PASS | 95 test scenarios defined in spec; tests written before implementation |
| II. Dependency Injection | ✅ PASS | All providers resolved via DI; ICompletionProvider injected into orchestrator |
| III. Security by Design | ✅ PASS | File path completion respects file system permissions; no credential caching |
| IV. Follow Existing Patterns | ✅ PASS | Extends AutoComplete folder structure; uses SignalR RPC patterns for remote |
| V. Integration Testing | ✅ PASS | Remote completion tests, cache integration, menu rendering tests defined |

**Gate Result**: PASS - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/005-autocomplete-redesign/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
BitPantry.CommandLine/
├── AutoComplete/
│   ├── AutoCompleteController.cs        # DELETE - Replaced by CompletionOrchestrator
│   ├── AutoCompleteOption.cs            # DELETE - Replaced by CompletionItem
│   ├── AutoCompleteOptionSet.cs         # DELETE - Replaced by CompletionResult
│   ├── AutoCompleteOptionSetBuilder.cs  # DELETE - Logic moves to providers
│   ├── AutoCompleteContext.cs           # DELETE - Replaced by CompletionContext
│   ├── CompletionOrchestrator.cs        # NEW - Main orchestrator, uses SelectionPrompt
│   ├── CompletionContext.cs             # NEW - Context passed to providers
│   ├── CompletionItem.cs                # NEW - Single completion suggestion
│   ├── CompletionResult.cs              # NEW - Result from provider
│   ├── Providers/                       # NEW - Provider implementations
│   │   ├── ICompletionProvider.cs       # NEW - Uniform interface
│   │   ├── CommandCompletionProvider.cs # NEW - Commands/groups completion
│   │   ├── ArgumentNameProvider.cs      # NEW - --argName completion
│   │   ├── ArgumentAliasProvider.cs     # NEW - -a completion
│   │   ├── FilePathProvider.cs          # NEW - Built-in [FilePath] attribute
│   │   ├── DirectoryPathProvider.cs     # NEW - Built-in [DirectoryPath] attribute
│   │   └── EnumValueProvider.cs         # NEW - Auto-complete enum argument values
│   ├── Attributes/                      # NEW - Completion hint attributes
│   │   ├── FilePathAttribute.cs         # NEW - Marks argument for file completion
│   │   ├── DirectoryPathAttribute.cs    # NEW - Marks argument for directory completion
│   │   └── CompletionValuesAttribute.cs # NEW - Static list of values
│   ├── Cache/                           # NEW - Caching layer
│   │   ├── CompletionCache.cs           # NEW - Session cache with LRU eviction
│   │   └── CacheKey.cs                  # NEW - (command, arg, prefix) key
│   └── GhostTextRenderer.cs             # NEW - Inline ghost suggestions
├── Input/
│   ├── InputController.cs               # MODIFY - Wire new autocomplete
│   └── ConsoleLineMirror.cs             # KEEP - Reuse buffer management
├── API/
│   └── ArgumentAttribute.cs             # MODIFY - Remove AutoCompleteFunctionName

BitPantry.CommandLine.Tests/
├── AutoComplete/
│   ├── Orchestrator/                    # NEW - Orchestrator behavior tests
│   │   ├── OrchestratorTests.cs         # Unit tests with mocked providers
│   │   └── OrchestratorIntegrationTests.cs # Integration with real console
│   ├── Ghost/                           # NEW - Ghost suggestion tests
│   │   ├── GhostDisplayTests.cs
│   │   └── GhostAcceptTests.cs
│   ├── Providers/                       # NEW - Provider unit tests
│   │   ├── FilePathProviderTests.cs     # Uses MockFileSystem
│   │   ├── DirectoryPathProviderTests.cs
│   │   ├── CommandProviderTests.cs
│   │   ├── ArgumentNameProviderTests.cs
│   │   └── EnumValueProviderTests.cs
│   ├── Cache/                           # NEW - Cache tests
│   │   └── CompletionCacheTests.cs
│   └── Integration/                     # NEW - End-to-end tests
│       └── RemoteCompletionTests.cs     # Uses test server from existing infra

BitPantry.CommandLine.Tests.Remote.SignalR/
├── AutoComplete/                        # NEW - Remote completion tests
│   └── RemoteCompletionIntegrationTests.cs  # Uses existing TestEnvironment
```

**Structure Decision**: Complete replacement of existing AutoComplete folder. No backward compatibility - this is the definitive autocomplete system. Tests use existing infrastructure:
- `MockFileSystem` from System.IO.Abstractions.TestingHelpers for file providers
- `TestEnvironment` from BitPantry.CommandLine.Tests.Remote.SignalR for remote tests

## Complexity Tracking

No constitution violations requiring justification. Design follows existing patterns.

---

## Phase 0: Research ✅

See [research.md](research.md) for detailed findings.

### Research Completed

1. ✅ **Terminal rendering for completion menu** - ANSI escape sequences with save/restore cursor
2. ✅ **Ghost text rendering** - `\x1b[90m` (bright black/gray) for muted text
3. ✅ **Debounce pattern in C#** - CancellationTokenSource reset pattern
4. ✅ **File system completion edge cases** - System.IO.Abstractions with error handling
5. ✅ **Existing InputController integration points** - Key handler modifications mapped

---

## Phase 1: Design ✅

See [data-model.md](data-model.md) for entity definitions.
See [contracts/](contracts/) for interface definitions:
- [ICompletionProvider.md](contracts/ICompletionProvider.md) - Uniform provider interface
- [ICompletionOrchestrator.md](contracts/ICompletionOrchestrator.md) - Main orchestrator contract

See [quickstart.md](quickstart.md) for implementation guide.

---

## Phase 2: Tasks

See [tasks.md](tasks.md) for implementation tasks (generated by `/speckit.tasks`).
