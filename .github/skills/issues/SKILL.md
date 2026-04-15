---
name: issues
description: "Break down a spec and plan into appropriately-sized GitHub issues, staged as local markdown files. Use when: creating work items from a spec, decomposing a plan into issues, preparing issues for GitHub."
argument-hint: "Spec number (e.g., 006)"
---

# Issue Staging

Break down a feature specification and implementation plan into GitHub-ready issues, staged as local markdown files for review before publishing.

## When to Use

- After `/plan` completes the implementation plan
- When ready to decompose work into implementable units
- Before `/analyze` to validate coverage

## Next Steps

After staging issues:
- `/analyze` â€” Validate completeness against spec and plan
- `/publish-issues` â€” Push staged issues to GitHub

## Procedure

### Step 0: Validate Inputs

The user **must** supply a **spec number** (e.g., `006`). If not provided, **ask the user** and **STOP**. Do not guess or assume.

Locate the spec directory: `specs/{NNN}-*/` matching the supplied number. Verify both `spec.md` and `plan.md` exist. If either is missing, inform the user which is missing and **STOP**.

### Step 1: Load Context

1. **Read spec artifacts**:
   - `specs/{NNN}-{name}/spec.md` â€” user stories, functional requirements, edge cases
   - `specs/{NNN}-{name}/plan.md` â€” architecture, phasing, testing strategy
   - `specs/{NNN}-{name}/data-model.md` â€” entities (if exists)
   - `specs/{NNN}-{name}/contracts/` â€” API specs (if exists)
   - `specs/{NNN}-{name}/research.md` â€” decisions (if exists)

2. **Read project conventions**:
   - `.github/copilot-instructions.md` â€” tech stack, project structure
   - `.github/instructions/` â€” coding conventions, testing approach
   - `.github/skills/create-issue/SKILL.md` â€” issue body structure and writing guidelines
   - `.github/skills/tdd-workflow/SKILL.md` â€” TDD workflow (for test sections in issues)
   - `.github/skills/work-issue/SKILL.md` â€” understand what the implementer will work with

### Step 2: Decompose into Issues

Group the work into issues following these principles:

1. **Right-sizing** â€” Each issue MUST be completable in a single PR. See [issue sizing guide](./references/issue-sizing-guide.md).

2. **Issue types**:
   - **Setup/Infrastructure issues** â€” Project scaffolding, shared dependencies, database migrations
   - **Feature issues** â€” One or more closely-related user stories that form a coherent unit of work
   - **Cross-cutting issues** â€” Testing improvements, documentation, polish

3. **Dependency management**:
   - Identify which issues block others
   - Issues MUST be independent unless they share a data-layer prerequisite (migration, entity, repository). If creating a dependency chain longer than 2 levels, justify each dependency in the Prerequisites section
   - Group tightly-coupled requirements into the same issue rather than creating unnecessary dependencies

4. **Each issue must include**:
   - Clear requirements traceable to spec (reference FR-IDs and US-IDs)
   - Prerequisites section listing blocking issues (by staging number)
   - Testing requirements aligned with project testing instructions
   - Implementation guidance referencing the plan
   - Implementer autonomy section (from `create-issue` skill pattern)

### Step 3: Present the Issue Plan

Before writing any files, present a summary of planned issues:

```markdown
## Planned Issues for Spec {NNN}

| # | Title | Type | User Stories | Prerequisites | Est. Complexity |
|---|-------|------|-------------|---------------|-----------------|
| 001 | Project scaffolding and base setup | Setup | â€” | None | Low |
| 002 | User authentication endpoints | Feature | US-001 | 001 | Medium |
| 003 | File upload service | Feature | US-002 | 001 | High |
| 004 | Notification system | Feature | US-003 | 002 | Medium |
```

Ask the user: "Create these {N} issues? Adjust anything?"

### Step 4: Write Staged Issue Files

On confirmation, create the `specs/{NNN}-{name}/issues/` directory and write:

1. **`000-tracking.md`** â€” The tracking/epic issue using the [tracking issue template](./templates/tracking-issue-template.md)

2. **`execution-plan.md`** â€” The execution plan document using the [execution plan template](./templates/execution-plan-template.md). This is a local-only document (not published to GitHub) that describes:
   - The implementation order with rationale for why each issue comes when it does
   - Dependency graph showing which issues block which
   - Parallelization opportunities â€” which issues can be worked simultaneously once their prerequisites are met
   - Critical path â€” the longest sequential chain that determines minimum total implementation time

3. **`001-{slug}.md`, `002-{slug}.md`, etc.** â€” Individual issues using the [issue template](./templates/issue-template.md)

Each issue file must:
- Follow the `create-issue` body structure (Summary, Current/Expected Behavior, Requirements, Implementation Guidance, Implementer Autonomy, Testing Requirements)
- Reference spec and plan documents: `See specs/{NNN}-{name}/spec.md` and `specs/{NNN}-{name}/plan.md`
- List prerequisites as: `Blocked by: 001, 003` (staging numbers, resolved to real `#N` during publish)
- Include the spec tag: `Labels: enhancement, spec-{NNN}`
- Map functional requirements by ID (e.g., "Implements FR-001, FR-002")
- Map user stories by ID (e.g., "Covers US-001")

### Step 5: Report Completion

Output:
- Path to issues directory
- Count of issues created (plus tracking issue)
- Issue list with titles and staging numbers
- Dependency summary (which issues block which)
- Suggested next command: `/analyze`
