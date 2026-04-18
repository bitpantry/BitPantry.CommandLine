---
name: clarify
description: "Reduce spec ambiguity via targeted clarification questions. Use when: clarifying spec, resolving ambiguity, refining requirements, addressing NEEDS CLARIFICATION markers, checking feasibility."
argument-hint: "Spec number (e.g., 006)"
---

# Specification Clarification

Identify underspecified areas in a feature spec, check for conflicts with existing functionality, and walk the user through interactive resolution.

## When to Use

- After `/spec` creates the initial spec
- When spec contains `[NEEDS CLARIFICATION]` markers
- Before `/plan` to ensure requirements are clear
- When requirements feel ambiguous or incomplete

## Next Steps

After completing clarification:
- `/plan` — Create a technical implementation plan

## Procedure

### Step 0: Validate Inputs

The user **must** supply a **spec number** (e.g., `006`). If not provided, **ask the user** and **STOP**. Do not guess or assume.

Locate the spec directory: `specs/{NNN}-*/` matching the supplied number. If no matching directory exists, inform the user and **STOP**.

### Step 1: Load Context

1. **Read the spec**: Load `specs/{NNN}-{name}/spec.md`

2. **Read project instructions** to understand technical conventions:
   - `.github/copilot-instructions.md`
   - `.github/instructions/` files (testing, code conventions, etc.)

3. **Build technical understanding** of the current application (concrete reads, not a skim):
   - List the main source directory structure (recursive, 2 levels deep)
   - Identify and read domain model / entity files
   - Identify and read service interfaces or classes
   - Identify and read repository interfaces or classes
   - Read the application entry point and dependency registration
   - Review other spec files in `specs/` to understand what features exist

   This enables detection of conflicts or disruptions the new feature might cause to existing functionality.

### Step 2: Scan for Ambiguities

Perform a structured ambiguity scan using the [ambiguity taxonomy](./references/ambiguity-taxonomy.md). For each detection category, assess the spec and mark status: **Clear** / **Partial** / **Missing**.

Additionally, perform a **technical feasibility and conflict scan**:
- For each user story, assess whether it overlaps with or could disrupt existing features
- Identify cases where new behavior might change existing behavior (intentionally or accidentally)
- Flag any user stories that assume capabilities the system doesn't currently have (this is informational — not a blocker)

### Step 3: Generate Clarification Items

Build a prioritized list of clarification items (no maximum, but prioritize by impact). Each item must have:
- **Index**: Sequential number
- **Title**: Brief label
- **Description**: 1-2 sentences on what's ambiguous or concerning
- **Severity**: CRITICAL / HIGH / MEDIUM / LOW
- **Category**: Which ambiguity taxonomy category it falls under, or "Conflict" for existing-feature disruptions

Apply these constraints:
- Only include items whose answers materially impact architecture, data modeling, task decomposition, test design, UX behavior, or operational readiness
- Exclude items already answered in the spec or better deferred to the planning phase
- Include any detected conflicts with existing features as CRITICAL or HIGH severity items

**Planning-phase filter**: Exclude items that ask *how* to implement rather than *what* to implement. Examples of planning concerns to exclude: service interface design, database schema details, implementation ordering/phasing, specific technology choices (unless they affect user-visible behavior), internal data flow, performance optimization strategies. These belong in `/plan`.

### Step 4: Present Summary Table

Output the full list as a table:

```markdown
| # | Title | Severity | Category | Description |
|---|-------|----------|----------|-------------|
| 1 | Auth method unspecified | CRITICAL | Functional Scope | No auth approach defined for the new endpoint |
| 2 | Overlaps with existing download | HIGH | Conflict | US-002 may break existing file download behavior |
```

Ask the user: "I found {N} items to clarify. Walk through them now?"

### Step 5: Interactive Walkthrough

Present items **one at a time**, ordered by severity (CRITICAL first, then HIGH, MEDIUM, LOW).

For each item, output:

```
--- Item {X} of {N} ---
**[Title]** [{Severity}]

[2-3 sentence description of the problem and why it matters]

**Recommended resolution:** [Specific suggestion based on best practices, existing application patterns, and project conventions]
```

Wait for user response. Accept:
- A direct answer or clarification
- "accept" or "yes" to take the recommendation
- "skip" to defer the item
- "done" to stop early

Record each response internally. **Do NOT edit the spec yet.**

### Step 6: Batch Update

After all items are addressed (or user says "done"):

1. Present a summary of decisions:

   ```markdown
   | # | Title | Decision |
   |---|-------|----------|
   | 1 | Auth method unspecified | Use JWT bearer tokens (accepted recommendation) |
   | 2 | Overlaps with existing download | User confirmed: new endpoint replaces old one |
   | 3 | Rate limiting | Skipped — defer to planning |
   ```

2. Ask: "Apply these changes to the spec?"

3. On confirmation, update the spec:
   - Add a `## Clarifications` section (if not present) with a `### Session [DATE]` subheading
   - Log each Q&A pair: `- **[Title]**: [Decision]`
   - Update the relevant spec sections inline:
     - Functional ambiguity → Functional Requirements / User Stories
     - Data shape → Key Entities
     - Edge case → Edge Cases section
     - Conflict resolution → Affected user story's acceptance scenarios
     - Terminology → Normalize across the entire spec
   - Remove resolved `[NEEDS CLARIFICATION]` markers

### Step 7: Report Completion

Output:
- Number of items addressed vs. skipped
- Path to updated spec
- Sections modified
- Any remaining `[NEEDS CLARIFICATION]` markers
- Suggested next command: `/plan` or another `/clarify` pass if items were skipped
