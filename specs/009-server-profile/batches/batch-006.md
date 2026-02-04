# Batch 6: server-profile

**Created**: 2026-02-04
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T079 [depends:T070] @test-case:009:CMD-LST-002 Test List_MultipleProfiles_ShowsTable in ProfileCommandTests.cs
- [ ] T080 [depends:T070] @test-case:009:CMD-LST-003 Test List_MarksDefault_WithIndicator in ProfileCommandTests.cs
- [ ] T081 [depends:T070] @test-case:009:CMD-LST-004 Test List_IncludesCredentials_Column in ProfileCommandTests.cs
- [ ] T082 [depends:T078,T079,T080,T081] @test-case:009:IMPL-LST Implement ProfileListCommand in Commands/Server/Profile/ProfileListCommand.cs
- [ ] T083 [depends:T066,T070] @test-case:009:CMD-SHW-001 Test Show_ExistingProfile_DisplaysDetails in ProfileCommandTests.cs
- [ ] T084 [depends:T070] @test-case:009:CMD-SHW-002 Test Show_NonExistent_ShowsError in ProfileCommandTests.cs
- [ ] T085 [depends:T070] @test-case:009:CMD-SHW-003 Test Show_WithCredential_ShowsMasked in ProfileCommandTests.cs
- [ ] T086 [depends:T066,T070] @test-case:009:CMD-SHW-004 Test Show_ProfileNameAutocomplete_Works in ProfileCommandTests.cs
- [ ] T087 [depends:T083,T084,T085,T086] @test-case:009:IMPL-SHW Implement ProfileShowCommand in Commands/Server/Profile/ProfileShowCommand.cs
- [ ] T088 [depends:T066,T070] @test-case:009:CMD-RMV-001 Test Remove_ExistingProfile_DeletesProfile in ProfileCommandTests.cs
- [ ] T089 [depends:T070] @test-case:009:CMD-RMV-002 Test Remove_NonExistent_ShowsError in ProfileCommandTests.cs
- [ ] T090 [depends:T070] @test-case:009:CMD-RMV-003 Test Remove_AlsoRemoves_Credential in ProfileCommandTests.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
