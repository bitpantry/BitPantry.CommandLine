# Execution Plan: 012 — Client File Access

**Spec**: `specs/012-client-file-access/spec.md`
**Plan**: `specs/012-client-file-access/plan.md`
**Date**: 2026-04-15

## Dependency Graph

```
Level 1 (independent):  001  002  004
                          \   |   /
Level 2:                  003  005
                            \  /
Level 3:                    006
                             |
Level 4:                    007
                             |
Level 5:                    008
```

## Execution Levels

### Level 1 — Core Types & Foundation (3 parallel tracks)

| Issue | Title | Blocked By | Est. Complexity |
|-------|-------|-----------|-----------------|
| 001 | Core types: IClientFileAccess, ClientFile, local impl | — | Medium |
| 002 | Protocol message envelopes | — | Low |
| 004 | Consent policy and --allow-path | — | Medium |

**Max parallelism**: 3 — all issues are independent.

### Level 2 — Server & Client Implementations

| Issue | Title | Blocked By | Est. Complexity |
|-------|-------|-----------|-----------------|
| 003 | RemoteClientFileAccess server impl | 001, 002 | High |
| 005 | Client handler and consent UX | 002, 004 | High |

**Max parallelism**: 2 — 003 and 005 are independent of each other.

### Level 3 — Integration Round-Trip

| Issue | Title | Blocked By | Est. Complexity |
|-------|-------|-----------|-----------------|
| 006 | End-to-end integration: single file | 003, 005 | High |

**Max parallelism**: 1 — requires both server and client implementations.

### Level 4 — Glob Pattern Support

| Issue | Title | Blocked By | Est. Complexity |
|-------|-------|-----------|------------------|
| 007 | Glob pattern support: GetFilesAsync | 006 | High |

### Level 5 — Hardening

| Issue | Title | Blocked By | Est. Complexity |
|-------|-------|-----------|------------------|
| 008 | Edge cases and hardening | 006, 007 | Medium |

## Critical Path

```
001 → 003 → 006 → 007 → 008
```

5 sequential steps across 5 levels. Issues 002 and 004 can execute in parallel with 001 but are not on the critical path.

## Issue Summary

| # | Title | Phase | Blocked By |
|---|-------|-------|-----------|
| 001 | Core types: IClientFileAccess, ClientFile, local impl | 1 | — |
| 002 | Protocol message envelopes | 1 | — |
| 003 | RemoteClientFileAccess server impl | 2 | 001, 002 |
| 004 | Consent policy and --allow-path | 1 | — |
| 005 | Client handler and consent UX | 2 | 002, 004 |
| 006 | End-to-end integration: single file | 3 | 003, 005 |
| 007 | Glob pattern support: GetFilesAsync | 4 | 006 |
| 008 | Edge cases and hardening | 4 | 006, 007 |

## Recommended Execution Order

1. Start 001, 002, 004 in parallel
2. When 001 + 002 complete → start 003
3. When 002 + 004 complete → start 005
4. When 003 + 005 complete → start 006
5. When 006 completes → start 007
6. When 007 completes → start 008
