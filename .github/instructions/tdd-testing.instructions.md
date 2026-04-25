---
description: "Use when: writing tests, fixing bugs with TDD, implementing features with tests, remediating test failures, reviewing test quality, red-green TDD cycle, test validation, test integrity."
applyTo: "**/*Tests*/**/*.cs"
---

# TDD Testing Principles

Non-negotiable rules for all test writing, modification, and remediation.

## Core Axiom

**Tests are specifications that encode business intent.** When a test fails, the **default assumption is the code is wrong**, not the test.

## Mandatory Validation Checkpoint

**Before writing ANY test, output this:**

```
Test Validity Check:
  Invokes code under test: [YES/NO]
  Breakage detection: [YES/NO]
  Not a tautology: [YES/NO]
```

**If any answer is NO, STOP. Redesign the test.**

## Verification Question

Before any test is considered done:

> "If someone broke the behavior this test specifies, would the test fail?"

If NO — the test is invalid. Rewrite it.

## UI Test Worthiness

For UI tests, the required behavior must be stated in user-facing terms.

**Good candidates for tests:**
- Control availability or absence: button/link exists, hidden, disabled, enabled
- Interaction wiring: tapping, typing, navigation, dialogs, callbacks
- State-driven rendering: empty/loading/error/success/duplicate states
- Accessibility structure: semantics, labels, focusable actions
- Layout contracts that affect usability: primary action remains in a stable slot, action does not disappear into wrapped content, critical content remains visible after mutation

**Poor candidates for routine tests:**
- Exact padding, margin, spacing, or alignment nudges
- Exact font weight, font size, or color values
- Minor cosmetic repositioning that does not affect meaning or interaction
- Assertions that only prove a screen looks a little tighter, looser, higher, or lower
- Coordinate comparisons between sibling controls solely to enforce visual consistency

Use this rule before adding a UI assertion:

> "Would changing this make a user unable to find, understand, or use the correct control or state?"

If NO, do not add a routine test for it.

## Valid Test Patterns

- Execute and verify outcome: `service.Connect().Should().BeTrue()`
- Mock and verify interaction: `mock.Verify(x => x.Send(msg), Times.Once)`
- Create fixture, verify state: `file.Exists.Should().BeTrue()`
- Capture output, verify content: `console.Output.Should().Contain("Connected")`

## Invalid Test Patterns — NEVER Do These

| Pattern | Example | Why Invalid | Fix |
|---------|---------|-------------|-----|
| Testing constants | `MaxRetries.Should().Be(3)` | Proves nothing about behavior | Test the behavior the constant controls |
| Testing inputs | `input.Contains("*").Should().BeTrue()` | Tests the input, not processing | Test what the code does with that input |
| Testing types exist | `typeof(Service).Should().NotBeNull()` | Compiler guarantees this | Test behavior of the type |
| Tautologies | `x.Should().Be(x)` | Always passes | Test observable outcomes |
| Recreating framework behavior | `new SemaphoreSlim(N)` limits to N | Tests .NET, not your code | Test that YOUR code uses the framework correctly |
| Testing without invoking code | Create mocks, assert on mocks | Never exercises real code | Call actual methods, verify actual outcomes |
| Testing formatting trivia | Assert exact pixel positions or style values | Locks in polish, not user-facing behavior | Test the control/state/layout contract the user depends on |
| Testing sibling alignment trivia | Assert coordinate relationships between controls | Encodes design polish, not user capability | Test that the control exists and triggers the right workflow |

### Transformation Examples

| Invalid Test | Valid Transformation |
|--------------|---------------------|
| `MaxConcurrentDownloads.Should().Be(4)` | Download 10 files via command, verify max 4 concurrent HTTP requests via mock |
| `ProgressThrottleMs.Should().BeLessOrEqualTo(1000)` | Download a large file, capture progress callback timestamps, verify no gap > 1 second |
| Assert a UI element's exact position | Verify the primary action remains in its expected slot and doesn't disappear into overflow |

## Test Integrity Protocol

**Before modifying ANY test assertion:**

1. **ARTICULATE** the test's original intent — what "When X, Then Y" is it verifying?
2. **DIAGNOSE** the failure:
   - Code not implementing intended behavior? → **Fix the code**
   - Test intent outdated due to legitimate spec change? → **Confirm with user**
   - Test technically flawed (wrong setup, race condition)? → **Fix test mechanics, preserve intent**
3. **NEVER do these without explicit user approval:**
   - Weaken assertions (e.g., `Should().Be("exact")` → `Should().NotBeNull()`)
   - Remove failing assertions
   - Change expected values to match current (buggy) behavior
   - Delete tests that are inconvenient to fix

**Legitimate modifications (no approval needed):**
- Fixing test mechanics (setup, teardown, timing, imports) without changing assertions
- Strengthening assertions (adding more specific checks)

**Acceptable no-op test doubles:**
- No-op or fake dependencies are acceptable in render-state tests when the test only verifies UI for a supplied state.

**Not acceptable with no-op test doubles:**
- Tests claiming to verify mutations, persistence, state invalidation, navigation completion, or workflow success when the action method does nothing.

## Test Conventions

- **Naming**: `MethodUnderTest_Scenario_ExpectedBehavior`
- **Structure**: Arrange/Act/Assert
- **Traceability**: `// Implements: UX-003` where applicable
- **Render-state naming**: If a test supplies a prebuilt state and only verifies the rendered output, name it as rendering (e.g., "renders success state") rather than behavior completion (e.g., "creates item successfully").

## Existing Tests Are Not Pre-Validated

"It's already in the codebase" is not evidence of correctness. Always apply the Mandatory Validation Checkpoint when following an existing pattern.

If you find an invalid existing test: report `⚠️ EXISTING TEST INVALID: [file]:[test name] — [reason]` and do NOT copy its pattern.
