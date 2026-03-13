# Batch 3: remote-fs-commands

**Created**: 2026-03-12
**Status**: in-progress
**Tasks**: 9 of 15 complete

## Tasks
- [x] T025 [depends:T024] @test-case:011:UX-001 Implement UX-001 (Default list — files and directories shown) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
  Notes: Consolidated T025+T026+T027 into Execute_DefaultList_ShowsFilesAndDirectoriesWithCorrectSuffixes
- [x] T026 [depends:T025] @test-case:011:UX-002 — Covered by T025
  Notes: Same test setup, assertion added to T025 test
- [x] T027 [depends:T026] @test-case:011:UX-003 — Covered by T025
  Notes: Same test setup, assertion added to T025 test
- [X] T028 [depends:T027] @test-case:011:UX-004 Implement UX-004 (Long format shows table with Type, Name, Size, Last Modified columns) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
  Notes: Consolidated T028+T029+T030 into Execute_LongFormat_ShowsTableWithHeadersAndFormattedSizes. Full RED→GREEN TDD.
- [X] T029 [depends:T028] @test-case:011:UX-005 — Covered by T028
  Notes: Same test setup, human-readable size assertion in T028 test
- [X] T030 [depends:T029] @test-case:011:UX-006 — Covered by T028
  Notes: Same test setup, em dash assertion in T028 test
- [X] T031 [depends:T030] @test-case:011:UX-007 Implement UX-007 (Tree view shows nested entries) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
  Notes: Full RED→GREEN TDD. Execute_Recursive_ShowsNestedEntriesWithHierarchy verifies relative paths show hierarchy.
- [X] T032 [depends:T031] @test-case:011:UX-008 Implement UX-008 (Entries ordered by size (smallest first)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
  Notes: Consolidated T032+T033 into Execute_SortBySize_OrdersByFileSizeAscAndDesc. Backfill.
- [X] T033 [depends:T032] @test-case:011:UX-009 — Covered by T032
  Notes: Same test, reverse assertion validates --sort size --reverse
- [X] T034 [depends:T033] @test-case:011:UX-010 Implement UX-010 (Entries ordered by last modified (oldest first)) in BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/LsCommandTests.cs and BitPantry.CommandLine.Remote.SignalR.Server/Commands/LsCommand.cs
  Notes: Backfill — Execute_SortByModified_OrdersByLastModifiedOldestFirst
- [X] T035 [depends:T034] @test-case:011:UX-011 — Consolidated T035+T036
  Notes: Backfill — Execute_SortByName_OrdersAlphabeticallyAndReverse covers both name sort and reverse
- [X] T036 [depends:T035] @test-case:011:UX-012 — Covered by T035
  Notes: Same test, reverse assertion validates --reverse with default name sort

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities












