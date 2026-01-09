```markdown
---
description: Systematically remediate all failing tests in the solution using a disciplined, iterative workflow that ensures test integrity and consistent suite stability.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty). The user input may specify:
- A subset of tests or test projects to focus on
- Known context about recent changes that may have caused failures
- Specific tests to prioritize or skip

## Goal

Remediate all failing tests in a structured, methodical way that:
1. Preserves test integrity (tests verify real behavior, not implementation artifacts)
2. Fixes root causes rather than masking symptoms
3. Ensures the entire suite passes consistently across multiple runs
4. Maintains traceability between tests, test cases, and specifications

---

## Operating Constraints

### 🔒 Constitution Authority

The project constitution (`.specify/memory/constitution.md`) is **non-negotiable**. Constitution conflicts require adjustment of code or tests—not dilution of principles. If a principle needs to change, that occurs separately outside this workflow.

### 🧪 Test Integrity Protocol

**Tests are specifications, not just verification.** A test encodes business intent and expected behavior.

**Default Assumption**: When a test fails, the **code is wrong**, not the test.

**Before modifying ANY test assertion**:

1. **ARTICULATE** the test's original intent:
   - What behavior was this test specifying?
   - What test case ID(s) does it implement (e.g., `// Implements: UX-001, CV-003`)?
   - What is the "When X, Then Y" it's verifying?

2. **DIAGNOSE** the failure:
   - Is the code not implementing the intended behavior? → **Fix the code**
   - Is the test's intent outdated due to a legitimate spec change? → **Confirm with user**
   - Is the test technically flawed (wrong setup, race condition)? → **Fix test mechanics, preserve intent**

3. **NEVER do the following without explicit user approval**:
   - Weaken assertions (e.g., `Should().Be("exact")` → `Should().NotBeNull()`)
   - Remove failing assertions
   - Change expected values to match current (buggy) behavior
   - Generalize specific checks
   - Delete tests that are inconvenient to fix

**Legitimate test modifications** (no approval needed):
- Fixing test setup/teardown mechanics
- Updating imports or references after refactoring
- Adjusting timing/async handling while preserving assertions
- Adding MORE specific assertions (strengthening, not weakening)

### 🚫 Scope Boundaries

**In scope:**
- Fixing test setup/teardown issues
- Fixing production code to match test expectations
- Fixing race conditions, timing issues, resource cleanup
- Updating tests to reflect intentional, user-approved spec changes
- Fixing flaky test infrastructure

**Out of scope (defer to separate work):**
- Feature additions or enhancements
- Refactoring unrelated code
- Performance optimizations beyond fixing timeouts
- New test coverage (unless required to understand existing test)

---

## Phase 1: Baseline the Test Suite

### 1.1 Run Full Test Suite

Execute the entire test suite with timeout protection to establish a baseline:

```powershell
# Run with results output for parsing
dotnet test --no-build --logger "trx;LogFileName=baseline.trx" --blame-hang-timeout 60s
```

**Timeout Protection**: Use `--blame-hang-timeout` to prevent hanging tests from blocking remediation. If a test hangs:
- Record it as a failure with reason "TIMEOUT"
- Continue with remaining tests

### 1.2 Capture Baseline Results

Parse test results and create a structured failure inventory:

```
📊 BASELINE RESULTS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total Tests:    XXX
Passed:         XXX
Failed:         XXX
Skipped:        XXX
Duration:       XX:XX
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 1.3 Build Failure Backlog

Create an in-memory backlog of all failing tests. For each failure, capture:

| Field | Description |
|-------|-------------|
| **Test ID** | Fully qualified test name |
| **Test File** | Path to test file |
| **Failure Type** | Assertion, Exception, Timeout, Setup |
| **Error Message** | First 200 chars of error |
| **Stack Trace** | Top 3 frames |
| **Status** | `PENDING` / `IN_PROGRESS` / `REMEDIATED` / `BLOCKED` |

**Backlog Rules**:
- ALL failing tests must be added—no deferrals, no dismissals
- Order by failure type: Setup failures first, then Assertion, then Exception, then Timeout
- Group related tests (same class/fixture) together

Output the initial backlog:

```
📋 FAILURE BACKLOG (X tests)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#1 [Setup]     TestClass.TestMethod - "Object reference not set..."
#2 [Assertion] OtherClass.OtherTest - "Expected 'foo' but got 'bar'"
#3 [Timeout]   SlowTest.HangingTest - "Test exceeded 60s timeout"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Phase 2: Iterative Remediation

For each test in the backlog, execute the following sub-phases. Complete one test fully before moving to the next.

### 2.1 Select Next Test

