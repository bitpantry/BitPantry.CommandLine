# Spec Kit: Micro-TDD Workflow

## Intent

Spec Kit enforces disciplined, test-driven development by making shortcuts structurally impossible. The workflow guarantees:

- **Correctness over progress** — stopping is a valid outcome
- **Tests define behavior** — no implementation without a failing test first
- **Atomic work units** — one behavioral change per task
- **Bounded execution** — agents work in closed batches, never the full scope
- **Externalized verification** — evidence proves compliance, not self-reporting

## Core Concepts

| Concept | Definition |
|---------|------------|
| **Task** | A single behavioral unit: one test case → one failing test → one code change → one passing test |
| **Batch** | A closed group of 10–15 tasks executed sequentially with explicit boundaries |
| **Evidence** | JSON proof of the red→green cycle for each task (timestamps, outputs, diffs) |
| **Gate** | Verification checkpoint that blocks progress until evidence is validated |
| **State** | `batch-state.json` tracks active batch, current task, and task phases |

## Workflow Phases

```
┌─────────────────────────────────────────────────────────────────┐
│ PHASE 1-2: SPECIFICATION & PLANNING                             │
│   Define WHAT to build and HOW to build it                      │
│   No execution decisions yet                                    │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ PHASE 3: TASK DECOMPOSITION                                     │
│   Break into atomic behavioral units                            │
│   Each task = ONE test case = ONE behavioral change             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ PHASE 4: BATCH PLANNING                                         │
│   Group tasks into bounded batches (10-15 tasks)                │
│   Future batches are NOT visible during execution               │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ PHASE 5-7: EXECUTION LOOP (per task)                            │
│                                                                 │
│   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐        │
│   │   EXECUTE   │───▶│   VERIFY    │───▶│  (next task)│        │
│   │  red→green  │    │   gate      │    │             │        │
│   └─────────────┘    └──────┬──────┘    └─────────────┘        │
│                             │ FAIL                              │
│                             ▼                                   │
│                      ┌─────────────┐                            │
│                      │   RECOVER   │                            │
│                      │  diagnose   │                            │
│                      └─────────────┘                            │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ PHASE 8: BATCH COMPLETION                                       │
│   All tasks verified → full test suite → commit → next batch    │
└─────────────────────────────────────────────────────────────────┘
```

## Commands Reference

### Specification Phase

| Command | Purpose | When to Use |
|---------|---------|-------------|
| `/speckit.specify` | Create feature spec from natural language description | Starting a new feature |
| `/speckit.clarify` | Reduce ambiguity via targeted questions | Before planning, when spec has gaps |
| `/speckit.constitution` | Manage project principles and governance | Establishing or amending project rules |

### Planning Phase

| Command | Purpose | When to Use |
|---------|---------|-------------|
| `/speckit.plan` | Generate technical plan, data model, contracts, test cases | After spec is complete |
| `/speckit.checklist` | Create requirements quality checklist ("unit tests for requirements") | Before implementation, for validation |

### Decomposition Phase

| Command | Purpose | When to Use |
|---------|---------|-------------|
| `/speckit.tasks` | Decompose plan into atomic tasks (one test case per task) | After plan is complete |
| `/speckit.analyze` | Validate workflow artifacts for consistency and integrity | After tasks, after batches, before batch completion |

### Execution Phase

| Command | Purpose | When to Use |
|---------|---------|-------------|
| `/speckit.batch` | Create task batches / advance to next batch | After tasks validated, or after batch completion |
| `/speckit.execute` | Execute exactly ONE task (red→green loop) | When ready to implement the next task |
| `/speckit.verify` | Validate task evidence and mark complete | Immediately after execute completes |
| `/speckit.recover` | Diagnose and fix verification failures | After verify fails |

### Maintenance

| Command | Purpose | When to Use |
|---------|---------|-------------|
| `/speckit.remediatetests` | Systematically fix failing tests in the solution | Test suite has failures outside feature work |
| `/speckit.bugfix` | Fix bugs using disciplined workflow | Bug discovered in production code |
| `/speckit.taskstoissues` | Export tasks to issue tracker | Project management integration needed |

## Typical Session Flow

```
/speckit.specify "Add user authentication"
    ↓
/speckit.clarify                        ← (if spec has gaps)
    ↓
/speckit.plan
    ↓
/speckit.tasks
    ↓
/speckit.analyze                        ← validates task format, test case coverage
    ↓
/speckit.batch                          ← creates batches, activates first batch
    ↓
┌───────────────────────────────────────┐
│ REPEAT for each task in batch:        │
│                                       │
│   /speckit.execute                    │
│       ↓                               │
│   /speckit.verify                     │
│       ↓                               │
│   (if FAIL: /speckit.recover → retry) │
│                                       │
└───────────────────────────────────────┘
    ↓
/speckit.batch complete                 ← validates batch, runs full suite, advances
    ↓
(repeat for next batch until all complete)
```

## Key Constraints (Non-Negotiable)

### No Batch-Wide Execution
`/speckit.execute` handles exactly ONE task. The agent writes one test, confirms it fails, implements minimal code, confirms it passes, then STOPS. No "let me do a few more while I'm here."

### No Skipping Verification
The verify gate blocks the next task. Evidence must exist and pass validation. There is no "I verified it mentally" or "the tests pass so we're good."

