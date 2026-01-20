# Batch 3: autocomplete-extensions

**Created**: 2026-01-19
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T025 [depends:T024] @test-case:008:TC-2.11 `GetOptionsAsync` filters by prefix
- [ ] T026 [depends:T021,T025] Register `EnumAutoCompleteHandler` and `BooleanAutoCompleteHandler` by default in registry constructor
- [ ] T027 [depends:T009] @test-case:008:TC-3.1 Verify generic constraint prevents invalid types at compile time
- [ ] T028 [depends:T027] @test-case:008:TC-3.2 `HandlerType` property returns correct type via `IAutoCompleteAttribute`
- [ ] T029 [depends:T028] @test-case:008:TC-3.3 Attribute works with `ITypeAutoCompleteHandler` types (compile-time test)
- [ ] T030 [depends:T029] @test-case:008:TC-3.4 Custom attributes inheriting `AutoCompleteAttribute<T>` are discoverable via marker interface
- [ ] T031 [depends:T026,T030] @test-case:008:TC-4.1 End-to-end enum autocomplete works with default application
- [ ] T032 [depends:T031] @test-case:008:TC-4.2 Custom Type Handler overrides built-in when registered after
- [ ] T033 [depends:T032] @test-case:008:TC-4.3 Attribute Handler used even when matching Type Handler exists
- [ ] T034 [depends:T033] @test-case:008:TC-4.4 Handler receives `ProvidedValues` in context with already-entered values
- [ ] T035 [depends:T034] @test-case:008:TC-4.5 Boolean autocomplete works end-to-end
- [ ] T036 [depends:T035] @test-case:008:TC-4.6 Nullable enum autocomplete works end-to-end

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
