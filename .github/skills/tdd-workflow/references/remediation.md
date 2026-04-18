# Test Regression Remediation Workflow

Systematically remediate failing tests while preserving test integrity. Fix root causes, not symptoms.

## Operating Constraints

- **Default assumption**: When a test fails, the **code is wrong**, not the test.
- **Test integrity is non-negotiable**: Do not weaken, remove, or alter test assertions without explicit user approval.
- **Scope**: Fix failures, not features. No new functionality, no unrelated refactoring.

## Phase 1: Baseline the Test Suite

Run all tests. Record:

```
ðŸ“Š BASELINE RESULTS
Total Tests:    XXX
Passed:         XXX
Failed:         XXX
Skipped:        XXX
```

Build a failure backlog: for each failure, capture fully qualified name, file path, failure type (Assertion/Exception/Timeout/Setup), and error message. ALL failing tests must be added â€” no deferrals. Order: Setup â†’ Assertion â†’ Exception â†’ Timeout. Group related tests together.

## Phase 2: Iterative Remediation

Complete one test fully before moving to the next.

### 2.1 Select Next Test

Pick the next `PENDING` test. Mark `IN_PROGRESS`.

### 2.2 Understand Test Intent

**Before any modification**, document what the test specifies:

```
ðŸ“– TEST INTENT
Test Case ID:   UX-001 (or "None â€” inferred from name/structure")
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
| Test expectation outdated (spec changed) | **STOP** â€” require user approval |
| Test is fundamentally flawed | **STOP** â€” require user approval |

#### Escalation: Modifying Test Intent

If the fix requires changing WHAT the test asserts (not just HOW):

```
âš ï¸ DECISION REQUIRED: Test Intent Modification

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

Apply the minimal fix. Run the test in isolation â€” must pass. Then run the full test class/file â€” no other tests should break. If they do, analyze whether the fix caused regression or they share the same root cause.

```
âœ… REMEDIATED: <TestGroup>.<TestName>
   Root cause: [Brief description]
   Fix applied: [What was changed]
   Files modified: [list]
```

Proceed to next test (return to 2.1).

## Phase 3: Suite Stability Verification

Once all backlog tests are remediated, run the full suite 3 times:

```
ðŸ“Š CONSISTENCY VERIFICATION
Run 1: âœ… / âŒ X failures
Run 2: âœ… / âŒ X failures
Run 3: âœ… / âŒ X failures
```

- **New failures**: Add to backlog, return to Phase 2, restart 3-run cycle after remediation.
- **Flaky tests** (pass some runs, fail others): Remediate immediately â€” these indicate timing or state leaks. Do not defer.
- **Exit criteria**: 3 consecutive full suite runs with 0 failures.

## Phase 4: Completion Report

```
âœ… TEST REMEDIATION COMPLETE

ðŸ“Š SUMMARY
   Original failures:     XX
   Tests remediated:      XX
   Code fixes:            XX
   Test setup fixes:      XX

ðŸ“ FILES MODIFIED
   Production code:
   - path/to/file (fix description)
   Test code:
   - path/to/test_file (fix description)

ðŸ” ROOT CAUSES
   - [Category]: X occurrences â€” [Description]

ðŸ“ˆ VERIFICATION
   - All tests pass in isolation and full suite
   - 3 consecutive clean runs achieved
   - No flaky tests remaining
```
