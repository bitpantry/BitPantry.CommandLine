# Batch 005: US4 Error Tests (Part 2) + Data Flow

**Created**: 2026-01-11
**Status**: pending
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

- [ ] T121 [depends:T133] @test-case:EH-008 Test invalid filename characters handling (cross-platform) in `DownloadCommandTests.cs`
- [ ] T122 [depends:T131] @test-case:EH-012 Test checksum mismatch message in `DownloadCommandTests.cs`
- [ ] T123 [depends:T133] @test-case:EH-013 Test partial download cleanup on exception in `DownloadCommandTests.cs`

### US4 Data Flow Tests

- [ ] T112 [depends:T012] @test-case:DF-013 Test HTTP GET with Authorization header in `FileTransferServiceTests.cs`
- [ ] T113 [depends:T012] @test-case:DF-014 Test 200 response streams to local file in `FileTransferServiceTests.cs`
- [ ] T114 [depends:T103] @test-case:DF-015 Test checksum verification from header in `FileTransferServiceTests.cs`
- [ ] T115 [depends:T027] @test-case:DF-016 Test parent directory creation in `FileTransferServiceTests.cs`

### US4 Integration Tests

- [ ] T127 [depends:T107] @test-case:IT-009 Test path traversal prevention E2E in `IntegrationTests_DownloadCommand.cs`
- [ ] T128 [depends:T103] @test-case:IT-005 Test checksum verification E2E in `IntegrationTests_DownloadCommand.cs`

### Cross-Platform Tests (No New Dependencies)

- [ ] T154 [depends:T159] @test-case:IT-011 Test path separator normalization (cross-platform) in `IntegrationTests_DownloadCommand.cs`
- [ ] T155 [depends:T157] @test-case:IT-012 Test case collision detection across platforms in `IntegrationTests_DownloadCommand.cs`
- [ ] T162 [depends:T159] @test-case:IT-CP-003 Test destination path with trailing backslash converts correctly in `UploadCommandTests.cs`

## Completion Criteria

All 12 tasks must be verified via `/speckit.verify` before batch advances.

## Notes

- T154, T155 depend on completed GlobPatternHelper tests (T157, T159)
- T162 tests UploadCommand, not DownloadCommand
