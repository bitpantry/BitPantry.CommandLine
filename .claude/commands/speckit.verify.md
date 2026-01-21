```markdown
---
description: Validate task evidence and mark task as complete. This is a mandatory gate - tasks cannot proceed without verification.
handoffs:
  - label: Execute Next Task
    agent: speckit.execute
    prompt: Execute the next task in the active batch
    send: true
  - label: Recover Failed Task
    agent: speckit.recover
    prompt: Diagnose and fix the verification failure
    send: true
  - label: Complete Batch
    agent: speckit.batch
    prompt: Complete the current batch and advance to next
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

If a task ID is provided (e.g., `T005`), verify that specific task.
If empty, verify the current task from `batch-state.json`.

---

## Intent

This command is a **mandatory gate** between task execution and completion. It validates that:

**For TDD tasks (with `@test-case`):**
1. The RED phase was properly recorded (test failed)
2. The GREEN phase was properly recorded (test passed)
3. The sequence is valid (RED before GREEN)
4. Implementation changes were captured (diff exists)

**For implementation-only tasks (without `@test-case`):**
1. The GREEN phase was recorded (regression tests passed)
2. Implementation changes were captured (diff exists)

**No task can be marked complete without passing verification.**

---

## Prerequisites

1. Run `.specify/scripts/powershell/get-current-task.ps1 -Json` to get the task being verified:
   - Returns task ID, current phase, test case reference
   - Returns `NO_TASK` if no task is in progress

   **If `NO_TASK`**: STOP. Report that no task is pending verification. Run `/speckit.execute` first.

2. Verify the task is in `green` phase:
   - Task must have completed both RED and GREEN phases
   - If task is in `red` phase, it's not ready for verification
   - If task is in `pending` phase, it hasn't been started

   **If not in `green` phase**: STOP. Report current phase and what's needed.

---

## Verification Process

### Step 1: Validate Evidence File

Run `.specify/scripts/powershell/check-task-evidence.ps1 -TaskId T### -Json`

**For TDD tasks (with `@test-case`)**, the script validates:

| Check | Pass Criteria | Failure Code |
|-------|---------------|--------------|
| Evidence file exists | `evidence/T###.json` exists | `MISSING_EVIDENCE` |
| RED section exists | `red` object present | `MISSING_RED` |
| RED shows failure | `red.exitCode != 0` | `RED_PASSED` |
| GREEN section exists | `green` object present | `MISSING_GREEN` |
| GREEN shows success | `green.exitCode == 0` | `GREEN_FAILED` |
| Diff section exists | `diff` object present | `MISSING_DIFF` |
| Files changed | `diff.files` is non-empty | `NO_CHANGES` |
| Valid sequence | `green.timestamp > red.timestamp` | `INVALID_SEQUENCE` |

**For implementation-only tasks (without `@test-case`)**, relaxed validation:

| Check | Pass Criteria | Failure Code |
|-------|---------------|--------------|
| Evidence file exists | `evidence/T###.json` exists | `MISSING_EVIDENCE` |
| GREEN section exists | `green` object present | `MISSING_GREEN` |
| GREEN shows success | `green.exitCode == 0` | `GREEN_FAILED` |
| Diff section exists | `diff` object present | `MISSING_DIFF` |
| Files changed | `diff.files` is non-empty | `NO_CHANGES` |

**Note**: RED phase is NOT required for implementation-only tasks.

### Step 2: Handle Validation Result

**If ALL checks pass:**

1. Run `.specify/scripts/powershell/complete-task.ps1 -TaskId T###` to:
   - Update task phase to `verified` in `batch-state.json`
   - Mark task `[X]` in the active batch file
   - Clear `currentTask` in state

2. Check remaining tasks:
   - Run `.specify/scripts/powershell/get-next-task.ps1 -Json`
   - If `BATCH_COMPLETE`: Report batch is ready for `/speckit.batch complete`
   - If task returned: Report next eligible task

