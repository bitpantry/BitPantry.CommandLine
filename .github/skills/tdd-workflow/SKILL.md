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

## Core TDD Loop (Shared)

All three workflows follow the same fundamental cycle:

1. **RED** — Write a test that fails, proving the behavior is missing or broken
2. **GREEN** — Write minimal code to make the test pass
3. **VERIFY** — Run broader test suite to ensure no regressions

### RED Phase Rules

- The test MUST fail before implementation. If it passes, it's invalid — it can't detect the missing behavior.
- Apply the Mandatory Validation Checkpoint (defined in the `tdd-testing` instructions) before writing the test.
- Follow Arrange/Act/Assert structure.

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
- Different test infrastructure (TestConsole vs VirtualConsole)
- Consolidation would exceed 50 lines of assertions

**Labeled assertion example:**
```csharp
// UX-032: Summary shows partial success
result.Should().Contain("2 of 3 files downloaded");

// UX-033: Batch continues after failure  
mockProxy.Verify(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
```

## Workflow Selection

| Situation | Workflow | Reference |
|-----------|----------|-----------|
| User reports a bug to fix | Bug Fix | [references/bugfix.md](./references/bugfix.md) |
| Implementing new behavior / feature | Feature Implementation | [references/feature.md](./references/feature.md) |
| Existing tests are failing / regressions | Regression Remediation | [references/remediation.md](./references/remediation.md) |

Load the appropriate reference file for the detailed procedure.
