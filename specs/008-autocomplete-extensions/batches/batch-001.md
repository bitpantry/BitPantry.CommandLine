# Batch 1: autocomplete-extensions

**Created**: 2026-01-19
**Status**: in-progress
**Tasks**: 12 of 15 complete

## Tasks
- [X] T001 Create `BitPantry.CommandLine/AutoComplete/Handlers/` directory structure
- [X] T002 [depends:T001] Create `BitPantry.CommandLine/AutoComplete/Syntax/` directory structure
- [X] T003 [depends:T001] Create `BitPantry.CommandLine.Tests/AutoComplete/Handlers/` test directory
- [X] T004 [depends:T002] Create `BitPantry.CommandLine.Tests/AutoComplete/Syntax/` test directory
- [X] T005 [depends:T003] Create `AutoCompleteConstants.cs` with `DefaultVisibleMenuItems = 5`
- [X] T006 [depends:T005] Create `IAutoCompleteHandler` interface in `Handlers/IAutoCompleteHandler.cs`
- [X] T007 [depends:T006] Create `ITypeAutoCompleteHandler` interface extending `IAutoCompleteHandler` in `Handlers/ITypeAutoCompleteHandler.cs`
- [X] T008 [depends:T006] Create `IAutoCompleteAttribute` marker interface in `Handlers/IAutoCompleteAttribute.cs`
- [X] T009 [depends:T008] Create `AutoCompleteAttribute<THandler>` generic attribute in `Handlers/AutoCompleteAttribute.cs`
- [X] T010 [depends:T006] Create `AutoCompleteContext` class in `Handlers/AutoCompleteContext.cs`
- [X] T011 [depends:T007,T010] @test-case:008:TC-1.1 Create `AutoCompleteHandlerRegistry` with `Register<T>()` in `Handlers/AutoCompleteHandlerRegistry.cs`
- [X] T012 [depends:T011] @test-case:008:TC-1.2 Implement `GetHandler()` returning null when no handler matches

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (3 consecutive clean runs)
- [ ] No open ambiguities












