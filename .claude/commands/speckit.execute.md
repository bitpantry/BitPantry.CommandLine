```markdown
---
description: Execute exactly ONE task from the active batch using strict red→green TDD loop.
handoffs:
  - label: Verify Task
    agent: speckit.verify
    prompt: Verify the completed task
    send: true
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

If a task ID is provided (e.g., `T005`), execute that specific task.
If empty, execute the next eligible task from the active batch.

---

## Intent

This command executes **exactly ONE task** using strict Test-Driven Development. The agent:
1. Writes a failing test (RED)
2. Captures evidence of failure
3. Implements minimal code to pass (GREEN)
4. Captures evidence of success
5. STOPS

**No batch-wide execution. No "let me do a few more." One task, then stop.**

---

## Prerequisites

1. Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks` to validate feature context.

2. Verify batch state exists:
   - `batch-state.json` must exist (run `/speckit.batch` first if not)
   - An active batch must be set

3. Run `.specify/scripts/powershell/get-next-task.ps1 -Json` to get the next eligible task:
   - Returns task ID, test case reference, description, dependencies
   - Returns `BATCH_COMPLETE` if all tasks in batch are verified
   - Returns `TASK_IN_PROGRESS` if a task is already started (resume it)

   **If `BATCH_COMPLETE`**: STOP. Report that batch is ready for completion via `/speckit.batch complete`.

   **If `TASK_IN_PROGRESS`**: Resume the in-progress task at its current phase.

---

## Execution Loop

### Step 1: Load Task Context

1. Read the task details from the script output:
   - Task ID (e.g., `T005`)
   - Test case reference (e.g., `@test-case:UX-003`)
   - Task description with file path
   - Dependencies (already satisfied if task is eligible)

2. Load supporting context:
   - `test-cases.md`: Find the test case's "When X, Then Y" definition
   - `plan.md`: Understand technical approach and file structure
   - `.specify/memory/constitution.md`: Review testing standards

3. Run `.specify/scripts/powershell/record-task-phase.ps1 -TaskId T### -Phase started` to mark task as in-progress.

### Step 1a: Check for Consolidation Opportunities — CONDITIONAL

**Before writing a standalone test, check if this task can be consolidated with nearby tasks.**

Consolidation is appropriate when multiple tasks share:
- **Same setup/arrange code** (identical mocking, fixtures, file structure)
- **Same code path** (same method call with same parameters)
- **Tightly coupled assertions** (verifying different aspects of same output)
- **No conflicting states** (can verify all behaviors in sequence)

**Consolidation Workflow:**

1. **Scan ahead 3-5 tasks** in the batch file for related test cases:
   - Look for shared test case prefixes (e.g., UX-032 through UX-035)
   - Look for tasks targeting the same file and component
   - Look for sequential dependencies or complementary behaviors

2. **Evaluate consolidation criteria**:
   | If... | Then... |
   |-------|--------|
   | Same Arrange, Same Act, different Assert | **Consolidate** into one test with labeled assertions |
   | Same Arrange, different Act | **Keep separate** — different code paths |
   | Conflicting preconditions | **Keep separate** — can't run sequentially |
   | Independent, unrelated behaviors | **Keep separate** — no benefit |

3. **If consolidating**:
   - Write one comprehensive test covering multiple test cases
   - Use labeled assertions for clarity:
     ```csharp
     // 007:UX-032: Summary shows partial success
     result.Should().Contain("2 of 3 files downloaded");
     
     // 007:UX-033: Batch continues after failure
     mockProxy.Verify(x => x.DownloadFile(...), Times.Exactly(3));
     
     // 007:UX-035: Failed files listed with reason
     result.Should().Contain("unavailable.txt").And.Contain("Failed:");
     ```
   - Name the test to reflect consolidated scope:
     `[Method]_[Scenario]_[ComprehensiveOutcome]`
   - Mark consolidated tasks in batch file:
     ```
     - [x] T139 @test-case:007:UX-034 — Covered by T137
       Notes: Consolidated with T137 — same test setup, complementary assertions
     ```

