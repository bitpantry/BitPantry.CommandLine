---
description: "GitHub tool policy for this project: always use gh CLI and git for GitHub operations. Never use MCP github tools. Use when performing any GitHub interaction."
---

# GitHub Operations Policy

## Do NOT Use the GitHub MCP Server

The GitHub MCP server (`mcp_github_*` tools) is **not used** in this project.

> If you are about to call any tool whose name starts with `mcp_github_`, stop and use
> the `gh` CLI equivalent from the `github-ops` skill instead.

## Required Tools

| Tool | Use For | Identity Setup |
|------|---------|---------------|
| `gh` / `gh.exe` | All GitHub API operations | Set `GH_TOKEN` — see `github-ops` skill |
| `git` | Local repo operations (checkout, commit, push) | None required |
| `github` / `github.exe` | Copilot CLI agent operations | None — uses VS Code default auth |

## Identity Rules — Non-Negotiable

`gh` identity is controlled by `GH_TOKEN`. Set it by dot-sourcing before any `gh` call:

```powershell
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity reviewer
```

These rules are **hard constraints**, not recommendations. Violations cause pipeline failures
because GitHub blocks a PR author from approving their own PR (HTTP 422).

### implementer MUST do — MUST NEVER review/approve/merge

| MUST use implementer for | MUST NEVER use implementer for |
|--------------------------|-------------------------------|
| Create branch | Submit any review (`gh pr review`) |
| Create draft PR | Approve a PR |
| Push implementation commits | Merge a PR |
| Mark PR ready / request review | |
| Respond to review feedback | |

### reviewer MUST do — MUST NEVER create/push

| MUST use reviewer for | MUST NEVER use reviewer for |
|-----------------------|-----------------------------|
| Submit reviews (approve / request-changes) | Create a branch |
| Merge the PR | Create or author a PR |
| Delete branch after merge | Push commits to the feature branch |
| Move issue to Done | |

### Either identity may — read-only operations

`gh issue view`, `gh pr view`, `gh pr diff`, `gh pr checks`, reading reviews/comments.

See the `github-ops` skill for the complete command reference and full workflow sequence.

## MCP → gh Quick Reference

| Instead of this MCP call… | Use this gh command |
|--------------------------|---------------------|
| `mcp_github_issue_read` | `gh issue view <N> --json ...` |
| `mcp_github_issue_write` | `gh issue create / gh issue edit` |
| `mcp_github_pull_request_read` | `gh pr view <N> --json ...` / `gh pr diff <N>` |
| `mcp_github_create_pull_request` | `gh pr create --title "..." --body "..." --draft` |
| `mcp_github_pull_request_review_write` | `gh pr review <N> --approve / --request-changes` |
| `mcp_github_merge_pull_request` | `gh pr merge <N> --squash` |
| `mcp_github_create_branch` | `git checkout -b <branch> && git push -u origin <branch>` |
| `mcp_github_list_commits` | `gh api /repos/{owner}/{repo}/pulls/<N>/commits` |

For the complete reference and all edge cases, invoke the `github-ops` skill.
