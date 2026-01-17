---
description: Execute one backlog item — refactor or consolidate tests per assessment.
---

## User Input

```text
$ARGUMENTS
```

**Required**: Backlog ID (e.g., `R001`)

If no ID provided, show the next high-priority item from `refactor-temp/backlog.md`.

---

## Intent

Phase 4 of the Test Remediation workflow. Executes one backlog item:
- Fixes invalid tests
- Consolidates tests where appropriate
- Verifies refactored tests pass

**One item per invocation. No batch execution.**

---

## Prerequisites

1. `refactor-temp/backlog.md` must exist with the specified item
2. Corresponding assessment in `refactor-temp/assessments/cluster-*.md`
3. Read `refactor-temp/standards.md` for test writing standards

---

## Execution Steps

### Step 1: Load Backlog Item

1. Read `refactor-temp/backlog.md`
2. Find the specified item (e.g., R001)
3. Load the corresponding assessment file

### Step 2: Understand the Decision

| Decision | Action |
|----------|--------|
| **CONSOLIDATE** | Merge valid tests into one |
| **FIX + CONSOLIDATE** | Fix invalid tests, then merge |
| **FIX INDIVIDUALLY** | Fix each invalid test in place |
| **REWRITE** | Delete invalid tests, write new valid test |
| **NO ACTION** | Mark complete, no changes needed |

### Step 3: Execute Based on Decision

#### For CONSOLIDATE or FIX + CONSOLIDATE:

1. **Read all original tests** in the cluster
2. **Identify shared setup** (Arrange section)
3. **Identify the Act** (should be identical across tests)
4. **Collect all assertions** with labels

5. **Write consolidated test**:
```csharp
/// <summary>
/// Consolidated from: T001, T002, T003
/// Covers: [list test cases covered]
/// </summary>
[TestMethod]
public async Task Execute_ValidFile_CompletesSuccessfully()
{
    // Arrange — shared setup from original tests
    var serverFiles = new[] { new FileInfoEntry("file.txt", 1000, DateTime.Now) };
    // ... setup code ...

    // Act — single execution
    var result = await command.Execute(CreateContext());

    // Assert T001: Success message displayed
    _console.Output.Should().Contain("Downloaded");

    // Assert T002: Progress shown for large files
    _console.Output.Should().Contain("%");

    // Assert T003: Returns success code
    result.Should().Be(0);
}
```

6. **Delete original tests** (or comment out first for safety)

#### For FIX INDIVIDUALLY:

1. **Read the invalid test**
2. **Identify the invalid pattern**
3. **Rewrite to test behavior**:

```csharp
// BEFORE (invalid — tests constant):
[TestMethod]
public void ProgressThreshold_IsConfigured()
{
    DownloadConstants.ProgressDisplayThreshold.Should().Be(25_000_000);
}

// AFTER (valid — tests behavior):
[TestMethod]
public async Task Execute_LargeFile_DisplaysProgress()
{
    // Arrange — file larger than threshold
    var largeFile = new FileInfoEntry("big.zip", 30_000_000, DateTime.Now);
    // ... setup ...

    // Act
    await command.Execute(context);

    // Assert — progress was displayed
    _console.Output.Should().Contain("%");
}
```

#### For REWRITE:

1. **Delete all invalid tests in cluster**
2. **Write single new valid test** based on assessment recommendations
3. **Follow all testing standards**

### Step 4: Run Tests

Use the `runTests` tool to verify:

```
runTests with files: ["path/to/TestFile.cs"] and testNames: ["NewTestMethodName"]
```

**Test must pass.**

### Step 5: Verify Breakage Detection

Ask yourself:
> "If someone broke the behavior these tests cover, would the refactored test fail?"

If uncertain, consider:
- Temporarily breaking the implementation
- Verifying the test fails
- Restoring the implementation

### Step 6: Update Backlog

Mark the item complete in `refactor-temp/backlog.md`:

```markdown
## ~~R001: Cluster C001 — DownloadCommand Success Path~~ ✅ COMPLETE

**Completed**: 2026-01-15  
**Result**: Consolidated 3 tests → 1 test

### Changes Made
- Created: `Execute_ValidFile_CompletesSuccessfully`
- Deleted: T001, T002, T003
- Fixed: T002 was testing constant, now tests behavior

### Verification
- Test passes: ✅
- Breakage detection verified: ✅
```

---

## Output Format

Report completion:

```
Backlog Item R001 Complete

Decision executed: FIX + CONSOLIDATE

Changes:
- Created: Execute_ValidFile_CompletesSuccessfully
- Deleted: 3 tests (T001, T002, T003)
- Fixed: T002 invalid pattern (was testing constant)

Verification:
- Test passes: ✅
- Breakage detection: ✅

Files modified:
- BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/DownloadCommandTests.cs

Backlog updated: R001 marked complete

Next: Run /test-remediation.refactor R002 for next item
```

---

## Error Handling

### Test Fails After Refactoring

```
ERROR: Refactored test fails

Test: Execute_ValidFile_CompletesSuccessfully
Result: Failed

This may indicate:
1. Consolidation introduced a bug
2. Original tests had conflicting assertions
3. Test setup is incomplete

Action: Debug the test, do not mark complete until passing.
```

### Conflicting Assertions

```
WARNING: Tests have conflicting assertions

T001 expects: result == 0
T002 expects: result == 1

Cannot consolidate tests with conflicting expectations.
Action: Keep separate or investigate which expectation is correct.
```

---

## Constraints

- **One backlog item per invocation**
- **Test must pass** before marking complete
- **Do not delete tests** until consolidated test is verified
- **Preserve test coverage** — consolidated test must cover all original assertions
- **Follow naming conventions** — `MethodUnderTest_Scenario_ExpectedBehavior`

---

## Rollback

If refactoring goes wrong:

1. Tests are in git — revert changes if needed
2. Comment out new test, restore originals
3. Re-assess the cluster with lessons learned
