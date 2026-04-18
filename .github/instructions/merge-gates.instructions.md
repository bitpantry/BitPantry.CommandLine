---
description: "Use when: merging PRs, syncing branches, resolving merge conflicts, rebasing onto main, updating a PR branch, finishing a PR that is behind its base branch."
---

# Merge Gates

Procedure for safely integrating a feature branch with its base branch. Referenced by `/finish-pr`, `/work-issue`, and `/review-pr` skills.

## Branch Sync Procedure

When a PR branch is behind its base branch (e.g., `main`), follow these steps to bring it up to date.

### 1. Check If Behind

```bash
git fetch origin
git rev-list --count HEAD..origin/<base-branch>
```

If the count is **0**, the branch is up to date — skip the rest of this procedure.

### 2. Attempt Rebase

Rebase is preferred over merge commits for a cleaner squash-merge later:

```bash
git rebase origin/<base-branch>
```

If the rebase completes cleanly (no conflicts), skip to **Step 5: Post-Sync Test Gate**.

### 3. Resolve Conflicts

When conflicts are detected during rebase:

#### 3a. Understand Both Sides

Before touching any conflict markers, gather full context from both the landed work and the current branch. Implementation decisions often diverge from original issue descriptions — PR review threads capture those decisions.

**For the landed work (base branch changes):**

1. Identify which PR(s) introduced the new commits on the base branch (from commit messages or `git log`).
2. Read each landed PR's **full review thread** — reviews, inline comments, and conversation. Implementation decisions, scope changes, and design pivots are documented here.
3. Read the landed PR's **linked issue** for original intent.

**For the current branch:**

1. Read the current PR's **linked issue** for original intent.
2. Read the current PR's **full review thread** — reviews and comments may contain decisions that changed how the issue was implemented vs. what was originally specified.

**Shared context (if both PRs trace to the same spec):**

1. Identify the spec from issue labels (e.g., `spec-006`) or issue body references.
2. Read `specs/{NNN}-{name}/spec.md` — the shared requirements both issues implement.
3. Read `specs/{NNN}-{name}/issues/execution-plan.md` — this describes the relationship between parallel issues, shared files, and expected interaction points.
4. If `specs/{NNN}-{name}/plan.md` exists, skim the relevant sections for architectural decisions that inform how both changes should coexist.

This prevents blind resolution — you must understand the **current** intent of both sides, not just the original issue descriptions. A PR comment saying "switched from X to Y because of discovery Z" overrides the original issue's approach.

#### 3b. Classify Conflict Severity

| Severity | Description | Example |
|----------|-------------|---------|
| **Trivial** | Adjacent-line changes, dependency file ordering, import statements, whitespace | Both branches added a line to the same dependency list |
| **Non-trivial** | Both sides modified the same method body, interface signature, or data model | Both branches changed the same model — one added a field, the other changed an existing property |
| **Semantic** | No textual conflict but landed changes alter assumptions this branch depends on | A service interface gained a new required method that this branch's mock doesn't implement |

#### 3c. Resolve

For each conflicted file:

1. Open the file and read the full conflict region (not just the markers — include surrounding context).
2. Resolve by **preserving the intent of both sides**:
   - Keep all additions from the base branch (these are already merged and tested).
   - Layer this branch's changes on top, adjusting as needed for the new base.
3. For **non-trivial** conflicts: read the affected method/class holistically after resolution to verify it still makes sense.
4. Stage the resolved file:
   ```bash
   git add <file>
   ```
5. Continue the rebase:
   ```bash
   git rebase --continue
   ```

If a conflict involves **both sides modifying the same method body, restructuring control flow, or changing interface signatures**, **abort and ask the user**. Do NOT attempt to resolve conflicts where both sides changed the logic (not just data/structure) of the same function:

```bash
git rebase --abort
```

```
⚠️  Complex conflict in <file> — both branches restructured <description>.
    Manual review recommended before proceeding.
```

#### 3d. Semantic Conflicts (No Textual Markers)

After rebase completes, check for **semantic conflicts** — situations where the code compiles but is incorrect because the base branch changed an interface, contract, or assumption. Run the project's build command to verify compilation.

If the build fails with errors like missing interface members, changed method signatures, or type mismatches:

1. Read the new interface/contract from the base branch changes.
2. Update this branch's code to satisfy the new contract.
3. These are **non-trivial** resolutions.

### 4. Record Resolution Complexity

After resolving all conflicts, classify the overall resolution:

- **Trivial**: Only trivial conflicts (adjacent lines, ordering, imports). No re-review needed.
- **Non-trivial**: At least one non-trivial or semantic conflict was resolved. Re-review is **required** before merge.

### 5. Post-Sync Test Gate

**Mandatory** after any branch sync, even if no conflicts occurred (the base branch may have changed behavior that affects this branch). Run the project's test suite.

- **All tests pass**: Sync is complete. Proceed.
- **Tests fail**: Diagnose whether failures are caused by the merge resolution or were pre-existing on the base branch:
  1. Check out the base branch tip and run tests to establish a baseline.
  2. If tests fail on the base branch too, they are pre-existing — note them and proceed.
  3. If tests only fail on the rebased branch, the resolution introduced a regression. Fix before proceeding.

### 6. Push the Updated Branch

After rebase and tests pass:

```bash
git push --force-with-lease
```

`--force-with-lease` is used instead of `--force` because rebase rewrites history. The `--force-with-lease` variant refuses to push if the remote branch has commits you haven't seen, preventing accidental overwrites of someone else's work.

### 7. Report

Output a sync summary:

```
Branch sync complete:
  - Commits from <base-branch>: <N>
  - Conflicts resolved: <N> (<trivial|non-trivial>)
  - Semantic conflicts: <N>
  - Tests: <pass count> passed, <fail count> failed
  - Re-review recommended: <yes/no>
```

If resolution was **non-trivial**, add a PR comment summarizing what was resolved and why, so reviewers can verify the resolution:

```
🔀 Branch synced with <base-branch> — <N> non-trivial conflict(s) resolved:

- `<file>`: <brief description of what both sides changed and how it was resolved>
- ...

Re-review of these areas recommended.
```
