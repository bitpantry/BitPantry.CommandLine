---
name: github-ops
description: "GitHub CLI operations using implementer and reviewer identities. Use when: performing any GitHub operation (create PR, review PR, merge PR, manage issues/branches), setting up GitHub App identity, or replacing any mcp_github_* call. ALWAYS use instead of MCP GitHub tools."
---

# GitHub Operations

All GitHub interactions in this project use `gh` (`gh.exe`) exclusively.
The GitHub MCP server is **not used** — see `github-ops.instructions.md` for the global policy.

---

## Tool Policy

| Tool | Purpose | Identity Setup |
|------|---------|---------------|
| `gh` / `gh.exe` | All GitHub API operations — PRs, issues, branches, reviews, merges | **Yes** — set `GH_TOKEN` before use |
| `git` | Local repo ops — checkout, commit, push, pull | No — uses existing git credential |
| `github` / `github.exe` | Copilot CLI agent operations | No — uses VS Code default auth |
| `mcp_github_*` tools | **NEVER USE IN THIS PROJECT** | N/A |

---

## Identity Model

Two GitHub App identities enforce a hard separation between implementation and review.
This separation exists because **GitHub will block a PR author from approving their own PR**,
which will stall the entire pipeline.

**Either identity can technically run any `gh` command** — identity is controlled by `GH_TOKEN`.
But the rules below are **non-negotiable**. Violating them will cause pipeline failures.

---

### Setting Up Identity

```powershell
# Must be dot-sourced — sets GH_TOKEN in the current shell
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity reviewer
```

Switch by calling the script again at any point. Tokens last ~1 hour. Re-run on HTTP 401.

---

### Hard Rules — implementer identity

The implementer identity **MUST** be active for:

| Step | Command |
|------|---------|
| Create a feature branch | `gh issue develop` or `git checkout -b ... && git push` |
| Create the draft PR | `gh pr create --draft` |
| Push all implementation commits | `git push` *(git credential — GH_TOKEN not used)* |
| Mark PR ready for review | `gh pr ready <N>` |
| Request a review | `gh pr edit <N> --add-reviewer ...` |
| Post a comment responding to review feedback | `gh pr comment <N> --body "..."` |

The implementer identity **MUST NEVER**:

- Submit a review (`gh pr review`) — GitHub will block it (same identity as PR author)
- Approve a PR — GitHub will block it
- Merge a PR — must be done after an independent review

---

### Hard Rules — reviewer identity

The reviewer identity **MUST** be active for:

| Step | Command |
|------|---------|
| Submit any review (approve, request-changes, comment) | `gh pr review <N> --approve / --request-changes` |
| Merge the PR | `gh pr merge <N> --squash` |
| Delete the feature branch after merge | `gh api -X DELETE .../git/refs/heads/<branch>` |
| Update project board to Done | `gh project item-edit ...` |

The reviewer identity **MUST NEVER**:

- Create a branch — branch must be owned by the implementer
- Create or author a PR — reviewer must not be the PR author
- Push commits to the feature branch — implementation belongs to the implementer

---

### Read Operations — Either Identity

Reading is non-destructive and does not affect the pipeline. Either identity may:

- `gh issue view` — read an issue
- `gh pr view` / `gh pr diff` — read a PR or its diff
- `gh api .../reviews` / `.../comments` — read prior reviews
- `gh pr checks` — check CI status

---

### Full Workflow Sequence

```
[implementer] Set-GitHubIdentity -Identity implementer
[implementer] Create branch
[implementer] Create draft PR (Closes #N)
[implementer] Implement — commit and push via git
[implementer] gh pr ready <N>
[implementer] gh pr edit <N> --add-reviewer agent-reviewer-app[bot]

[reviewer]    Set-GitHubIdentity -Identity reviewer
[reviewer]    Read PR, diff, issue
[reviewer]    Build and test in worktree
[reviewer]    gh pr review <N> --request-changes  (if issues found)
                  OR
[reviewer]    gh pr review <N> --approve           (if ready)

-- if changes requested, back to implementer --
[implementer] Set-GitHubIdentity -Identity implementer
[implementer] Address feedback — commit and push via git
[implementer] gh pr comment <N> --body "Feedback addressed: ..."

-- reviewer evaluates again --
[reviewer]    Set-GitHubIdentity -Identity reviewer
[reviewer]    gh pr review <N> --approve

[reviewer]    gh pr merge <N> --squash
[reviewer]    gh api -X DELETE .../git/refs/heads/<branch>
[reviewer]    gh project item-edit ... (move issue to Done)
```

### Why These Rules Are Non-Negotiable

GitHub enforces that a PR author cannot approve their own PR. If the implementer identity
creates the PR and then tries to approve it, the API call will fail with a 422 error and
the pipeline will stall. The hard rules above make this situation structurally impossible.

---

## Identity Configuration Files

```
.github/skills/github-ops/
├── identity/
│   ├── implementer/
│   │   ├── app.config.json    ← appId + installationId (committed to repo)
│   │   └── private-key.pem   ← NOT committed — see identity/README.md
│   ├── reviewer/
│   │   ├── app.config.json    ← appId + installationId (committed to repo)
│   │   └── private-key.pem   ← NOT committed — see identity/README.md
│   └── README.md              ← Key placement instructions
└── scripts/
    ├── New-GitHubAppToken.ps1  ← Generates a 1-hour installation access token
    └── Set-GitHubIdentity.ps1  ← Sets GH_TOKEN in the current terminal session
```

`app.config.json` files are safe to commit — they contain only public IDs.
`*.pem` files are excluded by `.github/skills/github-ops/identity/.gitignore`.

---

## Token Generation

GitHub App tokens are generated in two steps:
1. Sign a short-lived JWT with the App's private key (RS256, expires in ~9 min)
2. Exchange the JWT for a 1-hour installation access token

```powershell
# From repo root
$token = & .github/skills/github-ops/scripts/New-GitHubAppToken.ps1 -Identity implementer
$token = & .github/skills/github-ops/scripts/New-GitHubAppToken.ps1 -Identity reviewer
```

The script reads `app.config.json` and `private-key.pem` from `identity/{name}/`.
Tokens are valid for **1 hour**. Regenerate on HTTP 401 errors.

---

## Setting Up a Terminal Session

**MUST be dot-sourced** so `GH_TOKEN` persists in the calling shell:

```powershell
# Implementer terminal
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer

# Reviewer terminal
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity reviewer
```

Without the leading `. ` (dot-space), `GH_TOKEN` is set in a child process and lost
when the script exits. All subsequent `gh` commands in the session use that identity.

---

## Detecting Owner/Repo

For `gh api` calls that need `{owner}/{repo}` explicitly:

```powershell
$remote = git remote get-url origin
if ($remote -match 'github\.com[:/](.+?)(?:\.git)?$') {
    $ownerRepo = $matches[1]
    $owner     = $ownerRepo.Split('/')[0]
    $repo      = $ownerRepo.Split('/')[1]
}
```

Most `gh pr` and `gh issue` commands auto-detect the repo from the current directory.

## Resolve Base Branch from Issue

Use this helper whenever a workflow needs to decide whether issue work should branch from the repository default branch or from a spec branch.

### Inputs

- Issue number: `<issue-number>`
- Repository remote: `origin`

### Procedure

```powershell
$issue = gh issue view <issue-number> --json labels,title | ConvertFrom-Json
$specLabel = $issue.labels.name | Where-Object { $_ -match '^spec-(\d{3})$' } | Select-Object -First 1

if (-not $specLabel) {
  $baseBranch = git remote show origin |
    Select-String 'HEAD branch:' |
    ForEach-Object { $_.ToString().Split(':', 2)[1].Trim() }

  if (-not $baseBranch) {
    throw 'Could not determine the repository default branch from origin.'
  }

  return $baseBranch
}

$specNumber = $specLabel.Substring(5)
git fetch origin

$matchingBranches = git branch -r --list "origin/spec/$specNumber-*" |
  ForEach-Object { $_.Trim() } |
  Where-Object { $_ }

switch ($matchingBranches.Count) {
  0 { throw "No remote spec branch found for label $specLabel. Push spec/$specNumber-... before starting implementation." }
  1 { return $matchingBranches[0].Substring('origin/'.Length) }
  default { throw "Multiple spec branches match label $specLabel: $($matchingBranches -join ', '). Choose one explicitly before continuing." }
}
```

### Expected Behavior

- Issues without a `spec-{NNN}` label branch from the repository default branch.
- Issues with a `spec-{NNN}` label branch from the single matching `spec/{NNN}-...` branch.
- Missing or ambiguous spec branches are treated as blocking errors so the agent does not guess.

---

## Implementer Workflow

Set up the implementer identity before running these commands.

### I-1: Create Feature Branch

```powershell
# First determine the correct base branch using the helper above.

# Preferred for spec-scoped implementation work
git checkout <base-branch>
git pull origin <base-branch>
git checkout -b issue-<number>-<title-slug>
git push -u origin issue-<number>-<title-slug>

# Only use gh issue develop when the intended base is the repository default branch
gh issue develop <issue-number> --checkout --branch-repo <owner>/<repo>
git push -u origin issue-<number>-<title-slug>
```

Branch convention: `issue-{N}-{kebab-title}` (e.g., `issue-42-fix-auth-expiry`)

### I-2: Create Draft PR

```powershell
gh pr create `
  --title "<issue title>" `
  --body "Closes #<issue-number>

## Summary
<brief description>" `
  --draft `
  --base <base-branch>
```

`Closes #N` in the body auto-links the PR and auto-closes the issue on merge.
When the issue belongs to a spec, `<base-branch>` should be the matching `spec/{NNN}-...` branch so incomplete work stays off the default branch until the spec is complete.

### I-3: Push Implementation Commits

```powershell
git add -A
git commit -m "<message>"
git push origin <branch-name>
```

`git push` uses local git credentials — `GH_TOKEN` is not needed here.

### I-4: Request Review

```powershell
gh pr edit <pr-number> --add-reviewer <reviewer-github-username-or-team>
```

### I-5: Mark PR Ready for Review

```powershell
gh pr ready <pr-number>
```

### I-6: Respond to Review Feedback

After addressing feedback via new commits:

```powershell
gh pr comment <pr-number> --body "Addressed review feedback:
- <item 1>: <how resolved>
- <item 2>: <how resolved>"
```

---

## Reviewer Workflow

Set up the reviewer identity before running these commands.

### R-1: List PRs Available for Review

```powershell
gh pr list --state open --json number,title,headRefName,author,isDraft |
  ConvertFrom-Json |
  Where-Object { -not $_.isDraft } |
  Format-Table number, title, @{n='author';e={$_.author.login}}
```

### R-2: Check for Active Agent Work

Do not begin review while CI or an agent job is still running.

```powershell
# Block until all checks complete
gh pr checks <pr-number> --watch

# Or inspect current state
gh pr checks <pr-number> --json name,status,conclusion |
  ConvertFrom-Json | Where-Object { $_.status -ne 'completed' }
```

