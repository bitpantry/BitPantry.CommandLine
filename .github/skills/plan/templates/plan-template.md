# Implementation Plan: [FEATURE NAME]

**Spec**: `[NNN]-[short-name]` | **Date**: [DATE]
**Input**: `specs/[NNN]-[short-name]/spec.md`

## Summary

[1-2 sentences: what the feature does and the high-level technical approach]

## Technical Context

<!--
  Fill from the project's .github/copilot-instructions.md and existing codebase.
  Do not guess — use what's documented and what exists in the project.
-->

**Runtime**: [from project instructions]
**Framework**: [from project instructions]
**Key Dependencies**: [existing + any new ones needed for this feature]
**Storage**: [if applicable]
**Testing**: [framework and approach from project instructions]

## Project Structure

### New/Modified Files

```text
src/
├── [Project]/
│   ├── [new or modified files]
│   └── ...

tests/
├── [TestProject]/
│   ├── [new test files]
│   └── ...
```

### Documentation

```text
specs/[NNN]-[short-name]/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Research decisions (if applicable)
├── data-model.md        # Entity definitions (if applicable)
└── contracts/           # API contracts (if applicable)
```

## Data Model

<!-- Reference data-model.md if entities are involved, otherwise remove this section -->

See [data-model.md](./data-model.md) for entity definitions.

## API Contracts

<!-- Reference contracts/ if endpoints are involved, otherwise remove this section -->

See [contracts/](./contracts/) for endpoint specifications.

## Testing Strategy

<!--
  Align with the project's testing instructions and infrastructure.
  Reference existing test fixtures and helpers by name.
-->

| User Story | Test Type | Test Location | Fixtures/Helpers |
|-----------|-----------|---------------|------------------|
| US-001 | [Unit/Integration/Contract] | [test file path] | [fixture names] |
| US-002 | [Unit/Integration/Contract] | [test file path] | [fixture names] |

**TDD Approach**: Follow the `tdd-workflow` skill — write failing tests first (RED), implement to pass (GREEN), refactor.

## Deployment Plan

<!-- Reference deployment.md if deployment steps are needed, otherwise state "No deployment steps beyond standard build/run." -->

See [deployment.md](./deployment.md) for local development and production deployment requirements.

## Implementation Phases

<!--
  Define the logical order of implementation.
  Each phase should produce a testable increment.
  Note dependencies and parallelization opportunities.
-->

### Phase 1: [Name]
- **Purpose**: [what this phase delivers]
- **Dependencies**: None (foundational)
- **Delivers**: [testable outcome]

### Phase 2: [Name]
- **Purpose**: [what this phase delivers]
- **Dependencies**: Phase 1
- **Delivers**: [testable outcome]

### Phase 3: [Name]
- **Purpose**: [what this phase delivers]
- **Dependencies**: [Phase N]
- **Parallel with**: [Phase M, if applicable]
- **Delivers**: [testable outcome]
