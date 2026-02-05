# Batch 3: server-profile

**Created**: 2026-02-04
**Status**: in-progress
**Tasks**: 3 of 17 complete

## Tasks
- [X] T035A [depends:T003] @test-case:009:PM-006A Test CreateProfile_ExistingProfile_ThrowsException in ProfileManagerTests.cs
- [X] T036 [depends:T003] @test-case:009:PM-007 Test UpdateProfile_ExistingProfile_UpdatesProfile in ProfileManagerTests.cs
- [X] T036A [depends:T003] @test-case:009:PM-007A Test UpdateProfile_NonExistentProfile_ThrowsException in ProfileManagerTests.cs
- [ ] T037 [depends:T003] @test-case:009:PM-008 Test CreateProfile_SetsCreatedAt_OnNewProfile in ProfileManagerTests.cs
- [ ] T038 [depends:T003] @test-case:009:PM-009 Test DeleteProfile_ExistingProfile_ReturnsTrue in ProfileManagerTests.cs
- [ ] T039 [depends:T003] @test-case:009:PM-010 Test DeleteProfile_NonExistent_ReturnsFalse in ProfileManagerTests.cs
- [ ] T040 [depends:T003] @test-case:009:PM-011 Test DeleteProfile_RemovesFromStorage in ProfileManagerTests.cs
- [ ] T041 [depends:T003] @test-case:009:PM-020 Test GetDefaultProfileName_NoneSet_ReturnsNull in ProfileManagerTests.cs
- [ ] T042 [depends:T003] @test-case:009:PM-021 Test GetDefaultProfileName_WhenSet_ReturnsName in ProfileManagerTests.cs
- [ ] T043 [depends:T003] @test-case:009:PM-022 Test SetDefaultProfile_ValidName_PersistsDefault in ProfileManagerTests.cs
- [ ] T044 [depends:T003] @test-case:009:PM-023 Test SetDefaultProfile_Null_ClearsDefault in ProfileManagerTests.cs
- [ ] T045 [depends:T003] @test-case:009:PM-024 Test DeleteProfile_WasDefault_ClearsDefault in ProfileManagerTests.cs
- [ ] T046 [depends:T003] @test-case:009:PM-030 Test CreateProfile_EmptyName_ThrowsValidation in ProfileManagerTests.cs
- [ ] T047 [depends:T003] @test-case:009:PM-031 Test CreateProfile_InvalidCharacters_ThrowsValidation in ProfileManagerTests.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities

