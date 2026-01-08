---
description: Remediates any remaining failing tests in the solution.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Goal

Any failing or flakey tests are remediated in a structured way to ensure that the entire test suite is passing consistently.

## Operating Constraints

**Constitution Authority**: The project constitution (`.specify/memory/constitution.md`) is **non-negotiable** within this analysis scope. Constitution conflicts are automatically CRITICAL and require adjustment of the spec, plan, or tasks—not dilution, reinterpretation, or silent ignoring of the principle. If a principle itself needs to change, that must occur in a separate, explicit constitution update outside `/speckit.bp.remediatetests`.

## Execution Steps

### 1. Creata a Baseline

Run the entire test suite. Use a testing approach that will allow the agent to recover if tests hang. Any testing approach **must** avoid a scenario where a hanging test blocks this remediation process.

Once the test run is complete, compile a backlog of failing tests (**do not create a new document for this backlog - manage it in session memory**). All failing tests must be added to the backlog. No tests may be deferred and no failing tests may be dismissed.

### 2. Iterative Remediation

Select one test from the backlog for remediation. 

**Understand test intent before remediation:**

Before modifying any test, identify the test's intended behavior:
1. Check if the test references a test case ID (e.g., `// Implements: UX-001, CV-003`)
2. If a test case ID exists, read the corresponding "When X, Then Y" from the feature's `test-cases.md`
3. The test-cases.md definition is the authoritative specification for what the test should verify
4. If no test case ID exists, infer intent from test name, comments, and assertions

**Remediate the selected test:**

Run the selected test in isolation. 

If the test fails, design, plan, and implement a test remediation that is consistent with the spec, plan documents, test-cases.md definitions, and existing design and approaches. **Do not** subvert the intention of the test during the remediation. The test must ultimately evaluate a real happy path use case or edge case for the unit, feature, or integration under test as defined in test-cases.md.

If the test passes in isolation, run the entire test suite once, still focusing on the selected test, to ensure the selected test runs in concert with the full suite.

Once the test passes in isolation and when the entire suite is run, consider the test remediated and start this iterative remediation process over on the next test from the backlog.

### 3. Check for Consistency

Once all tests have been remediated using the iterative remediation process defined above. Run the entire suite 5 times to ensure that all tests pass consistently. Compile a new backlog of all failed tests from any of the five test runs. If the backlog contains any failed tests at the end of the fifth run, remediate those tests using the iterative remediation step defined above.

If the five test runs result in no failed tests, this remediation process can be considered complete.

## Context

$ARGUMENTS
