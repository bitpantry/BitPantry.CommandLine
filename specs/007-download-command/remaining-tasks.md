# Remaining Tasks: Download Command (Micro-TDD Format)

**Converted from**: tasks.md (original format preserved)
**Format**: Micro-TDD with explicit dependencies and test case references

## Task Format

```text
- [ ] T### [depends:T###] @test-case:XX-### Description with file path
```

---

## Completed Foundation

The following phases are **COMPLETE** and provide the foundation for remaining work:

- ✅ Phase 1: Setup (T001-T003)
- ✅ Phase 2: Foundational (T003-T013)
- ✅ US1 Core: Single file download working (T014-T036, except T018, T019, T030)
- ✅ US2 Core: Glob patterns working (T047, T049, T050, T053-T057, T064-T075)
- ✅ US3 Implementation: Progress display implemented (T093-T096)
- ✅ US4 Partial: Checksum validation (T103)
- ✅ Phase 8 Partial: Help text, quickstart, code review (T150-T153)
- ✅ Cross-Platform Unit: GlobPatternHelper tests (T156-T159)

---

## Remaining Tasks (Micro-TDD Format)

### US1 Deferred Tests (Need Mock FileTransferService Interface)

- [ ] T018 [depends:T036] @test-case:UX-005 Test download of nonexistent file shows error in `DownloadCommandTests.cs`
- [ ] T019 [depends:T036] @test-case:UX-030 Test single file success message format in `DownloadCommandTests.cs`
- [ ] T030 [depends:T036] @test-case:EH-004 Test remote file not found displays friendly error in `DownloadCommandTests.cs`

### US2 Remaining Tests

- [ ] T037 [depends:T071] @test-case:UX-006 Test glob pattern matches multiple files in `DownloadCommandTests.cs`
- [ ] T038 [depends:T071] @test-case:UX-007 Test directory-scoped glob pattern in `DownloadCommandTests.cs`
- [ ] T039 [depends:T073] @test-case:UX-008 Test recursive glob flattens to destination in `DownloadCommandTests.cs`
- [ ] T040 [depends:T071] @test-case:UX-009 Test single-char wildcard `?` matching in `DownloadCommandTests.cs`
- [ ] T041 [depends:T074] @test-case:UX-010 Test no matches shows yellow warning in `DownloadCommandTests.cs`
- [ ] T042 [depends:T071] @test-case:UX-011 Test recursive `**` includes subdirectories in `DownloadCommandTests.cs`
- [ ] T043 [depends:T072] @test-case:UX-027 Test collision detection with same filename in `DownloadCommandTests.cs`
- [ ] T044 [depends:T072] @test-case:UX-028 Test collision prevents all downloads in `DownloadCommandTests.cs`
- [ ] T045 [depends:T072] @test-case:UX-029 Test collision error lists all conflicting paths in `DownloadCommandTests.cs`
- [ ] T046 [depends:T075] @test-case:UX-031 Test multi-file success message format in `DownloadCommandTests.cs`
- [ ] T048 [depends:T071] @test-case:CV-004 Test ExpandSourcePattern returns FileInfoEntry list in `DownloadCommandTests.cs`
- [ ] T051 [depends:T011] @test-case:CV-016 Test FileTransferService.EnumerateFiles returns FileInfoEntry array in `FileTransferServiceTests.cs`
- [ ] T052 [depends:T011] @test-case:CV-017 Test FileTransferService.EnumerateFiles uses AllDirectories when recursive in `FileTransferServiceTests.cs`
- [ ] T058 [depends:T070] @test-case:DF-001 Test download start transitions to Expand Source Pattern in `DownloadCommandTests.cs`
- [ ] T059 [depends:T071] @test-case:DF-002 Test glob pattern triggers EnumerateFilesRequest in `DownloadCommandTests.cs`
- [ ] T060 [depends:T072] @test-case:DF-004 Test collision detected transitions to error state in `DownloadCommandTests.cs`
- [ ] T061 [depends:T072] @test-case:DF-005 Test unique filenames transitions to Calculate Total Size in `DownloadCommandTests.cs`
- [ ] T062 [depends:T011] @test-case:DF-017 Test EnumerateFiles request returns FileInfoEntry array in `FileTransferServiceTests.cs`
- [ ] T063 [depends:T011] @test-case:DF-018 Test searchOption=AllDirectories propagates to server in `FileTransferServiceTests.cs`

### US3 Remaining Tests

- [ ] T076 [depends:T093] @test-case:UX-012 Test progress bar shown for file >= threshold in `DownloadCommandTests.cs`
- [ ] T077 [depends:T093] @test-case:UX-013 Test no progress bar for file < threshold in `DownloadCommandTests.cs`
- [ ] T078 [depends:T095] @test-case:UX-014 Test aggregate progress bar for multiple files in `DownloadCommandTests.cs`
- [ ] T079 [depends:T096] @test-case:UX-015 Test transfer speed displayed in `DownloadCommandTests.cs`
- [ ] T080 [depends:T094] @test-case:UX-016 Test progress bar clears on completion in `DownloadCommandTests.cs`
- [ ] T081 [depends:T095] @test-case:UX-017 Test progress updates at least once per second in `DownloadCommandTests.cs`
- [ ] T082 [depends:T012] @test-case:CV-011 Test progressCallback invoked during download in `FileTransferServiceTests.cs`
- [ ] T083 [depends:T010] @test-case:CV-018 Test FileDownloadProgressRegistry.Register returns correlationId in `FileDownloadProgressRegistryTests.cs`
- [ ] T084 [depends:T010] @test-case:CV-019 Test FileDownloadProgressRegistry.Unregister removes callback in `FileDownloadProgressRegistryTests.cs`
- [ ] T085 [depends:T010] @test-case:CV-020 Test FileDownloadProgressRegistry invokes callback on message in `FileDownloadProgressRegistryTests.cs`
- [ ] T086 [depends:T093] @test-case:DF-006 Test total size >= threshold sets showProgress flag in `DownloadCommandTests.cs`
- [ ] T087 [depends:T010] @test-case:DF-009 Test progress message invokes registered callback in `FileDownloadProgressRegistryTests.cs`
- [ ] T088 [depends:T005] @test-case:DF-010 Test percent calculation from TotalRead/TotalSize in `FileDownloadProgressTests.cs`
- [ ] T089 [depends:T095] @test-case:DF-011 Test aggregate progress uses Interlocked.Add in `DownloadCommandTests.cs`
- [ ] T090 [depends:T095] @test-case:DF-012 Test progress delta calculation in `DownloadCommandTests.cs`
- [ ] T091 [depends:T012] @test-case:DF-019 Test progress message triggers callback in `FileTransferServiceTests.cs`
- [ ] T092 [depends:T096] @test-case:IT-003 Test progress callback E2E in `IntegrationTests_DownloadCommand.cs`

### US4 Tests (Error Handling)

- [ ] T097 [depends:T036] @test-case:UX-018 Test file not found error for literal path in `DownloadCommandTests.cs`
- [ ] T098 [depends:T129] @test-case:UX-019 Test permission denied error in `DownloadCommandTests.cs`
- [ ] T099 [depends:T130] @test-case:UX-020 Test connection lost error in `DownloadCommandTests.cs`
- [ ] T100 [depends:T131] @test-case:UX-021 Test checksum failure error in `DownloadCommandTests.cs`
- [ ] T101 [depends:T148] @test-case:UX-022 Test batch summary with failures in `DownloadCommandTests.cs`
- [ ] T102 [depends:T148] @test-case:UX-023 Test batch continues after individual failure in `DownloadCommandTests.cs`
- [ ] T104 [depends:T133] @test-case:CV-015 Test download failure deletes partial file in `FileTransferServiceTests.cs`
- [ ] T105 [depends:T009] @test-case:CV-021 Test FileTransferEndpointService streams with Content-Length in `FileTransferEndpointServiceTests.cs`
- [ ] T106 [depends:T009] @test-case:CV-022 Test FileTransferEndpointService sends progress via SignalR in `FileTransferEndpointServiceTests.cs`
- [ ] T107 [depends:T009] @test-case:CV-023 Test FileTransferEndpointService rejects path traversal in `FileTransferEndpointServiceTests.cs`
- [ ] T108 [depends:T009] @test-case:CV-024 Test FileTransferEndpointService returns 404 for missing file in `FileTransferEndpointServiceTests.cs`
- [ ] T109 [depends:T009] @test-case:CV-025 Test FileTransferEndpointService sets X-File-Checksum header in `FileTransferEndpointServiceTests.cs`
- [ ] T110 [depends:T148] @test-case:DF-007 Test successful download sets Status=Success in `DownloadCommandTests.cs`
- [ ] T111 [depends:T148] @test-case:DF-008 Test failed download sets Status=Failed with Error in `DownloadCommandTests.cs`
- [ ] T112 [depends:T012] @test-case:DF-013 Test HTTP GET with Authorization header in `FileTransferServiceTests.cs`
- [ ] T113 [depends:T012] @test-case:DF-014 Test 200 response streams to local file in `FileTransferServiceTests.cs`
- [ ] T114 [depends:T103] @test-case:DF-015 Test checksum verification from header in `FileTransferServiceTests.cs`
- [ ] T115 [depends:T027] @test-case:DF-016 Test parent directory creation in `FileTransferServiceTests.cs`
- [ ] T116 [depends:T130] @test-case:EH-002 Test connection lost cleanup and message in `DownloadCommandTests.cs`
- [ ] T117 [depends:T130] @test-case:EH-003 Test SignalR disconnect handling in `DownloadCommandTests.cs`
- [ ] T118 [depends:T129] @test-case:EH-005 Test permission denied message in `DownloadCommandTests.cs`
- [ ] T119 [depends:T132] @test-case:EH-006 Test disk space exhausted handling in `DownloadCommandTests.cs`
- [ ] T120 [depends:T133] @test-case:EH-007 Test path too long handling in `DownloadCommandTests.cs`
- [ ] T121 [depends:T133] @test-case:EH-008 Test invalid filename characters handling (cross-platform) in `DownloadCommandTests.cs`
- [ ] T122 [depends:T131] @test-case:EH-012 Test checksum mismatch message in `DownloadCommandTests.cs`
- [ ] T123 [depends:T133] @test-case:EH-013 Test partial download cleanup on exception in `DownloadCommandTests.cs`
- [ ] T124 [depends:T148] @test-case:EH-014 Test batch continues after one failure in `DownloadCommandTests.cs`
- [ ] T125 [depends:T149] @test-case:EH-015 Test all files fail summary in `DownloadCommandTests.cs`
- [ ] T126 [depends:T149] @test-case:EH-016 Test mixed success/failure summary in `DownloadCommandTests.cs`
- [ ] T127 [depends:T107] @test-case:IT-009 Test path traversal prevention E2E in `IntegrationTests_DownloadCommand.cs`
- [ ] T128 [depends:T103] @test-case:IT-005 Test checksum verification E2E in `IntegrationTests_DownloadCommand.cs`