4. **Record consolidation evidence**:
   - When completing consolidated tasks, use:
     ```powershell
     .specify/scripts/powershell/complete-task.ps1 -TaskId T### -Force
     ```
   - The `-Force` flag bypasses evidence requirement for covered tasks
   - Still provide Notes explaining the consolidation

**⚠️ Do NOT consolidate if:**
- Tasks have conflicting test states (different mock configurations)
- Tasks require different test infrastructure (TestConsole vs VirtualConsole)
- Consolidation would create a test over 50 lines of assertion code
- Tasks are in different dependency chains

---

### Step 1b: Analyze Existing Test Infrastructure — MANDATORY

**⚠️ DO NOT SKIP THIS STEP — even in backfill mode.**

**Before writing ANY test code, scan for reusable infrastructure to avoid duplication.**

1. **Read `CLAUDE.md` → "Testing Infrastructure"** section:
   - **Test Levels**: Understand when to use Unit vs Integration vs UX tests
   - **TestEnvironment**: For integration tests requiring client-server flows
   - **VirtualConsole**: For UX tests verifying console output WITH COLOR/MARKUP
   - **TestConsole**: Spectre.Console's test console — renders output, STRIPS markup
   - **Shared Test Helpers**: `TestServerProxyFactory`, `TestFileTransferServiceFactory`, `TempFileScope`, etc.

2. **Scan the target test file** for existing patterns:
   - Check `[TestInitialize]` setup — what fields/mocks are already available?
   - Check `#region Helper Methods` — what factory/builder methods exist?
   - List class-level mock fields (e.g., `_proxyMock`, `_console`)

3. **Scan shared test helpers** (`**/Helpers/*.cs`, `**/TestHelpers/**`):
   - Verify helpers listed in CLAUDE.md are current
   - Identify reusable mock builders or fixture creators

4. **Check sibling test files** for the same component:
   - If testing `FooCommand`, also check `FooServiceTests.cs` for patterns
   - Identify setup code that should be consolidated

5. **Console selection decision**:
   | Need | Use |
   |------|-----|
   | Verify text content only | `TestConsole` (Spectre.Console) |
   | Verify colors/markup/ANSI codes | `VirtualConsole` with `VirtualConsoleAnsiAdapter` |
   | Integration test with real console flow | `VirtualConsole` |

6. **Decision matrix**:
   | Situation | Action |
   |-----------|--------|
   | Helper exists and covers need | **Reuse** the existing helper |
   | Helper exists but needs extension | **Extend** the helper, then use it |
   | Pattern repeated 3+ times across files | **Create** new shared helper in `Helpers/` |
   | One-off need, no existing pattern | Create private helper in test class |

---

**⛔ CHECKPOINT — Output this BEFORE writing any test code:**

```
> Infrastructure Analysis:
>   Console: [TestConsole | VirtualConsole] because [reason]
>   Helpers: [list helpers being reused]
>   Pattern: [existing test being followed]
```

**If you skip this step and later discover you used the wrong infrastructure, STOP and fix the test before proceeding.**

### Step 2: Write Failing Test (RED Phase)

**Before writing any code, you must write a test that fails.**

1. **Articulate the behavioral hypothesis**:
   > "When [condition from test case], the system should [expected behavior] because [rationale]"
   
   If you cannot articulate this clearly, the test case may be underspecified. Trigger Decision Point Protocol.

2. **Check for existing patterns**:
   - Search for similar tests in the project
   - Reuse established test infrastructure (setup helpers, mocks, assertions)
   - Follow naming convention: `MethodUnderTest_Scenario_ExpectedBehavior`

   ⚠️ **Existing tests are NOT pre-validated.** Tests in the codebase may themselves be invalid. When following an existing pattern, you MUST still apply the Mandatory Validation Checkpoint (see `.specify/memory/invalid-test-patterns.md`). "It's already in the codebase" is not evidence of correctness.
   
   **If you find a pattern to copy:**
   - Verify the existing test itself passes the Mandatory Validation Checkpoint
   - If the existing test is invalid, do NOT copy it — write a valid test instead
   - Report: `Pattern found in [file] — validated as [VALID/INVALID because...]`
   - If INVALID, report: `⚠️ EXISTING TEST INVALID: [file]:[method] — [reason]`

3. **Write the test**:
   - Use the testing framework from constitution (MSTest + FluentAssertions + Moq)
   - Follow Arrange/Act/Assert structure
   - Test the behavior specified in test-cases.md, NOT implementation artifacts
   - Reference the test case: `// Implements: UX-003`

4. **Run the test** and confirm it FAILS:
   
   **Use the VS Code `runTests` tool** (preferred — more reliable filtering):
   ```
   runTests with files: ["path/to/TestFile.cs"] and testNames: ["TestMethodName"]
   ```
   
   The runTests tool is more reliable than terminal `dotnet test` commands because:
   - It handles test filtering correctly without shell escaping issues
   - It integrates with VS Code's test infrastructure
   - It provides cleaner output for evidence capture

5. **Capture RED evidence**:
   ```powershell
   .specify/scripts/powershell/record-task-phase.ps1 `
     -TaskId T### `
     -Phase red `
     -TestCommand "dotnet test --filter ..." `
     -ExitCode 1 `
     -TestOutput "Expected: X, Actual: Y" `
     -TestFile "path/to/TestFile.cs" `
     -TestMethod "MethodUnderTest_Scenario_ExpectedBehavior"
   ```

**If the test PASSES during RED phase**: STOP. The test is invalid — it cannot catch the bug it's supposed to catch. This is a test quality issue, not a success. Re-examine the test case and rewrite the test to actually verify the missing behavior.

### Step 3: Implement Minimal Code (GREEN Phase)

**Now and only now, write implementation code.**

1. **Implement the minimum code** to make the test pass:
   - Do NOT add extra functionality
   - Do NOT refactor unrelated code
   - Do NOT implement "while I'm here" improvements
   - Focus solely on the behavior this test verifies

2. **Run the test** and confirm it PASSES:
   
   **Use the VS Code `runTests` tool** (preferred — more reliable filtering):
   ```
   runTests with files: ["path/to/TestFile.cs"] and testNames: ["TestMethodName"]
   ```

3. **Capture GREEN evidence**:
   ```powershell
   .specify/scripts/powershell/record-task-phase.ps1 `
     -TaskId T### `
     -Phase green `
     -TestCommand "dotnet test --filter ..." `
     -ExitCode 0 `
     -TestOutput "Passed: 1, Failed: 0"
   ```

   The script automatically captures the git diff of changed files.

4. **Validate test integrity** (even in backfill mode):
   
   Answer: **"If someone deleted or broke the code I just wrote, would this test fail?"**
   
   - If NO → The test is invalid. Rewrite it to test actual behavior.
   - If YES → Proceed to capture evidence.

**If the test FAILS during GREEN phase**: Debug your implementation. Do NOT modify the test to make it pass. The test defines the correct behavior; your code must match it.

### Step 4: STOP

**You are done. Do not proceed to the next task.**

Report completion:
```
Task T### (UX-003) ready for verification.

RED phase:
  - Test: MethodUnderTest_Scenario_ExpectedBehavior
  - Result: Failed as expected (exit code 1)

GREEN phase:
  - Result: Passed (exit code 0)
  - Files changed: [list files]

Run /speckit.verify to validate and complete this task.
```

---

## Test Writing Discipline

### Valid Test Patterns