3. Report success:
   ```
   ✓ Task T### (UX-003) verified and complete
   
   Evidence validated:
     - RED: Test failed at 2026-01-11T14:32:00Z (exit code 1)
     - GREEN: Test passed at 2026-01-11T14:35:00Z (exit code 0)
     - DIFF: 2 files changed
   
   Batch progress: 5/12 tasks verified
   Next eligible task: T008 @test-case:CV-002
   
   Run /speckit.execute to continue.
   ```

**If ANY check fails:**

1. Run `.specify/scripts/powershell/fail-task.ps1 -TaskId T### -Reason "..."` to:
   - Update task phase to `failed` in `batch-state.json`
   - Record failure reason

2. Report failure with structured details:
   ```
   ✗ Task T### verification FAILED
   
   Failure: RED_PASSED
   Reason: Test passed during RED phase — the test did not fail before implementation
   
   Evidence shows:
     - red.exitCode: 0 (expected: non-zero)
     - red.output: "Passed: 1, Failed: 0"
   
   This indicates the test does not verify the behavior specified in test case UX-003.
   
   Run /speckit.recover to diagnose and fix.
   ```

3. **STOP** — do not proceed to next task.

---

## Verification Failure Codes

| Code | Meaning | Likely Cause | Recovery Action |
|------|---------|--------------|-----------------|
| `MISSING_EVIDENCE` | No evidence file | Execute phase didn't complete | Re-run `/speckit.execute` |
| `MISSING_RED` | No RED section | RED phase skipped | Re-run task from RED phase |
| `RED_PASSED` | Test passed during RED | Invalid test | Rewrite test to fail first |
| `MISSING_GREEN` | No GREEN section | GREEN phase not run | Complete implementation |
| `GREEN_FAILED` | Test still failing | Incomplete implementation | Fix implementation |
| `MISSING_DIFF` | No diff recorded | No code changes made | Capture missing diff |
| `NO_CHANGES` | Empty diff | Test passed without changes | Investigate — may be invalid test |
| `INVALID_SEQUENCE` | GREEN before RED | Corrupted evidence | Re-run task from beginning |

---

## Evidence Structure Reference

Valid evidence file (`evidence/T###.json`):

```json
{
  "taskId": "T003",
  "testCase": "UX-003",
  "red": {
    "timestamp": "2026-01-11T14:32:00Z",
    "testCommand": "dotnet test --filter FullyQualifiedName~Connect_ValidServer",
    "exitCode": 1,
    "output": "Expected: True\nActual: False",
    "testFile": "Tests/ConnectionTests.cs",
    "testMethod": "Connect_ValidServer_ReturnsTrue"
  },
  "green": {
    "timestamp": "2026-01-11T14:35:00Z",
    "testCommand": "dotnet test --filter FullyQualifiedName~Connect_ValidServer",
    "exitCode": 0,
    "output": "Passed: 1, Failed: 0, Skipped: 0"
  },
  "diff": {
    "timestamp": "2026-01-11T14:35:00Z",
    "files": [
      "src/Services/ConnectionService.cs"
    ],
    "patch": "@@ -15,6 +15,10 @@ public bool Connect()\n+    return true;\n"
  }
}
```

---

## Manual Override

In rare cases, evidence may be valid but the script reports failure due to edge cases (e.g., test framework output format). 

**If you believe verification should pass but the script disagrees:**

1. Present the evidence file contents
2. Explain why each validation rule is actually satisfied
3. Request user approval to override

**If user approves:**

Run `.specify/scripts/powershell/complete-task.ps1 -TaskId T### -Force` to mark complete despite script failure.

**This override should be rare.** If it happens frequently, the evidence capture or validation scripts need improvement.

---

## Constraints

### No Self-Verification
The agent cannot declare "I verified it" without evidence. Evidence files are required, and validation is scripted.

### No Skipping
Every task must pass verification before the next task can begin. There is no "I'll verify it later" or "it's obviously correct."

### No Weakening on Failure
If verification fails, the task is not complete. The agent must run `/speckit.recover` to diagnose and fix. Verification requirements are not negotiable.

### Failure Blocks Progress
A failed verification sets the task to `failed` state and blocks all further execution until recovery is complete.
```
