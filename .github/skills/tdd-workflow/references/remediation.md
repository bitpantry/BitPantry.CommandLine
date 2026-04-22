# Test Regression Remediation Workflow

Systematically remediate failing tests while preserving test integrity. Fix root causes, not symptoms.

## Operating Constraints

- **Default assumption**: When a test fails, the **code is wrong**, not the test.
- **Test integrity is non-negotiable**: Do not weaken, remove, or alter test assertions without explicit user approval.
- **Scope**: Fix failures, not features. No new functionality, no unrelated refactoring.

## Phase 1: Baseline the Test Suite

Run all tests. Record:

```
📊 BASELINE RESULTS
Total Tests:    XXX
Passed:         XXX
Failed:         XXX
Skipped:        XXX
```

Build a failure backlog: for each failure, capture fully qualified name, file path, failure type (Assertion/Exception/Timeout/Setup), and error message. ALL failing tests must be added — no deferrals. Order: Setup → Assertion → Exception → Timeout. Group related tests together.

## Phase 2: Iterative Remediation

Complete one test fully before moving to the next.

### 2.1 Select Next Test

Pick the next `PENDING` test. Mark `IN_PROGRESS`.

### 2.2 Understand Test Intent

**Before any modification**, document what the test specifies:

```
📖 TEST INTENT
Test Case ID:   UX-001 (or "None — inferred from name/structure")
When:           [Condition being tested]
Then:           [Expected outcome]
Validates:      [What production behavior this protects]
```

Sources: `// Implements:` comments, test name parsing (`Method_Scenario_Expected`), Arrange/Act/Assert analysis, related tests.

### 2.3 Diagnose Root Cause

Run the test in isolation. Trace from the failure to the defect. State hypothesis before proceeding:

> "[test/code] fails because [specific condition] due to [root cause]"

### 2.4 Determine Proper Fix

| Diagnosis | Fix Approach |
|-----------|--------------|
| Code doesn't match test expectation | Fix the **code** |
| Test setup is broken | Fix **setup**, preserve assertions |
| Test has race condition | Fix **mechanics** (timing, async), preserve intent |
| Test expectation outdated (spec changed) | **STOP** — require user approval |
| Test is fundamentally flawed | **STOP** — require user approval |

#### Prohibited Fixes — Suppressing Parallelism

The following are **never acceptable** fixes for flaky tests:

- Adding `[DoNotParallelize]` to a test class or method
- Reducing `Parallelize(Scope = ...)` to `ClassLevel` or removing it
- Adding `lock` statements around shared state in tests
- Any other mechanism that serializes test execution to mask a concurrency defect

**Why**: If a test is flaky under parallel execution, the root cause is shared mutable state — typically `static` fields, singletons, or global side effects. Suppressing parallelism hides the defect rather than fixing it. The same shared-state problem exists in production code when multiple callers invoke the same paths concurrently. Tests that only pass when serialized are revealing a real design flaw.

**What to do instead**: Identify the shared mutable state causing the race condition. Common root causes and their proper fixes:

| Root Cause | Proper Fix |
|------------|------------|
| Static `bool`/flag fields reset in `[TestInitialize]` | Replace with instance state or per-test isolated instances |
| Shared singleton modified across tests | Use DI to provide per-test instances |
| Global static registry or cache | Refactor to instance-scoped or provide test-specific instances |
| File system or environment variable side effects | Use `IFileSystem` abstractions, temp directories, or per-test isolation |

#### Escalation: Modifying Test Intent

If the fix requires changing WHAT the test asserts (not just HOW):

```
⚠️ DECISION REQUIRED: Test Intent Modification

Test:     TestClass.TestMethod
Current assertion: [what it currently checks]
Proposed change:   [what you want to change]
Reason:   [why you believe the test expectation is wrong]

Options:
1. Fix code to match original expectation (recommended)
2. Modify test to expect new behavior (risk: hiding a bug)
3. Delete test as obsolete (requires confirmation)

Awaiting your decision.
```

**WAIT for user response before proceeding.**

### 2.5 Apply Fix and Verify

Apply the minimal fix. Run the test in isolation — must pass. Then run the full test class/file — no other tests should break. If they do, analyze whether the fix caused regression or they share the same root cause.

```
✅ REMEDIATED: <TestGroup>.<TestName>
   Root cause: [Brief description]
   Fix applied: [What was changed]
   Files modified: [list]
```

Proceed to next test (return to 2.1).

## Phase 3: Suite Stability Verification

Once all backlog tests are remediated, run the full suite 3 times:

```
📊 CONSISTENCY VERIFICATION
Run 1: ✅ / ❌ X failures
Run 2: ✅ / ❌ X failures
Run 3: ✅ / ❌ X failures
```

- **New failures**: Add to backlog, return to Phase 2, restart 3-run cycle after remediation.
- **Flaky tests** (pass some runs, fail others): Remediate immediately — these indicate timing or state leaks. Do not defer.
- **Exit criteria**: 3 consecutive full suite runs with 0 failures.

## Phase 4: Completion Report

```
✅ TEST REMEDIATION COMPLETE

📊 SUMMARY
   Original failures:     XX
   Tests remediated:      XX
   Code fixes:            XX
   Test setup fixes:      XX

📁 FILES MODIFIED
   Production code:
   - path/to/file (fix description)
   Test code:
   - path/to/test_file (fix description)

🔍 ROOT CAUSES
   - [Category]: X occurrences — [Description]

📈 VERIFICATION
   - All tests pass in isolation and full suite
   - 3 consecutive clean runs achieved
   - No flaky tests remaining
```
