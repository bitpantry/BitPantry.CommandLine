# Implementation Tasks: Server Profile Management

**Branch**: `009-server-profile` | **Date**: 2026-02-02 | **Spec**: [spec.md](spec.md)

## Task Format

```text
- [ ] T### [depends:T###,T###] @test-case:009:XX-### Description with file path
```

**Components**:
- **Checkbox**: `- [ ]` (marked `[X]` when completed)
- **Task ID**: Sequential (T001, T002, ...) globally unique
- **Dependencies**: `[depends:T001,T002]` — tasks that must complete first (omit if none)
- **Test Case**: `@test-case:009:XX-###` — exactly ONE test case ID from test-cases.md
- **Description**: Clear action with exact file path

---

## Phase 1: Setup (Infrastructure)

**Purpose**: Create foundational entities and interfaces — no behavioral test cases

- [ ] T001 @test-case:009:SETUP-001 Create ServerProfile entity in Profiles/ServerProfile.cs
- [ ] T002 [depends:T001] @test-case:009:SETUP-002 Create ProfileConfiguration storage model in Profiles/ProfileConfiguration.cs
- [ ] T003 [depends:T001] @test-case:009:SETUP-003 Create IProfileManager interface in Profiles/IProfileManager.cs
- [ ] T004 [depends:T001] @test-case:009:SETUP-004 Create ICredentialStore interface (internal) in Profiles/ICredentialStore.cs
- [ ] T005 [depends:T001] @test-case:009:SETUP-005 Add Sodium.Core package reference to csproj

---

## Phase 2: CredentialStore (TDD)

**Goal**: Implement secure credential storage with DPAPI (Windows) and libsodium (Linux/macOS)

### Windows DPAPI Tests

- [ ] T010 [depends:T004] @test-case:009:CS-001 Test Store_ValidApiKey_EncryptsWithDPAPI in CredentialStoreTests.cs
- [ ] T011 [depends:T004] @test-case:009:CS-002 Test Retrieve_StoredKey_DecryptsCorrectly in CredentialStoreTests.cs
- [ ] T012 [depends:T004] @test-case:009:CS-003 Test Retrieve_NonExistent_ReturnsNull in CredentialStoreTests.cs
- [ ] T013 [depends:T004] @test-case:009:CS-004 Test Remove_ExistingCredential_RemovesEntry in CredentialStoreTests.cs
- [ ] T014 [depends:T004] @test-case:009:CS-005 Test Exists_StoredCredential_ReturnsTrue in CredentialStoreTests.cs
- [ ] T015 [depends:T004] @test-case:009:CS-006 Test Exists_NonExistent_ReturnsFalse in CredentialStoreTests.cs

### libsodium Tests

- [ ] T016 [depends:T004,T005] @test-case:009:CS-010 Test Store_ValidApiKey_EncryptsWithSecretBox in CredentialStoreTests.cs
- [ ] T017 [depends:T004,T005] @test-case:009:CS-011 Test Retrieve_StoredKey_DecryptsCorrectly (libsodium) in CredentialStoreTests.cs
- [ ] T018 [depends:T004,T005] @test-case:009:CS-012 Test Store_LibsodiumUnavailable_ThrowsWithMessage in CredentialStoreTests.cs

### Cross-Platform Tests

- [ ] T019 [depends:T004] @test-case:009:CS-020 Test Store_EmptyApiKey_ThrowsValidation in CredentialStoreTests.cs
- [ ] T020 [depends:T004] @test-case:009:CS-021 Test Store_MultipleProfiles_IndependentStorage in CredentialStoreTests.cs
- [ ] T021 [depends:T004] @test-case:009:CS-022 Test Remove_AlsoRemovesFromProfile_OnDelete in CredentialStoreTests.cs

### CredentialStore Implementation

- [ ] T022 [depends:T010,T011,T012,T013,T014,T015,T016,T017,T018,T019,T020,T021] @test-case:009:IMPL-CS Implement CredentialStore in Profiles/CredentialStore.cs

---

## Phase 3: ProfileManager (TDD)

**Goal**: Implement profile CRUD, validation, and default profile management

### Basic CRUD Tests

