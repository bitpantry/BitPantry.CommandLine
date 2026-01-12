```markdown
---
description: Execute all remaining tasks in the active batch automatically, stopping on failure or decision points.
handoffs:
  - label: Recover Failed Task
    agent: speckit.recover
    prompt: Diagnose and fix the verification failure
  - label: Complete Batch
    agent: speckit.batch
    prompt: Complete the current batch and advance to next
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Intent

This command automates the execute→verify loop for the entire active batch. It runs tasks sequentially until:
- ✓ All tasks in the batch are verified (success)
- ✗ A verification fails (stop for recovery)
- ⚠ A decision point is triggered (stop for clarification)

**This is safe because:**
- All guardrails from `execute` and `verify` remain in place
- Verification is still a mandatory gate between tasks
- Decision points still force a stop
- Batch size is bounded (10-15 tasks max)

---

## Prerequisites

1. Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks` to validate feature context.

2. Verify batch state exists:
   - `batch-state.json` must exist (run `/speckit.batch` first if not)
   - An active batch must be set

---

## Test Execution

**ALWAYS use the VS Code `runTests` tool for running tests** — it is more reliable than terminal `dotnet test` commands:

```
runTests with files: ["path/to/TestFile.cs"] and testNames: ["TestMethodName"]
```

Benefits:
- Reliable test filtering (no shell escaping issues)
- Clean output for evidence capture
- Integrates with VS Code test infrastructure

**AVOID** terminal commands like `dotnet test --filter "..."` which often fail due to quoting/escaping issues.

---

## Backfill Mode

Check the batch file for a "Backfill Execution Mode" section. If present:

1. **Skip RED phase** — Implementation already exists
2. **Write test against SPECIFICATION** — Use test-cases.md as truth, NOT current code
3. **If test passes immediately**: Verify test actually validates spec behavior
4. **If test fails**: Fix the CODE, not the test — implementation doesn't match spec
5. **Record only GREEN phase** evidence (RED is skipped for backfill)

**⚠️ CRITICAL: Backfill mode modifies ONLY the RED phase. All other steps remain MANDATORY:**
- **Step 1b (Infrastructure Analysis) — NEVER SKIP** — You must still analyze test infrastructure
- **CLAUDE.md review** — You must still check Testing Infrastructure section
- **Console selection** — If test verifies colors/markup, use VirtualConsole not TestConsole

For backfill tasks, call:
```powershell
.specify/scripts/powershell/record-task-phase.ps1 -TaskId T### -Phase green -ExitCode 0
```

The script will automatically set `currentTask` and update state properly.

---

## Execution Loop

Initialize a progress tracker:
```
┌─────────────────────────────────────────────────────────────┐
│ BATCH EXECUTION: batch-001                                  │
│ Tasks: 0/15 verified                                        │
└─────────────────────────────────────────────────────────────┘
```

### For Each Task:

#### Step 1: Get Next Task

Run `.specify/scripts/powershell/get-next-task.ps1 -Json`

**If `BATCH_COMPLETE`**: 
- All tasks verified — proceed to Batch Complete section below.

**If `TASK_IN_PROGRESS`**: 
- Resume at current phase (check if RED or GREEN needed).

**If task returned**:
- Begin execution of that task.

#### Step 2: Execute Task

Perform the full `/speckit.execute` workflow for this task:
1. Load task context and test case
2. **Analyze existing test infrastructure (Step 1b — NEVER SKIP)**
   - Output infrastructure checkpoint before writing test code
   - Select correct console type (TestConsole vs VirtualConsole)
3. Write failing test (RED phase) with evidence capture
4. Implement minimal code (GREEN phase) with evidence capture

**If DECISION POINT triggered during execution**:
```
⚠ DECISION POINT - Batch execution paused

Task: T005 @test-case:CV-002
Issue: [description of ambiguity]

Batch progress: 4/15 tasks verified

Resolve the decision point, then run /speckit.execute-batch to continue.
```
STOP and wait for user input.

#### Step 3: Verify Task

Perform the full `/speckit.verify` workflow:
1. Validate evidence file exists
2. Check RED phase shows failure
3. Check GREEN phase shows success
4. Validate RED→GREEN sequence

**If verification PASSES**:

Print task summary and continue:
```
✓ T005 (CV-002) verified                                    [5/15]
  RED:   Test failed as expected (exit 1)
  GREEN: Test passed after implementation (exit 0)
  DIFF:  2 files changed (+45, -3)
```

Loop back to Step 1 for next task.

**If verification FAILS**:
```
✗ VERIFICATION FAILED - Batch execution stopped

Task: T005 @test-case:CV-002
Failure: RED_PASSED
Reason: Test passed during RED phase — test does not verify the specified behavior

Batch progress: 4/15 tasks verified

Run /speckit.recover to diagnose and fix, then /speckit.execute-batch to continue.
```
STOP — do not proceed to next task.

---

## Batch Complete

When all tasks are verified:

```
┌─────────────────────────────────────────────────────────────┐
│ ✓ BATCH COMPLETE: batch-001                                 │
│ All 15/15 tasks verified                                    │
└─────────────────────────────────────────────────────────────┘

Task Summary:
  ✓ T001 (UX-001) DownloadCommand file structure
  ✓ T002 (UX-002) Progress callback integration
  ✓ T003 (CV-001) Path validation
  ... [all tasks listed]

Next: Run /speckit.batch complete to:
  - Run 5x full test suite verification
  - Commit batch artifacts
  - Advance to next batch
```

---

## Progress Output Format

After each task verification, output a single-line summary:

```
✓ T### (XX-###) [brief description]                         [N/M]
```

For example:
```
✓ T001 (UX-001) DownloadCommand basic structure             [1/15]
✓ T002 (UX-002) Progress display integration                [2/15]
✓ T003 (CV-001) Remote path validation                      [3/15]
✓ T004 (CV-002) Local path validation                       [4/15]
```

This allows watching progress stream by without intervention.

---

## Error Recovery

### After Verification Failure
1. Run `/speckit.recover` to diagnose and fix
2. Run `/speckit.execute-batch` to resume from the failed task

### After Decision Point
1. Resolve the ambiguity (update spec, clarify requirement)
2. Run `/speckit.execute-batch` to resume

### Resumption
The batch loop is **resumable** — it always queries `get-next-task.ps1` which returns:
- The next pending task, OR
- A task in progress (to resume), OR
- BATCH_COMPLETE

So you can safely re-run `/speckit.execute-batch` after any interruption.

---

## Constraints

### No Skipping
- Every task must pass verification before proceeding
- Cannot mark tasks as "skip" or "defer"

### No Parallelism
- Tasks execute sequentially in dependency order
- No concurrent task execution

### Bounded Execution
- Maximum 15 tasks per batch
- Natural stopping point at batch boundary

### Full Guardrails
- All `/speckit.execute` rules apply (TDD, evidence capture, decision points)
- All `/speckit.verify` rules apply (evidence validation, sequence checking)
```
