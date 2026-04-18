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

If NO â€” the test is invalid. Rewrite it.

## Valid Test Patterns

- Execute and verify outcome: `service.Connect().Should().BeTrue()`
- Mock and verify interaction: `mock.Verify(x => x.Send(msg), Times.Once)`
- Create fixture, verify state: `file.Exists.Should().BeTrue()`
- Capture output, verify content: `console.Output.Should().Contain("Connected")`

## Invalid Test Patterns â€” NEVER Do These

| Pattern | Example | Why Invalid | Fix |
|---------|---------|-------------|-----|
| Testing constants | `MaxRetries.Should().Be(3)` | Proves nothing about behavior | Test the behavior the constant controls |
| Testing inputs | `input.Contains("*").Should().BeTrue()` | Tests the input, not processing | Test what the code does with that input |
| Testing types exist | `typeof(Service).Should().NotBeNull()` | Compiler guarantees this | Test behavior of the type |
| Tautologies | `x.Should().Be(x)` | Always passes | Test observable outcomes |
| Recreating framework behavior | `new SemaphoreSlim(N)` limits to N | Tests .NET, not your code | Test that YOUR code uses the framework correctly |
| Testing without invoking code | Create mocks, assert on mocks | Never exercises real code | Call actual methods, verify actual outcomes |

### Transformation Examples

| Invalid Test | Valid Transformation |
|--------------|---------------------|
| `MaxConcurrentDownloads.Should().Be(4)` | Download 10 files via command, verify max 4 concurrent HTTP requests via mock |
| `ProgressThrottleMs.Should().BeLessOrEqualTo(1000)` | Download a large file, capture progress callback timestamps, verify no gap > 1 second |

## Test Integrity Protocol

**Before modifying ANY test assertion:**

1. **ARTICULATE** the test's original intent â€” what "When X, Then Y" is it verifying?
2. **DIAGNOSE** the failure:
   - Code not implementing intended behavior? â†’ **Fix the code**
   - Test intent outdated due to legitimate spec change? â†’ **Confirm with user**
   - Test technically flawed (wrong setup, race condition)? â†’ **Fix test mechanics, preserve intent**
3. **NEVER do these without explicit user approval:**
   - Weaken assertions (e.g., `Should().Be("exact")` â†’ `Should().NotBeNull()`)
   - Remove failing assertions
   - Change expected values to match current (buggy) behavior
   - Delete tests that are inconvenient to fix

**Legitimate modifications (no approval needed):**
- Fixing test mechanics (setup, teardown, timing, imports) without changing assertions
- Strengthening assertions (adding more specific checks)

## Test Conventions

- **Naming**: `MethodUnderTest_Scenario_ExpectedBehavior`
- **Structure**: Arrange/Act/Assert
- **Traceability**: `// Implements: UX-003` where applicable

## Existing Tests Are Not Pre-Validated

"It's already in the codebase" is not evidence of correctness. Always apply the Mandatory Validation Checkpoint when following an existing pattern.

If you find an invalid existing test: report `âš ï¸ EXISTING TEST INVALID: [file]:[test name] â€” [reason]` and do NOT copy its pattern.
