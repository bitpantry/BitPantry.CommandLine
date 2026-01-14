# Batch 005: US4 Error Tests (Part 2) + Data Flow

**Created**: 2026-01-11
**Status**: complete
**Tasks**: 12
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

Remaining US4 error tests and data flow tests.

## Tasks

### US4 Error Tests (Continued)

- [X] T121 [depends:T133] @test-case:EH-008 Test invalid filename characters handling (cross-platform) in `DownloadCommandTests.cs`
- [X] T122 [depends:T131] @test-case:EH-012 Test checksum mismatch message in `DownloadCommandTests.cs`
  > **Already exists**: `FileTransferServiceDownloadTests.DownloadFile_ChecksumMismatch_ThrowsInvalidDataException` tests checksum mismatch handling. `SecurityLoggingTests.ChecksumMismatch_LogsSecurityEvent` verifies logging.
- [X] T123 [depends:T133] @test-case:EH-013 Test partial download cleanup on exception in `DownloadCommandTests.cs`
  > **Already exists**: `FileTransferServiceDownloadTests.DownloadFile_ChecksumMismatch_ThrowsInvalidDataException` verifies partial file deleted. `PartialFileCleanupTests` covers server-side cleanup.

### US4 Data Flow Tests

- [X] T112 [depends:T012] @test-case:DF-013 Test HTTP GET with Authorization header in `FileTransferServiceTests.cs`
  > **Already exists**: `FileTransferServiceDownloadTests.DownloadFile_SendsAuthorizationBearerHeader` + `FileTransferServiceAuthTests.DownloadFile_SendsAuthorizationBearerHeader` both verify Bearer token.
- [X] T113 [depends:T012] @test-case:DF-014 Test 200 response streams to local file in `FileTransferServiceTests.cs`
  > **Already exists**: `FileTransferServiceDownloadTests.DownloadFile_ValidFile_ReturnsContent` (CV-010) tests 200 response writing to local file.
- [X] T114 [depends:T103] @test-case:DF-015 Test checksum verification from header in `FileTransferServiceTests.cs`
  > **Already exists**: `FileTransferServiceDownloadTests.DownloadFile_ChecksumMismatch_ThrowsInvalidDataException` verifies checksum is read from X-File-Checksum header and validated.
- [X] T115 [depends:T027] @test-case:DF-016 Test parent directory creation in `FileTransferServiceTests.cs`
  > **Already exists**: `FileTransferServiceDownloadTests.DownloadFile_CreatesParentDirectories` verifies parent directories are created when local path doesn't exist.

### US4 Integration Tests

- [X] T127 [depends:T107] @test-case:IT-009 Test path traversal prevention E2E in `IntegrationTests_DownloadCommand.cs`
  > **Already exists**: `IntegrationTests_Download.Download_PathTraversal_Returns403` tests E2E path traversal rejection. `SandboxedFileTests.*_PathTraversal_*` comprehensive unit tests.
- [X] T128 [depends:T103] @test-case:IT-005 Test checksum verification E2E in `IntegrationTests_DownloadCommand.cs`
  > **NEW TEST**: `IntegrationTests_DownloadCommand.DownloadCommand_ChecksumVerification_FileIntegrityPreserved` added. Verifies server sends X-File-Checksum header and client validates during download.

### Cross-Platform Tests (No New Dependencies)

- [X] T154 [depends:T159] @test-case:IT-011 Test path separator normalization (cross-platform) in `IntegrationTests_DownloadCommand.cs`
  > **Already exists**: `IntegrationTests_DownloadCommand.DownloadCommand_PathSeparators_NormalizedCorrectly` (IT-011 region).
- [X] T155 [depends:T157] @test-case:IT-012 Test case collision detection across platforms in `IntegrationTests_DownloadCommand.cs`
  > **Already exists**: `IntegrationTests_DownloadCommand.DownloadCommand_FilenameCollision_ShowsError` (IT-012 region, also EH-010).
- [X] T162 [depends:T159] @test-case:IT-CP-003 Test destination path with trailing backslash converts correctly in `UploadCommandTests.cs`
  > **Already exists**: `UploadCommandTests.ResolveDestinationPath_BackslashDirectory_AppendsFilename` verifies destination path with trailing backslash correctly appends filename.

## Completion Criteria

All 12 tasks must be verified via `/speckit.verify` before batch advances.

## Notes

- T154, T155 depend on completed GlobPatternHelper tests (T157, T159)
- T162 tests UploadCommand, not DownloadCommand





