```markdown
---
description: Diagnose and fix verification failures using Test Integrity Protocol. Recover failed tasks without weakening tests.
handoffs:
  - label: Verify Task
    agent: speckit.verify
    prompt: Re-verify the recovered task
    send: true
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

If a task ID is provided (e.g., `T005`), recover that specific task.
If empty, recover the failed task from `batch-state.json`.

---

## Intent

This command handles **verification failures** by:
1. Diagnosing the root cause of the failure
2. Applying the Test Integrity Protocol
3. Fixing the issue (code, not tests, unless user-approved)
4. Re-capturing evidence for the fixed phase
5. Triggering re-verification

**The default assumption is that CODE is wrong, not the test.**

---

## Prerequisites

1. Run `.specify/scripts/powershell/get-failed-task.ps1 -Json` to get:
   - Task ID
   - Test case reference
   - Failure code (e.g., `RED_PASSED`, `GREEN_FAILED`)
   - Failure reason
   - Current evidence (if any)

   **If no failed task**: STOP. Report that no task needs recovery.

2. Load task context:
   - Test case definition from `test-cases.md`
   - Task description from active batch
   - Evidence file (if exists) from `evidence/T###.json`
   - Current implementation state

---

## Diagnosis Workflow

### Step 1: Identify Failure Type

| Failure Code | Diagnosis | Root Cause |
|--------------|-----------|------------|
| `MISSING_EVIDENCE` | Evidence file doesn't exist | Execute phase crashed or wasn't run |
| `MISSING_RED` | No RED section in evidence | RED phase was skipped |
| `RED_PASSED` | Test passed during RED phase | Test is invalid — doesn't verify behavior |
| `MISSING_GREEN` | No GREEN section in evidence | GREEN phase wasn't completed |
| `GREEN_FAILED` | Test failed during GREEN phase | Implementation is incomplete or wrong |
| `MISSING_DIFF` | No diff section in evidence | Code changes weren't captured |
| `NO_CHANGES` | Diff is empty | Test passed without implementation |
| `INVALID_SEQUENCE` | GREEN timestamp before RED | Evidence is corrupted |

### Step 2: Apply Test Integrity Protocol

**For each failure type, follow the appropriate recovery path:**

---

#### `MISSING_EVIDENCE` or `MISSING_RED`

**Diagnosis**: Execute phase didn't complete properly.

**Recovery**:
1. Re-run the task from the beginning
2. Run `.specify/scripts/powershell/record-task-phase.ps1 -TaskId T### -Phase started -Retry`
3. Write the test (if not already written)
4. Run test, capture RED evidence
5. Implement code, capture GREEN evidence
6. Proceed to `/speckit.verify`

---

#### `RED_PASSED` — **Requires careful analysis**

**Diagnosis**: The test passed before implementation, which means it cannot verify missing behavior.

**This is a TEST QUALITY issue.** The test is invalid.

**Recovery**:
1. **Articulate** what the test case (from `test-cases.md`) requires:
   > "Test case UX-003 specifies: When user connects with valid credentials, Then connection returns success."

2. **Analyze** why the test passed:
   - Is the behavior already implemented? (task may be duplicate)
   - Is the test not actually checking the right thing?
   - Is the test checking inputs/constants instead of behavior?

3. **Present options to user**:
   ```
   Task T003 @test-case:UX-003 — RED_PASSED
   
   The test passed before implementation. This means either:
   
   A) The behavior already exists
      - Check if this duplicates existing functionality
      - If so, this task may be unnecessary
   
   B) The test doesn't verify the specified behavior
      - Test checks: [what the test actually asserts]
      - Test case requires: [what the test case says]
      - The test needs to be rewritten to fail first
   
   C) The test case itself is wrong
      - The "When/Then" definition may not match actual requirements
   
   TOP RECOMMENDATION: [Your analysis of most likely cause]
   
   Awaiting your decision before proceeding.
   ```

4. **WAIT** for user input.

5. **After user decision**:
   - If rewriting test: Write new test, confirm it fails, capture RED evidence
   - If task is duplicate: Mark task as skipped with user approval
   - If test case is wrong: Update test case first (user must approve)

---

#### `MISSING_GREEN`

**Diagnosis**: RED phase completed but GREEN phase wasn't run.

**Recovery**:
1. Check if implementation was written
2. If not: implement the code
3. Run test, capture GREEN evidence
4. Proceed to `/speckit.verify`

---

#### `GREEN_FAILED` — **Most common, usually means code is wrong**

**Diagnosis**: Test was correct (failed during RED), but implementation doesn't satisfy it.

**Default assumption**: The CODE is wrong, not the test.

**Recovery**:
1. **Examine the failure**:
   - What does the test expect?
   - What does the code produce?
   - Why is there a mismatch?

2. **Fix the implementation**:
   - Do NOT modify the test assertions
   - Do NOT weaken the test to pass
   - Implement the behavior correctly

3. **Re-run test** and capture GREEN evidence:
   ```powershell
   .specify/scripts/powershell/record-task-phase.ps1 `
     -TaskId T### `
     -Phase green `
     -Retry `
     -TestCommand "dotnet test --filter ..." `
     -ExitCode 0 `
     -TestOutput "Passed: 1"
   ```

4. Proceed to `/speckit.verify`

**If you believe the test is wrong**:

This triggers an escalation. You must:

1. **STOP** — do not modify the test
2. Articulate the test's original intent (from test case)
3. Explain why you believe the test is incorrect
4. Present your recommendation
5. **WAIT** for explicit user approval

```
Task T007 @test-case:CV-004 — GREEN_FAILED

Test: ParseConfig_MissingField_ThrowsException
Expects: ArgumentException with message "Field 'name' is required"
Actual: ArgumentException with message "Missing required field: name"

I believe the test assertion is overly strict. The behavior (throwing on missing field)
is correct, but the exact message format differs.

OPTIONS:
A) Fix implementation to match exact message format
   - Change error message to "Field 'name' is required"
   
B) Relax test to check exception type only [REQUIRES APPROVAL]
   - This weakens the test — only recommended if message format is not specified

TOP RECOMMENDATION: Option A — match the expected format

Awaiting your decision.
```

---

#### `MISSING_DIFF` or `NO_CHANGES`

**Diagnosis**: Test passed but no code was changed.

**This could indicate**:
- Changes weren't committed/staged
- Test was already passing (invalid test)
- Implementation was done outside this task

**Recovery**:
1. Check if there are unstaged changes that need capturing
2. If no changes exist and test passes: this is likely `RED_PASSED` in disguise
3. Capture the diff manually if changes exist:
   ```powershell
   git diff HEAD > evidence/T###-diff.patch
   ```
4. Re-run evidence capture with diff

---

#### `INVALID_SEQUENCE`

**Diagnosis**: Evidence is corrupted (GREEN recorded before RED).

**Recovery**:
1. Delete corrupted evidence file
2. Re-run task from beginning
3. Capture RED, then GREEN in proper sequence

---

## Post-Recovery

After fixing the issue:

1. **Verify evidence is now complete**:
   ```powershell
   .specify/scripts/powershell/check-task-evidence.ps1 -TaskId T### -Json
   ```

2. **If validation passes**:
   - Run `/speckit.verify` to complete the task

3. **If validation still fails**:
   - Repeat diagnosis for new failure code
   - Do not loop indefinitely — if recovery fails twice, escalate to user

---

## Escalation Triggers

**Always escalate to user (STOP and wait) when:**

1. **Test modification required**: Any change to assertions or expected values
2. **Test case appears wrong**: The "When/Then" doesn't match requirements
3. **Duplicate behavior**: Task tests something that already exists
4. **Ambiguous requirement**: Can't determine correct behavior from test case
5. **Recovery fails twice**: Same failure after two recovery attempts

**Escalation format:**
```
ESCALATION REQUIRED: [Brief description]

Task: T### @test-case:###:XX-###
Failure: [CODE]
Attempts: [N]

Analysis:
[Your diagnosis of the situation]

Options:
A) [First option with implications]
B) [Second option with implications]
C) [Third option if applicable]

TOP RECOMMENDATION: [Your recommendation with rationale]

Awaiting your decision before proceeding.
```

---

## Constraints

### Never Weaken Tests Autonomously
The agent cannot weaken, remove, or modify test assertions without explicit user approval. The Test Integrity Protocol is non-negotiable.

### Default: Code is Wrong
When a test fails, assume the implementation is incorrect. The test defines expected behavior; the code must match.

### No Infinite Loops
If recovery fails twice for the same failure code, escalate. Do not attempt endless retries.

### Recovery is Not Bypassing
Recovery fixes the actual problem. It does not find ways around verification requirements.
```
