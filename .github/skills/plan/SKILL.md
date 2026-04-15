---
name: plan
description: "Create a technical implementation plan from a specification. Use when: planning implementation, designing architecture, creating data models, defining API contracts, technical decision-making."
argument-hint: "Spec number (e.g., 006)"
---

# Implementation Planning

Create a technical implementation plan from a feature specification, aligned with project conventions and existing architecture.

## When to Use

- After `/clarify` resolves ambiguities
- When ready to make technical decisions
- Before `/issues` to establish architecture and design

## Next Steps

After completing planning:
- `/issues` â€” Break down spec and plan into GitHub issues

## Procedure

### Step 0: Validate Inputs

The user **must** supply a **spec number** (e.g., `006`). If not provided, **ask the user** and **STOP**. Do not guess or assume.

Locate the spec directory: `specs/{NNN}-*/` matching the supplied number. If no matching directory exists, inform the user and **STOP**.

### Step 1: Load Context

1. **Read the spec**: Load `specs/{NNN}-{name}/spec.md`

2. **Read project conventions and instructions**:
   - `.github/copilot-instructions.md` â€” tech stack, project structure, build/test commands
   - `.github/instructions/` â€” all instruction files (coding conventions, testing approach, test infrastructure, etc.)
   - `.github/skills/tdd-workflow/SKILL.md` â€” TDD workflow (skim for test planning)
   - `.github/skills/create-issue/SKILL.md` â€” issue body structure (skim so plan artifacts align with downstream issue creation)

3. **Understand the existing codebase** (concrete reads, not a skim):
   - List the main source directory structure (recursive, 2 levels deep)
   - Identify and read domain model / entity files
   - Identify and read service interfaces or classes
   - Identify and read repository interfaces or classes
   - Read the application entry point and dependency registration
   - List the test directory structure and read existing test fixtures or helpers
   - Read the most recent 1-2 specs' `plan.md` files for established architectural patterns

### Step 2: Research (if needed)

If the spec has unknowns or the feature involves unfamiliar technology:

1. Identify unknowns from the spec and technical context
2. Research each unknown â€” best practices, patterns, library choices
3. Document decisions in `specs/{NNN}-{name}/research.md`:

   ```markdown
   ## Decision: [what was chosen]
   **Rationale**: [why chosen]
   **Alternatives considered**: [what else was evaluated]
   ```

If no research is needed, skip this step.

### Step 3: Design

1. **Data Model** â€” If the feature introduces or modifies entities, create `specs/{NNN}-{name}/data-model.md`:
   - Entity name, fields, types, relationships
   - Validation rules from functional requirements
   - State transitions if applicable
   - Relationship to existing entities

2. **API Contracts** â€” If the feature introduces endpoints, create files in `specs/{NNN}-{name}/contracts/`:
   - For each user action â†’ endpoint
   - Request/response schemas
   - Error responses
   - Use patterns consistent with existing endpoints

3. **Testing Strategy** â€” Plan test approach aligned with project instructions:
   - Map each user story to required test types (unit, integration, contract)
   - Identify which test fixtures and helpers to reuse (reference `test-infrastructure` instructions)
   - Reference the `tdd-workflow` skill â€” implementation will follow red-green-refactor
   - Plan test file locations per existing project conventions

### Step 4: Write the Plan

Load the [plan template](./templates/plan-template.md) and write to `specs/{NNN}-{name}/plan.md`.

The plan must include:

1. **Summary** â€” What the feature does and the technical approach
2. **Technical Context** â€” Tech stack, dependencies, constraints (from project instructions, not guessed)
3. **Project Structure** â€” Where new files will live within the existing project layout
4. **Data Model** â€” Reference to data-model.md if created
5. **API Contracts** â€” Reference to contracts/ if created
6. **Testing Strategy** â€” Test types, fixtures, file locations, aligned with project testing instructions
7. **Phasing** â€” Logical order of implementation (what must come first, what can be parallelized)

### Step 4b: Quality Gate

Run the plan through the checklist in [plan quality criteria](./references/plan-quality-criteria.md). For each item:

- **REQUIRED items** that fail: fix the plan before proceeding. Do NOT report completion with unresolved required-item failures.
- **Optional items** that fail: note them in the completion report as deferred.

Verify these minimum gates pass before proceeding:
1. Tech stack matches project instructions (not guessed)
2. Every user story has a clear implementation path
3. Testing strategy maps every user story to test types
4. Phases are ordered by dependency with no circular dependencies

### Step 5: Report Completion

Output:
- Path to plan.md
- Generated artifacts list (research.md, data-model.md, contracts/, etc.)
- Any unresolved items
- Suggested next command: `/issues`
