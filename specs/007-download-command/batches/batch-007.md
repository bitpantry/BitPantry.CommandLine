# Batch 007: Final Tests + Batch Summary

**Created**: 2026-01-11
**Status**: pending
**Tasks**: 10
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

Final US4/US5 tests that depend on completed concurrent download implementation, plus remaining cross-platform integration.

## Tasks

### US4 Batch Summary Tests (Depend on US5 Implementation)

- [ ] T101 [depends:T148] @test-case:UX-022 Test batch summary with failures in `DownloadCommandTests.cs`
- [ ] T102 [depends:T148] @test-case:UX-023 Test batch continues after individual failure in `DownloadCommandTests.cs`
- [ ] T110 [depends:T148] @test-case:DF-007 Test successful download sets Status=Success in `DownloadCommandTests.cs`
- [ ] T111 [depends:T148] @test-case:DF-008 Test failed download sets Status=Failed with Error in `DownloadCommandTests.cs`
- [ ] T124 [depends:T148] @test-case:EH-014 Test batch continues after one failure in `DownloadCommandTests.cs`
  > Note: Similar to T102/T138. May be duplicate - consolidate.
- [ ] T125 [depends:T149] @test-case:EH-015 Test all files fail summary in `DownloadCommandTests.cs`
- [ ] T126 [depends:T149] @test-case:EH-016 Test mixed success/failure summary in `DownloadCommandTests.cs`
  > Note: Similar to T137. May be duplicate - consolidate.

### Final Integration Tests

- [ ] T144 [depends:T146] @test-case:IT-010 Test large batch E2E in `IntegrationTests_DownloadCommand.cs`

### Cross-Platform Integration (Upload Command)

- [ ] T160 [depends:T159] @test-case:IT-CP-001 Test upload with Windows-style source path to Linux-style destination in `IntegrationTests_UploadCommand.cs`
- [ ] T161 [depends:T157] @test-case:IT-CP-002 Test glob pattern matching is case-insensitive in both commands in integration tests

## Completion Criteria

All 9 tasks must be verified via `/speckit.verify` before batch advances.

## Notes

- This is the final batch
- T160, T161 test UploadCommand cross-platform behavior using shared GlobPatternHelper
- After this batch completes, all 007-download-command tasks are done
