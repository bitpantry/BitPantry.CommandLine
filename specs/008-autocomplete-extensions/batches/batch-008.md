# Batch 8: autocomplete-extensions

**Created**: 2026-01-19
**Status**: partial
**Tasks**: 9 of 12 complete

## Tasks
- [ ] T082 [depends:T081] @test-case:008:RMT-001 Create `AutoCompleteRequest` envelope with JSON serialization
- [ ] T083 [depends:T082] @test-case:008:RMT-002 Create `AutoCompleteResponse` envelope with JSON serialization
- [ ] T084 [depends:T083] @test-case:008:RMT-003 Verify `AutoCompleteOption` round-trip serialization
- [x] T085 [depends:T084] @test-case:008:RMT-004 Remote autocomplete invocation via SignalR
- [x] T086 [depends:T085] @test-case:008:RMT-005 Remote autocomplete has full parity with local
- [x] T087 [depends:T086] @test-case:008:RMT-006 Remote handler resolution uses server's handlers
- [x] T088 [depends:T087] @test-case:008:RMT-007 Remote attribute handler works
- [x] T089 [depends:T088] @test-case:008:RMT-008 Remote CursorPosition accurately identifies context
- [x] T090 [depends:T089] @test-case:008:RMT-009 Remote returns empty list (not null) when no matches
- [x] T091 [depends:T090] @test-case:008:RMT-UX-001 Ghost text appears over remote connection
- [x] T092 [depends:T091] @test-case:008:RMT-UX-002 Menu opens with server-provided options
- [x] T093 [depends:T092] @test-case:008:RMT-UX-003 Type-to-filter applied locally (no round-trip)

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [x] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities

## Verification Evidence

| Task | Test Case | Test Method | File | Status |
|------|-----------|-------------|------|--------|
| T082 | 008:RMT-001 | N/A | N/A | ⚠️ Missing unit test - serialization exercised via integration |
| T083 | 008:RMT-002 | N/A | N/A | ⚠️ Missing unit test - serialization exercised via integration |
| T084 | 008:RMT-003 | N/A | N/A | ⚠️ Missing unit test - serialization exercised via integration |
| T085 | 008:RMT-004 | `AutoComplete_RemoteEnumArgument_ReturnsAllValues` | IntegrationTests_AutoComplete.cs | ✅ |
| T086 | 008:RMT-005 | `AutoComplete_RemoteBoolArgument_ReturnsTrueFalse` | IntegrationTests_AutoComplete.cs | ✅ |
| T087 | 008:RMT-006 | `AutoComplete_RemoteFlagsEnum_ReturnsAllFlagValues` | IntegrationTests_AutoComplete.cs | ✅ |
| T088 | 008:RMT-007 | `AutoComplete_RemoteAttributeHandler_ReturnsCustomValues` | IntegrationTests_AutoComplete.cs | ✅ |
| T089 | 008:RMT-008 | `AutoComplete_MultipleArguments_CursorPositionAccurate` | IntegrationTests_AutoComplete.cs | ✅ |
| T090 | 008:RMT-009 | `AutoComplete_NoMatches_ReturnsEmptyList` | IntegrationTests_AutoComplete.cs | ✅ |
| T091 | 008:RMT-UX-001 | `E2E_GhostText_AppearsOverRemoteConnection` | IntegrationTests_AutoComplete.cs | ✅ |
| T092 | 008:RMT-UX-002 | `E2E_Menu_OpensWithServerProvidedOptions` | IntegrationTests_AutoComplete.cs | ✅ |
| T093 | 008:RMT-UX-003 | `E2E_TypeToFilter_AppliedLocally` | IntegrationTests_AutoComplete.cs | ✅ |

## Notes

T082-T084 specify unit tests for JSON serialization of envelope classes. These are implicitly validated by integration tests that exercise full SignalR round-trips (serialization occurs at the transport layer). Explicit unit tests could be added for defense-in-depth but are not strictly required since integration tests would fail if serialization was broken.
