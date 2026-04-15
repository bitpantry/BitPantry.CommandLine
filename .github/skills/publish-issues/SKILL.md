---
name: publish-issues
description: "Publish staged issue files to GitHub as real issues. Use when: pushing staged issues to GitHub, creating issues from staged markdown files, publishing a spec's work items."
argument-hint: "Spec number (e.g., 006)"
---

# Publish Issues

Read staged issue markdown files and create them as GitHub issues with labels and cross-references.

## When to Use

- After `/analyze` validates staged issues
- When ready to publish work items to GitHub

## Next Steps

After publishing:
- `/work-issue` â€” Pick up an issue and implement it

## Procedure

### Step 0: Validate Inputs

The user **must** supply a **spec number** (e.g., `006`). If not provided, **ask the user** and **STOP**. Do not guess or assume.

Locate the spec directory: `specs/{NNN}-*/`. Verify the `issues/` subdirectory exists with staged issue files. If not found, inform the user and suggest running `/issues` first. **STOP**.

### Step 1: Detect Repository

Determine the target GitHub `owner/repo`:

1. Run `git remote get-url origin` in the workspace root.
2. Extract the `owner/repo` slug from the URL (handles both `https://github.com/owner/repo.git` and `git@github.com:owner/repo.git` formats).
3. Use this value for all GitHub API calls.

If the remote is not a GitHub URL, inform the user and **STOP**.

### Step 2: Read Staged Issues

Read all numbered issue files from `specs/{NNN}-{name}/issues/`:
- `001-*.md`, `002-*.md`, etc. â€” individual issues, in order

Skip `000-tracking.md` and `execution-plan.md` â€” these are local planning artifacts, not published to GitHub.

Parse from each issue file:
- Title (first `#` heading)
- Labels (from the `Labels:` line)
- Body content

### Step 3: Labels

The `spec-{NNN}` label (e.g., `spec-006`) is created implicitly by GitHub when it is first attached to an issue. Do **not** create a throwaway issue just to establish the label. Simply include the label in the `labels` array when creating the first real issue â€” GitHub will create the label automatically.

### Step 4: Present Summary and Confirm

Before creating anything, show the user what will be published:

```
## Publishing {N} Issues for Spec {NNN}

| Staging # | Title | Labels |
|-----------|-------|--------|
| 001 | [title] | enhancement, spec-{NNN} |
| 002 | [title] | enhancement, spec-{NNN} |
| ... | ... | ... |

This will create {N} GitHub issues.
Proceed? (yes/no)
```

**Wait for confirmation.** Do not create issues without explicit approval.

### Step 5: Create Issues in Order

Create issues **in staging order** (001, 002, 003, ...) so that issue numbers are sequential and predictable.

For each staged issue:

1. **Resolve prerequisite references**: Replace `Blocked by: 001` with `Blocked by #XX` using the real GitHub issue number from the previously created issue.

2. **Create the issue** using `mcp_github_create_or_update_file` or `mcp_github_add_issue_comment` â€” actually, use the appropriate GitHub MCP tool for issue creation. Apply:
   - Title from the staged file
   - Body with resolved references
   - Labels: parsed from the file (e.g., `enhancement`, `spec-{NNN}`)

3. **Record the mapping**: staging number â†’ real GitHub issue number

4. **Update the staged file**: Add the real GitHub issue number to the file header comment:
   ```
   GitHub Issue Number: #XX
   ```

### Step 6: Report Completion

Output:

```
## Published Issues for Spec {NNN}

| Staging # | GitHub Issue | Title |
|-----------|-------------|-------|
| 001 | #47 | [title] |
| 002 | #48 | [title] |
| 003 | #49 | [title] |

Label: spec-{NNN}

To start implementation:
  /work-issue 47
```

If the current branch is a spec branch (`spec/*`), add a reminder:

```
Spec work is complete. Before starting implementation:
  1. Commit and push the spec branch
  2. Open a PR from spec/{NNN}-{short-name} â†’ main
  3. Merge the spec PR so implementation branches can base off main
```