Pick the next `PENDING` test from the backlog. Update status to `IN_PROGRESS`.

```
🔧 REMEDIATING: TestNamespace.TestClass.TestMethod
   File: path/to/TestFile.cs
   Type: [Assertion/Exception/Timeout/Setup]
```

### 2.2 Understand Test Intent

**Before any modification**, establish what the test is supposed to verify:

#### A. Check for Test Case Reference

Search the test for test case ID references:
```csharp
// Implements: UX-001, CV-003
// Test case: UX-001
/// <summary>Implements: CV-003</summary>
```

If found, locate the corresponding test-cases.md and extract the "When X, Then Y" definition.

#### B. Analyze Test Structure

Read the test and identify:
- **Arrange**: What preconditions are set up?
- **Act**: What action is being tested?
- **Assert**: What outcome is expected?

#### C. Infer Intent from Context

If no explicit test case reference:
- Parse test name: `MethodName_Scenario_ExpectedBehavior`
- Read XML doc comments
- Check for related tests in same class
- Review git history for original commit context (if helpful)

#### D. Document Understanding

Before proceeding, articulate:

```
📖 TEST INTENT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Test Case ID:   UX-001 (or "None - inferred from structure")
When:           [Condition being tested]
Then:           [Expected outcome]
Validates:      [What production behavior this protects]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 2.3 Diagnose Root Cause

Run the test in isolation:

```powershell
dotnet test --no-build --filter "FullyQualifiedName=Namespace.Class.Method"
```

Analyze the failure:

#### A. Classify Failure Type

| Type | Symptoms | Typical Cause |
|------|----------|---------------|
| **Assertion** | `Expected X but got Y` | Code behavior changed, or test expectation wrong |
| **Exception** | `NullReferenceException`, etc. | Missing setup, changed dependencies, code bug |
| **Timeout** | Test hangs, killed by timeout | Deadlock, infinite loop, missing async handling |
| **Setup** | Failure in `[TestInitialize]` | Shared fixture issues, missing resources |

#### B. Trace Execution Path

For assertion failures:
1. Set breakpoints (conceptually) at the assertion
2. Trace backwards: What value was actually produced?
3. Trace the code path that produced that value
4. Identify WHERE behavior diverges from expectation

For exceptions:
1. Read the full stack trace
2. Identify the throwing line
3. Trace what precondition was violated

#### C. Form Hypothesis

State clearly:
> "I hypothesize that [test/code] fails because [specific condition] due to [root cause]"

### 2.4 Determine Proper Fix

Based on diagnosis, categorize the fix:

| Diagnosis | Fix Approach |
|-----------|--------------|
| Code doesn't match test expectation | Fix the **code** to produce expected behavior |
| Test setup is broken | Fix test **setup** while preserving assertions |
| Test has race condition | Fix test **mechanics** (timing, async) preserving intent |
| Test expectation is outdated (spec changed) | **STOP** - require user approval before modifying assertion |
| Test is fundamentally flawed | **STOP** - require user approval before deletion/rewrite |

#### Decision Point: Modifying Test Intent

If the fix requires changing WHAT the test asserts (not just HOW):

```
⚠️ DECISION REQUIRED: Test Intent Modification
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Test:     TestClass.TestMethod
Current assertion: Expected "File too large" message
Proposed change:   Remove this assertion

