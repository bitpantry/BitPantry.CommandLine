# Analysis Checks

Detection algorithms for validating staged issues against spec, plan, and project guidelines.

## A. FR Coverage

**Goal**: Every functional requirement in the spec is covered by at least one issue.

**Process**:
1. Extract all FR-IDs from spec.md (e.g., FR-001, FR-002, ...)
2. For each staged issue, extract the "Implements" line and parse FR-IDs
3. Build a coverage map: FR-ID → [issue staging numbers]
4. Flag any FR-ID with an empty list

**Severity**: CRITICAL for MUST requirements, HIGH for SHOULD requirements

## B. US Coverage

**Goal**: Every user story in the spec is covered by at least one issue.

**Process**:
1. Extract all US-IDs from spec.md (e.g., US-001, US-002, ...)
2. For each staged issue, extract the "Covers" line and parse US-IDs
3. Build a coverage map: US-ID → [issue staging numbers]
4. Flag any US-ID with an empty list

**Severity**: HIGH

## C. Traceability

**Goal**: Every issue traces back to valid spec items.

**Process**:
1. For each staged issue, extract "Implements" FR-IDs and "Covers" US-IDs
2. Verify each FR-ID exists in spec.md
3. Verify each US-ID exists in spec.md
4. Flag any references to non-existent IDs

**Severity**: HIGH (likely a typo or stale reference)

## D. Dependency DAG Validation

**Goal**: No circular dependencies between issues.

