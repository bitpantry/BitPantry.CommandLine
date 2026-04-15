# Execution Plan: [FEATURE NAME]

**Spec**: `[NNN]-[short-name]`
**Date**: [DATE]

<!--
  This document describes the implementation order, dependencies, and
  parallelization opportunities for the staged issues. It is a local
  planning artifact â€” not published to GitHub.
-->

## Implementation Order

<!--
  List issues in recommended implementation sequence.
  For each, explain WHY it comes at this position (what it depends on,
  what it unblocks). Issues at the same level can be worked in parallel.
-->

### Level 1 â€” No prerequisites
| Issue | Title | Rationale |
|-------|-------|-----------|
| 001 | [title] | [why this is foundational â€” what it unblocks] |

### Level 2 â€” Requires Level 1
| Issue | Title | Rationale |
|-------|-------|-----------|
| 002 | [title] | Depends on 001 because [reason]. Unblocks [what]. |
| 003 | [title] | Depends on 001 because [reason]. Independent of 002. |

### Level 3 â€” Requires Level 2
| Issue | Title | Rationale |
|-------|-------|-----------|
| 004 | [title] | Depends on 002 because [reason]. |

## Dependency Graph

```
001 â”€â”€â”€â”€â–º 002 â”€â”€â”€â”€â–º 004
  â”‚
  â””â”€â”€â”€â”€â–º 003
```

## Parallelization Opportunities

<!--
  Identify groups of issues that can be worked simultaneously.
  This helps when multiple agents or developers are available.
-->

- **After 001 completes**: 002 and 003 can be worked in parallel
- **After 002 completes**: 004 can start (003 is independent)

## Critical Path

<!--
  The longest sequential dependency chain. This determines the minimum
  number of sequential steps to complete all issues, assuming unlimited
  parallelism for independent work.
-->

**Longest chain**: 001 â†’ 002 â†’ 004 (3 sequential steps)

**Minimum sequential steps**: [N]
**Total issues**: [N]
**Maximum parallelism**: [N issues at widest point]
