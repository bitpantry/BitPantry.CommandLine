# Batch 9: autocomplete-extensions

**Created**: 2026-01-19
**Status**: complete
**Tasks**: 8 of 8 complete

## Tasks
- [x] T094 [depends:T093] @test-case:008:RMT-UX-004 Connection failure degrades gracefully (silent)
- [x] T095 [depends:T094] @test-case:008:RMT-UX-005 Slow connection shows ghost text when response arrives
- [x] T096 [depends:T095] Remove `AutoCompleteFunctionName` from `ArgumentAttribute`
- [x] T097 [depends:T096] Remove `AutoCompleteFunctionName` from `ArgumentInfo`
- [x] T098 [depends:T097] Remove `IsAutoCompleteFunctionAsync` from `ArgumentInfo`
- [x] T099 [depends:T098] Remove legacy invocation code from `AutoCompleteOptionSetBuilder`
- [x] T100 [depends:T099] Delete `AutoComplete/AutoCompleteContext.cs` (replaced)
- [x] T101 [depends:T100] Delete legacy test commands from `Commands/AutoCompleteCommands/`

## Completion Criteria

- [x] All tasks verified (evidence validated)
- [x] Full test suite passes (5 consecutive clean runs)
- [x] No open ambiguities

## Verification Evidence

| Task | Test Case | Verification Method | Status |
|------|-----------|---------------------|--------|
| T094 | 008:RMT-UX-004 | `E2E_ConnectionFailure_DegradesGracefully` | ✅ |
| T095 | 008:RMT-UX-005 | `E2E_SlowConnection_ShowsGhostTextWhenResponseArrives` | ✅ |
| T096 | N/A | `grep_search` - no matches in source code | ✅ |
| T097 | N/A | `ArgumentInfo.cs` has no `AutoCompleteFunctionName` property | ✅ |
| T098 | N/A | `ArgumentInfo.cs` has no `IsAutoCompleteFunctionAsync` property | ✅ |
| T099 | N/A | `AutoCompleteOptionSetBuilder.cs` file not found (deleted) | ✅ |
| T100 | N/A | `AutoComplete/AutoCompleteContext.cs` not found (replaced by `Handlers/AutoCompleteContext.cs`) | ✅ |
| T101 | N/A | `Commands/AutoCompleteCommands/` directory not found (deleted) | ✅ |
