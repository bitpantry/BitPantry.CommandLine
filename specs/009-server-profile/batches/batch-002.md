# Batch 2: server-profile

**Created**: 2026-02-04
**Status**: complete
**Tasks**: 12 of 15 complete

## Tasks
- [X] T017 [depends:T004,T005] @test-case:009:CS-011 Test Retrieve_StoredKey_DecryptsCorrectly (libsodium) in CredentialStoreTests.cs
- [X] T018 [depends:T004,T005] @test-case:009:CS-012 Test Store_LibsodiumUnavailable_ThrowsWithMessage in CredentialStoreTests.cs
- [X] T019 [depends:T004] @test-case:009:CS-020 Test Store_EmptyApiKey_ThrowsValidation in CredentialStoreTests.cs
- [X] T020 [depends:T004] @test-case:009:CS-021 Test Store_MultipleProfiles_IndependentStorage in CredentialStoreTests.cs
- [X] T021 [depends:T004] @test-case:009:CS-022 Test Remove_AlsoRemovesFromProfile_OnDelete in CredentialStoreTests.cs
- [X] T022 [depends:T010,T011,T012,T013,T014,T015,T016,T017,T018,T019,T020,T021] @test-case:009:IMPL-CS Implement CredentialStore in Profiles/CredentialStore.cs
- [X] T030 [depends:T003] @test-case:009:PM-001 Test GetAllProfiles_EmptyStore_ReturnsEmptyList in ProfileManagerTests.cs
- [X] T031 [depends:T003] @test-case:009:PM-002 Test GetAllProfiles_MultipleProfiles_ReturnsAll in ProfileManagerTests.cs
- [X] T032 [depends:T003] @test-case:009:PM-003 Test GetProfile_ExistingProfile_ReturnsProfile in ProfileManagerTests.cs
- [X] T033 [depends:T003] @test-case:009:PM-004 Test GetProfile_NonExistentProfile_ReturnsNull in ProfileManagerTests.cs
- [X] T034 [depends:T003] @test-case:009:PM-005 Test GetProfile_CaseInsensitive_ReturnsProfile in ProfileManagerTests.cs
- [X] T035 [depends:T003] @test-case:009:PM-006 Test CreateProfile_NewProfile_PersistsToStorage in ProfileManagerTests.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities













