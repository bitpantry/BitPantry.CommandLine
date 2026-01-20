# Batch 2: autocomplete-extensions

**Created**: 2026-01-19
**Status**: pending
**Tasks**: 0 of 12 complete

## Tasks
- [ ] T013 [depends:T012] @test-case:008:TC-1.3 Implement last-registered-wins ordering in `GetHandler()`
- [ ] T014 [depends:T013,T009] @test-case:008:TC-1.4 Implement attribute precedence in `GetHandler()` (attribute over type handler)
- [ ] T015 [depends:T014] @test-case:008:TC-2.1 Create `EnumAutoCompleteHandler` with `CanHandle` returning true for enum types
- [ ] T016 [depends:T015] @test-case:008:TC-2.2 `CanHandle` returns false for non-enum types
- [ ] T017 [depends:T016] @test-case:008:TC-2.3 `CanHandle` returns false for `typeof(Enum)` base type
- [ ] T018 [depends:T017] @test-case:008:TC-2.4 `GetOptionsAsync` returns all enum values when query empty
- [ ] T019 [depends:T018] @test-case:008:TC-2.5 `GetOptionsAsync` filters by prefix case-insensitively
- [ ] T020 [depends:T019] @test-case:008:TC-2.6 `GetOptionsAsync` returns alphabetically sorted results
- [ ] T021 [depends:T020] @test-case:008:TC-2.7 `GetOptionsAsync` unwraps nullable enum types
- [ ] T022 [depends:T014] @test-case:008:TC-2.8 Create `BooleanAutoCompleteHandler` with `CanHandle` returning true for bool
- [ ] T023 [depends:T022] @test-case:008:TC-2.9 `CanHandle` returns false for non-bool types
- [ ] T024 [depends:T023] @test-case:008:TC-2.10 `GetOptionsAsync` returns ["false", "true"] when query empty

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
