---
description: Group tests by consolidation criteria to identify duplication and merge opportunities.
---

## User Input

```text
$ARGUMENTS
```

If empty, cluster all tests from the inventory.
If a project name is provided, cluster only tests from that project.

---

## Intent

Phase 2 of the Test Remediation workflow. Groups tests by consolidation criteria to identify:
- Tests that can be merged (same setup, same act, different assertions)
- Duplicate tests (same assertions)
- Infrastructure mismatches (same subject, different test infrastructure)

---

## Prerequisites

1. `refactor-temp/test-inventory.json` must exist (run `/test-remediation.inventory` first)
2. Read `refactor-temp/standards.md` for consolidation criteria

---

## Execution Steps

### Step 1: Load Inventory

Read `refactor-temp/test-inventory.json` and parse the test list.

### Step 2: Group by Subject

First, group tests by `subjectClass` + `subjectMethod`:

```
DownloadCommand.Execute:
  - T001, T002, T003, T010, T011...
  
UploadCommand.Execute:
  - T020, T021, T022...
  
FileTransferService.DownloadFile:
  - T050, T051, T052...
```

### Step 3: Sub-Group by Mock Setup

Within each subject group, sub-group by `mockSetup` signature:

```
DownloadCommand.Execute:
  Connected + Success:
    - T001, T002, T003 (same mocks, different assertions)
  Connected + Failure:
    - T010, T011 (file not found scenario)
  Disconnected:
    - T020, T021 (not connected scenario)
```

### Step 4: Identify Cluster Type

For each sub-group, determine consolidation potential:

| Cluster Type | Criteria | Consolidation? |
|--------------|----------|----------------|
| **Consolidation Candidate** | Same mockSetup + same actSignature + different assertions | YES |
| **Potential Duplicate** | Same mockSetup + same assertions | REVIEW |
| **Separate Scenarios** | Same subject, different mockSetup | NO |
| **Infrastructure Mismatch** | Same subject, different testInfrastructure | FLAG |

### Step 5: Generate Cluster ID

Assign cluster IDs: `C001`, `C002`, etc.

### Step 6: Create Clusters File

**Create `refactor-temp/clusters.md`:**

```markdown
# Test Clusters

**Generated**: 2026-01-15  
**Total Clusters**: 25  
**Consolidation Candidates**: 8  
**Potential Duplicates**: 3

---

## Consolidation Candidates

### Cluster C001: DownloadCommand.Execute — Connected + Success

**Type**: Consolidation Candidate  
**Reason**: Same arrange, same act, different assertions  
**Tests**: 3

| ID | Method | Unique Assertion |
|----|--------|------------------|
| T001 | Execute_ValidFile_Succeeds | console contains "Downloaded" |
| T002 | Execute_ValidFile_ShowsProgress | console contains progress % |
| T003 | Execute_ValidFile_ReturnsZero | result == 0 |

**Recommendation**: Consolidate into single test with labeled assertions

---

### Cluster C002: DownloadCommand.Execute — Partial Failure

**Type**: Consolidation Candidate  
**Reason**: Same arrange, same act, different assertions  
**Tests**: 4

| ID | Method | Unique Assertion |
|----|--------|------------------|
| T010 | Execute_SomeFilesFail_ContinuesBatch | all files attempted |
| T011 | Execute_SomeFilesFail_ShowsSummary | "2 of 3" in output |
| T012 | Execute_SomeFilesFail_ListsFailures | failed file names shown |
| T013 | Execute_SomeFilesFail_ShowsErrors | error messages shown |

**Recommendation**: Consolidate into single test with labeled assertions

---

## Potential Duplicates

### Cluster C010: FileTransferService.DownloadFile — Cancelled

**Type**: Potential Duplicate  
**Reason**: Nearly identical assertions  
**Tests**: 2

| ID | Method | File |
|----|--------|------|
| T050 | DownloadFile_Cancelled_ThrowsTaskCancelledException | FileTransferServiceDownloadTests.cs |
| T051 | Download_Cancellation_ThrowsException | IntegrationTests_Cancellation.cs |

**Recommendation**: Review — may be unit vs integration coverage

---

## Separate Scenarios (No Consolidation)

### Cluster C020: DownloadCommand.Execute — Different Scenarios

**Type**: Separate Scenarios  
**Reason**: Different mock configurations, different code paths  
**Tests**: 5

| ID | Method | Scenario |
|----|--------|----------|
| T001 | Execute_ValidFile_Succeeds | Happy path |
| T020 | Execute_NotConnected_ReturnsError | Disconnected |
| T030 | Execute_NoFiles_ShowsMessage | Empty result |

**Recommendation**: Keep separate — different code paths

---

## Infrastructure Mismatches

### Cluster C030: UploadCommand — Mixed Console Types

**Type**: Infrastructure Mismatch  
**Reason**: Same subject, inconsistent test infrastructure  
**Tests**: 3

| ID | Method | Infrastructure |
|----|--------|----------------|
| T070 | Upload_Success_ShowsMessage | TestConsole |
| T071 | Upload_Progress_DisplaysBar | VirtualConsole |
| T072 | Upload_Complete_ShowsGreen | TestConsole (should be VirtualConsole for color) |

**Recommendation**: Standardize — T072 should use VirtualConsole for color assertion

---

## Summary

| Category | Count | Action |
|----------|-------|--------|
| Consolidation Candidates | 8 | Merge in Phase 4 |
| Potential Duplicates | 3 | Review in Phase 3 |
| Separate Scenarios | 10 | No action needed |
| Infrastructure Mismatches | 4 | Fix in Phase 4 |
```

---

## Output Format

Report completion:

```
Test Clustering Complete

Tests analyzed: 150
Clusters created: 25

Breakdown:
- Consolidation candidates: 8 clusters (24 tests → ~8 tests)
- Potential duplicates: 3 clusters (6 tests to review)
- Separate scenarios: 10 clusters (no action)
- Infrastructure mismatches: 4 clusters (fix needed)

Output: refactor-temp/clusters.md

Next step: Run /test-remediation.assess C001 to assess the first consolidation candidate.
```

---

## Constraints

- Cluster by structural similarity, not semantic meaning
- A test can only belong to ONE cluster
- Prefer smaller clusters (3-6 tests) over mega-clusters
- If a cluster would have >10 tests, split by secondary criteria
