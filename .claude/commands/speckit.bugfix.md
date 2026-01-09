---
description: Fix a bug using strict TDD - first reproduce with a failing test, then fix, then verify the test passes.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty). The user input describes the bug to fix.

## Goal

Fix bugs using a disciplined Test-Driven Development approach that ensures:
1. The bug is reproducible via an automated test
2. The root cause is understood before code changes
3. The fix is verified by the previously failing test now passing
4. No regressions are introduced

---

## Phase 1: Bug Intake & Reproduction

### 1.1 Understand the Bug Report

Parse the user input to extract:
- **Symptom**: What observable behavior is wrong?
- **Expected behavior**: What should happen instead?
- **Reproduction steps**: How to trigger the bug (if provided)
- **Context**: Any relevant files, commands, error messages, or screenshots

If critical information is missing, ask ONE focused question to clarify before proceeding.

### 1.2 Locate Relevant Code

Search the codebase to identify:
- The component/class most likely responsible for the buggy behavior
- Existing test files for that component
- Related tests that exercise similar functionality

### 1.3 Form a Hypothesis

Before writing the test, articulate what you believe is causing the bug:

1. **State the hypothesis clearly**: "I hypothesize that [component/method] fails when [specific condition] because [suspected reason]"
2. **Identify the testable prediction**: What specific behavior would prove or disprove this hypothesis?
3. **Consider platform-specific factors**: Does the behavior involve path manipulation, encoding, or OS APIs that differ across platforms?

Example:
> "I hypothesize that `PathValidator.ValidatePath('/')` throws UnauthorizedAccessException because `Path.IsPathRooted('/')` returns true on Windows, causing `/` to resolve to the drive root instead of the sandbox root."

### 1.4 Check for Existing Test Coverage

Before writing a new test, search existing test files for similar scenarios:

- If tests exist but don't cover the bug's specific condition, **that gap is the bug's escape route**
- If tests exist and should catch the bug but pass, the bug may be in test setup, not production code
- Document what coverage exists and what's missing

This step prevents duplicate tests and provides context for where the new test belongs.

### 1.5 Write a Failing Test (RED)

**This step is MANDATORY. Do not skip to fixing code.**

Create a new test that:
1. **Names the bug clearly**: Use format `[Method]_[Scenario]_[ExpectedBehavior]` or similar
2. **Reproduces the exact symptom**: The test must fail with the same (or equivalent) error the user reported
3. **Asserts the expected behavior**: When fixed, this assertion will pass
4. **Documents the bug**: Add a comment describing the bug scenario being tested

Example test structure:
```csharp
[Fact]
public async Task UploadFile_WhenServerReturns413_ShowsFileTooLargeError()
{
    // Bug: When server rejects file as too large, user sees cryptic error instead of helpful message
    
    // Arrange - setup conditions that trigger the bug
    
    // Act - execute the code path
    
    // Assert - verify the EXPECTED behavior (this will fail until fixed)
}
```

#### ✅ Test Quality Requirements

The bug reproduction test MUST verify actual behavior, not implementation artifacts.

**Test the Expected Outcome Directly**:
- Define what the correct behavior should be
- Your test assertion MUST verify that specific outcome
- The test must execute real code paths, not just check constants or metadata

**Invalid Test Patterns** (NEVER do these for bug reproduction):
- ❌ Testing constants: `MaxFileSize.Should().Be(100MB)` - proves nothing about behavior
- ❌ Testing input strings: `errorMessage.Contains("413").Should().BeTrue()` - tests the check, not the fix
- ❌ Testing types exist: `typeof(FileSizeException).Should().NotBeNull()` - compiler guarantees this
- ❌ Tautologies: `result.Should().Be(result)` - always passes, proves nothing

**Valid Test Patterns**:
- ✅ Execute the buggy code path and verify the corrected observable outcome
- ✅ Mock dependencies to trigger the error condition, verify the handling
- ✅ Create real test fixtures (files, data) that trigger the bug
- ✅ Capture side effects (console output, exceptions, state changes)

**Verification Question**: Before proceeding past Phase 1, ask:
> "If someone reintroduced this bug, would this test fail?"
> If the answer is "no", the test is invalid and must be rewritten.

### 1.6 Run the Test and Confirm Failure

Execute ONLY the new test to confirm:
- ✅ The test fails (if it passes, the bug is not properly reproduced)
- ✅ The failure matches the reported symptom
- ✅ The test isolates the specific bug (not a general integration failure)

**If the test passes**: The reproduction is wrong. Re-analyze the bug and rewrite the test.

**Output at end of Phase 1**:
```
🔴 BUG REPRODUCED
Test: [TestName]
File: [TestFilePath]
Failure: [Actual error/assertion failure message]
```

---

## Phase 2: Root Cause Analysis

### 2.1 Trace the Code Path

