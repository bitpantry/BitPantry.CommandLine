# Bug Fix TDD Workflow

Fix bugs using strict red-green TDD: reproduce with a failing test, then fix, then verify.

## Phase 1: Bug Intake & Reproduction

### 1.1 Understand the Bug Report

You MUST have these before proceeding:
- **Symptom**: What observable behavior is wrong?
- **Expected behavior**: What should happen instead?
- **At least one of**: reproduction steps, error message, or affected file

If any are missing, ask ONE focused question to clarify.

### 1.2 Locate Relevant Code

Search the codebase to identify:
- The component/class most likely responsible
- Existing test files for that component
- Related tests that exercise similar functionality

### 1.3 Form a Hypothesis

Before writing the test, state your hypothesis in this format:

> "I hypothesize that [component/method] fails when [specific condition] because [suspected reason]"

If the bug involves file paths, encoding, or OS APIs: state whether the behavior is platform-dependent.

### 1.4 Check for Existing Test Coverage

Before writing a new test, search existing test files for tests that exercise the same component and condition.

**Triage existing tests against the bug:**

| Finding | Action |
|---------|--------|
| No existing test covers this condition | Proceed to write a new test (step 1.6) |
| Test exists, covers condition, and fails | Good — the bug is already caught. Skip to Phase 2 (root cause) using this test |
| Test exists, should cover condition, but passes | **The test is suspect.** Apply the Mandatory Validation Checkpoint to it (see below) |
| Test exists but covers a different condition | No conflict — proceed to write a new test for the missing condition |

**When an existing test should catch the bug but doesn't:**

This is the most important scenario. An existing test that passes despite the bug being present is either:
- **Invalid** — it doesn't actually invoke the code under test, or it asserts the wrong thing
- **Insufficient** — it covers a related but different condition (e.g., tests the happy path but not the error path this bug hits)
- **Broken setup** — the test's Arrange doesn't create the conditions that trigger the bug

Apply the Mandatory Validation Checkpoint (from `tdd-testing` instructions) to the existing test:
1. Does it invoke the code under test? If NO → the test is invalid
2. If the bug were fixed, would *breaking* the fix cause this test to fail? If NO → the test doesn't actually guard this behavior
3. Is it a tautology? If YES → the test is invalid

**Based on the assessment:**

- **Invalid test** — Report `⚠️ EXISTING TEST INVALID: [file]:[method] — [reason]`. Delete it and write a correct replacement in step 1.6 that both reproduces the bug (RED) and will guard against regression after the fix (GREEN).
- **Insufficient test** — Keep it (it guards other behavior). Write an additional test in step 1.6 for the specific condition the bug exploits.
- **Broken setup** — Fix the test's Arrange/setup so it properly triggers the bug condition. The test should now fail (RED), confirming it was the setup that was wrong. Proceed to Phase 2.

### 1.5 Analyze Test Infrastructure

Perform the Infrastructure Analysis per the `test-infrastructure` instructions. Output the checkpoint before writing any test code.

### 1.6 Write a Failing Test (RED)

**MANDATORY. Do not skip to fixing code.**

Create a test that:
1. Names the bug clearly: `[Method]_[Scenario]_[ExpectedBehavior]`
2. Reproduces the exact symptom
3. Asserts the expected (correct) behavior
4. Documents the bug with a comment

Apply the Mandatory Validation Checkpoint. Run the test and confirm it fails.

**If the test passes**: The reproduction is wrong. Re-analyze and rewrite.

**Output:**
```
🔴 BUG REPRODUCED
Test: [TestName]
File: [TestFilePath]
Failure: [Actual error/assertion failure message]
```

## Phase 2: Root Cause Analysis

Trace from the failing assertion to the defect. Document before writing any fix:

```
🔍 ROOT CAUSE IDENTIFIED
What: [Description of the defect]
Where: [File:LineNumber]
Why: [How this causes the symptom]
```

## Phase 3: Implement Fix (GREEN)

Apply GREEN phase rules from the `tdd-workflow` SKILL.md. Make the smallest change that addresses the root cause and passes the test.

Run the bug test. If still failing, iterate on the **implementation**, NOT the test.

Run all tests in the same test file/class to check for regressions.
- All pass → proceed to Phase 4
- Others fail → analyze if the fix broke something or revealed a latent issue

## Phase 4: Verification

Run the full test suite. For large suites, run at minimum: all tests in the affected component's file and any integration tests exercising the fixed code path.

**Output:**
```
🟢 BUG FIXED
Test: [TestName]
Root Cause: [Brief description]
Fix: [What was changed]
Files Modified: [list]
Regression Check: All tests pass
```
