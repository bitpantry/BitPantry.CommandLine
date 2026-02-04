# Batch 1: server-profile

**Created**: 2026-02-04
**Status**: in-progress
**Tasks**: 12 of 15 complete

## Tasks
- [X] T001 @test-case:009:SETUP-001 Create ServerProfile entity in Profiles/ServerProfile.cs
- [X] T002 [depends:T001] @test-case:009:SETUP-002 Create ProfileConfiguration storage model in Profiles/ProfileConfiguration.cs
- [X] T003 [depends:T001] @test-case:009:SETUP-003 Create IProfileManager interface in Profiles/IProfileManager.cs
- [X] T004 [depends:T001] @test-case:009:SETUP-004 Create ICredentialStore interface (internal) in Profiles/ICredentialStore.cs
- [X] T005 [depends:T001] @test-case:009:SETUP-005 Add Sodium.Core package reference to csproj
- [X] T010 [depends:T004] @test-case:009:CS-001 Test Store_ValidApiKey_EncryptsWithDPAPI in CredentialStoreTests.cs
- [X] T011 [depends:T004] @test-case:009:CS-002 Test Retrieve_StoredKey_DecryptsCorrectly in CredentialStoreTests.cs
- [X] T012 [depends:T004] @test-case:009:CS-003 Test Retrieve_NonExistent_ReturnsNull in CredentialStoreTests.cs
- [X] T013 [depends:T004] @test-case:009:CS-004 Test Remove_ExistingCredential_RemovesEntry in CredentialStoreTests.cs
- [X] T014 [depends:T004] @test-case:009:CS-005 Test Exists_StoredCredential_ReturnsTrue in CredentialStoreTests.cs
- [X] T015 [depends:T004] @test-case:009:CS-006 Test Exists_NonExistent_ReturnsFalse in CredentialStoreTests.cs
- [X] T016 [depends:T004,T005] @test-case:009:CS-010 Test Store_ValidApiKey_EncryptsWithSecretBox in CredentialStoreTests.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities












