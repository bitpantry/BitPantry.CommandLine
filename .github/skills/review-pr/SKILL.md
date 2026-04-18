---
name: review-pr
description: "Review a GitHub pull request against its linked issue and project standards. Use when: reviewing PRs, checking agent work on a PR, evaluating test coverage, code quality review, PR feedback, submitting review comments."
argument-hint: "PR number or URL to review (e.g., 42 or https://github.com/owner/repo/pull/42)"
---

# Review PR

Review a GitHub pull request against its linked issue, project instructions, and coding standards. Produces a structured assessment with actionable recommendations.

## Prerequisites

- `gh` CLI authenticated via reviewer identity
- The workspace must be the repository the PR belongs to

**Identity (local agents only):** This skill is executed by the **reviewer** agent. If you are running locally with the `gh` CLI, set up before any `gh` command:

```powershell
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity reviewer
```

Cloud-hosted agents (e.g., GitHub Copilot coding agent) should skip identity setup and use their built-in GitHub access.

## Repo Detection

Derive the GitHub `owner` and `repo` from the local git remote:

```bash
git remote get-url origin
```

Parse the owner and repo name from the URL (supports both `https://github.com/owner/repo.git` and `git@github.com:owner/repo.git` formats). Use these values for all `gh` calls throughout the review.

## Step 0: Obtain PR Number

If the user did not provide a PR number or URL when invoking this skill, **ask for one before continuing**. Do not guess or assume.

Extract the PR number from the input. If a full URL is provided (e.g., `https://github.com/<owner>/<repo>/pull/42`), parse the number from it.

## Step 1: Wait for Agent Completion (if applicable)

Before reviewing, check whether a Copilot coding agent or CI is still running on the PR:

```powershell
# Block until all checks complete
gh pr checks <pr-number> --watch

# Or inspect current state non-blocking
gh pr checks <pr-number> --json name,status,conclusion |
  ConvertFrom-Json | Where-Object { $_.status -ne 'completed' }
```

If checks are still in progress, wait for them to complete before reviewing.
Do not begin reviewing code that is still being modified.

## Step 2: Gather PR Context

Collect all information needed for the review:

1. **Read the PR** — PR title, description, and linked issue references:
   ```powershell
   gh pr view <pr-number> --json title,body,state,isDraft,baseRefName,headRefName,author
   ```
2. **Read the linked issue** — Extract the issue number from `Closes #N` in the PR body, then:
   ```powershell
   gh issue view <issue-number> --json title,body,labels,assignees,state
   ```
   If the issue references a spec file in the repo (e.g., under `specs/`), read that file too.
3. **Read prior review comments**:
   ```powershell
   # Review submissions (APPROVED, CHANGES_REQUESTED, etc.)
   gh api /repos/<owner>/<repo>/pulls/<pr-number>/reviews
   # Inline review comments
   gh api /repos/<owner>/<repo>/pulls/<pr-number>/comments
   ```
   These form the **progressive input** — the cumulative requirements the PR must satisfy:
   - The original issue defines the baseline requirements.
   - Each review that requested changes adds incremental requirements or refinements.
   - The most recent review's feedback is the **highest-priority input**.
4. **Get the changed files**:
   ```powershell
   gh pr view <pr-number> --json files --jq '.files[].path'
   ```
5. **Create a worktree for the PR branch** — so the review gets its own isolated directory:
   ```
   git fetch origin pull/<number>/head:pr-<number>
   git worktree add ../pr-review-<number> pr-<number>
   ```
   Then perform all file reads and build/test operations from the `../pr-review-<number>` directory. The user's main working tree is never disturbed.

## Step 3: Review the Code

Evaluate the PR across these dimensions, reading the actual changed files in the workspace.

**Progressive input principle**: The PR must satisfy the original issue requirements AND all feedback from prior reviews. When prior reviews exist, evaluate the full PR holistically but pay special attention to whether the most recent review's feedback was addressed. Structure findings accordingly — call out which prior recommendations were resolved, which were missed, and whether any new issues were introduced while addressing feedback.

### 3a. Issue Resolution

- Does the PR address **all** acceptance criteria or requirements from the linked issue?
- Are there any requirements that were missed or only partially implemented?
- Were any out-of-scope changes introduced that weren't part of the issue?

### 3b. Prior Review Feedback (when applicable)

If prior reviews with recommendations or requested changes exist:

- **List each recommendation** from the most recent review (and any earlier unresolved ones).
- **For each recommendation**, assess: Was it addressed? Partially addressed? Ignored? Did the fix introduce new issues?
- **Flag any regressions** — cases where addressing one recommendation broke something that was previously working.
- This section should make it easy to see the delta between "what was asked" and "what was done" since the last review.

### 3c. Test Coverage

Load the `tdd-testing` instructions and the `tdd-workflow` skill references to evaluate tests against project standards:

- Are there tests for the new/changed behavior?
- Do the tests follow the project's TDD principles (Arrange/Act/Assert, no tautologies, no constant-testing)?
- Apply the **Verification Question**: "If someone broke the behavior this test specifies, would the test fail?" Flag any tests where the answer is NO.
- Is there adequate coverage of edge cases and error paths?
- Are the right test levels used (unit vs integration vs end-to-end per the project's test infrastructure guidance)?

**Assessment rubric** (use these definitions, not subjective judgment):

| Rating | Criteria |
|--------|----------|
| GOOD | Every behavioral change has a corresponding test; edge cases covered; all tests pass Verification Question |
| ADEQUATE | Main paths tested; 1-2 edge cases missing; all tests pass Verification Question |
| INSUFFICIENT | Any behavioral change has no corresponding test, OR any test fails Verification Question, OR test violates TDD principles from `tdd-testing` instructions |

#### Invalid Test Patterns Are Blocking — Not LOW

The `tdd-testing` instructions are **non-negotiable**. When any of the following patterns are found in new or changed tests, the finding is **HIGH** and the verdict **must** be `REQUEST_CHANGES`:

| Pattern | Example |
|---------|---------|
| Tautology — assertions that always pass regardless of production code | Constructing a DTO and asserting the values just passed to its constructor |
| Testing constants | Asserting a configuration value equals a hardcoded literal |
| Testing inputs, not outputs | Asserting on a value the test itself created |
| Testing without invoking code under test | Creating mocks and asserting on them without calling the real class |

**Do not offer "rename the test" as a fix for a tautological assertion.** Renaming does not fix tautological logic. The only valid remediation is to replace the tautological assertions with ones that exercise and verify production behavior, or delete them if no meaningful behavior-testing assertion can be written for that scenario.

### 3d. Code Quality

- Is the code well-structured, readable, and idiomatic for the project's language?
- Are there fragile patterns (e.g., string parsing where structured data exists, brittle conditionals, over-engineering)?
- Does the code follow existing conventions in the codebase?
- Are there any security concerns (path traversal, injection, etc.)?
- Is error handling appropriate — not excessive, not missing at boundaries?

**Assessment rubric** (use these definitions, not subjective judgment):

| Rating | Criteria |
|--------|----------|
| SOLID | Follows project conventions; no fragile patterns; no security concerns; idiomatic code |
| ACCEPTABLE | Minor convention deviations or slight fragility; no security concerns; generally readable |
| NEEDS WORK | Security concern found, OR fragile patterns that will break under maintenance, OR significant convention violations |

### 3e. Project Conventions

- Do new files follow the project structure documented in the project instructions (e.g., `copilot-instructions.md`, `AGENTS.md`, or equivalent)?
- Are naming conventions consistent with the rest of the codebase?
- If new commands/endpoints were added, are they properly registered and documented?

## Step 4: Branch Freshness Check

Before building and testing, check whether the PR branch is behind its base branch:

```bash
cd ../pr-review-<number>
git fetch origin
git rev-list --count HEAD..origin/<base-branch>
```

If the branch is behind by **any** commits, record this for the review summary. Do **not** rebase or modify the worktree — the review is read-only. The `/finish-pr` skill handles sync before merge.

Note: If the branch is significantly behind (e.g., 10+ commits), there may be semantic conflicts even if there are no textual conflicts. Flag this in the review as a risk factor.

## Step 5: Build and Test

Run the build and tests **inside the worktree** to verify the PR is in a working state. Use the build and test commands documented in the project instructions (e.g., `copilot-instructions.md`, `AGENTS.md`, or equivalent).

```
cd ../pr-review-<number>
# Run the project's build command
# Run the project's test command
```

Note any build warnings or test failures in the review summary.

## Step 6: Present the Review Summary

Produce a structured summary with these sections:

### Summary Format

```
## PR Review: #<number> — <title>

### Branch Freshness
- [UP TO DATE / BEHIND BY <N> COMMITS] <base branch>
- <if behind: "⚠️ Branch sync required before merge — /finish-pr will handle this.">

### Issue Resolution
- [PASS/PARTIAL/FAIL] <assessment of whether the issue is resolved>
- <specific items addressed or missed>

### Prior Review Feedback (include when prior reviews exist)
- [ALL ADDRESSED / PARTIALLY ADDRESSED / NOT ADDRESSED] <assessment>
- For each prior recommendation: [RESOLVED/PARTIAL/MISSED] <item> — <how it was addressed or why not>
- <any regressions introduced while addressing feedback>

### Test Coverage
- [GOOD/ADEQUATE/INSUFFICIENT] <assessment>
- <specific gaps or strengths>

### Code Quality
- [SOLID/ACCEPTABLE/NEEDS WORK] <assessment>
- <specific praise or concerns>

### Build & Tests
- [PASS/FAIL] Build status
- [PASS/FAIL] Test results (<N> passed, <N> failed)

### Recommendations
1. <Most important improvement>
2. <Next most important>
3. ...

### Verdict
[APPROVE / REQUEST CHANGES / NEEDS DISCUSSION]
<one-sentence overall assessment>
```

## Step 7: Submit Review (Only When Explicitly Asked)

**Do NOT submit a review to the PR unless the user explicitly asks you to.** The default behavior is to present findings locally only.

When the user asks to submit:

1. Submit the review using `gh`:
   ```powershell
   # Approve
   gh pr review <pr-number> --approve --body "<review body>"

   # Request changes
   gh pr review <pr-number> --request-changes --body "<review body with itemised feedback>"

   # Comment only
   gh pr review <pr-number> --comment --body "<review body>"
   ```
2. Use the appropriate event:
   - `--approve` — PR meets all criteria
   - `--request-changes` — issues the agent should fix
   - `--comment` — feedback without a blocking verdict
3. Include the recommendations as actionable items in the review body so the agent (or author) can address them.

## Important Notes

- **Read-only by default**: This skill does not modify code. It only reads and evaluates.
- **Worktree cleanup**: After the review is complete, clean up the worktree and local branch:
  ```
  git worktree remove ../pr-review-<number>
  git branch -D pr-<number>
  ```
- **Parallel reviews**: Because each review uses a separate worktree (`../pr-review-<number>`), multiple agents can review different PRs simultaneously without conflicts. The user's main working tree is never modified.
- **Agent iteration**: If the user wants the agent to act on recommendations, they can ask you to submit the review as `REQUEST_CHANGES`, which will trigger the agent to make improvements.