Reason: [Explain why the test's expectation appears wrong]

Options:
1. Modify test to expect new behavior (risk: hiding a bug)
2. Fix code to match original expectation (recommended)
3. Delete test as obsolete (requires confirmation)

TOP RECOMMENDATION: [Your recommendation with rationale]

Proceed? (yes/no)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**WAIT for user response before proceeding.**

### 2.5 Apply Fix

Implement the fix with minimal changes:

1. **For code fixes**: Change only what's necessary to make the test pass
2. **For test setup fixes**: Preserve all assertions unchanged
3. **For timing fixes**: Add proper async handling without changing expectations

### 2.6 Verify in Isolation

Run the fixed test alone:

```powershell
dotnet test --no-build --filter "FullyQualifiedName=Namespace.Class.Method"
```

- ✅ **Passes**: Proceed to suite verification
- ❌ **Fails**: Return to step 2.3 with new information

### 2.7 Verify in Suite Context

Run the full test class/fixture:

```powershell
dotnet test --no-build --filter "FullyQualifiedName~Namespace.TestClass"
```

Check for:
- The fixed test still passes
- No other tests in the class broke
- No shared state contamination

If other tests broke:
- Analyze if the fix caused regression → refine the fix
- Or if other tests have the same root cause → batch the fix

### 2.8 Mark Remediated

Update backlog status to `REMEDIATED`:

```
✅ REMEDIATED: TestNamespace.TestClass.TestMethod
   Root cause: [Brief description]
   Fix applied: [What was changed]
   Files modified: [List]
```

Proceed to next test in backlog (return to 2.1).

---

## Phase 3: Suite Consistency Verification

Once all tests in the backlog are `REMEDIATED`:

### 3.1 Run Full Suite (Iteration 1 of 5)

```powershell
dotnet test --no-build --blame-hang-timeout 60s
```

Record results:
```
🔄 CONSISTENCY RUN 1/5
   Passed: XXX
   Failed: XXX
   New failures: [List any tests that weren't in original backlog]
```

### 3.2 Handle New Failures

If new failures appear that weren't in original backlog:
- Add them to backlog with status `PENDING`
- These may be regressions from fixes or previously masked failures
- Return to Phase 2 to remediate new failures

### 3.3 Repeat Consistency Runs

Run the full suite 5 times total. Track results:

```
📊 CONSISTENCY VERIFICATION
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Run 1: ✅ All passed / ❌ X failures
Run 2: ✅ All passed / ❌ X failures  
Run 3: ✅ All passed / ❌ X failures
Run 4: ✅ All passed / ❌ X failures
Run 5: ✅ All passed / ❌ X failures
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 3.4 Identify Flaky Tests

If a test passes some runs but fails others:
- Mark as `FLAKY` in backlog
- Document which runs it failed
- Prioritize flaky test remediation (often timing/race conditions)

### 3.5 Iterate Until Stable

**Exit criteria**: 5 consecutive full suite runs with 0 failures.

If any run has failures:
1. Add new failures to backlog
2. Return to Phase 2
3. After remediating, restart Phase 3 (5 clean runs required)

---

## Phase 4: Completion Report

Once 5 consecutive clean runs achieved:

```
✅ TEST REMEDIATION COMPLETE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 SUMMARY
   Original failures:     XX
   Tests remediated:      XX
   Code fixes:            XX
   Test fixes:            XX
   Iterations required:   X

📁 FILES MODIFIED
   Production code:
   - path/to/File1.cs (fix description)
   - path/to/File2.cs (fix description)
   
   Test code:
   - path/to/TestFile1.cs (fix description)
   - path/to/TestFile2.cs (fix description)

🔍 ROOT CAUSES IDENTIFIED
   - [Category]: X occurrences - [Description]
   - [Category]: X occurrences - [Description]

📈 VERIFICATION
   - [x] All tests pass in isolation
   - [x] All tests pass in full suite
   - [x] 5 consecutive clean runs achieved
   - [x] No flaky tests remaining

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Quick Reference

| Phase | Goal | Exit Criteria |
|-------|------|---------------|
| 1. Baseline | Identify all failures | Backlog created with all failures |
| 2. Remediate | Fix each test | All backlog items `REMEDIATED` |
| 3. Verify | Ensure stability | 5 consecutive clean runs |
| 4. Report | Document work | Summary generated |

---

## Testing Infrastructure Reference

### Available Test Levels

| Level | Infrastructure | Use For |
|-------|----------------|---------|
| Unit/Component | Moq, MockFileSystem | Isolated class behavior, mocked dependencies |
| Integration | `TestEnvironment`, `TestServer` | Client-server flows, RPC, file transfer |
| UX/Functional | `VirtualConsole`, `VirtualConsoleAssertions` | Console output, prompts, progress display |

### TestEnvironment (Integration Tests)

```csharp
using var env = new TestEnvironment(opts => {
    opts.RequireAuthentication = true;
    opts.StorageRootPath = tempDir;
});
await env.Cli.Run("server connect -u http://test/cli");
await env.Cli.Run("server upload file.txt");
env.Console.Should().ContainText("Uploaded");
```

### VirtualConsole (UX Tests)

```csharp
console.Should().ContainText("Upload complete");
console.Should().NotContainText("Error");
console.Should().HaveLineContaining(row: 5, "Progress:");
```

### Common Fix Patterns

| Symptom | Common Cause | Fix Pattern |
|---------|--------------|-------------|
| `ObjectDisposedException` | Resource disposed before async completes | Ensure `using` scope encompasses await |
| `NullReferenceException` in setup | Missing mock/dependency | Add missing mock setup |
| Assertion fails with different value | Code behavior changed | Fix code OR update test (with approval) |
| Test hangs | Missing async/await, deadlock | Add proper async handling |
| Flaky pass/fail | Race condition, shared state | Add synchronization, isolate state |
| `FileNotFoundException` | Temp file cleanup timing | Use unique paths per test |

```
