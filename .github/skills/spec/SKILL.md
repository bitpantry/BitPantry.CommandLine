---
name: spec
description: "Create a structured feature specification from a natural language description. Use when: starting a new feature, creating a spec, specifying requirements, defining user stories, writing acceptance criteria."
argument-hint: "Spec number and short name (e.g., 006 user-notifications), plus a description of the feature"
---

# Feature Specification

Create a feature specification document from a natural language description, contextualized within the existing application.

## When to Use

- Starting a new feature development
- Creating a feature specification document
- Converting a feature idea into structured requirements
- Defining user stories and acceptance criteria

## Next Steps

After completing the specification:
- `/clarify` — Identify and resolve ambiguities in the spec
- `/plan` — Create a technical implementation plan

## Procedure

### Step 0: Validate Inputs

The user **must** supply:
1. A **spec number** (e.g., `006`)
2. A **short name** (e.g., `user-notifications`)
3. A **feature description** (natural language)

If any of these are missing, **ask the user** and **STOP**. Do not guess or assume.

### Step 1: Understand the Current Application

Before writing any spec, build a functional understanding of what the application does today. This ensures user stories fit naturally into the existing system.

1. **Read project-level context**:
   - `.github/copilot-instructions.md` — tech stack, project structure, commands
   - `.github/instructions/` — coding conventions, testing approach

2. **Read the source code structure** (concrete reads, not a skim):
   - List the main source directory structure (recursive, 2 levels deep)
   - Identify and read domain model / entity files
   - Identify and read service interfaces or classes
   - Identify and read repository interfaces or classes
   - Read the application entry point and dependency registration
   - Identify existing features, services, and data entities from the files read above

3. **Review existing specs**:
   - List the `specs/` directory to see what features have been specified and implemented
   - Read the most recent 1-2 spec files to understand the application's functional scope

4. **Build a mental model** of:
   - What the application does (its core purpose)
   - What entities and services exist
   - What user-facing capabilities are already implemented
   - What infrastructure is in place (auth, storage, logging, etc.)

### Step 2: Create the Spec Branch and Directory

1. **Create and check out a spec branch**:
   - Branch name: `spec/{NNN}-{short-name}` (e.g., `spec/006-translation-management`)
   - Base: the repository's default branch (typically `main`)
   - Run: `git checkout -b spec/{NNN}-{short-name}`
   - If the branch already exists (resuming work), check it out instead: `git checkout spec/{NNN}-{short-name}`

2. **Create the spec directory**: `specs/{NNN}-{short-name}/`

All spec-phase work (`/spec`, `/clarify`, `/plan`, `/issues`, `/publish-issues`) happens on this branch. Implementation branches created to work a spec-labeled issue should also branch from this spec branch, and the resulting PRs should target the spec branch. The spec branch is not automatically opened as a PR to or merged into the repository default branch; that final integration is a separate, explicit user-directed step after the spec's implementation work is complete and stabilized.

### Step 3: Load the Spec Template

Read the [spec template](./templates/spec-template.md) to understand required sections.

### Step 4: Execute the Spec Generation Flow

> **Boundary rule — what vs. how**: The spec defines *what* the system should do (behavior, rules, constraints, expected outcomes) — not *how* it's implemented (service interfaces, API contracts, class designs, storage key formats, internal workflows). Technical design belongs in `/plan`. Do not add sections beyond the template structure. If a behavioral description requires domain-specific detail (e.g., validation rules, command syntax), describe the *observable behavior*, not the implementation mechanism.

1. **Parse user description** from input
   - If empty: ERROR "No feature description provided"

2. **Extract key concepts** from description
   - Identify: actors, actions, data, constraints
   - Cross-reference with existing application capabilities from Step 1

3. **For unclear aspects**:
   - Make informed guesses based on context, existing application patterns, and industry standards
   - Only mark with `[NEEDS CLARIFICATION: specific question]` if:
     - The choice significantly impacts feature scope or user experience
     - Multiple reasonable interpretations exist with different implications
     - No reasonable default exists
   - **LIMIT: Maximum 3 `[NEEDS CLARIFICATION]` markers total**
   - Prioritize: scope > security/privacy > user experience > technical details

4. **Fill User Stories section**
   - If no clear user flow: ERROR "Cannot determine user scenarios"
   - Each user story must be independently testable
   - Assign user story IDs: US-001, US-002, US-003, etc.
   - For each user story, define acceptance scenarios using Given/When/Then format
   - Include both happy path and exception/error scenarios

5. **Generate Functional Requirements**
   - Each requirement must be testable
   - Assign requirement IDs: FR-001, FR-002, FR-003, etc.
   - Map each requirement to one or more user stories
   - Use reasonable defaults for unspecified details (document in Assumptions section)

   Do not specify internal implementation details (storage key formats, cache strategies, internal service calls). Describe the observable behavior, not the mechanism.

6. **Define Edge Cases**
   - Consider boundary conditions, error scenarios, concurrent access
   - Reference how similar edge cases are handled in existing features

7. **Identify Key Entities** (if data involved)
   - List entities by name, describe what they represent, and note key relationships
   - Use 1-2 sentences per entity — conceptual level only
   - Do NOT specify field names, types, interface methods, or implementation patterns — those are defined in `/plan`'s `data-model.md`
   - Do NOT add extra sections for interfaces, handlers, contracts, or implementation details

### Step 5: Write the Specification

Write the specification to `specs/{NNN}-{short-name}/spec.md` using the template structure. Replace all placeholders with concrete details while preserving section order and headings.

### Step 6: Validation Checkpoint

Before reporting completion, verify all of the following. If any check fails, fix the spec before proceeding:

1. Every US-ID has at least one Given/When/Then acceptance scenario
2. Every FR-ID maps to at least one US-ID in the User Stories column
3. The `[NEEDS CLARIFICATION]` count is ≤ 3
4. No sections beyond the template structure have been added
5. Key Entities section (if present) contains only conceptual descriptions, no field-level detail

### Step 7: Report Completion

Output:
- Path to generated spec.md
- Summary of user stories created (with IDs)
- Count of functional requirements
- Any `[NEEDS CLARIFICATION]` markers that remain
- Suggested next command: `/clarify`