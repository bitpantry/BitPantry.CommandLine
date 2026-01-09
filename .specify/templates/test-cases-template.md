# Test Cases: {Feature Name}

> Generated from spec.md, plan.md, data-model.md, and contracts/
> Each test case defines a single "when X, then Y" validation requirement

## User Experience Validation

Test cases validating user-facing behavior from user stories and functional requirements.

| ID | When (Trigger/Action) | Then (Expected Result) | Source |
|----|----------------------|------------------------|--------|
| UX-001 | {user action or state} | {observable outcome} | {US-xxx, FR-xxx} |

## Component/Unit Validation

Test cases validating internal components, services, and architectural elements defined in the plan.

| ID | Component | When (Input/State) | Then (Expected Behavior) | Source |
|----|-----------|-------------------|-------------------------|--------|
| CV-001 | {ComponentName} | {input or precondition} | {return value, state change, or exception} | {plan.md: section, FR-xxx} |

## Data Flow Validation

Test cases validating data transformations, state transitions, and cross-component interactions.

| ID | Flow | When (Condition) | Then (State/Data Change) | Source |
|----|------|-----------------|-------------------------|--------|
| DF-001 | {FlowName} | {trigger condition} | {expected data state} | {data-model.md: entity, FR-xxx} |

## Error Handling Validation

Test cases validating error conditions, exception handling, and recovery behaviors.

| ID | Scenario | When (Error Condition) | Then (Recovery/Message) | Source |
|----|----------|----------------------|------------------------|--------|
| EH-001 | {error scenario} | {trigger condition} | {expected error handling} | {FR-xxx, Edge Cases} |

---

## Source Reference Guide

The **Source** column supports flexible references to trace test cases back to their origin:

- **User Stories**: `US-001`, `US-002`, etc.
- **Functional Requirements**: `FR-001`, `FR-003`, etc.
- **Plan Components**: `plan.md: SignalRServerProxy`, `plan.md: FileTransferService`
- **Data Model**: `data-model.md: UploadProgress`, `data-model.md: FileMetadata`
- **Contracts**: `contracts/upload.yaml: POST /upload`
- **Edge Cases**: `spec.md: Edge Cases` (reference the edge cases table)
- **Multiple Sources**: `US-001, FR-003` (comma-separated)

## Generation Guidelines

1. **Analyze all sources**: User stories, functional requirements, edge cases table, architectural components, data model entities, and API contracts
2. **Cover all levels**: Don't just test user-facing behavior—include deep architectural components that need validation
3. **Be specific**: "Then Y" should be a concrete, testable assertion, not vague ("works correctly")
4. **One behavior per row**: Each test case validates exactly one "when X, then Y" relationship
5. **Scale with complexity**: More complex features need more test cases; don't artificially limit
6. **Include negative cases**: Error handling section should cover all failure modes from edge cases table

---

## Test Implementation Guidance

When implementing tests for these cases:

**The "Then" Column Is Your Assertion**:
- The "Then" text must translate directly to your test assertion
- Example: "Then: only 4 uploads active simultaneously" → observe and assert actual concurrency count
- Example: "Then: error message displays path" → capture console output and verify it contains the path

**Observability Requirement**:
- If the "Then" describes behavior that's not externally observable, the implementation must expose it for testing
- Options: return values, output parameters, events, observable state, injected recorders
- Don't write fake tests because behavior is "internal" - make it observable or test at integration level

**Invalid Test Implementations**:
- ❌ Testing that constants have expected values (proves nothing about runtime behavior)
- ❌ Testing that input strings contain expected characters (tests the input, not the processing)
- ❌ Testing that code compiles or types exist (the compiler already verifies this)

**Valid Test Implementations**:
- ✅ Execute the code path described in "When" and verify the outcome described in "Then"
- ✅ Use mocks to verify interactions when direct observation isn't possible
- ✅ Use real fixtures (temp files, test servers) for integration-level behaviors
