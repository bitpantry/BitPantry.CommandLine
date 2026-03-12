# Case Sensitivity Removal — Test Remediation Plan

Resolution is now always case-sensitive (`StringComparison.Ordinal`). Autocomplete, help flags, and type-value handlers remain case-insensitive by design.

## Root Cause

`CommandReflection.cs` line 55 stores command names as the raw class name:
```csharp
info.Name = cmdAttr?.Name ?? commandType.Name;
```
Group names are stored with `ToLowerInvariant().Replace("group","")` transformation.

Previously, resolution used `OrdinalIgnoreCase` by default, so test inputs like `"command"` matched stored name `"Command"`. With the switch to `Ordinal`, these no longer match.

## Baseline

- **Total tests**: 1795
- **Passed**: 1733
- **Failed**: 62

## Failure Categories

### Category A — Command name case mismatch (52 tests)

Tests use camelCase inputs (e.g., `"command"`) but stored name is PascalCase class name (e.g., `"Command"`). Fix: correct test input to match canonical stored name.

### Category B — Group case-insensitive tests (5 tests)

Tests intentionally verify case-insensitive group/command resolution. Now that resolution is case-sensitive, these need rewriting to assert wrong case FAILS.

### Category C — Mixed-case variant test (1 test)

`ResolveCommandCaseVariant_Resolved` uses `"cOmMaNd"` expecting it to resolve. Now it shouldn't.

### Category D — Previously completed (4 compile-time fixes + 2 removed tests)

Already addressed in earlier passes.

---

## Completed Remediations (Category D)

| Test | Remediation | Status |
|---|---|---|
| `GroupResolutionTests.CaseSensitive_Enabled_ExactCaseRequired` | Removed — tests removed `CaseSensitive` toggle | ✅ DONE |
| `GroupResolutionTests.CaseSensitive_FindGroup_RespectsSettings` | Removed — tests removed `CaseSensitive` toggle | ✅ DONE |
| `TokenMatchResolverTests` (class-level setup) | Removed `_mockRegistry.Setup(r => r.CaseSensitive).Returns(false)` line | ✅ DONE |
| `SyntaxHighlighterTests` (class-level setup) | Removed `_mockRegistry.Setup(r => r.CaseSensitive).Returns(false)` line | ✅ DONE |
| `SyntaxHighlighterIntegrationTests` (class-level setup) | Removed `_mockRegistry.Setup(r => r.CaseSensitive).Returns(false)` line | ✅ DONE |

---

## Category A — Command Name Case Mismatch (52 tests)

**Root cause**: Test input uses camelCase, stored name is PascalCase class name.
**Fix**: Change test input string from camelCase to PascalCase.

### ResolveCommandTests.cs (21 tests)

