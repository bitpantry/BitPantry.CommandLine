# Batch 4: server-profile

**Created**: 2026-02-04
**Status**: in-progress
**Tasks**: 12 of 15 complete

## Tasks
- [X] T048 [depends:T003] @test-case:009:PM-032 Test CreateProfile_TooLongName_ThrowsValidation in ProfileManagerTests.cs
- [X] T049 [depends:T003] @test-case:009:PM-033 Test CreateProfile_HyphenInName_Succeeds in ProfileManagerTests.cs
- [X] T050 [depends:T003] @test-case:009:PM-034 Test CreateProfile_StartsWithHyphen_ThrowsValidation in ProfileManagerTests.cs
- [X] T051 [depends:T003] @test-case:009:PM-035 Test CreateProfile_InvalidUri_ThrowsValidation in ProfileManagerTests.cs
- [X] T052 [depends:T003] @test-case:009:PM-040 Test CreateProfile_CorruptedFile_RecreatesFile in ProfileManagerTests.cs
- [X] T053 [depends:T003] @test-case:009:PM-041 Test GetAllProfiles_MissingFile_ReturnsEmpty in ProfileManagerTests.cs
- [X] T054 [depends:T003] @test-case:009:PM-042 Test CreateProfile_DirectoryNotExists_CreatesDirectory in ProfileManagerTests.cs
- [X] T055 [depends:T022,T030,T031,T032,T033,T034,T035,T036,T037,T038,T039,T040,T041,T042,T043,T044,T045,T046,T047,T048,T049,T050,T051,T052,T053,T054] @test-case:009:IMPL-PM Implement ProfileManager in Profiles/ProfileManager.cs
- [X] T060 [depends:T055] @test-case:009:AC-001 Test GetOptions_NoProfiles_ReturnsEmpty in ProfileNameProviderTests.cs
- [X] T061 [depends:T055] @test-case:009:AC-002 Test GetOptions_MultipleProfiles_ReturnsAll in ProfileNameProviderTests.cs
- [X] T062 [depends:T055] @test-case:009:AC-003 Test GetOptions_WithPrefix_FiltersResults in ProfileNameProviderTests.cs
- [X] T063 [depends:T055] @test-case:009:AC-004 Test GetOptions_CaseInsensitivePrefix_Matches in ProfileNameProviderTests.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities












