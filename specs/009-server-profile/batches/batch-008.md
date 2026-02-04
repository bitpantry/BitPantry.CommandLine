# Batch 8: server-profile

**Created**: 2026-02-04
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T111 [depends:T055] @test-case:009:CMD-CON-002 Test Connect_ProfileNoCredential_PromptsForKey in ConnectProfileTests.cs
- [ ] T112 [depends:T055] @test-case:009:CMD-CON-003 Test Connect_ProfileAndUri_UriOverrides in ConnectProfileTests.cs
- [ ] T113 [depends:T055] @test-case:009:CMD-CON-004 Test Connect_ProfileNotFound_ShowsError in ConnectProfileTests.cs
- [ ] T114 [depends:T055] @test-case:009:CMD-CON-005 Test Connect_NoProfileNoUri_UsesDefault in ConnectProfileTests.cs
- [ ] T115 [depends:T066] @test-case:009:CMD-CON-006 Test Connect_ProfileAutocomplete_Works in ConnectProfileTests.cs
- [ ] T116 [depends:T110,T111,T112,T113,T114,T115] @test-case:009:IMPL-CON Modify ConnectCommand for --profile support in Commands/Server/ConnectCommand.cs
- [ ] T120 [depends:T022,T055,T066,T077,T082,T087,T092,T096,T101,T116] @test-case:009:SETUP-007 Register profile services in CommandLineApplicationBuilderExtensions.cs
- [ ] T150 [depends:T120] @test-case:009:PS-001 Test ConnectedViaProfile_PromptShowsProfileName in PromptSegmentTests.cs
- [ ] T151 [depends:T150] @test-case:009:PS-002 Test ConnectedViaProfile_PromptShowsBracketFormat in PromptSegmentTests.cs
- [ ] T152 [depends:T150] @test-case:009:PS-003 Test NotConnected_PromptHidesProfileSegment in PromptSegmentTests.cs
- [ ] T153 [depends:T150] @test-case:009:PS-004 Test ConnectedDirectUri_PromptHidesProfileSegment in PromptSegmentTests.cs
- [ ] T154 [depends:T150,T151,T152,T153] @test-case:009:IMPL-PS Implement ProfilePromptSegment in Prompt/ProfilePromptSegment.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
