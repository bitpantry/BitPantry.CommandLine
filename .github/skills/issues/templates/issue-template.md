<!--
  STAGED ISSUE — not yet published to GitHub.
  Use /publish-issues to create this issue on GitHub.
  
  Staging Number: [NNN]
  GitHub Issue Number: [filled by /publish-issues]
-->

# [Title]

**Labels**: enhancement, spec-[SPEC_NNN]
**Blocked by**: [list of staging numbers, e.g., 001, 003 — or "None"]
**Implements**: [FR-IDs, e.g., FR-001, FR-002]
**Covers**: [US-IDs, e.g., US-001]

## Summary

[1-2 sentences: what this issue delivers and why it matters]

## Current Behavior

<!-- What exists today — the starting point for this work -->

[Description of current state]

## Expected Behavior

<!-- What should exist after this issue is implemented -->

[Description of desired state]

## Affected Area

- **Project(s):** [which project(s) are involved]
- **Key files:**
  - `[path/to/file]` — [brief role]
- **Spec reference:** See `specs/[NNN]-[short-name]/spec.md`
- **Plan reference:** See `specs/[NNN]-[short-name]/plan.md`

## Requirements

<!-- Testable outcomes that MUST be true when this issue is resolved -->

- [ ] [Requirement 1 — a testable, observable outcome] (FR-001)
- [ ] [Requirement 2] (FR-002)

## Prerequisites

<!--
  Issues that must be completed before this one can start.
  During staging, these reference staging numbers (001, 002).
  /publish-issues replaces them with real GitHub issue numbers (#NN).
-->

- Blocked by: [staging number] — [brief reason]

_Or: No prerequisites — this issue can be started independently._

## Implementation Guidance

<!--
  Recommended approach based on the plan. This is guidance, not a mandate.
  The implementer has autonomy to deviate if they find a better approach.
-->

[Specific guidance: which classes/methods to create or modify, design considerations, edge cases to handle]

## Implementer Autonomy

This issue was authored from a specification and plan — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description.
3. **Do not** silently drop requirements or weaken test coverage.

## Testing Requirements

<!--
  Align with the project's testing instructions and TDD workflow.
  Follow the tdd-workflow skill: write failing tests first (RED), implement (GREEN).
-->

### Test Approach

- **Test level:** [Unit | Integration | Contract — based on what's being tested]
- **Test project:** [the appropriate test project]
- **Existing fixtures to reuse:** [list applicable fixtures from test-infrastructure instructions]

### Prescribed Test Cases

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `MethodUnderTest_Scenario_ExpectedBehavior` | [when this happens] | [then this should result] |

### Discovering Additional Test Cases

The test cases above are a starting point. During implementation, **discover and add additional test cases** as you encounter edge cases or error paths not covered above.

## Additional Context

[Any other information: links to related specs, design decisions, constraints]
