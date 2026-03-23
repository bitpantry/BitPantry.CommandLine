# Feature Implementation TDD Workflow

Implement new features using strict red-green TDD: write a failing test first, then implement to make it pass.

## Phase 1: Understand the Feature

### 1.1 Gather Context

Identify before writing any code:
- The behavior being added (what, not how)
- The file(s) to modify or create
- The test file where tests will go
- Existing tests for related components

### 1.2 Define the Behavioral Specification

For each behavior, state:

> "When [condition/action], the system should [expected behavior]"

If you cannot complete this statement, STOP and ask the user one specific question to resolve the ambiguity.

### 1.3 Analyze Test Infrastructure

Perform the Infrastructure Analysis per the `test-infrastructure` instructions. Output the checkpoint before writing any test code.

### 1.4 Check for Consolidation Opportunities

Check if this test can be consolidated with a nearby test per the consolidation criteria in the `tdd-workflow` SKILL.md.

## Phase 2: Write Failing Test (RED)

Write the test following the naming, structure, and traceability conventions from `tdd-testing` instructions. Apply the Mandatory Validation Checkpoint.

Run only the new test. Confirm it fails because the behavior doesn't exist yet (not a setup error).

**If the test passes**: STOP. The test is invalid — the behavior either already exists or the test isn't checking the right thing. Rewrite.

**Output:**
```
🔴 TEST FAILS (expected)
Test: [TestName]
File: [TestFilePath]
Failure: [Assertion failure message]
```

## Phase 3: Implement (GREEN)

Apply GREEN phase rules from the `tdd-workflow` SKILL.md. Implement ONLY what this test requires.

If the test still fails, debug the **implementation** — do NOT modify the test.

Apply the Verification Question from `tdd-testing` instructions.

**Output:**
```
🟢 TEST PASSES
Test: [TestName]
Files changed: [list]
```

## Phase 4: Iterate

Repeat Phases 2-3 for each remaining behavior.

## Phase 5: Verification

Run all tests in the same file/class, then the full suite. Report:

```
🟢 FEATURE COMPLETE
Tests added: [count]
Files modified: [list]
Regression check: All tests pass
```

## Implementation-Only Tasks (No Test Required)

Tasks that don't change observable behavior (DI wiring, configuration, structural refactoring):
1. Implement the change
2. Run existing tests to ensure no regressions
3. Report what changed and that tests pass