### No Weakening Tests
When verification fails, `/speckit.recover` diagnoses the issue. If the test is wrong, it escalates to the user. Agents NEVER soften assertions, remove failing checks, or change expected values to match buggy behavior.

### No Future Batch Visibility
Only the active batch file is readable. Agents cannot "look ahead" to optimize current work for future needs. Each batch is a closed execution window.

### No Self-Reported Compliance
Evidence files (JSON with timestamps, outputs, diffs) prove the red→green cycle happened. Scripts validate evidence structure. The agent's word is not sufficient.

## Test-Driven Development Discipline

### The Red→Green Loop (Per Task)

```
1. SELECT task from active batch (via script)
2. WRITE test that verifies the test case behavior
3. RUN test → MUST FAIL (red phase)
4. CAPTURE failing output to evidence file
5. IMPLEMENT minimal code to make test pass
6. RUN test → MUST PASS (green phase)
7. CAPTURE passing output + git diff to evidence
8. STOP → ready for verification
```

### What Makes a Valid Test

Tests MUST verify the actual behavior specified in `test-cases.md`, not implementation artifacts.

**Valid Test Patterns:**
- ✅ Execute code and verify observable outcome
- ✅ Mock dependencies and verify interactions
- ✅ Create real test fixtures and verify state changes
- ✅ Capture side effects (console output, file creation)

**Invalid Test Patterns:**
- ❌ Testing constants: `MaxConcurrency.Should().Be(4)`
- ❌ Testing input strings: `pattern.Contains("*").Should().BeTrue()`
- ❌ Testing types exist: `typeof(X).Should().NotBeNull()`
- ❌ Tautologies: `result.Should().Be(result)`

### Test Integrity Protocol

When a test fails during verification or recovery:

1. **Default assumption**: The CODE is wrong, not the test
2. **Before modifying any assertion**: Articulate the test's original intent
3. **Diagnose the failure**:
   - Code not implementing behavior? → Fix the code
   - Test intent outdated due to spec change? → Escalate to user
   - Test technically flawed (setup, timing)? → Fix mechanics, preserve intent
4. **NEVER without user approval**:
   - Weaken assertions
   - Remove failing assertions
   - Change expected values to match current behavior
   - Delete inconvenient tests

## State Management

All state is managed through `batch-state.json` in the feature directory. Agents invoke PowerShell scripts to read/write state—they never edit the JSON directly.

```json
{
  "activeBatch": "batch-001",
  "currentTask": "T003",
  "taskStates": {
    "T001": { "phase": "verified" },
    "T002": { "phase": "verified" },
    "T003": { "phase": "green" }
  },
  "batchStatus": "in-progress"
}
```

### Task Phases

| Phase | Meaning |
|-------|---------|
| `pending` | Not yet started |
| `red` | Test written and failing |
| `green` | Implementation complete, test passing |
| `verified` | Evidence validated, task complete |
| `failed` | Verification failed, needs recovery |

## Evidence Files

Each task produces an evidence file at `evidence/T###.json`:

```json
{
  "taskId": "T001",
  "testCase": "UX-001",
  "red": {
    "timestamp": "2026-01-11T14:32:00Z",
    "testCommand": "dotnet test --filter FullyQualifiedName~TestMethod",
    "exitCode": 1,
    "output": "Expected: 'connected'\nActual: null"
  },
  "green": {
    "timestamp": "2026-01-11T14:35:00Z",
    "testCommand": "dotnet test --filter FullyQualifiedName~TestMethod",
    "exitCode": 0,
    "output": "Passed: 1, Failed: 0"
  },
  "diff": {
    "timestamp": "2026-01-11T14:35:00Z",
    "files": ["src/Services/ConnectionService.cs"],
    "patch": "..."
  }
}
```

### Evidence Validation Rules

| Check | Failure Reason |
|-------|----------------|
| `red` section missing | "No RED phase recorded" |
| `red.exitCode == 0` | "Test passed during RED phase — invalid test" |
| `green` section missing | "No GREEN phase recorded" |
| `green.exitCode != 0` | "Test still failing after implementation" |
| `diff` section missing | "No implementation diff recorded" |
| `green.timestamp < red.timestamp` | "GREEN recorded before RED — invalid sequence" |

## Recovery Workflow

When `/speckit.verify` fails, the agent must run `/speckit.recover`:

1. **Load failure details** from state (which task, what failed)
2. **Diagnose failure type**:
   - Missing RED → re-run test, capture output
   - Test passed during RED → invalid test, escalate to user
   - Missing GREEN → implement and re-run
   - Test still failing → fix implementation
   - Missing diff → capture current diff
   - Sequence invalid → re-execute from RED phase
3. **Apply Test Integrity Protocol** — never weaken tests
4. **Re-run `/speckit.verify`** after fix

If diagnosis requires changing test intent, STOP and escalate via Decision Point Protocol.

## Success Criteria

The workflow is successful if:

- ✅ A fresh agent can resume work safely at any point (state is explicit)
- ✅ Long-running execution is impossible by design (one task at a time)
- ✅ Tests mechanically drive design (evidence proves red→green)
- ✅ Stopping is the default outcome, not failure (verification gates)
- ✅ Shortcuts are structurally impossible (scripts enforce, not guidelines)
