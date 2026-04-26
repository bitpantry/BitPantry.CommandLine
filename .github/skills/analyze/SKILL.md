---
name: analyze
description: "Validate staged issues against spec, plan, and project guidelines before publishing. Use when: reviewing staged issues for completeness, checking coverage, finding gaps before publishing to GitHub."
argument-hint: "Spec number (e.g., 006)"
---

# Issue Analysis

Perform a read-only consistency and coverage analysis across the spec, plan, and staged issues before publishing to GitHub.

## When to Use

- After `/issues` stages issue files
- Before `/publish-issues` to validate completeness
- When you want to verify nothing was missed

## Next Steps

After analysis:
- `/publish-issues` — Push validated issues to GitHub (if analysis passes)
- `/issues` — Re-stage issues to fix problems found

## Procedure

### Step 0: Validate Inputs

The user **must** supply a **spec number** (e.g., `006`). If not provided, **ask the user** and **STOP**. Do not guess or assume.

Locate the spec directory: `specs/{NNN}-*/`. Verify these exist:
- `spec.md` — the specification
- `plan.md` — the implementation plan
- `issues/` — the staged issues directory with at least `000-tracking.md` and one issue file

If any are missing, inform the user and **STOP**.

### Step 1: Load All Artifacts

1. **Spec artifacts**:
   - `spec.md` — user stories, functional requirements, edge cases
   - `plan.md` — architecture, phasing, testing strategy
   - `data-model.md` (if exists) — entities
   - `contracts/` (if exists) — API specs

2. **Staged issues**:
   - `issues/000-tracking.md` — tracking issue
   - `issues/001-*.md`, `issues/002-*.md`, etc. — all staged issues

3. **Project conventions**:
   - `.github/copilot-instructions.md` — tech stack, conventions
   - `.github/instructions/` — all instruction files (testing, coding, etc.)
   - `.github/skills/create-issue/SKILL.md` — issue body structure standards

### Step 2: Build Inventory

Create internal mappings:

- **Requirements inventory**: Every FR-ID from spec.md with description
- **User story inventory**: Every US-ID from spec.md with acceptance scenarios
- **Issue inventory**: Every staged issue with its Implements/Covers references, prerequisites, requirements
- **Edge case inventory**: Every edge case from spec.md
- **Operation surface inventory**: Every endpoint, command, import/export flow, state transition, and other delivery surface from spec.md, plan.md, and contracts/
- **Mutable field inventory**: Every independently mutable field on create/update flows described by the spec
- **Invariant inventory**: Every rule that must hold across multiple entry points
- **Validation parity inventory**: Every pair or family of flows that must share validation or serialization semantics
- **Prescribed test inventory**: Every staged test case row, mapped back to the behaviors it exercises

### Step 3: Run Analysis Checks

See [analysis checks](./references/analysis-checks.md) for the full detection algorithm.

Run these checks:

#### A. Coverage — Every FR maps to at least one issue
For each FR-ID in the spec, verify at least one staged issue references it in its "Implements" line. Flag uncovered FRs.

#### B. Coverage — Every US maps to at least one issue
For each US-ID in the spec, verify at least one staged issue references it in its "Covers" line. Flag uncovered user stories.

#### C. Traceability — Every issue traces back to the spec
For each staged issue, verify its "Implements" FR-IDs and "Covers" US-IDs actually exist in the spec. Flag orphaned references.

#### D. Dependency Validation — No circular dependencies
Parse the "Blocked by" lines in all issues. Verify the dependency graph is a valid DAG (directed acyclic graph). Flag any cycles.

#### E. Dependency Validation — All prerequisites reference valid issues
Every staging number in "Blocked by" lines must correspond to an actual staged issue file. Flag dangling references.

#### F. Structure — Issues follow the expected format
Each issue should have: Summary, Requirements, Prerequisites, Implementation Guidance, Implementer Autonomy, Testing Requirements. Flag issues missing required sections.

#### G. Testing — Testing sections align with project conventions
For each issue's Testing Requirements section, verify:
- Test levels match the type of work (unit for services, integration for endpoints, etc.)
- Test projects reference actual test project paths from the codebase
- Existing fixtures mentioned actually exist in the test utilities

#### H. Tracking Issue — Completeness
The tracking issue (000-tracking.md) must list every other staged issue. Flag any issues not listed in the tracker.

#### I. Edge Cases — Coverage
Verify that edge cases from the spec are addressed in at least one issue's requirements or testing section. Flag unaddressed edge cases.

#### J. Execution Plan — Validity
Validate the execution plan (`execution-plan.md`) to ensure issues can be implemented in the described order:
- The execution plan file exists in the issues directory
- Every staged issue appears in the execution plan and every plan entry maps to a staged issue
- Level assignments are consistent with "Blocked by" declarations — an issue must be at a strictly higher level than all of its prerequisites
- No issue is placed at a level that precedes any of its prerequisites (i.e., you cannot implement an issue before the issues it depends on)
- The dependency graph implied by the execution plan levels matches the "Blocked by" metadata in the staged issues — flag any conflicts where the plan ordering contradicts the declared dependencies

#### K. Operation Surface Coverage — Every operation variant is explicitly covered
For each operation surface in the inventory, verify the staged issues cover the concrete behavior variants, not just a generic verb. If an issue says "update" but does not mention a spec-defined mutable field or branch, flag it.

#### L. Invariant Propagation — Shared rules cover every violating entry point
For each invariant, verify every entry point that could violate it is covered by at least one staged issue requirement or prescribed test. Flag invariants that are only covered for one path when multiple paths exist.

#### M. Validation Parity — Shared semantics are explicitly required
For validate/import/save/export flows that should share semantics, verify at least one staged issue explicitly requires parity or shared validation behavior. Flag cases where parallel flows exist but issues only mention one of them or only prescribe structural checks.

#### N. Test Adequacy — Prescribed tests cover the required behavior shape
For each issue, verify its prescribed tests cover the highest-risk branches implied by its Requirements. Flag generic issue requirements that have no field-level, branch-level, or parity-focused tests.

### Step 4: Classify Findings

| Severity | Meaning | Examples |
|----------|---------|---------|
| **CRITICAL** | Must fix before publishing | Uncovered FR, circular dependency, missing required section, issue placed before its prerequisites in execution plan |
| **HIGH** | Should fix before publishing | Uncovered US, orphaned reference, missing testing section, staged issue missing from execution plan, invariant only covered for some entry points |
| **MEDIUM** | Fix unless deferring with stated rationale | Unaddressed edge case, test fixture not found, inaccurate critical path, generic requirement with no branch-level or parity-focused test |
| **LOW** | Nice to fix | Formatting inconsistency, verbose description |

### Step 5: Present Analysis Report

Output:

```markdown
## Issue Analysis Report — Spec {NNN}

**Issues analyzed**: {N}
**Findings**: {N} ({critical} critical, {high} high, {medium} medium, {low} low)

### Coverage Summary

| Item | Total | Covered | Uncovered |
|------|-------|---------|-----------|
| Functional Requirements | N | N | N |
| User Stories | N | N | N |
| Edge Cases | N | N | N |
| Operation Surfaces | N | N | N |
| Invariants | N | N | N |

### Findings

| # | Severity | Check | Description | Affected Issue(s) |
|---|----------|-------|-------------|-------------------|
| 1 | CRITICAL | Coverage | FR-003 not covered by any issue | — |
| 2 | HIGH | Structure | Issue 003 missing Testing Requirements section | 003 |
```

When reporting findings, prefer behavioral recommendations over mere ID bookkeeping. Example: "Issue 005 references FR-011, but it does not explicitly cover sort-order updates for item-update" is better than "FR-011 may be under-covered."

### Step 6: Guided Remediation

If there are CRITICAL or HIGH findings, walk the user through remediation:

1. Present items **one at a time**, CRITICAL first:

   ```
   --- Finding {X} of {N} ---
   **[Severity]** — [Description]

   **Recommendation:** [Specific fix — e.g., "Add FR-003 to issue 002's Requirements section"]
   ```

2. Wait for user input:
   - "accept" — apply the recommendation
   - "skip" — defer
   - Custom instructions

3. Collect all decisions, then ask: "Apply all changes now?"

4. On confirmation, batch-edit the staged issue files.

For MEDIUM findings, ask: "There are also {N} MEDIUM-severity items. Walk through those too?"

### Step 7: Report

Output:
- Final pass/fail status
- Remaining unresolved findings (if any)
- Suggested next command: `/publish-issues` (if clean) or "Fix the issues and run `/analyze` again"