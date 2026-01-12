---

description: "Task list template for feature implementation - Micro-TDD format"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), test-cases.md (required), data-model.md, contracts/

**Micro-TDD Format**: Each task is an atomic behavioral unit — ONE test case, ONE red→green cycle.

**Organization**: Tasks are grouped by user story for readability. Dependencies are explicit via `[depends:]`.

## Task Format

```text
- [ ] T### [depends:T###,T###] @test-case:XX-### Description with file path
```

**Components**:
- **Checkbox**: `- [ ]` (markdown checkbox, marked when completed)
- **Task ID**: Sequential (T001, T002, T003...) — globally unique
- **Dependencies**: `[depends:T001,T002]` — tasks that must complete first (omit if none)
- **Test Case**: `@test-case:UX-001` — REQUIRED, exactly ONE test case ID from test-cases.md
- **Description**: Clear action with exact file path

**Task Sizing**: Each task = ONE test case = ONE behavioral change = ONE red→green cycle

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure

<!-- 
  ============================================================================
  IMPORTANT: The tasks below are SAMPLE TASKS for illustration purposes only.
  
  The /speckit.tasks command MUST replace these with actual tasks based on:
  - Test cases from test-cases.md (each test case → ONE task)
  - User stories from spec.md (for grouping/readability)
  - Feature requirements from plan.md
  - Entities from data-model.md
  - Endpoints from contracts/
  
  Every task MUST have exactly ONE @test-case: reference.
  Dependencies MUST be explicit via [depends:].
  
  DO NOT keep these sample tasks in the generated tasks.md file.
  ============================================================================
-->

## Phase 1: Setup (Infrastructure)

**Purpose**: Project initialization and basic structure — no behavioral test cases

- [ ] T001 @test-case:SETUP-001 Create project structure per implementation plan
- [ ] T002 [depends:T001] @test-case:SETUP-002 Initialize [language] project with [framework] dependencies
- [ ] T003 [depends:T001] @test-case:SETUP-003 Configure linting and formatting tools

---

## Phase 2: User Story 1 - [Title] (Priority: P1) 🎯 MVP

**Goal**: [Brief description of what this story delivers]

**Test Cases**: UX-001 through UX-003, CV-001, EH-001

### Tasks

- [ ] T004 [depends:T002] @test-case:UX-001 Implement basic [action] in src/services/[service].py
- [ ] T005 [depends:T004] @test-case:UX-002 Add [secondary behavior] to [service].py
- [ ] T006 [depends:T004] @test-case:CV-001 Validate [input] in src/validators/[validator].py
- [ ] T007 [depends:T004] @test-case:EH-001 Handle [error condition] in [service].py
- [ ] T008 [depends:T005,T006,T007] @test-case:UX-003 Integrate [feature] endpoint in src/api/[endpoint].py

**Checkpoint**: User Story 1 complete — all tests pass, feature independently usable

---

## Phase 3: User Story 2 - [Title] (Priority: P2)

**Goal**: [Brief description of what this story delivers]

**Test Cases**: UX-004 through UX-006, DF-001

### Tasks

- [ ] T009 [depends:T002] @test-case:UX-004 Implement [action] in src/services/[service2].py
- [ ] T010 [depends:T009] @test-case:UX-005 Add [behavior] to [service2].py
- [ ] T011 [depends:T009] @test-case:DF-001 Connect [service2] to [data store]
- [ ] T012 [depends:T010,T011] @test-case:UX-006 Expose [feature] via endpoint

**Checkpoint**: User Story 2 complete — can work independently of Story 1

---

## Phase 4: User Story 3 - [Title] (Priority: P3)

**Goal**: [Brief description of what this story delivers]

**Test Cases**: UX-007, CV-002, EH-002

### Tasks

- [ ] T013 [depends:T002] @test-case:UX-007 Implement [action] in src/services/[service3].py
- [ ] T014 [depends:T013] @test-case:CV-002 Validate [complex input] in [validator].py
- [ ] T015 [depends:T013] @test-case:EH-002 Handle [edge case] gracefully