| Test | Current Input | Correct Input | Status |
|---|---|---|---|
| `ResolveCommandWithBadArgumentName_ResolvedWithErrors` | `"command --doesntExist"` | `"Command --doesntExist"` | ⬜ |
| `ResolveCommandWithTwoBadArgumentName_ResolvedWithErrors` | `"command --doesntExist --alsoDoesntExist"` | `"Command --doesntExist --alsoDoesntExist"` | ⬜ |
| `ResolveCommandWithArgument_Resolved` | `"commandWithArgument --ArgOne 42"` | `"CommandWithArgument --ArgOne 42"` | ⬜ |
| `ResolveCommandWithArgumentNameInvariantCase_Resolved` | `"commandWithArgument --aRgOnE 42"` | `"CommandWithArgument --aRgOnE 42"` | ⬜ |
| `ResolveCommandWithArgumentAlias_Resolved` | `"commandWithAlias -y 42"` | `"CommandWithAlias -y 42"` | ⬜ |
| `ResolveCommandWithArgumentAliasWrongCase_ResolvedWithErrors` | `"commandWithAlias -Y"` | `"CommandWithAlias -Y"` | ⬜ |
| `ResolveCommandWithMultipleArguments_Resolved` | `"multipleArgumentsAndAliases --myProperty 123 -y \"value\" --Prop propValue"` | `"MultipleArgumentsAndAliases --myProperty 123 -y \"value\" --Prop propValue"` | ⬜ |
| `ResolveExtendedCommand_Resolved` | `"extendedCommand"` | `"ExtendedCommand"` | ⬜ |
| `ResolveCommand_RES001_SinglePositionalResolved` | `"singlePositionalCommand value1"` | `"SinglePositionalCommand value1"` | ⬜ |
| `ResolveCommand_RES002_MultiplePositionalResolved` | `"multiplePositionalCommand first second 42"` | `"MultiplePositionalCommand first second 42"` | ⬜ |
| `ResolveCommand_RES003_PositionalAndNamedResolved` | `"positionalWithNamedCommand source.txt dest.txt --force --mode copy"` | `"PositionalWithNamedCommand source.txt dest.txt --force --mode copy"` | ⬜ |
| `ResolveCommand_RES004_IsRestCollectsRemaining` | `"isRestWithPrecedingCommand target a b c d"` | `"IsRestWithPrecedingCommand target a b c d"` | ⬜ |
| `ResolveCommand_RES005_IsRestWithZeroExtra` | `"isRestWithPrecedingCommand target"` | `"IsRestWithPrecedingCommand target"` | ⬜ |
| `ResolveCommand_RES006_MissingRequiredPositional` | `"requiredPositionalCommand source.txt"` | `"RequiredPositionalCommand source.txt"` | ⬜ |
| `ResolveCommand_RES007_ExcessPositionalValues` | `"singlePositionalCommand value1 value2 value3"` | `"SinglePositionalCommand value1 value2 value3"` | ⬜ |
| `ResolveCommand_RES008_RepeatedOptionCollection` | `"repeatedOptionArrayCommand --items a --items b --items c"` | `"RepeatedOptionArrayCommand --items a --items b --items c"` | ⬜ |
| `ResolveCommand_RES009_RepeatedOptionScalarError` | `"repeatedOptionScalarCommand --value a --value b"` | `"RepeatedOptionScalarCommand --value a --value b"` | ⬜ |
| `ResolveCommand_RES010_MixedDelimiterAndRepeated` | `"repeatedOptionArrayCommand --items a --verbose true --items b"` | `"RepeatedOptionArrayCommand --items a --verbose true --items b"` | ⬜ |
| `ResolveCommand_RES011_PositionalAfterEndOfOptions` | `"singlePositionalCommand -- --dashValue"` | `"SinglePositionalCommand -- --dashValue"` | ⬜ |
| `ResolveCommand_IsRestOnly` | `"isRestCommand a b c"` | `"IsRestCommand a b c"` | ⬜ |

### CommandActivatorTests.cs (16 tests)

| Test | Current Input | Correct Input | Status |
|---|---|---|---|
| `ActivateCommand_Activated` | `"command"` | `"Command"` | ⬜ |
| `ActivateWithoutArgInput_Activated` | `"withArgument"` | `"WithArgument"` | ⬜ |
| `ActivateIntArg_Activated` | `"withIntArg --intArg 10"` | `"WithIntArg --intArg 10"` | ⬜ |
| `ActivateAlias_Activated` | `"withAlias -a 10"` | `"WithAlias -a 10"` | ⬜ |
| `ActivateOption_Activated` | `"withOption --optOne"` | `"WithOption --optOne"` | ⬜ |
| `ActivateOptionAlias_Activated` | `"withOption -o"` | `"WithOption -o"` | ⬜ |
| `ActivateOptionNotSet_Activated` | `"withOption"` | `"WithOption"` | ⬜ |
| `ActivateOptionAbsent_Activated` | `"withOption"` | `"WithOption"` | ⬜ |
| `ActivateCommand_ACT001_StringPositional` | `"singlePositionalCommand myValue"` | `"SinglePositionalCommand myValue"` | ⬜ |
| `ActivateCommand_ACT002_IntPositional` | `"multiplePositionalCommand first second 42"` | `"MultiplePositionalCommand first second 42"` | ⬜ |
| `ActivateCommand_ACT003_IsRestStringArray` | `"isRestCommand a b c"` | `"IsRestCommand a b c"` | ⬜ |
| `ActivateCommand_ACT004_IsRestWithPreceding` | `"isRestWithPrecedingCommand target a b c d"` | `"IsRestWithPrecedingCommand target a b c d"` | ⬜ |
| `ActivateCommand_ACT006_RepeatedOptionPopulatesArray` | `"repeatedOptionArrayCommand --items a --items b --items c"` | `"RepeatedOptionArrayCommand --items a --items b --items c"` | ⬜ |
| `ActivateCommand_ACT007_PositionalTypeMismatch` | `"multiplePositionalCommand first second notAnInt"` | `"MultiplePositionalCommand first second notAnInt"` | ⬜ |
| `ActivateCommand_ACT008_EmptyIsRest` | `"isRestWithPrecedingCommand target"` | `"IsRestWithPrecedingCommand target"` | ⬜ |
| `ActivateCommand_ACT009_MixedPositionalAndNamed` | `"positionalWithNamedCommand source.txt dest.txt --force --mode copy"` | `"PositionalWithNamedCommand source.txt dest.txt --force --mode copy"` | ⬜ |

