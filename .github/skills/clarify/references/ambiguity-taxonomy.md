# Ambiguity Taxonomy

Categories for scanning specifications for underspecified areas.

## Detection Categories

### 1. Functional Scope & Behavior
- Core user goals & success criteria
- Explicit out-of-scope declarations
- User roles / personas differentiation

### 2. Domain & Data Model
- Entities, attributes, relationships
- Identity & uniqueness rules
- Lifecycle/state transitions
- Data volume / scale assumptions

> **Spec-level focus**: Do the right entities exist? Are identity/uniqueness rules defined as behavioral requirements? Defer field-level design, schema details, and interface definitions to `/plan`.

### 3. Interaction & UX Flow
- Critical user journeys / sequences
- Error/empty/loading states
- Accessibility or localization notes

### 4. Non-Functional Quality Attributes
- Performance (latency, throughput targets)
- Scalability (horizontal/vertical, limits)
- Reliability & availability (uptime, recovery expectations)
- Observability (logging, metrics, tracing signals)
- Security & privacy (authN/Z, data protection, threat assumptions)
- Compliance / regulatory constraints (if any)

### 5. Integration & External Dependencies
- External services/APIs and failure modes
- Data import/export formats
- Protocol/versioning assumptions

### 6. Edge Cases & Failure Handling
- Negative scenarios
- Rate limiting / throttling
- Conflict resolution (e.g., concurrent edits)

### 7. Constraints & Tradeoffs
- Technical constraints (language, storage, hosting)
- Explicit tradeoffs or rejected alternatives

### 8. Terminology & Consistency
- Canonical glossary terms
- Avoided synonyms / deprecated terms

### 9. Completion Signals
- Acceptance criteria testability
- Measurable Definition of Done indicators

### 10. Misc / Placeholders
- TODO markers / unresolved decisions
- Ambiguous adjectives ("robust", "intuitive") lacking quantification

## Status Levels

| Status | Meaning |
|--------|---------|
| **Clear** | Sufficiently specified, no action needed |
| **Partial** | Some information present but incomplete |
| **Missing** | No information, requires clarification |

## Prioritization Heuristic

When many categories need clarification, prioritize by:
`Impact Ã— Uncertainty`

Priority order:
1. Security posture
2. Functional scope
3. Data model
4. Non-functional requirements
5. Edge cases
6. Integration points
7. Terminology
