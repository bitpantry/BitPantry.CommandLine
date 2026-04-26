---
name: work-issue
description: "Work a GitHub issue end-to-end: move to In Progress, create branch/PR if needed, check out branch, implement, summarize. Use when: picking up an issue, starting implementation from an issue number, working a GitHub issue, resuming work on a PR."
argument-hint: "GitHub issue number (required)"
---

# Work Issue

Pick up a GitHub issue by number, ensure it is In Progress, ensure a branch and PR exist, set up an isolated worktree, understand the current state of work, implement the requirements, and summarize the completed work for review.

Worktrees allow multiple issues to be worked concurrently without switching branches or stashing changes.

## Prerequisites

- `gh` CLI (identity set per operation — see below)
- The workspace must be a git repository with a GitHub remote

**Identity (local agents only):** This skill is executed by the **implementer** agent. If you are running locally with the `gh` CLI, set up before Step 2:

```powershell
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer
```

Cloud-hosted agents (e.g., GitHub Copilot coding agent) should skip identity setup and use their built-in GitHub access.

## Step 0: Validate Issue Number

An issue number **must** be provided by the user. If no issue number was provided when this skill was invoked:

1. **Ask the user** for the issue number.
2. **Do NOT assume, guess, or proceed without it.**
3. Wait for an explicit issue number before continuing.

## Step 1: Detect Repository

Determine the target GitHub `owner/repo`:

1. Run `git remote get-url origin` in the workspace root.
2. Extract the `owner/repo` slug from the URL (handles both `https://github.com/owner/repo.git` and `git@github.com:owner/repo.git` formats).
3. Use this value for all GitHub API calls throughout the skill.

If the remote is not a GitHub URL, inform the user and **stop**.

## Step 2: Read the Issue

```powershell
gh issue view <issue-number> --json title,body,labels,assignees,state,milestone
```

Parse and retain:
- **Issue title and body** — these define what needs to be done
- **Labels** — may indicate issue type (bug, enhancement, `spec-{NNN}`, etc.)
- **Existing linked PRs** — determines whether Step 4 creates or reuses

## Step 2b: Check Prerequisites

Parse the issue body for a `## Prerequisites` section. Look for lines matching `Blocked by #<number>` or similar patterns.

For each referenced blocking issue:

1. Run `gh issue view <blocking-number> --json state --jq '.state'` to check if it is open or closed.
2. Collect any that are still **open**.

**If any blocking issues are open**, warn the user:

```
⚠️  Issue #<number> has open prerequisites:

  - #<blocking-number> — <blocking issue title> (open)
  - #<blocking-number> — <blocking issue title> (open)

These issues should be completed first to avoid implementation conflicts.
Proceed anyway? (yes/no)
```

**Wait for the user to respond.** Only continue if the user explicitly confirms. If the user declines, **STOP**.

**If no prerequisites section exists, or all blocking issues are closed**, proceed normally.

## Step 2c: Determine the Branch Base

Before creating or reusing an implementation branch, determine which branch this issue should build on.

Use the shared `Resolve Base Branch from Issue` helper in the `github-ops` skill.

1. Read the issue labels and look for `spec-{NNN}`.
2. If there is no `spec-{NNN}` label, use the repository default branch.
3. If there is a `spec-{NNN}` label, resolve the single matching `origin/spec/{NNN}-*` branch.
4. If the matching spec branch is missing or ambiguous, stop instead of guessing.

Record this as `<base-branch>` and use it consistently for branch creation, PR creation, and later sync checks.

## Step 3: Move Issue to In Progress

Check whether the issue is already in "In Progress" status on its GitHub Project board. If not, move it there.

**Approach — use `gh` CLI project commands:**

1. Find the project(s) associated with the repository:
   ```bash
   gh project list --owner <owner> --format json
   ```
2. For each project, find the issue's item ID:
   ```bash
   gh project item-list <project-number> --owner <owner> --format json
   ```
   Search the results for the issue number.
