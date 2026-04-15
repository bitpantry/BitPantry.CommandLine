# Analysis Checks

Detection algorithms for validating staged issues against spec, plan, and project guidelines.

## A. FR Coverage

**Goal**: Every functional requirement in the spec is covered by at least one issue.

**Process**:
1. Extract all FR-IDs from spec.md (e.g., FR-001, FR-002, ...)
2. For each staged issue, extract the "Implements" line and parse FR-IDs
3. Build a coverage map: FR-ID â†’ [issue staging numbers]
4. Flag any FR-ID with an empty list

**Severity**: CRITICAL for MUST requirements, HIGH for SHOULD requirements

## B. US Coverage

**Goal**: Every user story in the spec is covered by at least one issue.

**Process**:
1. Extract all US-IDs from spec.md (e.g., US-001, US-002, ...)
2. For each staged issue, extract the "Covers" line and parse US-IDs
3. Build a coverage map: US-ID â†’ [issue staging numbers]
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
2. Build a directed graph: issue â†’ [blocking issues]
3. Run topological sort (Kahn's algorithm or DFS-based)
4. If sort fails â†’ cycle exists. Report the cycle.

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
   - Verify test level makes sense for the work (services â†’ unit, endpoints â†’ integration)
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
2. Parse the execution plan's level tables â€” extract each issue's staging number and assigned level.
3. Parse "Blocked by" lines from all staged issue files to build the declared dependency map.
4. **Completeness**: Every staged issue (excluding 000-tracking.md and execution-plan.md) must appear in the execution plan, and every entry in the execution plan must correspond to a staged issue file. Flag mismatches in either direction.
5. **Level-prerequisite consistency**: For each issue at level N, verify that every issue listed in its "Blocked by" line is assigned to a level strictly less than N. If a prerequisite is at the same level or a higher level, flag it â€” the issue cannot be started before its dependency completes.
6. **Implicit ordering conflicts**: For each issue at level N, verify there is no issue at level N-1 or lower that declares it in a "Blocked by" line (i.e., a lower-level issue depending on a higher-level issue would be a contradiction). Flag any such reverse dependencies.
7. **Dependency graph alignment**: Walk the full dependency graph derived from "Blocked by" lines. Compute the minimum valid level for each issue (longest path from any root). Compare to the assigned level in the execution plan. Flag any issue whose assigned level is less than its minimum valid level (meaning it's placed too early). Issues placed at a level higher than the minimum are acceptable (they may be intentionally deferred).
8. **Critical path accuracy** (if present): If the execution plan includes a "Critical Path" section, verify the claimed longest chain by computing the actual longest path through the dependency graph. Flag if the stated critical path length or sequence is incorrect.

**Severity**:
- CRITICAL: Issue placed before its prerequisites (wrong level), prerequisite not in plan, reverse dependency
- HIGH: Staged issue missing from execution plan or plan entry with no matching staged file
- MEDIUM: Critical path section inaccurate

## Analysis Order

Run in this order (fail-fast on critical structural issues):
1. E â€” Dangling References (fast structural check)
2. D â€” Dependency DAG (structural integrity)
3. J â€” Execution Plan Validity (ordering and level consistency)
4. F â€” Issue Structure (format compliance)
5. A â€” FR Coverage (content completeness)
6. B â€” US Coverage (content completeness)
7. C â€” Traceability (reference integrity)
8. H â€” Tracking Issue (completeness)
9. G â€” Testing Alignment (convention compliance)
10. I â€” Edge Case Coverage (thoroughness)
