# Test Cases: Server Profile Management

**Branch**: `009-server-profile` | **Date**: 2026-02-02 | **Phase**: 2

## Test Naming Convention

`[Category]_[Scenario]_[ExpectedResult]`

## Unit Tests

### ProfileManager Tests

#### Basic CRUD Operations

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| PM-001 | GetAllProfiles_EmptyStore_ReturnsEmptyList | No profiles saved, returns empty collection | P0 |
| PM-002 | GetAllProfiles_MultipleProfiles_ReturnsAll | Returns all saved profiles | P0 |
| PM-003 | GetProfile_ExistingProfile_ReturnsProfile | Retrieves profile by exact name | P0 |
| PM-004 | GetProfile_NonExistentProfile_ReturnsNull | Missing profile returns null | P0 |
| PM-005 | GetProfile_CaseInsensitive_ReturnsProfile | "Production" matches "production" | P0 |
| PM-006 | CreateProfile_NewProfile_PersistsToStorage | Creates new profile in JSON file | P0 |
| PM-006A | CreateProfile_ExistingProfile_ThrowsException | Throws InvalidOperationException if profile exists | P0 |
| PM-007 | UpdateProfile_ExistingProfile_UpdatesProfile | Updates existing profile, sets ModifiedAt | P0 |
| PM-007A | UpdateProfile_NonExistentProfile_ThrowsException | Throws InvalidOperationException if profile doesn't exist | P0 |
| PM-008 | CreateProfile_SetsCreatedAt_OnNewProfile | CreatedAt is set on new profile | P1 |
| PM-009 | DeleteProfile_ExistingProfile_ReturnsTrue | Removes profile, returns true | P0 |
| PM-010 | DeleteProfile_NonExistent_ReturnsFalse | Non-existent profile, returns false | P0 |
| PM-011 | DeleteProfile_RemovesFromStorage | Profile no longer retrievable after delete | P0 |

#### Default Profile Operations

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| PM-020 | GetDefaultProfileName_NoneSet_ReturnsNull | No default configured | P0 |
| PM-021 | GetDefaultProfileName_WhenSet_ReturnsName | Returns configured default name | P0 |
| PM-022 | SetDefaultProfile_ValidName_PersistsDefault | Sets default, persists to storage | P0 |
| PM-023 | SetDefaultProfile_Null_ClearsDefault | Passing null clears default | P0 |
| PM-024 | DeleteProfile_WasDefault_ClearsDefault | Deleting default profile clears the default setting | P1 |

#### Validation

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| PM-030 | CreateProfile_EmptyName_ThrowsValidation | Empty name throws ArgumentException | P0 |
| PM-031 | CreateProfile_InvalidCharacters_ThrowsValidation | Special chars like `@#$` throw | P0 |
| PM-032 | CreateProfile_TooLongName_ThrowsValidation | Name > 64 chars throws | P1 |
| PM-033 | CreateProfile_HyphenInName_Succeeds | "my-profile" is valid | P0 |
| PM-034 | CreateProfile_StartsWithHyphen_ThrowsValidation | "-profile" is invalid | P1 |
| PM-035 | CreateProfile_InvalidUri_ThrowsValidation | Malformed URI throws | P0 |

#### Concurrency & Edge Cases

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| PM-040 | CreateProfile_CorruptedFile_RecreatesFile | Corrupted JSON handled gracefully | P1 |
| PM-041 | GetAllProfiles_MissingFile_ReturnsEmpty | No profiles.json file, returns empty | P0 |
| PM-042 | CreateProfile_DirectoryNotExists_CreatesDirectory | Creates ~/.bitpantry/commandline/profiles/ | P0 |

### CredentialStore Tests

#### Windows DPAPI

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CS-001 | Store_ValidApiKey_EncryptsWithDPAPI | API key stored encrypted | P0 |
| CS-002 | Retrieve_StoredKey_DecryptsCorrectly | Roundtrip encrypt/decrypt works | P0 |
| CS-003 | Retrieve_NonExistent_ReturnsNull | Missing credential returns null | P0 |
| CS-004 | Remove_ExistingCredential_RemovesEntry | Credential removed from store | P0 |
| CS-005 | Exists_StoredCredential_ReturnsTrue | Credential exists check | P0 |
| CS-006 | Exists_NonExistent_ReturnsFalse | Non-existent credential check | P0 |