3. Get the Status field ID and the "In Progress" option ID:
   ```bash
   gh project field-list <project-number> --owner <owner> --format json
   ```
4. Update the item's status:
   ```bash
   gh project item-edit --id <item-id> --field-id <status-field-id> --project-id <project-id> --single-select-option-id <in-progress-option-id>
   ```

**Fallback:** If no project board is found, or the `gh` project commands fail, log a warning and continue — do not block implementation on project board status. Note the failure in the final summary so the user can manually update the status.

## Step 4: Ensure Branch and PR Exist

Check whether a pull request already exists for this issue.

### 4a. Search for Existing PR

```powershell
gh pr list --state open --json number,title,body,headRefName |
  ConvertFrom-Json |
  Where-Object { $_.body -match "Closes #<issue-number>" -or $_.title -match "<issue-number>" }
```

### 4b. If No PR Exists — Create Branch and PR

1. **Create a branch linked to the issue** using `gh issue develop`:
   - Branch name format: `issue-<number>-<slugified-title>` (e.g., `issue-42-fix-auth-token-expiry`)
   - Slugify: lowercase, replace spaces/special chars with hyphens, truncate to ~50 chars
   - Base branch: `<base-branch>` from Step 2c
   ```powershell
   gh issue develop <issue-number> --base <base-branch> --name issue-<number>-<title-slug>
   ```
   This creates the branch on the remote, links it to the issue in GitHub's "Development" sidebar, and sets up tracking. It does NOT check out the branch locally (the worktree step handles that).
   
   If `gh issue develop` fails (e.g., permissions, older `gh` version), fall back to manual creation:
   ```powershell
   git branch issue-<number>-<title-slug> <base-branch>
   git push -u origin issue-<number>-<title-slug>
   ```

2. **Create a draft PR**:
   ```powershell
   gh pr create `
     --title "<issue title>" `
     --body "Closes #<issue-number>`n`n## Summary`n<brief description>" `
     --draft `
     --head issue-<number>-<title-slug> `
     --base <base-branch>
   ```
   - `Closes #<issue-number>` in the body auto-links the PR to the issue
   - Set as **draft** so it's clear the work is in progress
   - Use `--head` to specify the branch since we are not checked out on it
   - When the issue has a `spec-{NNN}` label, this keeps incomplete feature work isolated from the default branch and integrates it through the spec branch first

3. Confirm the PR was created and note the PR number.

### 4c. If PR Already Exists

Record the existing PR number and branch name. No creation needed.

## Step 5: Assess PR State and Determine Work Needed

This step determines **what, if any, work needs to be done**. The PR may be brand new (empty), may have complete implementation awaiting review, may have review feedback that hasn't been addressed, or may be fully approved with nothing left to do. You must assess all of this before proceeding.

### 5a. Collect All Signals

Gather **all four** of these data points:

1. **PR details** — status, description, merge state:
   ```powershell
   gh pr view <pr-number> --json title,body,state,isDraft,baseRefName,headRefName,mergeable
   ```
2. **PR diff and changed files**:
   ```powershell
   gh pr diff <pr-number>
   gh pr view <pr-number> --json files --jq '.files[].path'
   ```
3. **PR reviews** — list of review submissions with state and timestamp:
   ```powershell
   gh api /repos/<owner>/<repo>/pulls/<pr-number>/reviews
   ```
4. **PR review comments** — individual inline comments with resolved status:
   ```powershell
   gh api /repos/<owner>/<repo>/pulls/<pr-number>/comments
   ```

Also collect:
5. **PR commits** — list of commits on the branch with timestamps:
   ```powershell
   gh api /repos/<owner>/<repo>/pulls/<pr-number>/commits
   ```

### 5b. Determine the Work State

Evaluate these signals **in order** to classify the current state. Use the **first matching** state:

#### State 1: Already Merged or Closed

| Signal | Value |
|--------|-------|
| PR status | `merged` or `closed` |

**Action:** Inform the user that the PR is already merged/closed and there is no work to do. **STOP.**

#### State 2: Approved — No Work Remaining

| Signal | Value |
|--------|-------|
| Most recent review state | `APPROVED` |
| Unresolved review comments | None (0 unresolved) |

**Action:** Inform the user:
```
Issue #<number> / PR #<pr-number> has been approved with no unresolved
review comments. There is no remaining work to implement.
```
**STOP.** Do not proceed to implementation.

#### State 3: Review Feedback Not Yet Addressed

| Signal | Value |
|--------|-------|
| Reviews exist | Yes, at least one with `CHANGES_REQUESTED` or with unresolved comments |
| Unresolved review comments | 1 or more |
| Commits after latest review | None — no commits exist with a timestamp after the most recent review's timestamp |

This means a reviewer has left feedback and **no work has been done to address it yet**. The unresolved review comments are the current "instructions."

**Action:** The work to do is **address the unresolved review comments**. Compile a list of every unresolved comment with its file, line, and body. These become the implementation requirements for Step 7.

#### State 4: Review Feedback Partially Addressed

| Signal | Value |
|--------|-------|
| Reviews exist | Yes |
| Unresolved review comments | 1 or more |
| Commits after latest review | Yes — at least one commit exists after the review timestamp |

This means some review feedback was addressed (there are newer commits) but some comments remain unresolved.

**Action:** The work to do is **address the remaining unresolved review comments**. Read the diff of commits made after the review to understand what was already addressed, then compile the still-unresolved comments as the remaining work.

#### State 5: Implementation Done, Awaiting Review (No Unresolved Feedback)

| Signal | Value |
|--------|-------|
| PR diff | Non-empty (code changes exist) |
| Reviews | Either none, or all are `COMMENTED`/`DISMISSED` with no unresolved comments |
| Unresolved review comments | None (0 unresolved) |

This means implementation has been done and either hasn't been reviewed yet, or prior reviews have been fully addressed.

**Action:** Compare the PR diff against the issue requirements to check if the implementation appears complete. If it looks complete, inform the user:
```
Issue #<number> / PR #<pr-number> appears to have a complete implementation
with no unresolved review comments. The PR may be awaiting review.
There is no remaining work to implement.
```
**STOP.** Do not proceed to implementation.

If the diff clearly does NOT cover all issue requirements (e.g., the issue lists 5 requirements and only 2 are addressed in the diff), note what's missing and proceed to Step 6 to implement the remainder.

#### State 6: Fresh — No Work Done

| Signal | Value |
|--------|-------|
| PR diff | Empty or no meaningful changes |
| Reviews | None |
| Review comments | None |

**Action:** Full implementation needed from the issue requirements. Proceed to Step 6.

### 5c. Build Work Summary

For states that proceed to implementation (States 3, 4, 5-partial, 6), compile a structured summary:

```
Work State: <state name>

Original Issue Requirements:
  - <requirement 1 from issue body>
  - <requirement 2>
  ...

Already Implemented (from PR diff):
  - <what's been done, if anything>
  (or "Nothing — fresh start")

Unresolved Review Comments to Address:
  - [<file>:<line>] <reviewer comment summary>
  - [<file>:<line>] <reviewer comment summary>
  (or "None")

Remaining Work:
  - <specific item 1>
  - <specific item 2>
  ...
```

This summary becomes the input to Step 7 (implementation). The agent must implement **only the Remaining Work** items — not redo work that's already complete.

## Step 6: Create Worktree for the Branch

Use a git worktree so the main working tree is undisturbed and multiple issues can be worked concurrently.

1. **Fetch the latest refs**:
   ```bash
   git fetch origin
   ```