| Pattern | Example | Why Valid |
|---------|---------|-----------|
| Execute and verify outcome | `service.Connect().Should().BeTrue()` | Tests actual behavior |
| Mock and verify interaction | `mock.Verify(x => x.Send(msg), Times.Once)` | Tests integration behavior |
| Create fixture, verify state | `file.Exists.Should().BeTrue()` | Tests side effects |
| Capture output, verify content | `console.Output.Should().Contain("Connected")` | Tests observable output |

### Invalid Test Patterns

> **📋 See `.specify/memory/invalid-test-patterns.md` for the canonical list.**

Common invalid patterns include:
- Testing constants (`MaxRetries.Should().Be(3)`)
- Testing inputs, not processing
- Tautologies (`x.Should().Be(x)`)
- Recreating framework behavior (testing that `SemaphoreSlim` works)

### Mandatory Validation Checkpoint

> **📋 Full checkpoint details in `.specify/memory/invalid-test-patterns.md`**

**⛔ CHECKPOINT — Output these answers BEFORE writing test code:**

```
> Test Validity Check:
>   Invokes code under test: [YES/NO] — Does this call the actual method/class being tested?
>   Breakage detection: [YES/NO] — If implementation breaks, does test fail?
>   Not a tautology: [YES/NO] — Testing behavior, not restating structure?
```

**If any answer is NO, do not proceed. Redesign the test.**

### Verification Question

Before capturing GREEN evidence, ask yourself:
> "If someone broke the behavior described in the test case's 'Then' column, would this test fail?"

If the answer is "no", the test is invalid. Rewrite it.

---

## Decision Point Protocol

**STOP execution and escalate when:**

1. **Test case is ambiguous**: The "When X, Then Y" doesn't clearly define verifiable behavior
2. **Multiple valid interpretations**: You could test this several ways with different implications
3. **Missing dependency**: Implementation requires code that doesn't exist yet
4. **Test passes unexpectedly**: The test should fail but passes — indicates test or understanding issue

**When escalating, present:**
- What specific ambiguity or issue was encountered
- Where it was found (test case ID, task description)
- Available options with trade-offs
- Your TOP RECOMMENDATION

**Wait for user input before proceeding.**

---

## Test Integrity Protocol

**Tests define behavior. Implementation must match tests, not vice versa.**

If you feel the test is wrong:
1. **STOP** — do not modify the test
2. Articulate what the test is trying to verify
3. Explain why you believe the test's intent is incorrect
4. Present your recommendation
5. **WAIT** for user approval before any test modification

**NEVER without user approval:**
- Weaken assertions (e.g., `Be("exact")` → `NotBeNull()`)
- Remove failing assertions
- Change expected values to match current behavior
- Generalize specific checks

---

## Error Handling

### No Active Batch
```
ERROR: No active batch found

batch-state.json is missing or has no activeBatch set.
Run /speckit.batch to create batches from tasks.md.
```

### Task Dependencies Not Met
```
ERROR: Task T007 has unmet dependencies

Dependencies: T005, T006
Status:
  - T005: verified ✓
  - T006: pending ✗

Complete T006 first, then T007 becomes eligible.
```

### Test Passes During RED
```
ERROR: Test passed during RED phase — invalid test

Task: T003 @test-case:UX-003
Test: Connect_ValidServer_ReturnsTrue

The test should FAIL before implementation, but it passed.
This means either:
1. The behavior already exists (task may be duplicate)
2. The test doesn't actually verify the specified behavior
3. The test case definition is wrong

Review test-cases.md UX-003 and rewrite the test to verify
the actual missing behavior.
```

---

## Constraints

### One Task Only
This command executes exactly one task. After capturing GREEN evidence, STOP. Do not continue to the next task.

### No Implementation Before RED
You must have a failing test before writing any implementation code. The failing test proves the behavior is missing.

### No Test Modification During GREEN
If the test fails during GREEN phase, fix your implementation. Do not modify the test to make it pass.

### No Speculative Abstractions
Implement only what this specific test requires. Do not add extensibility, generalization, or "future-proofing" that isn't tested.
```
