# Implementation Tasks: Server Profile Management

**Branch**: `009-server-profile` | **Date**: 2026-02-02 | **Spec**: [spec.md](spec.md)

## Task Ordering Principles

1. **TDD**: Tests written before implementation
2. **Bottom-up**: Core services before commands
3. **Dependencies**: Each task builds on previous completed work
4. **Incremental**: Small, reviewable units of work

---

## Phase 1: Foundation (Entities & Interfaces)

### T001 ✅ Create ServerProfile entity
- [ ] Create `Profiles/ServerProfile.cs` with Name, Uri, ApiKey (JsonIgnore), CreatedAt, ModifiedAt
- [ ] Add XML documentation
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Profiles/ServerProfile.cs`
- **Tests**: None (POCO)

### T002 ✅ Create ProfileConfiguration storage model  
- [ ] Create `Profiles/ProfileConfiguration.cs` with DefaultProfile, Profiles dictionary, Version
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Profiles/ProfileConfiguration.cs`
- **Tests**: None (POCO)

### T003 ✅ Create IProfileManager interface
- [ ] Create `Profiles/IProfileManager.cs` with all methods from data-model.md
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Profiles/IProfileManager.cs`
- **Tests**: None (interface)

### T004 ✅ Create ICredentialStore interface (internal)
- [ ] Create `Profiles/ICredentialStore.cs` as internal interface
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Profiles/ICredentialStore.cs`
- **Tests**: None (interface)

---

## Phase 2: Credential Store (TDD)

### T010 🧪 Write CredentialStore unit tests
- [ ] Create `ProfileTests/CredentialStoreTests.cs`
- [ ] Implement tests: CS-001 through CS-006 (Windows DPAPI)
- [ ] Implement tests: CS-010 through CS-012 (libsodium - conditional)
- [ ] Implement tests: CS-020 through CS-022 (cross-platform)
- **File**: `BitPantry.CommandLine.Tests.Remote.SignalR/ProfileTests/CredentialStoreTests.cs`
- **Depends**: T004

### T011 ✅ Implement CredentialStore
- [ ] Create `Profiles/CredentialStore.cs`
- [ ] Implement platform detection (RuntimeInformation.IsOSPlatform)
- [ ] Implement DPAPI encryption for Windows
- [ ] Implement libsodium encryption for Linux/macOS
- [ ] Implement machine ID retrieval for key derivation
- [ ] Handle DllNotFoundException with actionable error (FR-017a)
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Profiles/CredentialStore.cs`
- **Depends**: T010 (tests first)
- **Verify**: All T010 tests pass

---

## Phase 3: Profile Manager (TDD)

### T020 🧪 Write ProfileManager unit tests - CRUD
- [ ] Create `ProfileTests/ProfileManagerTests.cs`
- [ ] Implement tests: PM-001 through PM-011 (basic CRUD)
- [ ] Use MockFileSystem for file operations
- [ ] Mock ICredentialStore for credential operations
- **File**: `BitPantry.CommandLine.Tests.Remote.SignalR/ProfileTests/ProfileManagerTests.cs`
- **Depends**: T003, T004

### T021 🧪 Write ProfileManager unit tests - Default & Validation
- [ ] Add tests: PM-020 through PM-024 (default profile)
- [ ] Add tests: PM-030 through PM-035 (validation)
- [ ] Add tests: PM-040 through PM-042 (edge cases)
- **File**: `BitPantry.CommandLine.Tests.Remote.SignalR/ProfileTests/ProfileManagerTests.cs`
- **Depends**: T020

### T022 ✅ Implement ProfileManager
- [ ] Create `Profiles/ProfileManager.cs`
- [ ] Inject IFileSystem, ICredentialStore
- [ ] Implement JSON serialization with System.Text.Json
- [ ] Implement path resolution (~/.bitpantry/commandline/profiles/)
- [ ] Implement atomic writes (temp file + rename)
- [ ] Implement profile name validation
- [ ] Implement credential integration (populate ApiKey on GetProfileAsync)
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Profiles/ProfileManager.cs`
- **Depends**: T020, T021 (tests first), T011
- **Verify**: All T020, T021 tests pass

