# Batch 7: server-profile

**Created**: 2026-02-04
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T091 [depends:T070] @test-case:009:CMD-RMV-004 Test Remove_WasDefault_ClearsDefault in ProfileCommandTests.cs
- [ ] T092 [depends:T088,T089,T090,T091] @test-case:009:IMPL-RMV Implement ProfileRemoveCommand in Commands/Server/Profile/ProfileRemoveCommand.cs
- [ ] T093 [depends:T066,T070] @test-case:009:CMD-DEF-001 Test SetDefault_ExistingProfile_SetsDefault in ProfileCommandTests.cs
- [ ] T094 [depends:T070] @test-case:009:CMD-DEF-002 Test SetDefault_NonExistent_ShowsError in ProfileCommandTests.cs
- [ ] T095 [depends:T070] @test-case:009:CMD-DEF-003 Test SetDefault_ClearWithNone_ClearsDefault in ProfileCommandTests.cs
- [ ] T096 [depends:T093,T094,T095] @test-case:009:IMPL-DEF Implement ProfileSetDefaultCommand in Commands/Server/Profile/ProfileSetDefaultCommand.cs
- [ ] T097 [depends:T066,T070] @test-case:009:CMD-KEY-001 Test SetKey_ExistingProfile_UpdatesCredential in ProfileCommandTests.cs
- [ ] T098 [depends:T070] @test-case:009:CMD-KEY-002 Test SetKey_NonExistent_ShowsError in ProfileCommandTests.cs
- [ ] T099 [depends:T070] @test-case:009:CMD-KEY-003 Test SetKey_PromptsWithMasking_WhenNoValue in ProfileCommandTests.cs
- [ ] T100 [depends:T070] @test-case:009:CMD-KEY-004 Test SetKey_EmptyInput_ShowsError in ProfileCommandTests.cs
- [ ] T101 [depends:T097,T098,T099,T100] @test-case:009:IMPL-KEY Implement ProfileSetKeyCommand in Commands/Server/Profile/ProfileSetKeyCommand.cs
- [ ] T110 [depends:T055,T066] @test-case:009:CMD-CON-001 Test Connect_WithProfile_UsesProfileSettings in ConnectProfileTests.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
