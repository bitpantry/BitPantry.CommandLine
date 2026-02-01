# Batch 7: autocomplete-extensions

**Created**: 2026-01-19
**Status**: complete
**Tasks**: 12 of 12 complete

## Tasks
- [x] T070 [depends:T069] @test-case:008:UX-016 Attribute Handler overrides Type Handler
- [x] T071 [depends:T070] @test-case:008:UX-017 Positional enum shows ghost text
- [x] T072 [depends:T071] @test-case:008:UX-018 Multiple positionals track independently
- [x] T073 [depends:T072] @test-case:008:UX-019 Positional without handler shows no ghost text
- [x] T074 [depends:T073] @test-case:008:UX-031 Positional set positionally excluded from `--` suggestions
- [x] T075 [depends:T074] @test-case:008:UX-032 Positional set by name has no positional autocomplete
- [x] T076 [depends:T075] @test-case:008:UX-033 Named arg set but positional unsatisfied still autocompletes
- [x] T077 [depends:T076] @test-case:008:UX-034 After named arg only named args available
- [x] T078 [depends:T077] @test-case:008:UX-035 Unsatisfied positional appears in `--` suggestions
- [x] T079 [depends:T078] @test-case:008:UX-028 Values with spaces auto-quoted
- [x] T080 [depends:T079] @test-case:008:UX-029 Values without spaces not quoted
- [x] T081 [depends:T080] @test-case:008:UX-030 Completion within existing quotes continues quote context

## Completion Criteria

- [x] All tasks verified (evidence validated)
- [x] Full test suite passes (5 consecutive clean runs)
- [x] No open ambiguities

## Verification Evidence

| Task | Test Case | Test Method | File |
|------|-----------|-------------|------|
| T070 | 008:UX-016 | `Update_AttributeHandler_OverridesTypeHandler` | AutoCompleteControllerTests.cs |
| T071 | 008:UX-017 | `Update_PositionalEnumArgument_ShowsGhostText` | AutoCompleteControllerTests.cs |
| T072 | 008:UX-018 | `Update_MultiplePositionalArgs_TrackIndependently` | AutoCompleteControllerTests.cs |
| T073 | 008:UX-019 | `Update_PositionalWithoutHandler_NoGhostText` | AutoCompleteControllerTests.cs |
| T074 | 008:UX-031 | `Resolve_PositionalSetPositionally_ExcludedFromNamedSuggestions` | CursorContextResolverTests.cs |
| T075 | 008:UX-032 | `Resolve_PositionalSetByName_NoPositionalAutocomplete` | CursorContextResolverTests.cs |
| T076 | 008:UX-033 | `Resolve_NamedArgSetButPositionalUnsatisfied_StillAutocompletes` | CursorContextResolverTests.cs |
| T077 | 008:UX-034 | `Resolve_AfterNamedArg_OnlyNamedArgsAvailable` | CursorContextResolverTests.cs |
| T078 | 008:UX-035 | `GetOptions_UnsatisfiedPositional_AppearsInNamedSuggestions` | ArgumentNameHandlerTests.cs |
| T079 | 008:UX-028 | `AutoComplete_ValueWithSpaces_AutoQuoted` | InputBuilderAutoCompleteTests.cs |
| T080 | 008:UX-029 | `AutoComplete_ValueWithoutSpaces_NotQuoted` | InputBuilderAutoCompleteTests.cs |
| T081 | 008:UX-030 | `AutoComplete_WithinExistingQuotes_ContinuesQuoteContext` | InputBuilderAutoCompleteTests.cs |