---

## Phase 4: Autocomplete Handler (TDD)

### T030 🧪 Write ProfileNameProvider tests
- [ ] Create autocomplete tests section in ProfileManagerTests or separate file
- [ ] Implement tests: AC-001 through AC-005
- **File**: `BitPantry.CommandLine.Tests.Remote.SignalR/ProfileTests/ProfileNameProviderTests.cs`
- **Depends**: T022

### T031 ✅ Implement ProfileNameProvider
- [ ] Create `AutoComplete/ProfileNameProvider.cs`
- [ ] Implement IAutoCompleteHandler
- [ ] Inject IProfileManager
- [ ] Filter by prefix, mark default with indicator
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/AutoComplete/ProfileNameProvider.cs`
- **Depends**: T030 (tests first)
- **Verify**: All T030 tests pass

---

## Phase 5: Profile Commands (TDD)

### T040 ✅ Create ProfileGroup
- [ ] Create `Commands/Server/Profile/ProfileGroup.cs`
- [ ] Use [Group(Name = "profile")] attribute
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/Profile/ProfileGroup.cs`
- **Tests**: None (attribute-only)

### T041 🧪 Write ProfileAddCommand tests
- [ ] Create `ProfileTests/ProfileCommandTests.cs`
- [ ] Implement tests: CMD-ADD-001 through CMD-ADD-006
- **File**: `BitPantry.CommandLine.Tests.Remote.SignalR/ProfileTests/ProfileCommandTests.cs`
- **Depends**: T022, T040

### T042 ✅ Implement ProfileAddCommand
- [ ] Create `Commands/Server/Profile/ProfileAddCommand.cs`
- [ ] Arguments: name (positional), --uri/-u, --api-key/-k, --default
- [ ] Inject IProfileManager
- [ ] Handle masked input for API key
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/Profile/ProfileAddCommand.cs`
- **Depends**: T041 (tests first)
- **Verify**: CMD-ADD tests pass

### T043 🧪 Write ProfileListCommand tests
- [ ] Add tests: CMD-LST-001 through CMD-LST-004
- **Depends**: T041

### T044 ✅ Implement ProfileListCommand
- [ ] Create `Commands/Server/Profile/ProfileListCommand.cs`
- [ ] Display table with Name, URI, Default, Has Credential columns
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/Profile/ProfileListCommand.cs`
- **Depends**: T043 (tests first)
- **Verify**: CMD-LST tests pass

### T045 🧪 Write ProfileShowCommand tests
- [ ] Add tests: CMD-SHW-001 through CMD-SHW-004
- **Depends**: T041

### T046 ✅ Implement ProfileShowCommand
- [ ] Create `Commands/Server/Profile/ProfileShowCommand.cs`
- [ ] Argument: name (positional) with ProfileNameProvider autocomplete
- [ ] Display profile details, mask API key
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/Profile/ProfileShowCommand.cs`
- **Depends**: T045 (tests first), T031
- **Verify**: CMD-SHW tests pass

### T047 🧪 Write ProfileRemoveCommand tests
- [ ] Add tests: CMD-RMV-001 through CMD-RMV-004
- **Depends**: T041

### T048 ✅ Implement ProfileRemoveCommand
- [ ] Create `Commands/Server/Profile/ProfileRemoveCommand.cs`
- [ ] Argument: name (positional) with autocomplete
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/Profile/ProfileRemoveCommand.cs`
- **Depends**: T047 (tests first)
- **Verify**: CMD-RMV tests pass

### T049 🧪 Write ProfileSetDefaultCommand tests
- [ ] Add tests: CMD-DEF-001 through CMD-DEF-003
- **Depends**: T041