**Checkpoint**: User Story 3 complete — all three stories independently functional

---

[Add more user story phases as needed, following the same pattern]

---

## Phase N: Integration & Cross-Cutting

**Purpose**: Cross-story integration that wasn't covered in individual stories

- [ ] TXXX [depends:T008,T012,T015] @test-case:DF-002 Integration between Story 1 and Story 2
- [ ] TXXX [depends:TXXX] @test-case:CV-003 Cross-cutting validation rules

**Purpose**: Improvements that affect multiple user stories

- [ ] TXXX [P] Documentation updates in docs/
- [ ] TXXX Code cleanup and refactoring
- [ ] TXXX Performance optimization across all stories
- [ ] TXXX [P] Additional unit tests (if requested) in tests/unit/
- [ ] TXXX Security hardening
- [ ] TXXX Run quickstart.md validation

---

## Dependencies & Execution Order

### Dependency Resolution

Dependencies are explicit in each task via `[depends:T###]`. The workflow engine:

1. Parses all tasks and builds a dependency graph (DAG)
2. Topologically sorts tasks to determine execution order
3. A task is only eligible when all its dependencies are completed
4. Batches group eligible tasks for bounded execution (10-15 per batch)

### Batch Execution Model

- **Batch 1**: Setup tasks + initial story tasks without dependencies
- **Batch 2**: Tasks whose dependencies were satisfied in Batch 1
- **Batch N**: Continues until all tasks complete

Each batch is:
- Created by `/speckit.batch`
- Executed via `/speckit.execute` (one task at a time)
- Verified via `/speckit.verify` (mandatory gate)

### Within Each Task (Micro-TDD Cycle)

1. **Write Test**: Create test for the @test-case behavior
2. **Red Phase**: Run test, capture failure output as evidence
3. **Implement**: Write minimal code to make test pass
4. **Green Phase**: Run test, capture success output as evidence
5. **Verify**: Script validates evidence before task completes

### Story Independence

- User stories are grouped for readability only
- Actual execution order comes from `[depends:]` constraints
- Each story's tasks can interleave with others if dependencies allow
- Checkpoints mark when a story's tasks are all complete

---

## Batch Example

Given these tasks:
```text
- [ ] T001 @test-case:SETUP-001 Create project structure
- [ ] T002 [depends:T001] @test-case:SETUP-002 Add dependencies
- [ ] T003 [depends:T001] @test-case:UX-001 Implement feature A
- [ ] T004 [depends:T002,T003] @test-case:UX-002 Integrate A with deps
```

Batch creation produces:
- **Batch 1**: T001 (no dependencies)
- **Batch 2**: T002, T003 (both depend only on T001)
- **Batch 3**: T004 (depends on T002 and T003)

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Setup tasks (batch includes T001-T003)
2. Complete User Story 1 tasks
3. **STOP and VALIDATE**: All Story 1 tests pass
4. Deploy/demo if ready — remaining stories are additive

### Incremental Delivery

1. Setup → Story 1 → Deploy/Demo (MVP!)
2. Add Story 2 → Deploy/Demo
3. Add Story 3 → Deploy/Demo
4. Each increment adds value without breaking previous work

### Bounded Execution

- Each `/speckit.execute` processes ONE task
- Each `/speckit.verify` validates that ONE task
- Batches limit scope to 10-15 tasks
- Agent never sees future batches (prevents over-design)

---

## Test Case Validation

Before generating final tasks.md, verify:

- [ ] Every test case ID in test-cases.md has exactly ONE task
- [ ] Every task has exactly ONE @test-case: reference
- [ ] Dependencies form a valid DAG (no cycles)
- [ ] Task IDs are sequential and unique
- [ ] Each task has a clear file path

Run `/speckit.analyze` to validate before batching.

---

## Notes

- `[depends:]` replaces the old `[P]` parallel marker — dependencies are explicit
- Story labels (US1, US2) are REMOVED — use story sections for grouping
- Each task = ONE test case = ONE red→green cycle
- Evidence files capture red and green outputs for verification
- Commit after each verified task or batch completion
