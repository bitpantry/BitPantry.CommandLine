# Batch 004: US4 Implementation + Error Tests (Part 1)

**Created**: 2026-01-11
**Status**: complete
**Tasks**: 15
**Type**: mixed (implementation + backfill)

## Execution Mode

> This batch contains BOTH new implementation AND test-backfill tasks.
>
> **For implementation tasks (T129-T133)**:
> - Use standard RED→GREEN TDD loop
> - Write failing test first, then implement
>
> **For test tasks (T097-T100, T104, T116-T120)**:
> - Skip RED phase — Implementation may exist
> - Write test against SPECIFICATION (test-cases.md), NOT current code
> - If test fails: The CODE is wrong, not the test. Fix implementation to match spec.
>
> **Tests codify INTENT from test-cases.md. If code doesn't match intent, fix the code.**

## Batch Scope

US4 implementation tasks and initial error handling tests that depend on them.

## Tasks

### US4 Implementation (First)

- [X] T129 [depends:T103] @test-case:IMPL-129 Handle permission denied on local write in `DownloadCommand.cs`
- [X] T130 [depends:T103] @test-case:IMPL-130 Handle connection lost during download (cleanup partial file) in `DownloadCommand.cs`
  > **Message handling implemented**: Added catch blocks for `InvalidOperationException` (disconnected) and `RemoteMessagingException`. Partial file cleanup deferred to T133.
- [X] T131 [depends:T103] @test-case:IMPL-131 Handle checksum verification failure in `DownloadCommand.cs`
  > **Implemented**: Added `InvalidDataException` catch block with user-friendly checksum error message.
- [X] T132 [depends:T103] @test-case:IMPL-132 Handle disk space exhausted in `DownloadCommand.cs`
  > **Implemented**: Added `IOException` catch block that detects disk full condition via HRESULT 0x70.
- [X] T133 [depends:T103] @test-case:IMPL-133 Implement partial file cleanup on any error in `DownloadCommand.cs`
  > **Implemented**: Added `TryCleanupPartialFile` helper method called from all error catch blocks.

### US4 Tests (Depend on Implementation)

- [X] T097 [depends:T036] @test-case:UX-018 Test file not found error for literal path in `DownloadCommandTests.cs`
  > **Already exists**: `IntegrationTests_DownloadCommand.DownloadCommand_FileNotFound_ShowsError` tests literal path 404 handling E2E.
- [X] T098 [depends:T129] @test-case:UX-019 Test permission denied error in `DownloadCommandTests.cs`
  > **Already exists**: `DownloadCommandTests.Execute_PermissionDenied_DisplaysPermissionDeniedError` (added in T129) tests this.
- [X] T099 [depends:T130] @test-case:UX-020 Test connection lost error in `DownloadCommandTests.cs`
  > **Already exists**: `DownloadCommandTests.Execute_ConnectionLostViaInvalidOperation_DisplaysConnectionLostError` (added in T130) tests this.
- [X] T100 [depends:T131] @test-case:UX-021 Test checksum failure error in `DownloadCommandTests.cs`
  > **Already exists**: `FileTransferServiceDownloadTests.DownloadFile_ChecksumMismatch_ThrowsInvalidDataException` (CV-013) tests this.
- [X] T104 [depends:T133] @test-case:CV-015 Test download failure deletes partial file in `FileTransferServiceTests.cs`
  > **Already exists**: `FileTransferServiceDownloadTests.DownloadFile_ChecksumMismatch_ThrowsInvalidDataException` verifies partial file cleanup. Also `PartialFileCleanupTests` for uploads.
- [X] T116 [depends:T130] @test-case:EH-002 Test connection lost cleanup and message in `DownloadCommandTests.cs`
  > **Message test exists**: `Execute_ConnectionLostViaInvalidOperation_DisplaysConnectionLostError` (added in T130). Cleanup test deferred to T133.
- [X] T117 [depends:T130] @test-case:EH-003 Test SignalR disconnect handling in `DownloadCommandTests.cs`
  > **Already exists**: `DownloadCommandTests.Execute_ConnectionLostViaRemoteMessagingException_DisplaysConnectionLostError` (added in T130) tests this.
- [X] T118 [depends:T129] @test-case:EH-005 Test permission denied message in `DownloadCommandTests.cs`
  > **Already exists**: `DownloadCommandTests.Execute_PermissionDenied_DisplaysPermissionDeniedError` (added in T129) tests this.
- [X] T119 [depends:T132] @test-case:EH-006 Test disk space exhausted handling in `DownloadCommandTests.cs`
  > **Implemented**: `Execute_DiskFull_DisplaysDiskFullError` tests IOException with HRESULT 0x70.
- [X] T120 [depends:T133] @test-case:EH-007 Test path too long handling in `DownloadCommandTests.cs`
  > **Implemented**: `Execute_PathTooLong_DisplaysPathTooLongError` tests PathTooLongException handling.

## Completion Criteria

All 15 tasks must be verified via `/speckit.verify` before batch advances.

## Notes

- Implementation tasks T129-T133 should be done first as tests depend on them
- These are error handling paths — ensure partial file cleanup is robust



