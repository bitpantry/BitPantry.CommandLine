---
description: Assess one cluster for test validity and determine remediation action.
---

## User Input

```text
$ARGUMENTS
```

**Required**: Cluster ID (e.g., `C001`)

If no cluster ID provided, list available clusters from `refactor-temp/clusters.md`.

---

## Intent

Phase 3 of the Test Remediation workflow. Evaluates each test in a cluster for:
- Validity (per testing standards)
- Consolidation opportunity
- Required remediation action

Produces an assessment file and updates the backlog.

---

## Prerequisites

1. `refactor-temp/clusters.md` must exist (run `/test-remediation.cluster` first)
2. Read `refactor-temp/standards.md` for validity criteria
3. Identify the cluster to assess from user input

---

## Execution Steps

### Step 1: Load Cluster

1. Read `refactor-temp/clusters.md`
2. Find the specified cluster (e.g., C001)
3. Extract the list of test IDs in the cluster

### Step 2: Read Each Test

For each test in the cluster:
1. Read the actual test code from the source file
2. Identify Arrange/Act/Assert sections
3. Understand what behavior is being tested

### Step 3: Validate Each Test

Apply the **Mandatory Validation Checkpoint** to each test:

```
> Test Validity Check for [TestMethod]:
>   Invokes code under test: [YES/NO] — Does this call the actual method/class being tested?
>   Breakage detection: [YES/NO] — If implementation breaks, does test fail?
>   Not a tautology: [YES/NO] — Testing behavior, not restating structure?
```

Also check:
- Correct console type for assertions?
- Uses shared helpers appropriately?
- Follows naming convention?

### Step 4: Identify Invalid Patterns

For any failing test, identify which invalid pattern applies:

| Pattern | Indicators |
|---------|------------|
| Testing constants | `Constant.Should().Be(value)` |
| Testing inputs | Asserting on input data, not output |
| Tautology | `x.Should().Be(x)` or similar |
| Testing framework | Creating framework objects and testing them |
| Not invoking code | Mocks set up but never triggered |

### Step 5: Determine Cluster Decision

Based on assessment:

| All Tests Valid? | Consolidation Candidate? | Decision |
|------------------|--------------------------|----------|
| YES | YES | **CONSOLIDATE** |
| YES | NO | **NO ACTION** |
| SOME | YES | **FIX + CONSOLIDATE** |
| SOME | NO | **FIX INDIVIDUALLY** |
| NO (all invalid) | — | **REWRITE** |

### Step 6: Create Assessment File

**Create `refactor-temp/assessments/cluster-[ID].md`:**

```markdown
# Cluster C001 Assessment

**Cluster**: C001 — DownloadCommand.Execute — Connected + Success  
**Assessed**: 2026-01-15  
**Tests**: 3

## Summary

| Decision | CONSOLIDATE |
|----------|-------------|
| Valid Tests | 2 of 3 |
| Invalid Tests | 1 (T002) |
| Effort | Medium |

---

## Test Assessments

### T001: Execute_ValidFile_Succeeds ✅ VALID

**File**: `ClientTests/DownloadCommandTests.cs:45`

**Validity Check**:
- Invokes code under test: YES — calls `command.Execute(context)`
- Breakage detection: YES — would fail if download logic broken
- Not a tautology: YES — verifies output contains "Downloaded"

**Console Type**: TestConsole ✅ (text-only assertion)
**Helpers**: Uses TestServerProxyFactory ✅

---

### T002: Execute_ValidFile_ShowsProgress ❌ INVALID

**File**: `ClientTests/DownloadCommandTests.cs:78`

**Validity Check**:
- Invokes code under test: YES
- Breakage detection: NO — tests constant value, not behavior
- Not a tautology: NO

**Invalid Pattern**: Testing constants
**Current Code**:
```csharp
DownloadConstants.ProgressDisplayThreshold.Should().Be(25_000_000);
```

**Fix Required**: Test that progress IS displayed when file size exceeds threshold

---

### T003: Execute_ValidFile_ReturnsZero ✅ VALID

**File**: `ClientTests/DownloadCommandTests.cs:95`

**Validity Check**:
- Invokes code under test: YES
- Breakage detection: YES
- Not a tautology: YES

---

## Consolidation Plan

### Target Test Name
`Execute_ValidFile_SucceedsAndDisplaysProgressAndReturnsZero`

Or better:
`Execute_ValidFile_CompletesWithExpectedOutputAndResult`

### Consolidated Assertions

```csharp
// T001: Verify success message
_console.Output.Should().Contain("Downloaded");

// T002 (FIXED): Verify progress displayed for large file
_console.Output.Should().Contain("%"); // or progress indicator

// T003: Verify return code
result.Should().Be(0);
```

### Tests to Delete After Consolidation
- T001: Execute_ValidFile_Succeeds
- T002: Execute_ValidFile_ShowsProgress
- T003: Execute_ValidFile_ReturnsZero

---

## Backlog Entry

**ID**: R001  
**Priority**: Medium  
**Cluster**: C001  
**Decision**: FIX + CONSOLIDATE  
**Effort**: Medium

**Actions**:
1. Fix T002 to test behavior, not constant
2. Write consolidated test with labeled assertions
3. Delete original 3 tests
4. Verify consolidated test passes
```

### Step 7: Update Backlog

Append to `refactor-temp/backlog.md`:

```markdown
## R001: Cluster C001 — DownloadCommand Success Path

**Priority**: Medium  
**Decision**: FIX + CONSOLIDATE  
**Cluster**: C001  
**Tests**: T001, T002, T003 → 1 consolidated test

### Summary
- 2 valid tests, 1 invalid (T002 tests constant)
- Consolidate into single test with labeled assertions
- Fix T002 to test actual progress display behavior

### Actions
1. [ ] Fix T002 invalid pattern
2. [ ] Create consolidated test
3. [ ] Delete original tests
4. [ ] Verify test passes

**Assessment**: [cluster-C001.md](assessments/cluster-C001.md)
```

---

## Output Format

Report completion:

```
Cluster C001 Assessment Complete

Tests assessed: 3
  - Valid: 2 (T001, T003)
  - Invalid: 1 (T002 — tests constant)

Decision: FIX + CONSOLIDATE
  - Fix T002 to test behavior
  - Merge into single test with labeled assertions

Backlog entry created: R001 (Medium priority)

Output:
- refactor-temp/assessments/cluster-C001.md
- refactor-temp/backlog.md (updated)

Next step: 
- Assess another cluster: /test-remediation.assess C002
- Or refactor this one: /test-remediation.refactor R001
```

---

## Constraints

- Assess ONE cluster per invocation
- Read actual test code, don't guess from names
- Be specific about which invalid pattern applies
- Provide concrete fix recommendations
- Always create a backlog entry, even for "NO ACTION" decisions
