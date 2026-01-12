# Batch 002: US2 Remaining + US3 Progress Tests

**Created**: 2026-01-11
**Status**: pending
**Tasks**: 15
**Type**: backfill

## Backfill Execution Mode

> ⚠️ **This batch contains test-backfill tasks for already-implemented code.**
>
> The standard RED→GREEN loop is modified:
>
> 1. **Skip RED phase** — Implementation exists; test may pass immediately
> 2. **Write test against SPECIFICATION** — Use test-cases.md as source of truth, NOT current code
> 3. **If test passes**: Verify the test actually validates the spec behavior (not a tautology)
> 4. **If test fails**: The CODE is wrong, not the test. Fix implementation to match spec.
>
> **Tests codify INTENT from test-cases.md. If code doesn't match intent, fix the code.**

## Batch Scope

US2 remaining tasks (flattening, multi-file message) plus US3 progress display tests.

## Tasks

### US2 Completion

- [ ] T039 [depends:T073] @test-case:UX-008 Test recursive glob flattens to destination in `DownloadCommandTests.cs`
- [ ] T046 [depends:T075] @test-case:UX-031 Test multi-file success message format in `DownloadCommandTests.cs`
- [ ] T062 [depends:T011] @test-case:DF-017 Test EnumerateFiles request returns FileInfoEntry array in `FileTransferServiceTests.cs`
- [ ] T063 [depends:T011] @test-case:DF-018 Test searchOption=AllDirectories propagates to server in `FileTransferServiceTests.cs`

### US3 Progress Tests

- [ ] T076 [depends:T093] @test-case:UX-012 Test progress bar shown for file >= threshold in `DownloadCommandTests.cs`
- [ ] T077 [depends:T093] @test-case:UX-013 Test no progress bar for file < threshold in `DownloadCommandTests.cs`
- [ ] T078 [depends:T095] @test-case:UX-014 Test aggregate progress bar for multiple files in `DownloadCommandTests.cs`
- [ ] T079 [depends:T096] @test-case:UX-015 Test transfer speed displayed in `DownloadCommandTests.cs`
- [ ] T080 [depends:T094] @test-case:UX-016 Test progress bar clears on completion in `DownloadCommandTests.cs`
- [ ] T081 [depends:T095] @test-case:UX-017 Test progress updates at least once per second in `DownloadCommandTests.cs`
- [ ] T082 [depends:T012] @test-case:CV-011 Test progressCallback invoked during download in `FileTransferServiceTests.cs`
- [ ] T086 [depends:T093] @test-case:DF-006 Test total size >= threshold sets showProgress flag in `DownloadCommandTests.cs`
- [ ] T088 [depends:T005] @test-case:DF-010 Test percent calculation from TotalRead/TotalSize in `FileDownloadProgressTests.cs`
- [ ] T089 [depends:T095] @test-case:DF-011 Test aggregate progress uses Interlocked.Add in `DownloadCommandTests.cs`
- [ ] T090 [depends:T095] @test-case:DF-012 Test progress delta calculation in `DownloadCommandTests.cs`

## Completion Criteria

All 15 tasks must be verified via `/speckit.verify` before batch advances.

## Notes

- Dependencies T005, T011, T012, T073, T075, T093, T094, T095, T096 are all completed
- May need to create FileDownloadProgressTests.cs for T088