- [ ] T030 [depends:T003] @test-case:009:PM-001 Test GetAllProfiles_EmptyStore_ReturnsEmptyList in ProfileManagerTests.cs
- [ ] T031 [depends:T003] @test-case:009:PM-002 Test GetAllProfiles_MultipleProfiles_ReturnsAll in ProfileManagerTests.cs
- [ ] T032 [depends:T003] @test-case:009:PM-003 Test GetProfile_ExistingProfile_ReturnsProfile in ProfileManagerTests.cs
- [ ] T033 [depends:T003] @test-case:009:PM-004 Test GetProfile_NonExistentProfile_ReturnsNull in ProfileManagerTests.cs
- [ ] T034 [depends:T003] @test-case:009:PM-005 Test GetProfile_CaseInsensitive_ReturnsProfile in ProfileManagerTests.cs
- [ ] T035 [depends:T003] @test-case:009:PM-006 Test CreateProfile_NewProfile_PersistsToStorage in ProfileManagerTests.cs
- [ ] T035A [depends:T003] @test-case:009:PM-006A Test CreateProfile_ExistingProfile_ThrowsException in ProfileManagerTests.cs
- [ ] T036 [depends:T003] @test-case:009:PM-007 Test UpdateProfile_ExistingProfile_UpdatesProfile in ProfileManagerTests.cs
- [ ] T036A [depends:T003] @test-case:009:PM-007A Test UpdateProfile_NonExistentProfile_ThrowsException in ProfileManagerTests.cs
- [ ] T037 [depends:T003] @test-case:009:PM-008 Test CreateProfile_SetsCreatedAt_OnNewProfile in ProfileManagerTests.cs
- [ ] T038 [depends:T003] @test-case:009:PM-009 Test DeleteProfile_ExistingProfile_ReturnsTrue in ProfileManagerTests.cs
- [ ] T039 [depends:T003] @test-case:009:PM-010 Test DeleteProfile_NonExistent_ReturnsFalse in ProfileManagerTests.cs
- [ ] T040 [depends:T003] @test-case:009:PM-011 Test DeleteProfile_RemovesFromStorage in ProfileManagerTests.cs

### Default Profile Tests

- [ ] T041 [depends:T003] @test-case:009:PM-020 Test GetDefaultProfileName_NoneSet_ReturnsNull in ProfileManagerTests.cs
- [ ] T042 [depends:T003] @test-case:009:PM-021 Test GetDefaultProfileName_WhenSet_ReturnsName in ProfileManagerTests.cs
- [ ] T043 [depends:T003] @test-case:009:PM-022 Test SetDefaultProfile_ValidName_PersistsDefault in ProfileManagerTests.cs
- [ ] T044 [depends:T003] @test-case:009:PM-023 Test SetDefaultProfile_Null_ClearsDefault in ProfileManagerTests.cs
- [ ] T045 [depends:T003] @test-case:009:PM-024 Test DeleteProfile_WasDefault_ClearsDefault in ProfileManagerTests.cs

### Validation Tests

- [ ] T046 [depends:T003] @test-case:009:PM-030 Test CreateProfile_EmptyName_ThrowsValidation in ProfileManagerTests.cs
- [ ] T047 [depends:T003] @test-case:009:PM-031 Test CreateProfile_InvalidCharacters_ThrowsValidation in ProfileManagerTests.cs
- [ ] T048 [depends:T003] @test-case:009:PM-032 Test CreateProfile_TooLongName_ThrowsValidation in ProfileManagerTests.cs
- [ ] T049 [depends:T003] @test-case:009:PM-033 Test CreateProfile_HyphenInName_Succeeds in ProfileManagerTests.cs
- [ ] T050 [depends:T003] @test-case:009:PM-034 Test CreateProfile_StartsWithHyphen_ThrowsValidation in ProfileManagerTests.cs
- [ ] T051 [depends:T003] @test-case:009:PM-035 Test CreateProfile_InvalidUri_ThrowsValidation in ProfileManagerTests.cs

### Edge Case Tests

- [ ] T052 [depends:T003] @test-case:009:PM-040 Test CreateProfile_CorruptedFile_RecreatesFile in ProfileManagerTests.cs
- [ ] T053 [depends:T003] @test-case:009:PM-041 Test GetAllProfiles_MissingFile_ReturnsEmpty in ProfileManagerTests.cs
- [ ] T054 [depends:T003] @test-case:009:PM-042 Test CreateProfile_DirectoryNotExists_CreatesDirectory in ProfileManagerTests.cs

