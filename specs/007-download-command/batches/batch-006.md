# Batch 006: US5 Implementation + Concurrent Download Tests

**Created**: 2026-01-11
**Status**: pending
**Tasks**: 15
**Type**: mixed (implementation + backfill)

## Execution Mode

> This batch contains BOTH new implementation AND test-backfill tasks.
>
> **For implementation tasks (T145-T149)**:
> - Use standard RED→GREEN TDD loop
> - Write failing test first, then implement
>
> **For test tasks (T134-T144)**:
> - Skip RED phase — Implementation may exist
> - Write test against SPECIFICATION (test-cases.md), NOT current code
> - If test fails: The CODE is wrong, not the test. Fix implementation to match spec.
>
> **Tests codify INTENT from test-cases.md. If code doesn't match intent, fix the code.**

## Batch Scope

US5 concurrent download implementation and all related tests.

## Tasks

### US5 Implementation (First)

- [ ] T145 [depends:T073] @test-case:IMPL-145 Implement SemaphoreSlim throttling with DownloadConstants.MaxConcurrentDownloads in `DownloadCommand.cs`
- [ ] T146 [depends:T145] @test-case:IMPL-146 Implement parallel download loop with Task.WhenAll in `DownloadCommand.cs`
- [ ] T147 [depends:T095] @test-case:IMPL-147 Aggregate progress across concurrent downloads in `DownloadCommand.cs`
- [ ] T148 [depends:T146] @test-case:IMPL-148 Track success/failure counts across concurrent operations in `DownloadCommand.cs`
- [ ] T149 [depends:T148] @test-case:IMPL-149 Display mixed success/failure summary in `DownloadCommand.cs`

### US5 Tests (Depend on Implementation)

- [ ] T134 [depends:T145] @test-case:UX-024 Test concurrent download limit in `DownloadCommandTests.cs`
- [ ] T135 [depends:T147] @test-case:UX-025 Test aggregate progress for concurrent downloads in `DownloadCommandTests.cs`
- [ ] T136 [depends:T149] @test-case:UX-026 Test completion summary shows total count in `DownloadCommandTests.cs`
- [ ] T137 [depends:T149] @test-case:UX-032 Test mixed success/failure in batch in `DownloadCommandTests.cs`
- [ ] T138 [depends:T148] @test-case:UX-033 Test batch continues after failure in `DownloadCommandTests.cs`
- [ ] T139 [depends:T149] @test-case:UX-034 Test partial success uses yellow color in `DownloadCommandTests.cs`
- [ ] T140 [depends:T149] @test-case:UX-035 Test failed files listed with reason in `DownloadCommandTests.cs`
- [ ] T141 [depends:T133] @test-case:EH-017 Test cancellation cleans up partial files in `DownloadCommandTests.cs`
- [ ] T142 [depends:T145] @test-case:EH-018 Test timeout handling in `DownloadCommandTests.cs`
- [ ] T143 [depends:T146] @test-case:IT-004 Test concurrent downloads E2E in `IntegrationTests_DownloadCommand.cs`

## Completion Criteria

All 15 tasks must be verified via `/speckit.verify` before batch advances.

## Notes

- Implementation tasks T145-T149 should be done first as tests depend on them
- T141 depends on T133 (partial file cleanup from Batch 004)