### US4 Implementation

- [ ] T129 [depends:T103] @test-case:IMPL-129 Handle permission denied on local write in `DownloadCommand.cs`
- [ ] T130 [depends:T103] @test-case:IMPL-130 Handle connection lost during download (cleanup partial file) in `DownloadCommand.cs`
- [ ] T131 [depends:T103] @test-case:IMPL-131 Handle checksum verification failure in `DownloadCommand.cs`
- [ ] T132 [depends:T103] @test-case:IMPL-132 Handle disk space exhausted in `DownloadCommand.cs`
- [ ] T133 [depends:T103] @test-case:IMPL-133 Implement partial file cleanup on any error in `DownloadCommand.cs`

### US5 Tests (Concurrent Downloads)

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
- [ ] T144 [depends:T146] @test-case:IT-010 Test large batch E2E in `IntegrationTests_DownloadCommand.cs`

### US5 Implementation

- [ ] T145 [depends:T073] @test-case:IMPL-145 Implement SemaphoreSlim throttling with DownloadConstants.MaxConcurrentDownloads in `DownloadCommand.cs`
- [ ] T146 [depends:T145] @test-case:IMPL-146 Implement parallel download loop with Task.WhenAll in `DownloadCommand.cs`
- [ ] T147 [depends:T095] @test-case:IMPL-147 Aggregate progress across concurrent downloads in `DownloadCommand.cs`
- [ ] T148 [depends:T146] @test-case:IMPL-148 Track success/failure counts across concurrent operations in `DownloadCommand.cs`
- [ ] T149 [depends:T148] @test-case:IMPL-149 Display mixed success/failure summary in `DownloadCommand.cs`

### Cross-Platform Integration Tests

- [ ] T154 [depends:T159] @test-case:IT-011 Test path separator normalization (cross-platform) in `IntegrationTests_DownloadCommand.cs`
- [ ] T155 [depends:T157] @test-case:IT-012 Test case collision detection across platforms in `IntegrationTests_DownloadCommand.cs`
- [ ] T160 [depends:T159] @test-case:IT-CP-001 Test upload with Windows-style source path to Linux-style destination in `IntegrationTests_UploadCommand.cs`
- [ ] T161 [depends:T157] @test-case:IT-CP-002 Test glob pattern matching is case-insensitive in both commands in integration tests
- [ ] T162 [depends:T159] @test-case:IT-CP-003 Test destination path with trailing backslash converts correctly in `UploadCommandTests.cs`

---

## Task Summary

| Category | Remaining |
|----------|-----------|
| US1 Deferred | 3 |
| US2 Tests | 19 |
| US3 Tests | 17 |
| US4 Tests | 31 |
| US4 Implementation | 5 |
| US5 Tests | 11 |
| US5 Implementation | 5 |
| Cross-Platform | 5 |
| **Total Remaining** | **96** |

---

## Batch Execution

Use `/speckit.batch`, `/speckit.execute`, and `/speckit.verify` commands to work through these tasks.

Batches are stored in `batches/` directory.
Evidence files are stored in `evidence/` directory.
State is tracked in `batch-state.json`.