### ProfileManager Implementation

- [ ] T055 [depends:T022,T030,T031,T032,T033,T034,T035,T036,T037,T038,T039,T040,T041,T042,T043,T044,T045,T046,T047,T048,T049,T050,T051,T052,T053,T054] @test-case:009:IMPL-PM Implement ProfileManager in Profiles/ProfileManager.cs

---

## Phase 4: Autocomplete Handler (TDD)

**Goal**: Implement profile name autocomplete with default indicator

- [ ] T060 [depends:T055] @test-case:009:AC-001 Test GetOptions_NoProfiles_ReturnsEmpty in ProfileNameProviderTests.cs
- [ ] T061 [depends:T055] @test-case:009:AC-002 Test GetOptions_MultipleProfiles_ReturnsAll in ProfileNameProviderTests.cs
- [ ] T062 [depends:T055] @test-case:009:AC-003 Test GetOptions_WithPrefix_FiltersResults in ProfileNameProviderTests.cs
- [ ] T063 [depends:T055] @test-case:009:AC-004 Test GetOptions_CaseInsensitivePrefix_Matches in ProfileNameProviderTests.cs
- [ ] T064 [depends:T055] @test-case:009:AC-005 Test GetOptions_IncludesDefault_MarksIndicator in ProfileNameProviderTests.cs

### ProfileNameProvider Implementation

- [ ] T066 [depends:T060,T061,T062,T063,T064] @test-case:009:IMPL-AC Implement ProfileNameProvider in AutoComplete/ProfileNameProvider.cs

---

## Phase 5: Profile Commands (TDD)

**Goal**: Implement profile management commands

### ProfileGroup Setup

- [X] T070 [depends:T055] @test-case:009:SETUP-006 Create ProfileGroup in Commands/Server/Profile/ProfileGroup.cs

### profile add Command

- [X] T071 [depends:T070] @test-case:009:CMD-ADD-001 Test Add_ValidProfile_CreatesProfile in ProfileCommandTests.cs
- [X] T072 [depends:T070] @test-case:009:CMD-ADD-002 Test Add_DuplicateName_ShowsError in ProfileCommandTests.cs
- [X] T073 [depends:T070] @test-case:009:CMD-ADD-003 Test Add_WithApiKey_StoresCredential in ProfileCommandTests.cs
- [X] T074 [depends:T070] @test-case:009:CMD-ADD-004 Test Add_WithApiKeyPrompt_MasksInput in ProfileCommandTests.cs
- [X] T075 [depends:T070] @test-case:009:CMD-ADD-005 Test Add_InvalidUri_ShowsError in ProfileCommandTests.cs
- [X] T076 [depends:T070] @test-case:009:CMD-ADD-006 Test Add_SetAsDefault_SetsDefault in ProfileCommandTests.cs
- [X] T077 [depends:T071,T072,T073,T074,T075,T076] @test-case:009:IMPL-ADD Implement ProfileAddCommand in Commands/Server/Profile/ProfileAddCommand.cs

### profile list Command

- [X] T078 [depends:T070] @test-case:009:CMD-LST-001 Test List_NoProfiles_ShowsEmptyMessage in ProfileCommandTests.cs
- [ ] T079 [depends:T070] @test-case:009:CMD-LST-002 Test List_MultipleProfiles_ShowsTable in ProfileCommandTests.cs
- [ ] T080 [depends:T070] @test-case:009:CMD-LST-003 Test List_MarksDefault_WithIndicator in ProfileCommandTests.cs
- [ ] T081 [depends:T070] @test-case:009:CMD-LST-004 Test List_IncludesCredentials_Column in ProfileCommandTests.cs
- [ ] T082 [depends:T078,T079,T080,T081] @test-case:009:IMPL-LST Implement ProfileListCommand in Commands/Server/Profile/ProfileListCommand.cs

### profile show Command

- [ ] T083 [depends:T066,T070] @test-case:009:CMD-SHW-001 Test Show_ExistingProfile_DisplaysDetails in ProfileCommandTests.cs
- [ ] T084 [depends:T070] @test-case:009:CMD-SHW-002 Test Show_NonExistent_ShowsError in ProfileCommandTests.cs
- [ ] T085 [depends:T070] @test-case:009:CMD-SHW-003 Test Show_WithCredential_ShowsMasked in ProfileCommandTests.cs
- [ ] T086 [depends:T066,T070] @test-case:009:CMD-SHW-004 Test Show_ProfileNameAutocomplete_Works in ProfileCommandTests.cs
- [ ] T087 [depends:T083,T084,T085,T086] @test-case:009:IMPL-SHW Implement ProfileShowCommand in Commands/Server/Profile/ProfileShowCommand.cs