2. **Create the worktree** under the `worktrees/` directory:
   ```powershell
   git worktree add worktrees/issue-<issue-number> <branch-name>
   ```
   - Worktree path: `worktrees/issue-<issue-number>` (e.g., `worktrees/issue-42`)
   - If the worktree already exists (from a prior session), reuse it:
     ```powershell
     if (Test-Path worktrees/issue-<issue-number>) {
       # Worktree already exists — just pull latest
       Push-Location worktrees/issue-<issue-number>
       git pull origin <branch-name>
       Pop-Location
     } else {
       git worktree add worktrees/issue-<issue-number> <branch-name>
     }
     ```

3. **Verify** the worktree is on the correct branch:
   ```powershell
   cd worktrees/issue-<issue-number>
   git branch --show-current
   ```

4. **Install dependencies** in the worktree if needed (build caches may not be shared across worktrees):
   ```powershell
   cd worktrees/issue-<issue-number>
   # Run the project's dependency install command
   ```

All subsequent steps (6b, 7, 8) operate from within the `worktrees/issue-<issue-number>/` directory.

## Step 6b: Branch Sync Check

When resuming work on an **existing** PR (Step 4c path — the PR already existed), check if the branch is behind the base branch. This is critical when parallel issues have been merged since this branch was created.

1. **From inside the worktree**, follow the Branch Sync Procedure from the `merge-gates` instructions:
   ```powershell
   cd worktrees/issue-<issue-number>
   # Check if behind, rebase if needed, run tests, push
   ```
   - If behind, rebase, resolve conflicts, run the post-sync test gate, and push.
   - If the resolution was **non-trivial**, note this in the Step 8 summary.

2. If the branch was freshly created in Step 4b, it is already based on the latest `<base-branch>` — skip this check.

This ensures you implement against the current state of the codebase, not a stale snapshot.

## Step 7: Implement

Implement the issue requirements following project conventions.

### 7a. Understand What to Implement

Use the **Remaining Work** list from Step 5c as the authoritative scope:

- If the state was **Fresh (State 6)**, implement all issue requirements from scratch.
- If the state was **Review Feedback Not Yet Addressed (State 3)**, implement each unresolved review comment. The original issue requirements are context, but the review comments are the primary work items.
- If the state was **Review Feedback Partially Addressed (State 4)**, implement only the still-unresolved review comments. Review the post-review commits to avoid duplicating work.
- If the state was **Implementation Incomplete (State 5-partial)**, implement only the missing requirements that the current diff doesn't cover.

Do NOT redo work that the PR diff shows is already complete and correct. Do NOT ignore unresolved review comments — they carry the same weight as issue requirements.

### 7b. Follow Project Conventions

- Read and follow the project's instruction files (from `.github/instructions/`) as they apply to the files being modified.
- For implementation work, follow the `tdd-workflow` skill by default. Do not treat TDD as optional just because the issue body does not explicitly mention tests.
- Establish a full-suite baseline before the first code change, following the `tdd-workflow` baseline requirements.

### 7c. Execute Implementation

All implementation work happens **inside the worktree** at `worktrees/issue-<issue-number>/`.

1. **Before the first code change**, run the project's full test suite from the worktree and record:
   - exact command(s) used
   - passing test count
   - failing test count
   - names or signatures of any pre-existing failures
2. Write or identify the RED tests for the behavior being implemented, following the `tdd-workflow` guidance.
3. Make the necessary code changes to satisfy the issue requirements and any unresolved review comments.
4. Run targeted tests after each significant change to confirm correctness using the project's test command (see `copilot-instructions.md`). Run from the worktree directory.
5. Ensure the build is clean using the project's build/analyze command (see `copilot-instructions.md`). Run from the worktree directory.
6. **Before pushing**, rerun the project's full test suite from the worktree and compare it to the recorded baseline.

