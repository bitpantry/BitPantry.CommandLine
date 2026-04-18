---
name: finish-pr
description: "Finish and merge a completed GitHub pull request. Use when: merging a PR, finishing a PR, closing out a PR, completing a pull request, merge PR after review."
argument-hint: "PR number (required, e.g., 42)"
---

# Finish PR

Examine a GitHub pull request to determine if it is fully implemented and all review feedback has been addressed, then approve the review and merge it into the target branch.

## Prerequisites

- `gh` CLI authenticated via reviewer identity
- The workspace must be a git repository with a GitHub remote

**Identity:** This skill is executed by the **reviewer** agent. Set up before any `gh` command:

```powershell
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity reviewer
```

## Step 0: Obtain PR Number

If the user did not provide a PR number when invoking this skill, **ask for one and STOP**. Do not guess, assume, or search for a PR. Wait for an explicit PR number before continuing.

## Step 1: Detect Repository

Derive the GitHub `owner` and `repo` from the local git remote:

```bash
git remote get-url origin
```

Parse the owner and repo name from the URL. Supports both formats:
- `https://github.com/owner/repo.git`
- `git@github.com:owner/repo.git`

Strip any trailing `.git`. Use the extracted `owner` and `repo` for all `gh` calls throughout this skill.

If the remote is not a GitHub URL, inform the user and **STOP**.

## Step 2: Read the PR

```powershell
gh pr view <pr-number> --json title,body,state,isDraft,baseRefName,headRefName,mergeable
```

Fetch:
- State (open, closed, merged)
- Title and description
- Base branch (the branch the PR merges into â€” e.g., `main`, `master`)
- Head branch (the PR's feature branch)
- Whether it is a draft

Record the **base branch** â€” this is the merge target used in Step 7.

## Step 3: Gate â€” Is the PR Eligible?

Evaluate the PR state. If **any** of the following are true, inform the user and **STOP**:

| Condition | Message |
|-----------|---------|
| PR state is `merged` | PR #\<number\> is already merged. Nothing to do. |
| PR state is `closed` | PR #\<number\> is closed. It cannot be merged in this state. |
| PR diff is empty / no meaningful changes | PR #\<number\> has no code changes. It appears unimplemented. |

### Draft Auto-Ready

If the PR is a **draft**, automatically mark it as ready for review before proceeding:

```powershell
gh pr ready <pr-number>
```

Inform the user:

```
â„¹ï¸  PR #<number> was a draft. Marked as ready for review.
```

If the command fails, report the error and **STOP**.

If the PR passes all gates, proceed.

## Step 4: Assess Implementation Completeness

Determine whether the PR is fully implemented and all review feedback has been addressed. Collect these signals:

### 4a. Gather Review Timeline

Collect:

1. **All reviews** â€” list of review submissions with state, body, and timestamp:
   ```powershell
   gh api /repos/<owner>/<repo>/pulls/<pr-number>/reviews
   ```
2. **All review comments** â€” inline comments with resolved/unresolved status:
   ```powershell
   gh api /repos/<owner>/<repo>/pulls/<pr-number>/comments
   ```
3. **All commits** â€” list of commits on the PR branch with timestamps:
   ```powershell
   gh api /repos/<owner>/<repo>/pulls/<pr-number>/commits
   ```

### 4b. Evaluate the Timeline

Work through these checks **in order**. The first failing check causes a **STOP**.

#### Check 1: Unresolved Review Comments

If there are **any unresolved review comments**, inform the user:

```
PR #<number> has <N> unresolved review comment(s). These must be
resolved before finishing:

  - [<file>:<line>] <comment body summary>
  ...
```

**STOP.**

#### Check 2: Unanswered Change Requests

Find the **most recent review** with state `CHANGES_REQUESTED`. If one exists, check whether there are **any commits after that review's timestamp**.

- If **no commits exist after the most recent CHANGES_REQUESTED review**, inform the user:

  ```
  PR #<number> has a review requesting changes (by <reviewer>, <date>)
  with no subsequent commits. The requested changes do not appear to
  have been addressed.

  Review summary:
    <review body, truncated to ~500 chars>
  ```

  **STOP.**

- If commits exist after the review but there is **no subsequent APPROVED review**, warn the user but do NOT stop:

  ```
  âš ï¸  PR #<number> has commits after the last CHANGES_REQUESTED review,
      but no subsequent APPROVED review exists. Proceeding based on
      commit activity, but you may want to verify the changes are adequate.
  ```

#### Check 3: No Reviews At All

If the PR has **zero reviews**, inform the user and **ask for explicit confirmation**:

```
âš ï¸  PR #<number> has no reviews. Merging without any review requires
    explicit approval.

    Do you want to proceed anyway? (yes/no)
```

**Wait for the user to respond.** Only continue to Step 5 if the user explicitly confirms. If the user declines (or does not respond), **STOP**.

### 4c. Summary

If all checks pass (or only produced warnings), present a brief status:

```
PR #<number> assessment:
  - State: Open
  - Reviews: <count> (<latest state>)
  - Unresolved comments: 0
  - Commits after last review: <count or N/A>
  - Target branch: <base branch>

Ready to approve and merge.
```

## Step 5: Branch Sync Gate

Before merging, ensure the PR branch is up to date with its base branch. This prevents merge failures and ensures the combined code has been tested.

1. **Check out the PR branch locally**:
   ```bash
   git fetch origin
   git checkout <head-branch>
   git pull origin <head-branch>
   ```

2. **Follow the Branch Sync Procedure** from the `merge-gates` instructions:
   - Check if the branch is behind the base branch.
   - If behind, rebase onto the base branch, resolve any conflicts, run the post-sync test gate, and push.
   - If the resolution was **non-trivial**, inform the user that re-review is recommended before merging. **Ask for confirmation** to proceed with the merge or stop for re-review.

3. If the branch is already up to date (or sync completed cleanly with trivial/no conflicts), proceed to Step 6.

## Step 6: Full Test Gate

Before merging, run the project's **full** test suite â€” including any extended or integration-level tests documented in the project's test infrastructure instructions. All tests must pass before proceeding.

- Consult the project's test infrastructure instructions (e.g., `test-infrastructure.instructions.md`) for the exact commands and any multi-step test requirements.
- If any tests fail, diagnose and fix before proceeding to Step 7.
- This is the definitive pre-merge quality gate. Even if the post-sync test gate in Step 5 passed, run the full suite here to confirm nothing was missed.

Proceed to Step 7 only when all tests pass.

## Step 7: Approve and Merge

### 7a. Submit Approval

```powershell
gh pr review <pr-number> --approve `
  --body "Approved via finish-pr skill. All review feedback addressed."
```

### 7b. Merge the PR

```powershell
gh pr merge <pr-number> --squash --subject "<PR title> (#<pr-number>)"
```

### 7c. Delete the Head Branch

```powershell
# Via API â€” no local checkout required
gh api -X DELETE /repos/<owner>/<repo>/git/refs/heads/<head-branch>
```

If branch deletion fails (e.g., branch protection), warn but do not treat as a failure:

```
âš ï¸  Could not delete branch <head-branch>: <error>. Delete it manually.
```

### 7d. Sync Local Branch

After a successful merge, reset the local base branch to match origin so no spurious merge commits are created:

```bash
git checkout <base-branch>
git fetch origin
git reset --hard origin/<base-branch>
```

Also delete the local feature branch if it exists:

```bash
git branch -D <head-branch>
```

### 7e. Confirm

After a successful merge, report:

```
âœ… PR #<number> has been squash-merged into <base branch>.
   Branch <head-branch> has been deleted.
```

If the merge fails after sync (e.g., branch protection rules, unexpected error), report the error and **STOP**:

```
âŒ Merge failed for PR #<number>: <error message>
   Please resolve the issue and retry.
```

## Important Notes

- **This skill does NOT implement code.** It only evaluates, syncs, and merges. If the PR is not ready, it stops and tells the user why.
- **Branch sync before merge**: Step 5 ensures the PR branch is up to date with the base branch before merging, following the `merge-gates` instructions. This handles the common case where parallel PRs have landed since this branch was created.
- **Repository is detected dynamically** from the local git remote â€” no repo names are hardcoded. This skill is portable across projects.
- **The base branch is read from the PR itself** â€” it merges into whatever branch the PR targets (e.g., `main`, `master`, `develop`).