### profile remove Command

- [ ] T088 [depends:T066,T070] @test-case:009:CMD-RMV-001 Test Remove_ExistingProfile_DeletesProfile in ProfileCommandTests.cs
- [ ] T089 [depends:T070] @test-case:009:CMD-RMV-002 Test Remove_NonExistent_ShowsError in ProfileCommandTests.cs
- [ ] T090 [depends:T070] @test-case:009:CMD-RMV-003 Test Remove_AlsoRemoves_Credential in ProfileCommandTests.cs
- [ ] T091 [depends:T070] @test-case:009:CMD-RMV-004 Test Remove_WasDefault_ClearsDefault in ProfileCommandTests.cs
- [ ] T092 [depends:T088,T089,T090,T091] @test-case:009:IMPL-RMV Implement ProfileRemoveCommand in Commands/Server/Profile/ProfileRemoveCommand.cs

### profile set-default Command

- [ ] T093 [depends:T066,T070] @test-case:009:CMD-DEF-001 Test SetDefault_ExistingProfile_SetsDefault in ProfileCommandTests.cs
- [ ] T094 [depends:T070] @test-case:009:CMD-DEF-002 Test SetDefault_NonExistent_ShowsError in ProfileCommandTests.cs
- [ ] T095 [depends:T070] @test-case:009:CMD-DEF-003 Test SetDefault_ClearWithNone_ClearsDefault in ProfileCommandTests.cs
- [ ] T096 [depends:T093,T094,T095] @test-case:009:IMPL-DEF Implement ProfileSetDefaultCommand in Commands/Server/Profile/ProfileSetDefaultCommand.cs

### profile set-key Command

- [ ] T097 [depends:T066,T070] @test-case:009:CMD-KEY-001 Test SetKey_ExistingProfile_UpdatesCredential in ProfileCommandTests.cs
- [ ] T098 [depends:T070] @test-case:009:CMD-KEY-002 Test SetKey_NonExistent_ShowsError in ProfileCommandTests.cs
- [ ] T099 [depends:T070] @test-case:009:CMD-KEY-003 Test SetKey_PromptsWithMasking_WhenNoValue in ProfileCommandTests.cs
- [ ] T100 [depends:T070] @test-case:009:CMD-KEY-004 Test SetKey_EmptyInput_ShowsError in ProfileCommandTests.cs
- [ ] T101 [depends:T097,T098,T099,T100] @test-case:009:IMPL-KEY Implement ProfileSetKeyCommand in Commands/Server/Profile/ProfileSetKeyCommand.cs

---

## Phase 6: ConnectCommand Integration (TDD)

**Goal**: Add --profile support to existing ConnectCommand

- [ ] T110 [depends:T055,T066] @test-case:009:CMD-CON-001 Test Connect_WithProfile_UsesProfileSettings in ConnectProfileTests.cs
- [ ] T111 [depends:T055] @test-case:009:CMD-CON-002 Test Connect_ProfileNoCredential_PromptsForKey in ConnectProfileTests.cs
- [ ] T112 [depends:T055] @test-case:009:CMD-CON-003 Test Connect_ProfileAndUri_UriOverrides in ConnectProfileTests.cs
- [ ] T113 [depends:T055] @test-case:009:CMD-CON-004 Test Connect_ProfileNotFound_ShowsError in ConnectProfileTests.cs
- [ ] T114 [depends:T055] @test-case:009:CMD-CON-005 Test Connect_NoProfileNoUri_UsesDefault in ConnectProfileTests.cs
- [ ] T115 [depends:T066] @test-case:009:CMD-CON-006 Test Connect_ProfileAutocomplete_Works in ConnectProfileTests.cs
- [ ] T116 [depends:T110,T111,T112,T113,T114,T115] @test-case:009:IMPL-CON Modify ConnectCommand for --profile support in Commands/Server/ConnectCommand.cs

---

## Phase 7: DI Registration

**Goal**: Wire up all services in the DI container