#### Linux/macOS libsodium

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CS-010 | Store_ValidApiKey_EncryptsWithSecretBox | API key stored with libsodium | P0 |
| CS-011 | Retrieve_StoredKey_DecryptsCorrectly | Roundtrip works on non-Windows | P0 |
| CS-012 | Store_LibsodiumUnavailable_ThrowsWithMessage | Clear error message with install instructions | P0 |

#### Cross-Platform

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CS-020 | Store_EmptyApiKey_ThrowsValidation | Empty string rejected | P0 |
| CS-021 | Store_MultipleProfiles_IndependentStorage | Each profile has separate credential | P1 |
| CS-022 | Remove_AlsoRemovesFromProfile_OnDelete | Credential removed when profile deleted | P1 |

### ProfileNameProvider Tests (Autocomplete)

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| AC-001 | GetOptions_NoProfiles_ReturnsEmpty | Empty autocomplete list | P1 |
| AC-002 | GetOptions_MultipleProfiles_ReturnsAll | All profile names returned | P0 |
| AC-003 | GetOptions_WithPrefix_FiltersResults | "pro" filters to "production" | P0 |
| AC-004 | GetOptions_CaseInsensitivePrefix_Matches | "PRO" matches "production" | P1 |
| AC-005 | GetOptions_IncludesDefault_MarksIndicator | Default profile marked with "(default)" | P1 |
| AC-006 | GetOptions_Performance_Under100ms | Autocomplete completes in < 100ms | P2 |

## Command Tests

### profile add Command

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CMD-ADD-001 | Add_ValidProfile_CreatesProfile | `server profile add prod -u https://api.com` creates profile | P0 |
| CMD-ADD-002 | Add_DuplicateName_ShowsError | Duplicate name shows error | P0 |
| CMD-ADD-003 | Add_WithApiKey_StoresCredential | `-k` stores encrypted credential | P0 |
| CMD-ADD-004 | Add_WithApiKeyPrompt_MasksInput | `--api-key` without value prompts with masking | P0 |
| CMD-ADD-005 | Add_InvalidUri_ShowsError | Malformed URI shows validation error | P0 |
| CMD-ADD-006 | Add_SetAsDefault_SetsDefault | `--default` flag sets as default | P1 |

### profile list Command

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CMD-LST-001 | List_NoProfiles_ShowsEmptyMessage | "No profiles configured" message | P0 |
| CMD-LST-002 | List_MultipleProfiles_ShowsTable | Table with Name, URI, Default columns | P0 |
| CMD-LST-003 | List_MarksDefault_WithIndicator | Default profile marked with `*` | P0 |
| CMD-LST-004 | List_IncludesAutocomplete_Column | Autocomplete column shows Yes/No for credentials | P1 |

### profile show Command

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CMD-SHW-001 | Show_ExistingProfile_DisplaysDetails | Shows name, URI, created date | P0 |
| CMD-SHW-002 | Show_NonExistent_ShowsError | "Profile 'x' not found" error | P0 |
| CMD-SHW-003 | Show_WithCredential_ShowsMasked | Shows "API Key: ****" not actual key | P0 |
| CMD-SHW-004 | Show_ProfileNameAutocomplete_Works | Tab completion for profile name | P1 |

### profile remove Command

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CMD-RMV-001 | Remove_ExistingProfile_DeletesProfile | Profile removed from storage | P0 |
| CMD-RMV-002 | Remove_NonExistent_ShowsError | "Profile 'x' not found" error | P0 |
| CMD-RMV-003 | Remove_AlsoRemoves_Credential | Associated credential deleted | P0 |
| CMD-RMV-004 | Remove_WasDefault_ClearsDefault | Removes default setting | P1 |

### profile set-default Command

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CMD-DEF-001 | SetDefault_ExistingProfile_SetsDefault | Profile becomes default | P0 |
| CMD-DEF-002 | SetDefault_NonExistent_ShowsError | "Profile 'x' not found" error | P0 |
| CMD-DEF-003 | SetDefault_ClearWithNone_ClearsDefault | `--none` flag clears default | P1 |

