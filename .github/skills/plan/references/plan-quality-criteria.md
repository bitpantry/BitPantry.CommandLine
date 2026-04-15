# Plan Quality Criteria

Standards for evaluating implementation plan completeness.

## Required Artifacts

| Artifact | Required | Purpose |
|----------|----------|---------|
| plan.md | Always | Technical decisions, architecture, phasing |
| research.md | If unknowns exist | Decision rationale and alternatives |
| data-model.md | If data entities exist | Entity definitions and relationships |
| contracts/ | If API endpoints exist | Request/response specifications |

## Plan Completeness Checklist

### Technical Context
- [ ] Tech stack matches project instructions (not guessed)
- [ ] All new dependencies identified
- [ ] Project structure shows where new files go within existing layout
- [ ] No unresolved `NEEDS CLARIFICATION` markers

### Design Quality
- [ ] Each user story has a clear implementation path
- [ ] Data model covers all entities from the spec
- [ ] API contracts cover all user-facing actions
- [ ] Relationships to existing entities are defined

### Testing Strategy
- [ ] Every user story mapped to test types
- [ ] Test file locations follow existing project conventions
- [ ] Existing fixtures and helpers identified for reuse
- [ ] TDD workflow referenced

### Phasing
- [ ] Phases are ordered by dependency
- [ ] Each phase produces a testable increment
- [ ] Parallelization opportunities identified
- [ ] No circular dependencies between phases

## Readiness Gate

Plan is ready for `/issues` when:

1. All `NEEDS CLARIFICATION` markers resolved
2. Required artifacts generated
3. Testing strategy is complete
4. Phasing is defined with clear dependencies