Rules:
- If a previously-passing test now fails, stop and fix the regression before pushing.
- If new failures appear and you cannot prove they were already present in the baseline, treat them as regressions introduced by the work.
- Do not push code that has only been validated with nearby unit tests when the project instructions require a broader suite.

### 7d. Commit and Push

Once the post-implementation full-suite run matches or improves on the recorded baseline and the build is clean, commit and push the changes to the remote branch.

**Identity:** Ensure the **implementer** identity is active before committing/pushing:
```powershell
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer
```

1. **Stage and commit** all changes in the worktree:
   ```powershell
   cd worktrees/issue-<issue-number>
   git add -A
   ```

2. **Commit message** depends on the work state:
   - **Fresh implementation (State 6):** Use a conventional commit message describing the work:
     ```powershell
     git commit -m "<type>(<scope>): <short description>"
     ```
   - **Addressing review feedback (States 3 or 4):** Include a summary of what feedback was addressed:
     ```powershell
     git commit -m "<type>(<scope>): address review feedback

     <brief summary of feedback addressed>"
     ```

3. **Push** to the remote branch:
   ```powershell
   git push origin <branch-name>
   ```

4. If a draft PR was created in Step 4b (fresh issue), the PR is already on GitHub. No additional PR action is needed.

## Step 8: Report Progress

Present a structured progress report after pushing:

```
══════════════════════════════════════════
Issue #<number>: <title>
PR #<pr-number>: <pr-title>
Branch: <branch-name>
══════════════════════════════════════════
```

**If responding to review feedback (States 3 or 4)**, include a feedback summary section:

```
Review Feedback Addressed:
  - <comment summary> — <how it was resolved>
  - <comment summary> — <how it was resolved>
  ...
```

**Always include** a work summary:

```
Work Done:
  - <file path> — <what changed and why>
  - <file path> — <what changed and why>
  ...

Tests:
   - Baseline command(s): <exact command(s)>
   - Baseline result: <count passed>, <count failed>
   - Final command(s): <exact command(s)>
  - Tests passing: <count>
  - Tests failing: <count>
   - Pre-existing failures preserved: <yes/no; list if relevant>
  - New tests added: <count> (if any)

Build: Clean / Has warnings
```

**Always confirm** the push and PR status:

```
✅ Changes pushed to origin/<branch-name>
```

For **initial work** (State 6), also confirm:
```
✅ Draft PR #<pr-number> created on GitHub
```

If the project board status update failed in Step 3, include a note:
```
⚠️  Could not update project board status to "In Progress".
    Please update manually.
```

## Step 9: Capture Learnings

After pushing, review the work session for anything discovered that would be useful in future sessions. Write to `/memories/repo/` if any of the following occurred:

- A build or test command behaved unexpectedly
- A workaround was needed for a framework or library quirk
- A project convention was unclear from instructions and had to be inferred
- A dependency had undocumented behavior
- A pattern that worked (or failed) for a specific problem type

**Format:** One file per topic. Append to existing files when the topic already has a note. Keep entries to 1–3 lines.

**Skip this step** if the implementation was straightforward with no surprises.

## Worktree Cleanup

Worktrees are cleaned up when the work is fully complete (e.g., after the PR is merged via the `finish-pr` skill). Do **not** remove the worktree at the end of implementation — the user may need to review changes, run additional tests, or resume work later.

To manually clean up a worktree:
```powershell
git worktree remove worktrees/issue-<issue-number>
```

## Important Notes

- **Parallel work**: Because each issue uses a separate worktree (`worktrees/issue-<number>`), multiple issues can be worked concurrently without branch switching or stashing. The user's main working tree is never modified.
- **Worktree reuse**: If the worktree already exists from a prior session, it is reused (Step 6). This supports resuming work across multiple sessions.
- **Dependencies**: Each worktree may need its own dependency install since build caches may not be shared across worktrees.
- **Consistency with review-pr**: This follows the same worktree convention as the `review-pr` skill, which uses `worktrees/pr-review-<number>`.