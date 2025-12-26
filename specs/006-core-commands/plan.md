# Implementation Plan: Core CLI Commands & Prompt Redesign

**Branch**: `006-core-commands` | **Date**: 2025-12-26 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-core-commands/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature redesigns the core CLI command infrastructure: removes the obsolete `ListCommands` command, adds a `version` command, completely redesigns server connection commands (`connect`, `disconnect`, `status`), implements a server profile management system for saved connections, and replaces the manual prompt system with an extensible segment-based architecture.

**Technical Approach**: 
- Add new `VersionCommand` to core library with assembly version extraction
- Replace existing `ConnectCommand`/`DisconnectCommand` with redesigned implementations
- Add `ProfileManager` for profile CRUD operations with JSON storage
- Add `CredentialStore` for secure credential storage (DPAPI/libsodium)
- Implement `IPromptSegment`/`IPrompt` interfaces with `CompositePrompt` aggregator
- Create autocomplete provider for profile names

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: Spectre.Console, Microsoft.AspNetCore.SignalR.Client, System.Security.Cryptography.ProtectedData (Windows), Sodium.Core (cross-platform)  
**Storage**: JSON file for profiles, OS credential store + encrypted file fallback for credentials  
**Testing**: MSTest with FluentAssertions and Moq  
**Target Platform**: Windows, Linux, macOS  
**Project Type**: Multi-project .NET solution  
**Performance Goals**: Commands execute in <1 second, connection timeout 10 seconds (configurable)  
**Constraints**: Cross-platform credential storage, no breaking changes to existing command API patterns  
**Scale/Scope**: ~15 new/modified files across 2 projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-Driven Development | ✅ PASS | Tests written first for all new commands, profile manager, credential store |
| II. Dependency Injection | ✅ PASS | All services (IPromptSegment, ProfileManager, CredentialStore) registered in DI |
| III. Security by Design | ✅ PASS | Credentials in OS store/encrypted file, never in URLs, profile names validated |
| IV. Follow Existing Patterns | ✅ PASS | Commands follow `CommandBase` pattern, use existing `[Command]`, `[Argument]` attributes, autocomplete uses `ICompletionProvider` |
| V. Integration Testing | ✅ PASS | Server connection commands tested via existing `TestEnvironment` infrastructure |

**No violations requiring justification.**

## Project Structure

### Documentation (this feature)

```text
specs/006-core-commands/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
BitPantry.CommandLine/
├── Commands/
│   ├── ListCommandsCommand.cs    # DELETE
│   └── VersionCommand.cs         # NEW
├── Input/
│   ├── Prompt.cs                 # MODIFY (deprecate or remove)
│   ├── IPromptSegment.cs         # NEW
│   ├── IPrompt.cs                # NEW
│   ├── CompositePrompt.cs        # NEW
│   └── AppNameSegment.cs         # NEW
└── CommandLineApplicationBuilder.cs  # MODIFY

BitPantry.CommandLine.Remote.SignalR.Client/
├── ConnectCommand.cs             # REPLACE
├── DisconnectCommand.cs          # REPLACE
├── StatusCommand.cs              # NEW
├── ServerGroup.cs                # KEEP (exists)
├── ProfileGroup.cs               # NEW
├── Commands/
│   ├── ProfileListCommand.cs     # NEW
│   ├── ProfileAddCommand.cs      # NEW
│   ├── ProfileRemoveCommand.cs   # NEW
│   ├── ProfileShowCommand.cs     # NEW
│   ├── ProfileSetDefaultCommand.cs # NEW
│   └── ProfileSetKeyCommand.cs   # NEW
├── Profiles/
│   ├── ProfileManager.cs         # NEW
│   ├── ServerProfile.cs          # NEW
│   └── CredentialStore.cs        # NEW
├── AutoComplete/
│   └── ProfileNameProvider.cs    # NEW
├── Prompt/
│   ├── ServerConnectionSegment.cs # NEW
│   └── ProfileSegment.cs         # NEW
└── ClientLogic.cs                # MODIFY

BitPantry.CommandLine.Tests/
└── Commands/
    └── VersionCommandTests.cs    # NEW

BitPantry.CommandLine.Tests.Remote.SignalR/
├── ClientTests/
│   ├── ProfileManagerTests.cs    # NEW
│   ├── CredentialStoreTests.cs   # NEW
│   └── ProfileNameProviderTests.cs # NEW
├── IntegrationTests/
│   ├── IntegrationTests_Connect.cs      # NEW/REPLACE
│   ├── IntegrationTests_Disconnect.cs   # NEW
│   ├── IntegrationTests_Status.cs       # NEW
│   └── IntegrationTests_Profiles.cs     # NEW
└── Prompt/
    ├── CompositePromptTests.cs   # NEW
    └── SegmentTests.cs           # NEW

Docs/
├── CommandLine/
│   └── BuiltInCommands.md        # MODIFY
└── Remote/
    └── ServerCommands.md         # NEW or MODIFY
```

**Structure Decision**: Follows existing multi-project structure. New profile/prompt functionality organized into logical subdirectories within existing projects.

## Complexity Tracking

> No violations requiring justification. Feature follows existing patterns.