### T050 ✅ Implement ProfileSetDefaultCommand
- [ ] Create `Commands/Server/Profile/ProfileSetDefaultCommand.cs`
- [ ] Argument: name (positional) with autocomplete, --none flag
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/Profile/ProfileSetDefaultCommand.cs`
- **Depends**: T049 (tests first)
- **Verify**: CMD-DEF tests pass

### T051 🧪 Write ProfileSetKeyCommand tests
- [ ] Add tests: CMD-KEY-001 through CMD-KEY-004
- **Depends**: T041

### T052 ✅ Implement ProfileSetKeyCommand
- [ ] Create `Commands/Server/Profile/ProfileSetKeyCommand.cs`
- [ ] Arguments: name (positional), --api-key/-k (prompts if not provided)
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/Profile/ProfileSetKeyCommand.cs`
- **Depends**: T051 (tests first)
- **Verify**: CMD-KEY tests pass

---

## Phase 6: ConnectCommand Integration (TDD)

### T060 🧪 Write ConnectCommand --profile tests
- [ ] Add tests: CMD-CON-001 through CMD-CON-006
- **File**: `BitPantry.CommandLine.Tests.Remote.SignalR/ProfileTests/ConnectProfileTests.cs`
- **Depends**: T022

### T061 ✅ Modify ConnectCommand for --profile support
- [ ] Add --profile/-p argument with ProfileNameProvider autocomplete
- [ ] Inject IProfileManager
- [ ] Resolve profile → use profile.Uri and profile.ApiKey
- [ ] Handle --profile + --uri override (--uri wins)
- [ ] Handle missing credential → prompt for API key
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/ConnectCommand.cs` (MODIFY)
- **Depends**: T060 (tests first)
- **Verify**: CMD-CON tests pass

---

## Phase 7: DI Registration & Bootstrap

### T070 ✅ Register profile services in ConfigureSignalRClient
- [ ] Add ICredentialStore → CredentialStore (Singleton)
- [ ] Add IProfileManager → ProfileManager (Singleton)
- [ ] Add ProfileNameProvider (Transient)
- [ ] Register all profile commands
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/CommandLineApplicationBuilderExtensions.cs` (MODIFY)
- **Depends**: T022, T031, T042-T052

### T071 ✅ Add Sodium.Core package reference
- [ ] Add `<PackageReference Include="Sodium.Core" Version="1.3.7" />` to csproj
- **File**: `BitPantry.CommandLine.Remote.SignalR.Client/BitPantry.CommandLine.Remote.SignalR.Client.csproj` (MODIFY)
- **Depends**: T011

---

## Phase 8: Integration Tests

### T080 🧪 Write end-to-end integration tests
- [ ] Create `ProfileTests/ProfileIntegrationTests.cs`
- [ ] Implement tests: INT-001 through INT-004 (workflows)
- [ ] Implement tests: XP-001 through XP-004 (cross-platform)
- [ ] Implement tests: ERR-001 through ERR-003 (error handling)
- **File**: `BitPantry.CommandLine.Tests.Remote.SignalR/ProfileTests/ProfileIntegrationTests.cs`
- **Depends**: T070

---

## Task Summary

| Phase | Tasks | Description |
|-------|-------|-------------|
| 1 | T001-T004 | Foundation (entities, interfaces) |
| 2 | T010-T011 | CredentialStore (TDD) |
| 3 | T020-T022 | ProfileManager (TDD) |
| 4 | T030-T031 | Autocomplete (TDD) |
| 5 | T040-T052 | Profile commands (TDD) |
| 6 | T060-T061 | ConnectCommand integration (TDD) |
| 7 | T070-T071 | DI & packages |
| 8 | T080 | Integration tests |

**Total**: 26 tasks  
**Test-first tasks**: 12 (🧪)  
**Implementation tasks**: 14 (✅)

---

## Completion Checklist

- [ ] All unit tests pass (`dotnet test`)
- [ ] All integration tests pass
- [ ] Code coverage meets targets (ProfileManager 95%, CredentialStore 90%, Commands 85%)
- [ ] No compiler warnings
- [ ] XML documentation complete
- [ ] Manual smoke test on Windows
- [ ] Manual smoke test on Linux/macOS (if available)