- [ ] T120 [depends:T022,T055,T066,T077,T082,T087,T092,T096,T101,T116] @test-case:009:SETUP-007 Register profile services in CommandLineApplicationBuilderExtensions.cs

---

## Phase 8: Prompt Segment (TDD)

**Goal**: Display connected profile name in command prompt (US-7, FR-026-028)

- [ ] T150 [depends:T120] @test-case:009:PS-001 Test ConnectedViaProfile_PromptShowsProfileName in PromptSegmentTests.cs
- [ ] T151 [depends:T150] @test-case:009:PS-002 Test ConnectedViaProfile_PromptShowsBracketFormat in PromptSegmentTests.cs
- [ ] T152 [depends:T150] @test-case:009:PS-003 Test NotConnected_PromptHidesProfileSegment in PromptSegmentTests.cs
- [ ] T153 [depends:T150] @test-case:009:PS-004 Test ConnectedDirectUri_PromptHidesProfileSegment in PromptSegmentTests.cs
- [ ] T154 [depends:T150,T151,T152,T153] @test-case:009:IMPL-PS Implement ProfilePromptSegment in Prompt/ProfilePromptSegment.cs

---

## Phase 9: Integration Tests

**Goal**: Verify end-to-end workflows across platforms

### End-to-End Workflows

- [ ] T130 [depends:T120] @test-case:009:INT-001 Test FullWorkflow_AddListShowRemove in ProfileIntegrationTests.cs
- [ ] T131 [depends:T120] @test-case:009:INT-002 Test FullWorkflow_AddSetDefaultConnect in ProfileIntegrationTests.cs
- [ ] T132 [depends:T120] @test-case:009:INT-003 Test FullWorkflow_UpdateCredential in ProfileIntegrationTests.cs
- [ ] T133 [depends:T120] @test-case:009:INT-004 Test FullWorkflow_MultipleProfiles in ProfileIntegrationTests.cs

### Cross-Platform

- [ ] T134 [depends:T120] @test-case:009:XP-001 Test Encryption_Windows_UseDPAPI in ProfileIntegrationTests.cs
- [ ] T135 [depends:T120] @test-case:009:XP-002 Test Encryption_Linux_UseLibsodium in ProfileIntegrationTests.cs
- [ ] T136 [depends:T120] @test-case:009:XP-003 Test Encryption_MacOS_UseLibsodium in ProfileIntegrationTests.cs
- [ ] T137 [depends:T120] @test-case:009:XP-004 Test Credential_DifferentMachine_FailsDecrypt in ProfileIntegrationTests.cs

### Error Handling

- [ ] T138 [depends:T120] @test-case:009:ERR-001 Test StorageInaccessible_ShowsErrorMessage in ProfileIntegrationTests.cs
- [ ] T139 [depends:T120] @test-case:009:ERR-002 Test CorruptedCredentials_ShowsReenterMessage in ProfileIntegrationTests.cs
- [ ] T140 [depends:T120] @test-case:009:ERR-003 Test LibsodiumMissing_ShowsInstallInstructions in ProfileIntegrationTests.cs

---

## Task Summary

| Phase | Task Range | Count | Description |
|-------|------------|-------|-------------|
| 1: Setup | T001-T005 | 5 | Entities, interfaces, package refs |
| 2: CredentialStore | T010-T022 | 13 | Encryption tests + implementation |
| 3: ProfileManager | T030-T055 | 26 | CRUD, validation, edge case tests + implementation |
| 4: Autocomplete | T060-T066 | 6 | ProfileNameProvider tests + implementation |
| 5: Commands | T070-T101 | 32 | All profile commands |
| 6: Connect Integration | T110-T116 | 7 | ConnectCommand --profile support |
| 7: DI Registration | T120 | 1 | Service wiring |
| 8: Prompt Segment | T150-T154 | 5 | Prompt display for connected profile |
| 9: Integration Tests | T130-T140 | 11 | E2E and cross-platform tests |

**Total Tasks**: 106  
**Test Case Tasks**: 79  
**Setup/Implementation Tasks**: 27

---

## Test Case Validation

- [X] Every test case from test-cases.md has exactly ONE task
- [X] Every task (except SETUP/IMPL) has exactly ONE `@test-case:` reference
- [X] Dependencies form a valid DAG (no circular dependencies)
- [X] Task IDs are sequential and unique
- [X] Each task has a clear file path in description