### CommandLineApplicationTests.cs (13 tests)

| Test | Current Input | Correct Input | Status |
|---|---|---|---|
| `TestExecute_Success` | `"testExecute"` | `"TestExecute"` | ⬜ |
| `ExecuteCancel_Success` | `"testExecuteCancel"` | `"TestExecuteCancel"` | ⬜ |
| `ExecuteError_Error` | `"testExecuteError"` | `"TestExecuteError"` | ⬜ |
| `ExecuteReturnType_Success` | `"testExecuteWithReturnType"` | `"TestExecuteWithReturnType"` | ⬜ |
| `ExecuteReturnTypeAsync_Success` | `"testExecuteWithReturnTypeAsync"` | `"TestExecuteWithReturnTypeAsync"` | ⬜ |
| `ExecuteReturnTypeAsyncGeneric_Success` | `"testExecuteWithReturnTypeAsyncGeneric"` | `"TestExecuteWithReturnTypeAsyncGeneric"` | ⬜ |
| `ExecuteBasicPipeline_Success` | `"testExecute \| testExecuteWithReturnType"` | `"TestExecute \| TestExecuteWithReturnType"` | ⬜ |
| `PassDataBetweenCommands_Success` | `"returnsZero \| returnsInputPlusOne"` | `"ReturnsZero \| ReturnsInputPlusOne"` | ⬜ |
| `PassDataBetweenCommandsMany_Success` | `"returnsZero \| returnsInputPlusOne \| returnsInputPlusOne \| returnsInputPlusOne"` | `"ReturnsZero \| ReturnsInputPlusOne \| ReturnsInputPlusOne \| ReturnsInputPlusOne"` | ⬜ |
| `PassByteArray_success` | `"returnsByteArray \| receivesByteArray"` | `"ReturnsByteArray \| ReceivesByteArray"` | ⬜ |
| `ExtendedCommand_success` | `"extendedCommand"` | `"ExtendedCommand"` | ⬜ |
| `PositionalExecution_INT001_FullPositionalExecution` | `"testPositionalCommand source.txt dest.txt"` | `"TestPositionalCommand source.txt dest.txt"` | ⬜ |
| `PositionalExecution_INT004_BackwardCompatibility` | `"testExecuteWithReturnType"` | `"TestExecuteWithReturnType"` | ⬜ |
| `PositionalExecution_MixedPositionalAndNamed` | `"testPositionalCommand first second"` | `"TestPositionalCommand first second"` | ⬜ |

### ResolveInputTests.cs (2 tests)

| Test | Current Input | Correct Input | Status |
|---|---|---|---|
| `ResolvePipelineInputWithData_Resolved` | `"returnsString \| acceptsString"` | `"ReturnsString \| AcceptsString"` | ⬜ |
| `ResolvePipelineInputVoidToAccepts_Resolved` | `"Command \| acceptsString"` | `"Command \| AcceptsString"` | ⬜ |

### CommandActivatorWithDITests.cs (2 tests)

| Test | Current Input | Correct Input | Status |
|---|---|---|---|
| `CommandExecute_NoDeps_Executes` | `"testCommandOneNoDeps"` | `"TestCommandOneNoDeps"` | ⬜ |
| `CommandExecute_Deps_Executes` | `"testCommandTwoWithDeps"` | `"TestCommandTwoWithDeps"` | ⬜ |

---

## Category B — Group Case-Insensitive Tests (5 tests)

**Root cause**: Tests assert that wrong-case group/command names resolve successfully. With case-sensitive resolution, wrong case should fail.
**Fix**: Rewrite to assert that wrong case does NOT resolve.

