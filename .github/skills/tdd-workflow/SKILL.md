---
name: tdd-workflow
description: "TDD workflow for bug fixes, feature implementation, and test regression remediation. Use when: fixing bugs, implementing features with tests, remediating failing tests, red-green TDD cycle, writing failing tests first, test-driven development, reproducing bugs with tests."
---

# TDD Workflow

Structured red-green TDD workflows for three use cases: bug fixes, feature implementation, and test regression remediation. All workflows enforce the testing principles from the `tdd-testing` instructions file.

## When to Use

- **Bug fix**: User reports a bug → reproduce with failing test → fix → verify
- **Feature implementation**: New behavior needed → write failing test → implement → verify
- **Regression remediation**: Tests are failing → understand intent → fix code (not tests) → stabilize

## Pre-Implementation Baseline

Before writing any code, establish a known-good baseline for the codebase:

1. **Run the full test suite** using the project's test command (see `copilot-instructions.md`)
2. **Record the results**: Note the total number of passing tests across all test projects and the **total elapsed time** for each test project.
3. **If all tests pass**: The baseline is clean. Any test that fails after your implementation is either a gap in your implementation or a regression you introduced — both are your responsibility to fix before committing.
4. **If some tests already fail**: Record which tests fail and why. These are pre-existing failures and are NOT your responsibility. However, every other test that was passing must still pass after your changes.

**This baseline check is mandatory.** Do not skip it. At the end of your work, run the full test suite again and compare against your baseline:
- If any previously-passing test now fails, fix the regression before committing.
- If the total test duration for any test project increased by more than 5%, flag it in a comment in your PR description (e.g., "⚠️ Test duration increased from 28s → 30s"). New tests are expected to add some time, but increases may indicate inefficient test setup, missing parallelization, or accidentally spinning up expensive infrastructure per-test instead of sharing fixtures.

## Core TDD Loop (Shared)

All three workflows follow the same fundamental cycle:

1. **RED** — Write a test that fails, proving the behavior is missing or broken
2. **GREEN** — Write minimal code to make the test pass
3. **VERIFY** — Run broader test suite to ensure no regressions

### RED Phase Rules

- The test MUST fail before implementation. If it passes, it's invalid — it can't detect the missing behavior.
- Apply the Mandatory Validation Checkpoint (defined in the `tdd-testing` instructions) before writing the test.
- Follow Arrange/Act/Assert structure.
- Reject tests that only compare UI element coordinates or spacing to enforce stylistic consistency unless the geometry directly affects discoverability or interaction.

### GREEN Phase Rules

- Implement the **minimum code** to make the test pass.
- Do NOT add extra functionality, refactor unrelated code, or make "while I'm here" improvements.
- If the test still fails, debug the **implementation** — do NOT modify the test. The test defines correct behavior.

### Post-GREEN Validation

Apply the Verification Question from the `tdd-testing` instructions. If the answer is NO, the test is invalid.

## Test Consolidation

Before writing a standalone test, check if it can be consolidated with a nearby test that shares identical setup.

**Consolidate when:**
- Same Arrange, Same Act, different Assert → one test with labeled assertions
- Use comments to label which test case each assertion covers

**Keep separate when:**
- Different Act (different code paths)
- Conflicting preconditions
- Different test infrastructure or harnesses
- Consolidation would exceed 50 lines of assertions

**Labeled assertion example:**
```
// Case A: Summary shows partial success
assert result contains "2 of 3 items processed"

// Case B: Operation continues after individual failure
assert service.process was called 3 times
```

## Workflow Selection

| Situation | Workflow | Reference |
|-----------|----------|-----------|
| User reports a bug to fix | Bug Fix | [references/bugfix.md](./references/bugfix.md) |
| Implementing new behavior / feature | Feature Implementation | [references/feature.md](./references/feature.md) |
| Existing tests are failing / regressions | Regression Remediation | [references/remediation.md](./references/remediation.md) |

Load the appropriate reference file for the detailed procedure.
