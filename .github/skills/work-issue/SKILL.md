---
name: work-issue
description: "Work a GitHub issue end-to-end: move to In Progress, create branch/PR if needed, check out branch, implement, summarize. Use when: picking up an issue, starting implementation from an issue number, working a GitHub issue, resuming work on a PR."
argument-hint: "GitHub issue number (required)"
---

# Work Issue

Pick up a GitHub issue by number, ensure it is In Progress, ensure a branch and PR exist, check out the branch, understand the current state of work, implement the requirements, and summarize the completed work for review.

## Prerequisites

- GitHub MCP tools (for reading/writing issues and PRs)
- `gh` CLI installed and authenticated (for project board status updates)
- The workspace must be a git repository with a GitHub remote

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

Use `mcp_github_issue_read` to fetch the issue details:

- Title, body, labels, assignees, state, milestone
- Linked pull requests (if any)

Parse and retain:
- **Issue title and body** — these define what needs to be done
- **Labels** — may indicate issue type (bug, enhancement, etc.)
- **Existing linked PRs** — determines whether Step 4 creates or reuses

## Step 2b: Check Prerequisites

Parse the issue body for a `## Prerequisites` section. Look for lines matching `Blocked by #<number>` or similar patterns.

For each referenced blocking issue:

1. Use `mcp_github_issue_read` to check its state (open or closed).
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

Use `mcp_github_search_pull_requests` or `mcp_github_list_pull_requests` to find PRs that reference this issue (search for the issue number in PR titles or bodies, or check for linked PRs from the issue metadata).

### 4b. If No PR Exists — Create Branch and PR

1. **Create a branch** using `mcp_github_create_branch`:
   - Branch name format: `issue-<number>-<slugified-title>` (e.g., `issue-42-fix-auth-token-expiry`)
   - Slugify: lowercase, replace spaces/special chars with hyphens, truncate to ~50 chars
   - Base branch: the repository's default branch (typically `main`)

2. **Create a draft PR** using `mcp_github_create_pull_request`:
   - Title: the issue title (or a concise summary)
   - Body must include: `Closes #<issue-number>` (this auto-links the PR to the issue)
   - Set as **draft** so it's clear the work is in progress
   - Base: default branch, Head: the newly created branch

3. Confirm the PR was created and note the PR number.

### 4c. If PR Already Exists

Record the existing PR number and branch name. No creation needed.

## Step 5: Assess PR State and Determine Work Needed

This step determines **what, if any, work needs to be done**. The PR may be brand new (empty), may have complete implementation awaiting review, may have review feedback that hasn't been addressed, or may be fully approved with nothing left to do. You must assess all of this before proceeding.

### 5a. Collect All Signals

Gather **all four** of these data points using `mcp_github_pull_request_read`:

1. **PR details** — status (draft/open/merged/closed), description, merge state
2. **PR diff and changed files** — what code has been written on this branch
3. **PR reviews** — list of review submissions with state (`APPROVED`, `CHANGES_REQUESTED`, `COMMENTED`, `DISMISSED`) and timestamp
4. **PR review comments** — individual inline comments with resolved/unresolved status, file path, line number, body, and timestamp

Also collect:
5. **PR commits** — list of commits on the branch with timestamps (use `mcp_github_list_commits` on the PR branch, or inspect the PR's commit list)

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

## Step 6: Check Out the Branch

1. Ensure the working tree is clean:
   ```bash
   git status --porcelain
   ```
   If there are uncommitted changes, **warn the user** and ask how to proceed (stash, discard, or abort).

2. Fetch and check out the PR branch:
   ```bash
   git fetch origin
   git checkout <branch-name>
   git pull origin <branch-name>
   ```

3. Verify the checkout succeeded.

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

- Read and follow the project's instruction files (csharp-conventions, tdd-testing, test-infrastructure, cli-ux) as they apply to the files being modified.
- If the issue involves writing or changing tests, follow the `tdd-workflow` skill.
- If the issue involves CLI commands, follow the `cli-ux` instructions.

### 7c. Execute Implementation

1. Make the necessary code changes to satisfy the issue requirements and any unresolved review comments.
2. Run tests after each significant change to confirm correctness:
   ```bash
   dotnet test --filter "Category!=Performance"
   ```
3. Ensure the build is clean:
   ```bash
   dotnet build
   ```

### 7d. DO NOT COMMIT

**CRITICAL: Do NOT commit, push, or publish any changes unless the user explicitly instructs you to do so.** All changes remain local and uncommitted until the user reviews and approves.

## Step 8: Summarize

Present a structured summary of the work completed:

```
══════════════════════════════════════════
Issue #<number>: <title>
PR #<pr-number>: <pr-title>
Branch: <branch-name>
Work State: <state from Step 5b>
──────────────────────────────────────────

Status: Implementation complete (uncommitted)

Changes Made:
  - <file path> — <what changed and why>
  - <file path> — <what changed and why>
  ...

Review Comments Addressed:
  - <comment summary> — <how it was resolved>
  ...
  (or "No review comments to address")

Original Issue Requirements Implemented:
  - <requirement> — <how it was satisfied>
  ...
  (or "Previously implemented — no changes needed")

Tests:
  - Tests passing: <count>
  - Tests failing: <count>
  - New tests added: <count> (if any)

Build: Clean / Has warnings

⚠️  Changes are LOCAL and UNCOMMITTED.
    Review the changes, then instruct me to commit if satisfied.
══════════════════════════════════════════
```

If the project board status update failed in Step 3, include a note:
```
⚠️  Could not update project board status to "In Progress".
    Please update manually.
```
