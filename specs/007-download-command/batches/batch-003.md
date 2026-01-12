# Batch 003: US3 Registry + US1 Deferred + US4 Server Tests

**Created**: 2026-01-11
**Status**: pending
**Tasks**: 14
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

Progress registry tests, US1 deferred tests (need mock interface), and US4 server-side component tests.

## Tasks

### US3 Registry Tests

- [ ] T083 [depends:T010] @test-case:CV-018 Test FileDownloadProgressRegistry.Register returns correlationId in `FileDownloadProgressRegistryTests.cs`
- [ ] T084 [depends:T010] @test-case:CV-019 Test FileDownloadProgressRegistry.Unregister removes callback in `FileDownloadProgressRegistryTests.cs`
- [ ] T085 [depends:T010] @test-case:CV-020 Test FileDownloadProgressRegistry invokes callback on message in `FileDownloadProgressRegistryTests.cs`
- [ ] T087 [depends:T010] @test-case:DF-009 Test progress message invokes registered callback in `FileDownloadProgressRegistryTests.cs`
- [ ] T091 [depends:T012] @test-case:DF-019 Test progress message triggers callback in `FileTransferServiceTests.cs`

### US1 Deferred Tests

- [ ] T018 [depends:T036] @test-case:UX-005 Test download of nonexistent file shows error in `DownloadCommandTests.cs`
- [ ] T019 [depends:T036] @test-case:UX-030 Test single file success message format in `DownloadCommandTests.cs`
- [ ] T030 [depends:T036] @test-case:EH-004 Test remote file not found displays friendly error in `DownloadCommandTests.cs`

### US4 Server-Side Tests

- [ ] T105 [depends:T009] @test-case:CV-021 Test FileTransferEndpointService streams with Content-Length in `FileTransferEndpointServiceTests.cs`
- [ ] T106 [depends:T009] @test-case:CV-022 Test FileTransferEndpointService sends progress via SignalR in `FileTransferEndpointServiceTests.cs`
- [ ] T107 [depends:T009] @test-case:CV-023 Test FileTransferEndpointService rejects path traversal in `FileTransferEndpointServiceTests.cs`
- [ ] T108 [depends:T009] @test-case:CV-024 Test FileTransferEndpointService returns 404 for missing file in `FileTransferEndpointServiceTests.cs`
- [ ] T109 [depends:T009] @test-case:CV-025 Test FileTransferEndpointService sets X-File-Checksum header in `FileTransferEndpointServiceTests.cs`
- [ ] T092 [depends:T096] @test-case:IT-003 Test progress callback E2E in `IntegrationTests_DownloadCommand.cs`

## Completion Criteria

All 14 tasks must be verified via `/speckit.verify` before batch advances.

## Notes

- May need to create FileDownloadProgressRegistryTests.cs
- May need to create FileTransferEndpointServiceTests.cs
- T018, T019, T030 may require extracting IFileTransferService interface for mocking
