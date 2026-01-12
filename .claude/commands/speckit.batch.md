```markdown
---
description: Create task batches from tasks.md, manage batch lifecycle, and advance to next batch after completion.
handoffs:
  - label: Execute Next Task
    agent: speckit.execute
    prompt: Execute the next task in the active batch
    send: true
  - label: Analyze Workflow
    agent: speckit.analyze
    prompt: Validate workflow artifacts before proceeding
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

Valid arguments:
- `create` or empty — Create batches from tasks.md and activate the first batch
- `complete` — Validate and complete the current batch, advance to next
- `status` — Show current batch status without modifying state

---

## Intent

This command manages the **bounded execution** principle of Micro-TDD. Tasks are partitioned into closed batches of 10–15 tasks. Only one batch is active at a time. Future batches are not visible during execution.

**Why batches exist:**
- Prevent agents from reasoning over the entire work set
- Create natural stopping points for review
- Enable safe resumption by any agent
- Enforce incremental delivery with verification

---

## Phase 1: Batch Creation (`create` or no argument)

### Prerequisites

1. Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` to validate:
   - Feature branch exists
   - `tasks.md` exists and is complete
   - `plan.md` exists

2. Run `.specify/scripts/powershell/analyze-workflow.ps1 -Phase tasks -Json` to validate task format:
   - All tasks have `@test-case:` reference
   - All dependencies use valid `[depends:T###]` syntax
   - No circular dependencies
   - Each test case has exactly one task

   **If validation fails**: STOP and report issues. Do not create batches from invalid tasks.

### Execution

3. Run `.specify/scripts/powershell/create-batch.ps1 -Json` to:
   - Parse `tasks.md` and extract all tasks
   - Partition into batches of 10–15 tasks respecting dependencies
   - Create `batches/batch-001.md`, `batch-002.md`, etc.
   - Initialize `batch-state.json` with first batch active

4. Report batch creation summary:
   - Total batches created
   - Tasks per batch
   - Active batch path
   - First eligible task

### Batch File Format

Each batch file is self-contained:

```markdown
# Batch 001: [Feature Name]

**Created**: [DATE]
**Status**: pending | in-progress | complete
**Tasks**: X of Y complete

## Tasks

- [ ] T001 @test-case:UX-001 Description with file path
- [ ] T002 [depends:T001] @test-case:UX-002 Description with file path
- [ ] T003 [depends:T001] @test-case:CV-001 Description with file path
...

## Completion Criteria

- [ ] All tasks verified (evidence validated)
- [ ] Full test suite passes (5 consecutive clean runs)
- [ ] No open ambiguities
```

---

## Phase 2: Batch Status (`status`)

1. Run `.specify/scripts/powershell/check-batch-complete.ps1 -Json -StatusOnly` to get:
   - Active batch name
   - Total tasks in batch
   - Tasks by phase (pending, red, green, verified, failed)
   - Current task (if any in progress)
   - Blocking issues (if any)

2. Display status table and any blocking issues.

---

## Phase 3: Batch Completion (`complete`)

### Prerequisites

1. Run `.specify/scripts/powershell/check-batch-complete.ps1 -Json` to validate:
   - All tasks in active batch have `phase: verified`
   - No tasks in `failed` or `pending` state
   - `currentTask` is null (no task in progress)

   **If not ready**: STOP and report which tasks are incomplete.

### Verification Gate

2. Run `.specify/scripts/powershell/analyze-workflow.ps1 -Phase evidence -Json` to validate:
   - All evidence files exist for batch tasks
   - All evidence passes validation rules
   - RED→GREEN sequence is valid for all tasks

   **If validation fails**: STOP and report evidence issues.

### Full Test Suite

3. Run the full test suite with stability verification using the VS Code `runTests` tool:

   - Run tests **without** specifying files to execute all tests in the project
   - Verify the test run passes completely
   - Run 3–5 times to confirm stability (no flaky tests)

   **Why use `runTests` instead of `dotnet test`:**
   - Consistent with task execution workflow
   - Proper test discovery and filtering
   - Integrated output in VS Code

   **If any run fails**: STOP. Do not advance batch. Report failing tests.

### Commit and Advance

4. Stage and commit batch artifacts:
   - `batches/batch-###.md` (completed batch)
   - `evidence/*.json` (all evidence for this batch)
   - Updated source files

   Commit message: `feat([feature]): complete batch ### - [summary]`

5. Run `.specify/scripts/powershell/advance-batch.ps1 -Json` to:
   - Mark current batch as `complete` in state
   - Activate next batch (if exists)
   - Reset `currentTask` and task states for new batch

6. Report:
   - Completed batch summary
   - Next batch activated (or "All batches complete")
   - Next eligible task (if applicable)

---

## State Management

**Never edit `batch-state.json` directly.** Use scripts:

| Script | Purpose |
|--------|---------|
| `create-batch.ps1` | Initialize batches and state from tasks.md |
| `check-batch-complete.ps1` | Validate batch readiness for completion |
| `advance-batch.ps1` | Move to next batch after completion |

### State File Structure

```json
{
  "activeBatch": "batch-001",
  "currentTask": null,
  "taskStates": {
    "T001": { "phase": "verified" },
    "T002": { "phase": "pending" }
  },
  "batchStatus": "in-progress",
  "completedBatches": []
}
```

---

## Constraints

### Batch Size Limits
- Minimum: 10 tasks (unless fewer remain)
- Maximum: 15 tasks
- Rationale: Large enough for meaningful progress, small enough for bounded execution

### Dependency Ordering
- A task cannot be in an earlier batch than its dependencies
- Within a batch, dependency order determines execution sequence
- Cross-batch dependencies are resolved by batch ordering

### Future Batch Isolation
- Only the active batch file should be read during execution
- Do NOT read ahead to future batch files
- Do NOT optimize current batch for future batch needs

### No Partial Completion
- A batch is either complete or not
- All tasks must be verified before advancing
- No "good enough" or "we'll fix it later"

---

## Error Handling

### Tasks Invalid
```
ERROR: Task format validation failed

Issues found:
- T005: Missing @test-case reference
- T008: Invalid dependency [depends:T099] - T099 does not exist
- Circular dependency: T010 → T012 → T010

Run /speckit.tasks to regenerate task list with correct format.
```

### Batch Incomplete
```
ERROR: Cannot complete batch - tasks not verified

Batch: batch-001
Status: 12/15 tasks verified

Incomplete tasks:
- T013: phase=green (needs verification - run /speckit.verify)
- T014: phase=pending (not started - run /speckit.execute)
- T015: phase=failed (verification failed - run /speckit.recover)
```

### Test Suite Failing
```
ERROR: Full test suite failed - cannot advance batch

Run 3 of 5 failed:
  DownloadCommandTests.Execute_NotConnected_DisplaysFriendlyError
  UploadCommandTests.Execute_WithGlob_MatchesFiles

Batch completion blocked. Fix failing tests before proceeding.
Consider running /speckit.remediatetests for systematic remediation.
```

---

## Handoff Guidance

After `create`:
- Use `/speckit.execute` to begin executing tasks

After `complete` (if more batches):
- Use `/speckit.execute` to begin next batch

After `complete` (if all batches done):
- Feature implementation is complete
- Run final validation and prepare for merge
```
