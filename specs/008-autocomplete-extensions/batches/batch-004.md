# Batch 4: autocomplete-extensions

**Created**: 2026-01-19
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T036a [depends:T036] @test-case:008:TC-4.7 Handler exception gracefully degrades with logging
- [ ] T036b [depends:T036a] @test-case:008:TC-4.8 Handler returning empty is valid result (no fallback)
- [ ] T036c [depends:T036b] @test-case:008:TC-4.9 New input cancels pending autocomplete request
- [ ] T037 [depends:T014] @test-case:008:SYNTAX-001 Create `UsedArgumentTracker` in `AutoComplete/UsedArgumentTracker.cs`
- [ ] T038 [depends:T037] @test-case:008:SYN-001 Create `CommandSyntaxHandler` suggesting groups at command position
- [ ] T039 [depends:T038] @test-case:008:SYN-002 `CommandSyntaxHandler` suggests commands within typed group
- [ ] T040 [depends:T039] @test-case:008:SYN-003 `CommandSyntaxHandler` suggests root-level commands
- [ ] T041 [depends:T040] @test-case:008:SYN-004 `CommandSyntaxHandler` suggests command aliases alongside full names
- [ ] T042 [depends:T041] @test-case:008:SYN-005 Create `ArgumentNameHandler` suggesting `--argName` after `--`
- [ ] T043 [depends:T042] @test-case:008:SYN-006 Create `ArgumentAliasHandler` suggesting `-alias` after `-`
- [ ] T044 [depends:T043,T037] @test-case:008:SYN-007 Filter already-used arguments from suggestions
- [ ] T045 [depends:T036,T044] @test-case:008:UX-001 Ghost text auto-appears at autocomplete-applicable position

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
