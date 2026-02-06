# Batch 1: input-syntax-highlight

**Created**: 2026-02-05
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T001 @test-case:010:TI-001 Implement HaveCellWithForegroundColor for cyan detection in BitPantry.VirtualConsole.Testing/VirtualConsoleAssertions.cs
- [ ] T002 [depends:T001] @test-case:010:TI-002 Add failure message for color mismatch in HaveCellWithForegroundColor
- [ ] T003 [depends:T001] @test-case:010:TI-003 Implement HaveRangeWithForegroundColor for multi-cell validation in VirtualConsoleAssertions.cs
- [ ] T004 [depends:T003] @test-case:010:TI-004 Add first-mismatch identification to HaveRangeWithForegroundColor failure message
- [ ] T005 [depends:T001] @test-case:010:TI-005 Implement HaveCellWithForeground256 for extended palette in VirtualConsoleAssertions.cs
- [ ] T006 [depends:T005] @test-case:010:TI-006 Implement HaveRangeWithForeground256 for extended palette ranges in VirtualConsoleAssertions.cs
- [ ] T007 [depends:T001] @test-case:010:TI-007 Implement HaveCellWithFullStyle for complete CellStyle comparison in VirtualConsoleAssertions.cs
- [ ] T008 [depends:T007] @test-case:010:TI-008 Add specific difference reporting to HaveCellWithFullStyle failure message
- [ ] T009 [depends:T001] @test-case:010:TI-009 Handle null foreground (default color) in HaveCellWithForegroundColor
- [ ] T010 [depends:T003] @test-case:010:TI-010 Add bounds validation to HaveRangeWithForegroundColor
- [ ] T011 @test-case:010:SETUP-001 Create ColoredSegment record type in BitPantry.CommandLine/Input/ColoredSegment.cs
- [ ] T012 @test-case:010:CV-001 Implement SyntaxColorScheme.Group returning cyan Style in BitPantry.CommandLine/AutoComplete/SyntaxColorScheme.cs

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
