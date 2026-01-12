# Batch 001: US2 and US3 Unit Tests (Foundation Dependencies Met)

**Created**: 2026-01-11
**Status**: complete
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

These tasks depend only on completed foundation work (T001-T013, T070-T075, T093-T096) and can be executed immediately.

## Tasks

- [x] T037 [depends:T071] @test-case:UX-006 Test glob pattern matches multiple files in `DownloadCommandTests.cs`
- [X] T038 [depends:T071] @test-case:UX-007 Test directory-scoped glob pattern in `DownloadCommandTests.cs`
- [X] T040 [depends:T071] @test-case:UX-009 Test single-char wildcard `?` matching in `DownloadCommandTests.cs`
- [X] T041 [depends:T074] @test-case:UX-010 Test no matches shows yellow warning in `DownloadCommandTests.cs`
- [X] T042 [depends:T071] @test-case:UX-011 Test recursive `**` includes subdirectories in `DownloadCommandTests.cs`
- [X] T043 [depends:T072] @test-case:UX-027 Test collision detection with same filename in `DownloadCommandTests.cs`
- [X] T044 [depends:T072] @test-case:UX-028 Test collision prevents all downloads in `DownloadCommandTests.cs`
- [X] T045 [depends:T072] @test-case:UX-029 Test collision error lists all conflicting paths in `DownloadCommandTests.cs`
- [X] T048 [depends:T071] @test-case:CV-004 Test ExpandSourcePattern returns FileInfoEntry list in `DownloadCommandTests.cs`
- [X] T051 [depends:T011] @test-case:CV-016 Test FileTransferService.EnumerateFiles returns FileInfoEntry array in `FileTransferServiceTests.cs`
- [X] T052 [depends:T011] @test-case:CV-017 Test FileTransferService.EnumerateFiles uses AllDirectories when recursive in `FileTransferServiceTests.cs`
- [X] T058 [depends:T070] @test-case:DF-001 Test download start transitions to Expand Source Pattern in `DownloadCommandTests.cs`
- [X] T059 [depends:T071] @test-case:DF-002 Test glob pattern triggers EnumerateFilesRequest in `DownloadCommandTests.cs`
- [X] T060 [depends:T072] @test-case:DF-004 Test collision detected transitions to error state in `DownloadCommandTests.cs`
- [X] T061 [depends:T072] @test-case:DF-005 Test unique filenames transitions to Calculate Total Size in `DownloadCommandTests.cs`

## Completion Criteria

All 15 tasks must be verified via `/speckit.verify` before batch advances.

## Notes

- All dependencies (T011, T070, T071, T072, T074) are already completed
- Focus on DownloadCommandTests.cs and FileTransferServiceTests.cs
- These are unit tests — no integration test infrastructure needed

















