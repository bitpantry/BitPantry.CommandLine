---
description: Perform a non-destructive cross-artifact consistency and quality analysis across spec.md, plan.md, and tasks.md after task generation.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Goal

Identify inconsistencies, duplications, ambiguities, and underspecified items across the three core artifacts (`spec.md`, `plan.md`, `tasks.md`) before implementation. This command MUST run only after `/speckit.tasks` has successfully produced a complete `tasks.md`.

## Operating Constraints

**ANALYSIS IS READ-ONLY**: The analysis phase (Steps 1-7) does **not** modify any files. Output a structured analysis report first. Remediation (Step 8) may modify files only after explicit user approval through the interactive workflow.

**Constitution Authority**: The project constitution (`.specify/memory/constitution.md`) is **non-negotiable** within this analysis scope. Constitution conflicts are automatically CRITICAL and require adjustment of the spec, plan, or tasks—not dilution, reinterpretation, or silent ignoring of the principle. If a principle itself needs to change, that must occur in a separate, explicit constitution update outside `/speckit.analyze`.

## Execution Steps

### 1. Initialize Analysis Context

Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` once from repo root and parse JSON for FEATURE_DIR and AVAILABLE_DOCS. Derive absolute paths:

- SPEC = FEATURE_DIR/spec.md
- PLAN = FEATURE_DIR/plan.md
- TASKS = FEATURE_DIR/tasks.md
- TEST_CASES = FEATURE_DIR/test-cases.md (if exists)

Abort with an error message if any required file is missing (instruct the user to run missing prerequisite command).
For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

### 2. Load Artifacts (Progressive Disclosure)

Load only the minimal necessary context from each artifact:

**From spec.md:**

- Overview/Context
- Functional Requirements
- Non-Functional Requirements
- User Stories
- Edge Cases (if present)

**From plan.md:**

- Architecture/stack choices
- Data Model references
- Phases
- Technical constraints

**From tasks.md:**

- Task IDs
- Descriptions
- Phase grouping
- Parallel markers [P]
- Referenced file paths
- Test case ID references (implements UX-xxx, CV-xxx, etc.)

**From test-cases.md (if exists):**

- Test case IDs (UX-xxx, CV-xxx, DF-xxx, EH-xxx)
- When/Then definitions
- Source references (US-xxx, FR-xxx, plan components)

**From constitution:**

- Load `.specify/memory/constitution.md` for principle validation

### 3. Build Semantic Models

Create internal representations (do not include raw artifacts in output):

- **Requirements inventory**: Each functional + non-functional requirement with a stable key (derive slug based on imperative phrase; e.g., "User can upload file" → `user-can-upload-file`)
- **User story/action inventory**: Discrete user actions with acceptance criteria
- **Test case inventory**: Each test case ID with its When/Then definition and source reference
- **Task coverage mapping**: Map each task to one or more requirements, stories, or test cases (inference by keyword / explicit reference patterns like IDs or key phrases)
- **Test case coverage mapping**: Map each test case to its implementing task(s)
- **Constitution rule set**: Extract principle names and MUST/SHOULD normative statements

### 4. Detection Passes (Token-Efficient Analysis)

Focus on high-signal findings. Limit to 50 findings total; aggregate remainder in overflow summary.

#### A. Duplication Detection

- Identify near-duplicate requirements
- Mark lower-quality phrasing for consolidation

#### B. Ambiguity Detection

- Flag vague adjectives (fast, scalable, secure, intuitive, robust) lacking measurable criteria
- Flag unresolved placeholders (TODO, TKTK, ???, `<placeholder>`, etc.)

#### C. Underspecification

- Requirements with verbs but missing object or measurable outcome
- User stories missing acceptance criteria alignment
- Tasks referencing files or components not defined in spec/plan

#### D. Constitution Alignment

- Any requirement or plan element conflicting with a MUST principle
- Missing mandated sections or quality gates from constitution

#### E. Coverage Gaps

- Requirements with zero associated tasks
- Tasks with no mapped requirement/story
- Non-functional requirements not reflected in tasks (e.g., performance, security)

#### F. Test Case Coverage (if test-cases.md exists)

**Spec → Test Case (completeness):**
- User stories with zero test cases referencing them
- Functional requirements with zero test cases referencing them
- Edge case table rows with no corresponding EH-xxx test case
- Architectural components in plan.md Technical Design with no CV-xxx test cases

**Test Case → Task (implementation):**
- Test cases with zero implementing tasks (orphaned test cases)
- Tasks claiming to implement test cases that don't exist
- Test cases with vague "Then" assertions (not specific/testable)
- Missing test case categories (e.g., no error handling test cases for features with edge cases)

#### G. Inconsistency

- Terminology drift (same concept named differently across files)
- Data entities referenced in plan but absent in spec (or vice versa)
- Task ordering contradictions (e.g., integration tasks before foundational setup tasks without dependency note)
- Conflicting requirements (e.g., one requires Next.js while other specifies Vue)

#### H. Workflow Integrity (Micro-TDD Validation)

Run `.specify/scripts/powershell/analyze-workflow.ps1 -Json` to validate workflow state:

**Task Format Compliance:**
- Every task must match format: `- [ ] T### [depends:T###,T###] @test-case:XX-### Description`
- Task IDs must be sequential (T001, T002, T003...)
- Dependencies must reference valid task IDs
- Each task (except SETUP-###) must have exactly ONE `@test-case:` reference
- The old `[P]` and `[US#]` markers must NOT appear (obsolete)

**Test Case Coverage:**
- Every test case ID in test-cases.md must have exactly ONE corresponding task
- No test case should be referenced by multiple tasks
- No task should reference multiple test cases (except SETUP-### tasks)

**Dependency Acyclicity:**
- Dependencies must form a valid Directed Acyclic Graph (DAG)
- No circular dependencies (T001 → T002 → T001)
- Report cycle paths if detected

**Batch Integrity (if batches exist):**
- Batch files in `batches/` directory must contain valid task references
- Batch task order must respect dependency constraints
- Each batch should have 10-15 tasks
- Tasks should not appear in multiple batches

**State Consistency (if batch-state.json exists):**
- Active batch must reference existing batch file
- Current task must be in active batch
- Task phases must be valid (started → red → green → verified)
- No task in later phase without evidence for earlier phases

**Evidence Completeness (if evidence/ directory exists):**
- Each completed task must have evidence file at `evidence/T###.json`
- Evidence must contain: phase, taskId, red section, green section
- Red section must show test failure output
- Green section must show test passing output
- Diff section should show implementation changes

**Sequence Integrity:**
- Tasks marked complete must have been verified
- Tasks in "green" phase must have evidence of red phase first
- No task should be marked complete without both red and green evidence

### 5. Severity Assignment

Use this heuristic to prioritize findings:

- **CRITICAL**: Violates constitution MUST, missing core spec artifact, requirement with zero coverage that blocks baseline functionality, test-cases.md missing when TDD mandated, circular dependencies in tasks, task format violations, missing evidence for completed tasks
- **HIGH**: Duplicate or conflicting requirement, ambiguous security/performance attribute, untestable acceptance criterion, test cases with no implementing tasks, user stories or functional requirements with no test cases, batch-state.json inconsistencies, multiple tasks referencing same test case
- **MEDIUM**: Terminology drift, missing non-functional task coverage, underspecified edge case, vague test case assertions, edge cases or components without test cases, batch sizing issues (too few or too many tasks)
- **LOW**: Style/wording improvements, minor redundancy not affecting execution order, evidence format warnings

### 6. Produce Compact Analysis Report

Output a Markdown report (no file writes) with the following structure:

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| A1 | Duplication | HIGH | spec.md:L120-134 | Two similar requirements ... | Merge phrasing; keep clearer version |

(Add one row per finding; generate stable IDs prefixed by category initial.)

**Coverage Summary Table:**

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|

**Spec → Test Case Coverage Table (if test-cases.md exists):**

| Spec Item | Type | Has Test Case? | Test Case IDs | Notes |
|-----------|------|----------------|---------------|-------|

(Lists user stories, functional requirements, edge cases, and plan components with their test case coverage)

**Test Case → Task Coverage Table (if test-cases.md exists):**

| Test Case ID | Category | Has Task? | Implementing Task IDs | Notes |
|--------------|----------|-----------|----------------------|-------|

**Constitution Alignment Issues:** (if any)

**Unmapped Tasks:** (if any)

**Uncovered Spec Items:** (user stories, requirements, edge cases without test cases)

**Orphaned Test Cases:** (test cases with no implementing task)

**Metrics:**

- Total Requirements
- Total Tasks
- Coverage % (requirements with >=1 task)
- Total Test Cases (if test-cases.md exists)
- Spec Item Test Coverage % (spec items with >=1 test case)
- Test Case Task Coverage % (test cases with >=1 implementing task)
- Ambiguity Count
- Duplication Count
- Critical Issues Count

**Workflow Integrity (if batches exist):**

| Check | Status | Details |
|-------|--------|---------|
| Task Format Compliance | ✅/❌ | X of Y tasks valid |
| Dependency DAG | ✅/❌ | Acyclic / Cycles detected |
| Batch Sizing | ✅/❌ | Batches in 10-15 range |
| State Consistency | ✅/❌ | batch-state.json valid |
| Evidence Completeness | ✅/❌ | X of Y completed tasks have evidence |
| Sequence Integrity | ✅/❌ | All red→green sequences valid |

### 7. Provide Next Actions

At end of report, output a concise Next Actions block:

- If CRITICAL issues exist: Recommend resolving before `/speckit.batch`
- If only LOW/MEDIUM: User may proceed, but provide improvement suggestions
- Provide explicit command suggestions:
  - For spec issues: "Run /speckit.specify with refinement"
  - For plan issues: "Run /speckit.plan to adjust architecture"
  - For task coverage: "Manually edit tasks.md to add coverage for 'performance-metrics'"
  - For task format issues: "Run /speckit.tasks to regenerate with correct format"
  - For workflow state issues: "Run /speckit.recover to diagnose and fix state"
  - For batch issues: "Run /speckit.batch to create or advance batches"

### 8. Interactive Remediation Workflow

After presenting the analysis report, walk the user through remediation decisions using a **sequential question, batched execution** approach:

#### Phase A: Decision Collection (One at a Time)

For each finding (ordered by severity: CRITICAL → HIGH → MEDIUM → LOW):

1. **Present ONE finding at a time** with:
   - Finding ID, category, severity
   - Location(s) and summary
   - **TOP RECOMMENDATION**: Your suggested remediation with brief reasoning
   - Alternative options (if applicable) as a table:

   | Option | Description |
   |--------|-------------|
   | A | <Recommended action> |
   | B | <Alternative approach> |
   | Skip | Leave unchanged for now |

2. **Wait for user response** before presenting the next finding
   - User can reply with option letter, "yes"/"recommended" to accept suggestion, or "skip"
   - If ambiguous, ask for quick clarification (does not count as new finding)

3. **Store decision in memory** - do NOT apply changes yet

4. **Early termination signals**: If user says "done", "stop", "apply now", or "skip remaining":
   - Stop presenting further findings
   - Proceed to Phase B with decisions collected so far

#### Phase B: Decision Summary

After all findings are addressed (or early termination):

1. Present a **Decision Summary Table**:

   | Finding ID | Severity | Decision | Action |
   |------------|----------|----------|--------|
   | A1 | HIGH | Option A | Merge requirements in spec.md |
   | C2 | CRITICAL | Recommended | Add missing task for FR-005 |
   | D3 | MEDIUM | Skip | No change |

2. Show count: "X changes to apply, Y skipped"

3. Ask: **"Apply these changes now? (yes/no)"**
   - If "no": End without changes, suggest running again later
   - If "yes": Proceed to Phase C

#### Phase C: Batch Execution

Apply all collected decisions in a single batch:

1. Group changes by file (spec.md, plan.md, tasks.md)
2. Apply all changes to each file
3. Report completion:
   - Files modified
   - Changes applied per file
   - Any changes that could not be applied (with reason)

**Behavior Rules**:
- Never apply changes during Phase A (collection only)
- Present findings in severity order (CRITICAL first)
- Maximum 20 findings in interactive mode; if more exist, ask "Continue with remaining N findings?" after each batch of 20
- Respect user's pace - do not rush or combine findings
- If zero issues found, skip this workflow entirely (report success only)

## Operating Principles

### Context Efficiency

- **Minimal high-signal tokens**: Focus on actionable findings, not exhaustive documentation
- **Progressive disclosure**: Load artifacts incrementally; don't dump all content into analysis
- **Token-efficient output**: Limit findings table to 50 rows; summarize overflow
- **Deterministic results**: Rerunning without changes should produce consistent IDs and counts

### Analysis Guidelines

- **NEVER modify files** (this is read-only analysis)
- **NEVER hallucinate missing sections** (if absent, report them accurately)
- **Prioritize constitution violations** (these are always CRITICAL)
- **Use examples over exhaustive rules** (cite specific instances, not generic patterns)
- **Report zero issues gracefully** (emit success report with coverage statistics)

## Context

$ARGUMENTS
