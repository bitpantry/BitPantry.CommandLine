---
description: Execute the implementation planning workflow using the plan template to generate design artifacts.
handoffs: 
  - label: Create Tasks
    agent: speckit.tasks
    prompt: Break the plan into tasks
    send: true
  - label: Create Checklist
    agent: speckit.checklist
    prompt: Create a checklist for the following domain...
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. **Setup**: Run `.specify/scripts/powershell/setup-plan.ps1 -Json` from repo root and parse JSON for FEATURE_SPEC, IMPL_PLAN, SPECS_DIR, BRANCH. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

2. **Load context**: Read FEATURE_SPEC and `.specify/memory/constitution.md`. Load IMPL_PLAN template (already copied).

3. **Execute plan workflow**: Follow the structure in IMPL_PLAN template to:
   - Fill Technical Context (mark unknowns as "NEEDS CLARIFICATION")
   - Fill Constitution Check section from constitution
   - Evaluate gates (ERROR if violations unjustified)
   - Phase 0: Generate research.md (resolve all NEEDS CLARIFICATION)
   - Phase 1: Generate data-model.md, contracts/, quickstart.md
   - Phase 1: Update agent context by running the agent script
   - Phase 2: Generate test-cases.md (comprehensive test case definitions)
   - Re-evaluate Constitution Check post-design

4. **Stop and report**: Command ends after Phase 2 planning. Report branch, IMPL_PLAN path, and generated artifacts.

## Phases

### Phase 0: Outline & Research

1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:

   ```text
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

### Phase 1: Design & Contracts

**Prerequisites:** `research.md` complete

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Agent context update**:
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType claude`
   - These scripts detect which AI agent is in use
   - Update the appropriate agent-specific context file
   - Add only new technology from current plan
   - Preserve manual additions between markers

**Output**: data-model.md, /contracts/*, quickstart.md, agent-specific file

### Phase 2: Test Case Generation

**Prerequisites:** Phase 1 complete (design artifacts exist)

Generate comprehensive test cases by analyzing ALL available artifacts:

1. **Load all design context**:
   - spec.md: User stories, functional requirements, edge cases table
   - plan.md: Architectural components, services, technical design
   - data-model.md: Entities, validation rules, state transitions
   - contracts/: API endpoints, request/response schemas

2. **Generate test cases at all levels**:

   **User Experience Validation** (from user stories + functional requirements):
   - For each user story acceptance criterion → UX test case
   - For each functional requirement → UX test case(s)
   - Format: "When {user action}, then {observable outcome}"

   **Component/Unit Validation** (from plan.md architecture):
   - For each service/component in Technical Design → component test cases
   - For each method with non-trivial logic → unit test cases
   - Include validation logic, edge cases, boundary conditions
   - Format: "When {ComponentName} receives {input}, then {expected behavior}"

   **Data Flow Validation** (from data-model.md + plan.md):
   - For each entity state transition → data flow test case
   - For each cross-component interaction → integration test case
   - Format: "When {trigger}, then {data/state change occurs}"

   **Error Handling Validation** (from edge cases + requirements):
   - For each edge case in spec.md table → error test case
   - For each component that can fail → failure mode test cases
   - Format: "When {error condition}, then {recovery/message}"

3. **Assign unique IDs** with category prefix:
   - UX-001, UX-002... (User Experience)
   - CV-001, CV-002... (Component Validation)
   - DF-001, DF-002... (Data Flow)
   - EH-001, EH-002... (Error Handling)

4. **Link to sources**: Each test case references its origin (US-xxx, FR-xxx, plan.md: ComponentName, etc.)

5. **Copy template and populate**:
   - Copy `.specify/templates/test-cases-template.md` to FEATURE_DIR/test-cases.md
   - Fill all four sections with generated test cases
   - Scale with feature complexity (no artificial limits)

**Output**: test-cases.md with comprehensive "when X, then Y" test cases

## Key rules

- Use absolute paths
- ERROR on gate failures or unresolved clarifications