### profile set-key Command

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CMD-KEY-001 | SetKey_ExistingProfile_UpdatesCredential | New API key stored | P0 |
| CMD-KEY-002 | SetKey_NonExistent_ShowsError | "Profile 'x' not found" error | P0 |
| CMD-KEY-003 | SetKey_PromptsWithMasking_WhenNoValue | Masked input for security | P0 |
| CMD-KEY-004 | SetKey_EmptyInput_ShowsError | Empty API key rejected | P1 |

### server connect --profile Integration

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CMD-CON-001 | Connect_WithProfile_UsesProfileSettings | Connects using profile's URI and API key | P0 |
| CMD-CON-002 | Connect_ProfileNoCredential_PromptsForKey | Missing credential prompts for API key | P0 |
| CMD-CON-003 | Connect_ProfileAndUri_UriOverrides | `--profile prod --uri https://other.com` uses --uri | P0 |
| CMD-CON-004 | Connect_ProfileNotFound_ShowsError | "Profile 'x' not found" error | P0 |
| CMD-CON-005 | Connect_NoProfileNoUri_UsesDefault | Uses default profile when no args | P1 |
| CMD-CON-006 | Connect_ProfileAutocomplete_Works | Tab completion for --profile | P1 |

## Integration Tests

### End-to-End Workflows

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| INT-001 | FullWorkflow_AddListShowRemove | Complete profile lifecycle | P0 |
| INT-002 | FullWorkflow_AddSetDefaultConnect | Add profile, set default, connect | P0 |
| INT-003 | FullWorkflow_UpdateCredential | Add, set-key, verify connection uses new key | P1 |
| INT-004 | FullWorkflow_MultipleProfiles | Manage multiple profiles simultaneously | P1 |

### Cross-Platform

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| XP-001 | Encryption_Windows_UseDPAPI | Verify DPAPI used on Windows | P0 |
| XP-002 | Encryption_Linux_UseLibsodium | Verify libsodium used on Linux | P0 |
| XP-003 | Encryption_MacOS_UseLibsodium | Verify libsodium used on macOS | P0 |
| XP-004 | Credential_DifferentMachine_FailsDecrypt | Credential from another machine fails | P1 |

### Error Handling

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| ERR-001 | StorageInaccessible_ShowsErrorMessage | Permission denied handled gracefully | P1 |
| ERR-002 | CorruptedCredentials_ShowsReenterMessage | Corrupted credentials prompt re-entry | P1 |
| ERR-003 | LibsodiumMissing_ShowsInstallInstructions | Missing library shows install steps | P0 |

### Prompt Segment Display

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| PS-001 | ConnectedViaProfile_PromptShowsProfileName | Prompt includes profile name when connected via profile (FR-026) | P2 |
| PS-002 | ConnectedViaProfile_PromptShowsBracketFormat | Prompt shows format `[profile-name]` (FR-027) | P2 |
| PS-003 | NotConnected_PromptHidesProfileSegment | No profile segment when not connected (FR-028) | P2 |
| PS-004 | ConnectedDirectUri_PromptHidesProfileSegment | No profile segment when connected via direct URI (FR-028) | P2 |

## Test Data

### Valid Profiles

```csharp
new ServerProfile { Name = "production", Uri = "https://api.example.com" }
new ServerProfile { Name = "staging", Uri = "https://staging.example.com" }
new ServerProfile { Name = "dev-local", Uri = "http://localhost:5000" }
new ServerProfile { Name = "my-server-1", Uri = "https://server1.example.com" }
```

### Invalid Profile Names

```csharp
""                    // Empty
"@invalid"            // Special character
"with spaces"         // Spaces not allowed
"-starts-with-hyphen" // Must start alphanumeric
"a".PadRight(65, 'a') // Too long (> 64)
```

### Test API Keys

```csharp
"test-api-key-12345"
"sk_live_abcdefghijklmnop"
"very-long-api-key-" + new string('x', 100)
```

## Coverage Requirements

| Component | Target Coverage |
|-----------|----------------|
| ProfileManager | 95% |
| CredentialStore | 90% |
| Commands | 85% |
| AutoComplete Handler | 80% |

## Test Execution Matrix

| Platform | Encryption | CI Required |
|----------|------------|-------------|
| Windows x64 | DPAPI | Yes |
| Ubuntu 22.04 | libsodium | Yes |
| macOS 14 (ARM) | libsodium | Yes |
| macOS 14 (x64) | libsodium | Optional |
