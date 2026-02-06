# Batch 3: input-syntax-highlight

**Created**: 2026-02-05
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T025 [depends:T019] @test-case:010:CV-036 Implement ResolveMatch detecting subgroups within parent group in TokenMatchResolver.cs
- [ ] T026 [depends:T018,T011] @test-case:010:CV-010 Implement Highlight returning empty list for empty input in BitPantry.CommandLine/Input/SyntaxHighlighter.cs
- [ ] T027 [depends:T026] @test-case:010:CV-011 Handle null input gracefully in SyntaxHighlighter.Highlight
- [ ] T028 [depends:T026,T019] @test-case:010:CV-012 Implement Highlight returning cyan segment for known group in SyntaxHighlighter.cs
- [ ] T029 [depends:T028] @test-case:010:CV-013 Implement Highlight returning default segment for root command in SyntaxHighlighter.cs
- [ ] T030 [depends:T028] @test-case:010:CV-014 Implement Highlight for "group command" returning two segments in SyntaxHighlighter.cs
- [ ] T031 [depends:T030] @test-case:010:CV-015 Implement Highlight for "group command --arg" returning three segments in SyntaxHighlighter.cs
- [ ] T032 [depends:T031] @test-case:010:CV-016 Implement Highlight for full command with arg value returning four segments in SyntaxHighlighter.cs
- [ ] T033 [depends:T031] @test-case:010:CV-017 Implement Highlight for alias -h with value returning four segments in SyntaxHighlighter.cs
- [ ] T034 [depends:T010,T028] @test-case:010:UX-001 Verify complete group name displays cyan in integration test
- [ ] T035 [depends:T010,T029] @test-case:010:UX-002 Verify complete command name displays default/white in integration test
- [ ] T036 [depends:T010,T031] @test-case:010:UX-003 Verify --flag displays yellow in integration test

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