**Process**:
1. Parse "Blocked by" lines from all issues
2. Build a directed graph: issue → [blocking issues]
3. Run topological sort (Kahn's algorithm or DFS-based)
4. If sort fails → cycle exists. Report the cycle.

**Severity**: CRITICAL

## E. Dangling References

**Goal**: All prerequisite references point to real issues.

**Process**:
1. Collect all staging numbers from "Blocked by" lines
2. Verify each references a file that exists in the issues directory
3. Flag references to non-existent staging numbers

**Severity**: CRITICAL

## F. Issue Structure

**Goal**: Issues follow the expected body format.

**Required sections** (from create-issue skill pattern):
- Summary
- Requirements (with at least one item)
- Prerequisites (can be "None")
- Implementation Guidance
- Implementer Autonomy
- Testing Requirements

**Process**:
1. For each issue, parse markdown headings
2. Check for presence of each required section
3. Verify Requirements section has at least one checkboxed item
4. Verify Testing Requirements has a test approach and at least one test case

**Severity**: HIGH for missing Requirements or Testing; MEDIUM for others

## G. Testing Alignment

**Goal**: Testing sections reflect project testing conventions.

**Process**:
1. Read project testing instructions from `.github/instructions/`
2. For each issue's Testing Requirements:
   - Verify test level makes sense for the work (services → unit, endpoints → integration)
   - Verify test project paths reference actual projects in the workspace
   - Verify fixture names reference fixtures that exist in test utilities
3. Flag mismatches

**Severity**: MEDIUM

## H. Tracking Issue Completeness

**Goal**: The tracking issue lists every staged issue.

**Process**:
1. Collect all staged issue filenames (excluding 000-tracking.md)
2. Parse the tracking issue's checklist items
3. Verify every staging number appears in the tracking issue
4. Flag any missing

**Severity**: HIGH

## I. Edge Case Coverage

**Goal**: Spec edge cases are addressed somewhere.

**Process**:
1. Extract edge cases from spec.md
2. For each edge case, search all issue Requirements and Testing sections for related content
3. Flag edge cases with no apparent coverage

**Severity**: MEDIUM

## J. Execution Plan Validity

**Goal**: The execution plan accurately reflects the dependency structure and issues can be implemented in the described order without missing or misordered prerequisites.

**Process**:
1. Verify `execution-plan.md` exists in the issues directory. If missing, flag and skip remaining sub-checks.
2. Parse the execution plan's level tables — extract each issue's staging number and assigned level.
3. Parse "Blocked by" lines from all staged issue files to build the declared dependency map.
4. **Completeness**: Every staged issue (excluding 000-tracking.md and execution-plan.md) must appear in the execution plan, and every entry in the execution plan must correspond to a staged issue file. Flag mismatches in either direction.
5. **Level-prerequisite consistency**: For each issue at level N, verify that every issue listed in its "Blocked by" line is assigned to a level strictly less than N. If a prerequisite is at the same level or a higher level, flag it — the issue cannot be started before its dependency completes.
6. **Implicit ordering conflicts**: For each issue at level N, verify there is no issue at level N-1 or lower that declares it in a "Blocked by" line (i.e., a lower-level issue depending on a higher-level issue would be a contradiction). Flag any such reverse dependencies.
7. **Dependency graph alignment**: Walk the full dependency graph derived from "Blocked by" lines. Compute the minimum valid level for each issue (longest path from any root). Compare to the assigned level in the execution plan. Flag any issue whose assigned level is less than its minimum valid level (meaning it's placed too early). Issues placed at a level higher than the minimum are acceptable (they may be intentionally deferred).
8. **Critical path accuracy** (if present): If the execution plan includes a "Critical Path" section, verify the claimed longest chain by computing the actual longest path through the dependency graph. Flag if the stated critical path length or sequence is incorrect.

**Severity**:
- CRITICAL: Issue placed before its prerequisites (wrong level), prerequisite not in plan, reverse dependency
- HIGH: Staged issue missing from execution plan or plan entry with no matching staged file
- MEDIUM: Critical path section inaccurate

## K. Operation Surface Coverage

**Goal**: Every concrete operation surface and its important variants are explicitly represented in staged issue requirements or prescribed tests.

**Process**:
1. Extract operation surfaces from the spec, plan, and contracts:
   - endpoints
   - commands
   - import/export/validate flows
   - state transitions
2. For each operation, identify its concrete variants:
   - independently mutable fields for create/update flows
   - flags and optional arguments
   - success/failure branches called out in the spec
3. For each variant, search staged issue Requirements and Prescribed Test Cases for explicit coverage.
4. Flag any operation that is only covered by a generic verb like "update" or "validate" when the spec defines concrete sub-behaviors.

**Severity**:
- HIGH: Variant missing for a MUST requirement or user-visible behavior
- MEDIUM: Variant implied by guidance but not requirements/tests

## L. Invariant Propagation Coverage

**Goal**: Shared rules are covered for every entry point that can violate them.

**Process**:
1. Build an invariant inventory from the spec and plan.
2. For each invariant, list all entry points that can violate it if implemented incorrectly.
3. Search staged issues for explicit requirement or test coverage of each invariant-entry-point pair.
4. Flag invariants that appear only once even though multiple entry points exist.

Examples:
- Empty sets cannot become listed via any path
- Duplicate detection must apply to both single-item and bulk flows
- Content-hash changes must apply to every listed-content mutation path

**Severity**:
- HIGH: Invariant missing for one or more entry points
- MEDIUM: Invariant only mentioned in guidance, not requirements/tests

## M. Validation and Serialization Parity

**Goal**: Parallel flows that should share semantics explicitly require parity.

**Process**:
1. Identify flows that operate on the same logical data in different contexts:
   - validate vs import
   - single-item save vs bulk import
   - detail response vs export serialization
2. Verify at least one staged issue explicitly requires parity or shared semantics between those flows.
3. Verify at least one prescribed test exercises parity or cross-flow consistency when the risk is material.
4. Flag issues that discuss one flow only while the spec clearly requires matching behavior across multiple flows.

**Severity**:
- HIGH: Missing parity requirement for MUST behavior
- MEDIUM: Missing parity-focused prescribed test

## N. Prescribed Test Adequacy

**Goal**: Prescribed tests are specific enough to catch the important branches implied by the issue requirements.

**Process**:
1. For each issue, map each requirement bullet to at least one prescribed test case.
2. Verify the prescribed tests cover:
   - critical failure branches
   - independently mutable fields on update flows
   - invariant enforcement paths
   - parity paths when required
3. Flag issues whose tests only cover happy paths while requirements imply multiple branches.
4. Flag issues whose requirement text is more specific than any prescribed test.

**Severity**:
- HIGH: Requirement has no prescribed test at all
- MEDIUM: Only happy-path tests exist for branch-heavy behavior

## Analysis Order

Run in this order (fail-fast on critical structural issues):
1. E — Dangling References (fast structural check)
2. D — Dependency DAG (structural integrity)
3. J — Execution Plan Validity (ordering and level consistency)
4. F — Issue Structure (format compliance)
5. A — FR Coverage (content completeness)
6. B — US Coverage (content completeness)
7. C — Traceability (reference integrity)
8. H — Tracking Issue (completeness)
9. G — Testing Alignment (convention compliance)
10. I — Edge Case Coverage (thoroughness)
11. K — Operation Surface Coverage (behavior completeness)
12. L — Invariant Propagation Coverage (cross-path completeness)
13. M — Validation and Serialization Parity (semantic consistency)
14. N — Prescribed Test Adequacy (behavioral test coverage)