| Test | Current Behavior | New Behavior | Remediation | Status |
|---|---|---|---|---|
| `GroupResolutionTests.Resolve_GroupAndCommand_CaseInsensitive` | `"MATH ADD"` resolves to `add` | Should NOT resolve | Rewrite: assert `result.CommandInfo` is null and `result.IsValid` is false; rename to `Resolve_GroupAndCommand_WrongCase_DoesNotResolve` | ⬜ |
| `GroupResolutionTests.CaseInsensitive_MixedCase_ResolvesSuccessfully` | `"Math Add"` resolves | Should NOT resolve | **REMOVE** — overlaps with rewritten `Resolve_GroupAndCommand_CaseInsensitive` above (both test wrong-case group+command) | ⬜ |
| `GroupInvocationTests.InvokeGroupedCommand_CaseInsensitive` | `"MATH ADD --num1 2 --num2 2"` returns Success | Should return ResolutionError | Rewrite: assert `result.ResultCode == ResolutionError`; rename to `InvokeGroupedCommand_WrongCase_ReturnsError` | ⬜ |
| `GroupRegistrationTests.FindGroup_CaseInsensitive_Found` | `FindGroup("MATH")` returns group | Should return null | Rewrite: assert `group` is null; rename to `FindGroup_WrongCase_ReturnsNull` | ⬜ |
| `CommandRegistryGroupTests.FindGroup_CaseInsensitive_ReturnsGroup` | `FindGroup("MATH")` returns group | Should return null | **REMOVE** — overlaps with rewritten `GroupRegistrationTests.FindGroup_WrongCase_ReturnsNull` | ⬜ |

---

## Category C — Mixed-Case Variant Test (1 test)

| Test | Current Behavior | New Behavior | Remediation | Status |
|---|---|---|---|---|
| `ResolveCommandCaseVariant_Resolved` | `"cOmMaNd"` resolves to Command | Should NOT resolve | Rewrite: assert `result.CommandInfo` is null, `result.IsValid` is false, error is `CommandNotFound`; rename to `ResolveCommand_WrongCase_DoesNotResolve` | ⬜ |

---

## Overlap / Duplicate Intent Analysis

With case-sensitive-only resolution, several tests that previously had distinct intent now test the same thing:

### 1. `CaseInsensitive_MixedCase_ResolvesSuccessfully` vs `Resolve_GroupAndCommand_CaseInsensitive`

**Before**: Tested mixed case ("Math Add") vs all-caps ("MATH ADD") — two distinct case-insensitive variants.
**After**: Both test "wrong case fails" on group+command resolution. Same intent.
**Recommendation**: REMOVE `CaseInsensitive_MixedCase_ResolvesSuccessfully`. Keep only `Resolve_GroupAndCommand_CaseInsensitive` → renamed to `Resolve_GroupAndCommand_WrongCase_DoesNotResolve`.

### 2. `CommandRegistryGroupTests.FindGroup_CaseInsensitive_ReturnsGroup` vs `GroupRegistrationTests.FindGroup_CaseInsensitive_Found`

**Before**: Both tested `FindGroup("MATH")` returning a result — one in CommandRegistryGroupTests, other in GroupRegistrationTests.
**After**: Both would test `FindGroup("MATH")` returning null. Same intent.
**Recommendation**: REMOVE `CommandRegistryGroupTests.FindGroup_CaseInsensitive_ReturnsGroup`. Keep only `GroupRegistrationTests.FindGroup_CaseInsensitive_Found` → renamed to `FindGroup_WrongCase_ReturnsNull`.

### 3. `ActivateOptionNotSet_Activated` vs `ActivateOptionAbsent_Activated`

**Before AND After**: Both use input `"WithOption"` (no --optOne flag), both assert `OptOne.Should().BeFalse()`. These are identical tests regardless of case sensitivity. Not caused by this change but worth noting.
**Recommendation**: REMOVE `ActivateOptionAbsent_Activated` — exact duplicate of `ActivateOptionNotSet_Activated`.

### 4. `ResolveCommandCaseVariant_Resolved` (rewritten) — no overlap

The rewritten `ResolveCommand_WrongCase_DoesNotResolve` is the ONLY test verifying command-level (non-group) wrong-case rejection. Unique intent. Keep.

### Summary of Removals

| Test to Remove | Reason |
|---|---|
| `GroupResolutionTests.CaseInsensitive_MixedCase_ResolvesSuccessfully` | Overlaps with rewritten `Resolve_GroupAndCommand_WrongCase_DoesNotResolve` |
| `CommandRegistryGroupTests.FindGroup_CaseInsensitive_ReturnsGroup` | Overlaps with rewritten `FindGroup_WrongCase_ReturnsNull` |
| `CommandActivatorTests.ActivateOptionAbsent_Activated` | Exact duplicate of `ActivateOptionNotSet_Activated` (pre-existing) |

**Net test count change**: 62 failures → 52 input fixes, 3 rewrites, 4 removals, 3 pre-existing removals (Category D already complete) = all resolved with **3 fewer tests** than before.
