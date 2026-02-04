# Batch 5: server-profile

**Created**: 2026-02-04
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T064 [depends:T055] @test-case:009:AC-005 Test GetOptions_IncludesDefault_MarksIndicator in ProfileNameProviderTests.cs
- [ ] T065 [depends:T055] @test-case:009:AC-006 Test GetOptions_Performance_Under100ms in ProfileNameProviderTests.cs
- [ ] T066 [depends:T060,T061,T062,T063,T064,T065] @test-case:009:IMPL-AC Implement ProfileNameProvider in AutoComplete/ProfileNameProvider.cs
- [ ] T070 [depends:T055] @test-case:009:SETUP-006 Create ProfileGroup in Commands/Server/Profile/ProfileGroup.cs
- [ ] T071 [depends:T070] @test-case:009:CMD-ADD-001 Test Add_ValidProfile_CreatesProfile in ProfileCommandTests.cs
- [ ] T072 [depends:T070] @test-case:009:CMD-ADD-002 Test Add_DuplicateName_ShowsError in ProfileCommandTests.cs
- [ ] T073 [depends:T070] @test-case:009:CMD-ADD-003 Test Add_WithApiKey_StoresCredential in ProfileCommandTests.cs
- [ ] T074 [depends:T070] @test-case:009:CMD-ADD-004 Test Add_WithApiKeyPrompt_MasksInput in ProfileCommandTests.cs
- [ ] T075 [depends:T070] @test-case:009:CMD-ADD-005 Test Add_InvalidUri_ShowsError in ProfileCommandTests.cs
- [ ] T076 [depends:T070] @test-case:009:CMD-ADD-006 Test Add_SetAsDefault_SetsDefault in ProfileCommandTests.cs
- [ ] T077 [depends:T071,T072,T073,T074,T075,T076] @test-case:009:IMPL-ADD Implement ProfileAddCommand in Commands/Server/Profile/ProfileAddCommand.cs
- [ ] T078 [depends:T070] @test-case:009:CMD-LST-001 Test List_NoProfiles_ShowsEmptyMessage in ProfileCommandTests.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
