---
description: Generate an actionable, dependency-ordered tasks.md for the feature based on available design artifacts. Tasks are atomic Micro-TDD units.
handoffs: 
  - label: Analyze Workflow
    agent: speckit.analyze
    prompt: Validate task format and test case coverage
    send: true
  - label: Create Batches
    agent: speckit.batch
    prompt: Create task batches for bounded execution
    send: true
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. **Setup**: Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json` from repo root and parse FEATURE_DIR and AVAILABLE_DOCS list. All paths must be absolute. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

2. **Load design documents**: Read from FEATURE_DIR:
   - **Required**: plan.md (tech stack, libraries, structure), spec.md (user stories with priorities), test-cases.md (validation requirements)
   - **Optional**: data-model.md (entities), contracts/ (API endpoints), research.md (decisions), quickstart.md (test scenarios)
   - Note: Not all projects have all documents. Generate tasks based on what's available.

3. **Execute task generation workflow**:
   - Load plan.md and extract tech stack, libraries, project structure
   - Load spec.md and extract user stories with their priorities (P1, P2, P3, etc.)
   - Load test-cases.md and extract all test case IDs (UX-xxx, CV-xxx, DF-xxx, EH-xxx)
   - If data-model.md exists: Extract entities and map to user stories
   - If contracts/ exists: Map endpoints to user stories
   - If research.md exists: Extract decisions for setup tasks
   - Generate tasks organized by user story (see Task Generation Rules below)
   - Generate test tasks that reference specific test case IDs from test-cases.md
   - Generate dependency graph showing user story completion order
   - Create parallel execution examples per user story
   - Validate task completeness (each user story has all needed tasks, independently testable)
   - Validate test coverage (every test case ID has exactly one corresponding test task)

4. **Generate tasks.md**: Use `.specify/templates/tasks-template.md` as structure, fill with:
   - Correct feature name from plan.md
   - Phase 1: Setup tasks (project initialization)
   - Phase 2: Foundational tasks (blocking prerequisites for all user stories)
   - Phase 3+: One phase per user story (in priority order from spec.md)
   - Each phase includes: story goal, independent test criteria, tests (if requested), implementation tasks
   - Final Phase: Polish & cross-cutting concerns
   - All tasks must follow the strict checklist format (see Task Generation Rules below)
   - Clear file paths for each task
   - Dependencies section showing story completion order
   - Parallel execution examples per story
   - Implementation strategy section (MVP first, incremental delivery)

5. **Report**: Output path to generated tasks.md and summary:
   - Total task count
   - Task count per user story
   - Test task count and test case coverage (X of Y test cases mapped to tasks)
   - Parallel opportunities identified
   - Independent test criteria for each story
   - Suggested MVP scope (typically just User Story 1)
   - Format validation: Confirm ALL tasks follow the checklist format (checkbox, ID, labels, file paths)

Context for task generation: $ARGUMENTS

The tasks.md should be immediately executable - each task must be specific enough that an LLM can complete it without additional context. Do not generate tasks as optiona or deferrable. The final task list should represent a committed unit of work to be implemented in full.

## Task Generation Rules

**CRITICAL**: Each task is an ATOMIC Micro-TDD unit — ONE test case, ONE behavioral change.

**Tests are not OPTIONAL**: The constitution enforces strict TDD. Every task includes both test and implementation as a single behavioral unit.

**Test cases drive tasks**: Each task implements exactly ONE test case from test-cases.md. The task IS the behavioral unit (test + implementation together).

### Checklist Format (REQUIRED)

Every task MUST strictly follow this format:

```text
- [ ] T### [depends:T###,T###] @test-case:XX-### Description with file path
```

**Format Components**:

1. **Checkbox**: ALWAYS start with `- [ ]` (markdown checkbox)
2. **Task ID**: Sequential number (T001, T002, T003...) globally unique
3. **Dependencies**: `[depends:T001,T002]` — tasks that must complete first (optional, omit if none)
4. **Test Case**: `@test-case:UX-001` — REQUIRED, exactly ONE test case ID from test-cases.md
5. **Description**: Clear action with exact file path

**Examples**:

- ✅ CORRECT: `- [ ] T001 @test-case:UX-001 Implement single file download in FileTransferService.cs`
- ✅ CORRECT: `- [ ] T005 [depends:T001] @test-case:UX-002 Add glob pattern support to DownloadCommand.cs`
- ✅ CORRECT: `- [ ] T012 [depends:T005,T006] @test-case:CV-001 Validate path traversal prevention in PathValidator.cs`
- ❌ WRONG: `- [ ] T001 Create download command` (missing @test-case)
- ❌ WRONG: `- [ ] T001 @test-case:UX-001,UX-002 Multiple behaviors` (multiple test cases — split into separate tasks)
- ❌ WRONG: `- [ ] T001 [P] @test-case:UX-001 Description` ([P] marker is obsolete — use [depends:] instead)

**IMPORTANT**: The old `[P]` (parallel) and `[US#]` (user story) markers are REMOVED. Dependencies are explicit via `[depends:]`. Story organization is informational only.

### Task Sizing Rules

Each task must be **Micro-TDD sized**:

1. **ONE test case** — maps to exactly one test case ID from test-cases.md
2. **ONE behavioral change** — implements one "When X, Then Y" from the test case
3. **ONE red→green cycle** — agent writes test, sees it fail, implements, sees it pass

If a task seems to need multiple tests, it's too large. Split it.

### Task Organization

1. **From User Stories (spec.md)** - GROUP by story for readability:
   - Each user story (P1, P2, P3...) is documented as a section
   - Tasks within a story section implement that story's test cases
   - Stories are informational grouping — dependencies are explicit via `[depends:]`

2. **From Test Cases (test-cases.md)** - ONE TASK per test case:
   - Each test case ID (UX-xxx, CV-xxx, DF-xxx, EH-xxx) becomes exactly ONE task
   - The task's `@test-case:` reference is the test case ID
   - No bundling multiple test cases into one task
   - No test case without a corresponding task

3. **Dependencies via `[depends:]`**:
   - If task B requires task A's implementation, use `[depends:T001]`
   - Multiple dependencies: `[depends:T001,T002,T003]`
   - Dependencies are verified before task becomes eligible for execution

4. **From Data Model**:
   - Each entity/model that needs implementation gets tasks
   - Map entities to the test cases that require them
   - Use dependencies to ensure models exist before services that use them

5. **From Contracts**:
   - Each endpoint/API contract maps to test cases
   - Implementation tasks depend on model/service tasks

6. **Setup/Infrastructure** (no test case required):
   - Project setup tasks (create files, configure dependencies) may use `@test-case:SETUP-###`
   - These are the only tasks without behavioral test cases
   - Keep setup tasks minimal — prefer tasks with real test cases

### Output Structure

The generated tasks.md should have:

1. **Header**: Feature name, prerequisites, format reference
2. **Setup Section**: Infrastructure tasks (if any)
3. **User Story Sections**: Grouped by story for readability
   - Each task has explicit `@test-case:` and `[depends:]`
4. **Summary**: Total tasks, test case coverage validation

### Validation Before Output

Before finalizing tasks.md, verify:

- [ ] Every test case from test-cases.md has exactly ONE task
- [ ] Every task (except setup) has exactly ONE `@test-case:` reference
- [ ] Dependencies form a valid DAG (no circular dependencies)
- [ ] Task IDs are sequential and unique (T001, T002, ...)
- [ ] Each task has a clear file path in description