Starting from the failing test:
1. Follow the execution path through the code
2. Identify WHERE the behavior diverges from expected
3. Understand WHY the current code produces the wrong result

### 2.2 Document Root Cause

Before writing any fix, articulate:
- **What**: The specific code defect (missing handling, wrong condition, etc.)
- **Where**: File and line number(s)
- **Why**: How this defect causes the observed symptom

**Output at end of Phase 2**:
```
🔍 ROOT CAUSE IDENTIFIED
What: [Description of the defect]
Where: [File:LineNumber]
Why: [How this causes the symptom]
```

---

## Phase 3: Implement Fix (GREEN)

### 3.1 Make the Minimal Change

Fix the code with the smallest change that:
- Addresses the root cause
- Makes the failing test pass
- Does not break existing functionality

**Principles**:
- Prefer targeted fixes over refactoring (refactor separately if needed)
- Add error handling, not remove assertions
- Preserve existing behavior for non-buggy paths

### 3.2 Run the Bug Test

Execute the test from Phase 1:
- ✅ The test should now pass
- ❌ If still failing, the fix is incomplete—iterate

### 3.3 Run Related Tests

Run all tests in the same test file/class to check for regressions:
- ✅ All tests pass → proceed to Phase 4
- ❌ Other tests fail → analyze if the fix broke something or revealed a latent issue

---

## Phase 4: Verification & Cleanup

### 4.1 Run Full Test Suite (if feasible)

If the test suite is fast enough, run all tests to ensure no regressions.

For large suites, run at minimum:
- All tests in the affected component's test file
- Any integration tests that exercise the fixed code path

### 4.2 Manual Verification (if applicable)

If the user provided manual reproduction steps, verify the fix manually:
- Execute the same steps that triggered the bug
- Confirm the expected behavior now occurs

### 4.3 Final Report

**Output at end of Phase 4**:
```
✅ BUG FIXED

Symptom: [Original bug description]
Root Cause: [What was wrong]
Fix: [What was changed]

Test Added: [TestFilePath#TestName]
Files Modified: [List of changed files]

Verification:
- [x] New test passes
- [x] Related tests pass
- [x] No regressions detected
```

---

## Protocols

### 🚫 No Fix Without Failing Test

**NEVER** modify production code until a failing test exists that reproduces the bug. If you cannot write a test for the bug:

1. **STOP** and explain why the bug is untestable
2. **PROPOSE** refactoring to make it testable, OR
3. **PROPOSE** an integration test approach, OR
4. **ASK** if manual verification is acceptable (last resort)

### 🧪 Test Integrity Protocol

**Tests are specifications, not just verification.** A bug reproduction test encodes the correct behavior that was missing.

**Default Assumption**: When writing the fix, if existing tests fail, the **fix may be incomplete or wrong**—not the existing tests.

**Before modifying ANY existing test assertion**:

1. **ARTICULATE** the test's original intent:
   - What behavior was this test specifying?
   - What is the "When X, Then Y" it's verifying?
   - Why were these specific assertions chosen?

2. **DIAGNOSE** the conflict:
   - Is the fix incomplete? → **Extend the fix**
   - Is the existing test outdated due to this bug being a spec change? → **Confirm with user**
   - Is the test technically flawed (wrong setup, race condition)? → **Fix test mechanics, preserve intent**

3. **NEVER do the following without explicit user approval**:
   - Weaken assertions (e.g., `Should().Be("File too large")` → `Should().NotBeNull()`)
   - Remove assertions that are failing
   - Change expected values to match current (still buggy) behavior
   - Generalize specific checks

**Legitimate test modifications** (no approval needed):
- Fixing test setup/teardown mechanics
- Updating imports or references after the fix
- Adjusting timing/async handling while preserving assertions
- Adding MORE specific assertions (strengthening, not weakening)

### ⚠️ Decision Point Protocol

If during analysis you discover:
- The "bug" is actually intended behavior
- The fix would require breaking changes
- Multiple valid approaches exist with different tradeoffs
- An existing test conflicts with the expected fix

**STOP** and present the decision point with your **TOP RECOMMENDATION** before proceeding.

### 🔄 Scope Creep Prevention

The bug fix scope is LIMITED to:
- Writing the reproduction test
- Fixing the specific defect
- Adding any directly related error handling

Do NOT during a bugfix:
- Refactor unrelated code
- Add features beyond the fix
- "While I'm here" improvements

If you notice other issues, note them for separate work but do not address them.

---

## Quick Reference

| Phase | Goal | Output |
|-------|------|--------|
| 1. Reproduce | Write failing test | 🔴 Test fails with expected symptom |
| 2. Analyze | Find root cause | 🔍 Defect location and explanation |
| 3. Fix | Minimal code change | 🟢 Test passes |
| 4. Verify | Confirm no regressions | ✅ All tests pass |