> There is no direct `gh` equivalent for `mcp_github_get_copilot_job_status`.
> See [Known Gaps](#known-gaps) for the workaround.

### R-3: Read the PR and Linked Issue

```powershell
# PR details
gh pr view <pr-number> --json title,body,state,isDraft,baseRefName,headRefName,author

# PR diff
gh pr diff <pr-number>

# Changed files
gh pr view <pr-number> --json files --jq '.files[].path'

# Linked issue (extract #N from PR body "Closes #N")
gh issue view <issue-number> --json title,body,labels,assignees,state
```

### R-4: Check Out PR for Review (Isolated Worktree)

```powershell
git fetch origin pull/<pr-number>/head:pr-<pr-number>
git worktree add ../pr-review-<pr-number> pr-<pr-number>

# Build and test inside the worktree
Set-Location ../pr-review-<pr-number>
# Run the project's build command (see copilot-instructions.md)
# Run the project's test command (see copilot-instructions.md)
```

### R-5: Get Prior Reviews and Comments

```powershell
# Review submissions (APPROVED, CHANGES_REQUESTED, etc.)
gh api /repos/<owner>/<repo>/pulls/<pr-number>/reviews

# Inline review comments
gh api /repos/<owner>/<repo>/pulls/<pr-number>/comments

# Commits on the PR
gh api /repos/<owner>/<repo>/pulls/<pr-number>/commits
```

### R-6: Submit Review

```powershell
# Approve
gh pr review <pr-number> --approve --body "All requirements met. Tests pass."

# Request changes (blocks merge)
gh pr review <pr-number> --request-changes --body "## Changes Required
1. [file.cs:42] <specific issue>
2. [other/file.cs] <specific issue>"

# Comment only (informational, non-blocking)
gh pr review <pr-number> --comment --body "## Notes
<observations>"
```

### R-7: Merge the PR

Only after approval and all checks pass:

```powershell
gh pr merge <pr-number> --squash --subject "<PR title> (#<pr-number>)"
```

### R-8: Delete Feature Branch

```powershell
# Via API — no local checkout required (preferred for reviewer)
gh api -X DELETE /repos/<owner>/<repo>/git/refs/heads/<branch-name>

# Via git — if repo is locally checked out
git push origin --delete <branch-name>
```

### R-9: Clean Up Worktree

```powershell
Set-Location <repo-root>
git worktree remove ../pr-review-<pr-number>
git branch -D pr-<pr-number>
```

---

## Complete Operation Reference

### Issues

| Need | Command |
|------|---------|
| Read issue | `gh issue view <N> --json title,body,labels,assignees,state,milestone` |
| Read issue comments | `gh api /repos/{owner}/{repo}/issues/<N>/comments` |
| Create issue | `gh issue create --title "..." --body "..." --label "..."` |
| Update issue | `gh issue edit <N> --title "..." --body "..."` |
| Close issue | `gh issue close <N>` |
| List open issues | `gh issue list --state open` |
| Search issues | `gh search issues "query" --repo <owner>/<repo>` |
| Add label | `gh issue edit <N> --add-label "label-name"` |
| Comment on issue | `gh issue comment <N> --body "..."` |

### Pull Requests

| Need | Command |
|------|---------|
| Create PR | `gh pr create --title "..." --body "Closes #N\n\n..." --draft` |
| View PR | `gh pr view <N> --json title,body,state,isDraft,baseRefName,headRefName,author` |
| List PRs | `gh pr list --state open` |
| Search PRs | `gh search prs "..." --repo <owner>/<repo>` |
| Get diff | `gh pr diff <N>` |
| Get changed files | `gh pr view <N> --json files --jq '.files[].path'` |
| Edit PR | `gh pr edit <N> --title "..." --body "..."` |
| Remove draft flag | `gh pr ready <N>` |
| Add reviewer | `gh pr edit <N> --add-reviewer <user>` |
| Comment on PR | `gh pr comment <N> --body "..."` |
| List PR comments | `gh pr comment <N> --list` |
| Check CI/checks | `gh pr checks <N>` |
| Wait for checks | `gh pr checks <N> --watch` |
| Get reviews | `gh api /repos/{owner}/{repo}/pulls/<N>/reviews` |
| Get inline comments | `gh api /repos/{owner}/{repo}/pulls/<N>/comments` |
| Get PR commits | `gh api /repos/{owner}/{repo}/pulls/<N>/commits` |
| Approve | `gh pr review <N> --approve --body "..."` |
| Request changes | `gh pr review <N> --request-changes --body "..."` |
| Comment review | `gh pr review <N> --comment --body "..."` |
| Merge (squash) | `gh pr merge <N> --squash` |
| Close PR | `gh pr close <N>` |

### Branches

| Need | Command |
|------|---------|
| Create + push | `git checkout <base-branch> && git pull origin <base-branch> && git checkout -b <branch> && git push -u origin <branch>` |
| Create from issue | `gh issue develop <N> --checkout` only when the intended base is the repository default branch |
| Delete branch (API) | `gh api -X DELETE /repos/{owner}/{repo}/git/refs/heads/<branch>` |
| Delete branch (git) | `git push origin --delete <branch>` |
| List remote branches | `gh api /repos/{owner}/{repo}/branches` |

### Project Board

| Need | Command |
|------|---------|
| List projects | `gh project list --owner <owner> --format json` |
| List project items | `gh project item-list <N> --owner <owner> --format json` |
| List project fields | `gh project field-list <N> --owner <owner> --format json` |
| Update item field | `gh project item-edit --id <item-id> --field-id <fid> --project-id <pid> --single-select-option-id <opt>` |

---

## MCP → gh Replacement Table

Every `mcp_github_*` call has a `gh` equivalent. Use this table when replacing MCP calls
in existing skills or writing new ones.

| MCP Tool | gh CLI Replacement |
|----------|--------------------|
| `mcp_github_issue_read(get)` | `gh issue view <N> --json title,body,labels,assignees,state,milestone` |
| `mcp_github_issue_read(get_comments)` | `gh api /repos/{owner}/{repo}/issues/<N>/comments` |
| `mcp_github_issue_read(get_labels)` | `gh issue view <N> --json labels` |
| `mcp_github_issue_write(create)` | `gh issue create --title "..." --body "..." --label "..."` |
| `mcp_github_issue_write(update)` | `gh issue edit <N> [--title "..."] [--body "..."]` |
| `mcp_github_add_issue_comment` | `gh issue comment <N> --body "..."` |
| `mcp_github_add_issue_comment (on PR)` | `gh pr comment <N> --body "..."` |
| `mcp_github_search_issues` | `gh search issues "..." --repo <owner>/<repo>` |
| `mcp_github_search_pull_requests` | `gh search prs "..." --repo <owner>/<repo>` |
| `mcp_github_list_pull_requests` | `gh pr list --json number,title,headRefName,body,state` |
| `mcp_github_create_branch` | `git checkout -b <branch> && git push -u origin <branch>` |
| `mcp_github_create_pull_request` | `gh pr create --title "..." --body "Closes #N\n..." --draft` |
| `mcp_github_pull_request_read(get)` | `gh pr view <N> --json title,body,state,isDraft,baseRefName,headRefName,author` |
| `mcp_github_pull_request_read(get_diff)` | `gh pr diff <N>` |
| `mcp_github_pull_request_read(get_files)` | `gh pr view <N> --json files` |
| `mcp_github_pull_request_read(get_reviews)` | `gh api /repos/{owner}/{repo}/pulls/<N>/reviews` |
| `mcp_github_pull_request_read(get_review_comments)` | `gh api /repos/{owner}/{repo}/pulls/<N>/comments` |
| `mcp_github_pull_request_read(get_check_runs)` | `gh pr checks <N> --json name,status,conclusion` |
| `mcp_github_pull_request_read(get_comments)` | `gh pr comment <N> --list` |
| `mcp_github_list_commits` | `gh api /repos/{owner}/{repo}/pulls/<N>/commits` |
| `mcp_github_update_pull_request` | `gh pr edit <N> [--draft=false] [--title "..."] [--body "..."]` |
| `mcp_github_pull_request_review_write(APPROVE)` | `gh pr review <N> --approve --body "..."` |
| `mcp_github_pull_request_review_write(REQUEST_CHANGES)` | `gh pr review <N> --request-changes --body "..."` |
| `mcp_github_pull_request_review_write(COMMENT)` | `gh pr review <N> --comment --body "..."` |
| `mcp_github_merge_pull_request` | `gh pr merge <N> --squash [--subject "..."]` |
| `mcp_github_get_file_contents` | `gh api /repos/{owner}/{repo}/contents/<path>` |
| `mcp_github_list_branches` | `gh api /repos/{owner}/{repo}/branches` |
| `mcp_github_get_copilot_job_status` | See [Known Gaps](#known-gaps) |
| `mcp_github_pull_request_review_write(resolve_thread)` | See [Known Gaps](#known-gaps) |

---

## Known Gaps

### 1. Copilot Agent Status (`mcp_github_get_copilot_job_status`)

No direct `gh` CLI equivalent.

**Workaround** — poll PR check runs as proxy for agent activity:

```powershell
# Block until all checks complete
gh pr checks <pr-number> --watch

# Non-blocking poll
do {
    $pending = gh pr checks <pr-number> --json name,status |
        ConvertFrom-Json | Where-Object { $_.status -ne 'completed' }
    if ($pending) { Write-Host "Waiting for checks..."; Start-Sleep 30 }
} while ($pending)
```

If the agent runs via GitHub Actions:

```powershell
gh run list --branch <branch-name> --json status,conclusion,name,databaseId
gh run watch <run-id>
```

### 2. Resolve Review Thread (`resolve_thread`)

No `gh pr` subcommand. Use GraphQL:

```powershell
# Get thread node IDs
gh api graphql -f query='
  query {
    repository(owner: "<owner>", name: "<repo>") {
      pullRequest(number: <N>) {
        reviewThreads(first: 100) {
          nodes { id isResolved comments(first: 1) { nodes { body path line } } }
        }
      }
    }
  }'

# Resolve a specific thread
gh api graphql -f query='
  mutation { resolveReviewThread(input: { threadId: "<THREAD_NODE_ID>" }) { thread { isResolved } } }'
```

### 3. Inline Review Comments by Line

`gh pr review` only accepts a body comment on the whole PR. For file+line-specific
comments, use the REST API directly:

```powershell
# Get head SHA
$headSha = gh pr view <N> --json headRefOid --jq '.headRefOid'

# Post inline comment
gh api POST /repos/<owner>/<repo>/pulls/<N>/comments `
  -f body="<comment text>" `
  -f commit_id="$headSha" `
  -f path="<relative/file/path>" `
  -F line=<line-number> `
  -f side="RIGHT"
```
