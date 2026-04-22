---
name: create-issue
description: "Create a detailed GitHub issue for the current repository. Use when: filing bugs, requesting features, creating enhancement issues, reporting defects, proposing changes, creating work items, writing issue descriptions."
argument-hint: "Description of the issue to create, or invoke after discussing a problem in chat"
---

# Create Issue

Create a well-structured GitHub issue on the current workspace's GitHub repository with enough detail for an implementer (human or agent) to understand and resolve it without additional context.

## Prerequisites

- `gh` CLI authenticated via implementer identity (see `github-ops` skill)
- The workspace must be a git repository with a GitHub remote

**Identity setup (local agents only)** — if running locally with the `gh` CLI, run this before Step 6 (the only step that calls `gh`):

```powershell
. .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer
```

Cloud-hosted agents (e.g., GitHub Copilot coding agent) should skip identity setup and use their built-in GitHub access.

## Step 0: Detect Repository

Before anything else, determine the target GitHub `owner/repo` by running `git remote get-url origin` in the workspace root and extracting the `owner/repo` slug from the URL (handles both `https://github.com/owner/repo.git` and `git@github.com:owner/repo.git` formats). Use this value for all GitHub API calls throughout the skill. If the remote is not a GitHub URL, inform the user and stop.

## Step 1: Gather Context

Determine the issue content from one of two sources:

1. **Explicit description** — The user provided a description of the issue when invoking this skill. Use that as the primary input.
2. **Conversation context** — The user has been exploring, debugging, or discussing a problem in this chat session. Review the full conversation history to extract the issue details: what was discovered, what the root cause is, what behavior is expected vs. actual, and what areas of the codebase are involved.

If neither source provides enough information to write a clear issue, **ask the user targeted questions** before proceeding:
- What is the expected behavior vs. actual behavior? (for bugs)
- What capability is missing or desired? (for enhancements)
- Which commands, classes, or areas of the codebase are involved?

## Step 2: Classify the Issue

Determine the issue type and select the appropriate label:

| Type | Label | When to Use |
|------|-------|-------------|
| Bug | `bug` | Something isn't working as expected; broken behavior, crashes, incorrect output |
| Enhancement | `enhancement` | New feature, new command, new capability, improved behavior, UX improvement |
| Documentation | `documentation` | Missing or incorrect docs, README updates, inline documentation gaps |

If the issue spans multiple types (e.g., a bug that also needs a new feature to fix properly), use the **primary** label — the one that best describes the core ask.

## Step 3: Research the Codebase

Before writing the issue, gather enough context to make the description actionable:

1. **Identify affected code** — Search the workspace to locate the specific classes, commands, methods, or files involved. Use `grep_search`, `file_search`, or `semantic_search` as needed.
2. **Check for existing related issues** — Run `gh search issues "<keywords>" --repo <owner>/<repo> --state open` to check if a similar issue already exists. If one does, inform the user and ask whether to proceed or update the existing issue.
3. **Understand the current behavior** — Read the relevant source code to understand what the code currently does. For bugs, trace the problematic code path. For enhancements, understand what exists today.
4. **Identify test coverage** — Check if existing tests cover the affected area. Note which test project and test classes are relevant. This informs the testing section of the issue.

## Step 4: Draft the Issue

Compose the issue with the following structure. Every issue must be detailed enough for someone unfamiliar with the conversation to implement the fix or feature.

### Title

- Concise, specific, and action-oriented
- Bug format: `Bug: <what's broken> when <condition>`
- Enhancement format: `Enhancement: <what should be added/improved>`
- Documentation format: `Docs: <what needs documenting>`

### Body Template

Use this structure for the issue body:

**Required sections (all issues):** Summary, Requirements, Implementation Guidance, Implementer Autonomy, Testing Requirements

**Conditional sections:** Current/Expected Behavior (omit for documentation issues), Reproduction Steps (bugs only), Additional Context (when relevant)

Do NOT omit required sections. Apply the template as follows:

````markdown
## Summary

One to two sentences describing what this issue is about.

## Current Behavior

<!-- For bugs: what happens now (the broken behavior) -->
<!-- For enhancements: what the current state is (what's missing) -->

<description of current behavior or state>

## Expected Behavior

<!-- For bugs: what should happen instead -->
<!-- For enhancements: what the new capability should look like -->

<description of desired behavior>

## Affected Area

- **Module(s):** <which project module(s) or packages are involved>
- **Key classes/files:**
  - `<path/to/file>` — <brief role>
  - `<path/to/file>` — <brief role>

## Reproduction Steps (Bugs Only)

1. <step 1>
2. <step 2>
3. <step 3>

## Requirements (Non-Negotiable)

<!-- These are the outcomes that MUST be true when this issue is resolved. The implementer does not have discretion to skip or weaken these. -->

- <requirement 1 — a testable, observable outcome>
- <requirement 2>
- ...

## Implementation Guidance (Suggested Approach)

<!-- This section describes a recommended implementation path based on what we know right now. It is guidance, not a mandate — see "Implementer Autonomy" below. -->

<specific guidance on how to implement the fix or feature, including:>
- Which classes/methods likely need changes
- Any constraints or design considerations
- Edge cases to handle
- Security considerations (path traversal, injection, etc.) if applicable

## Implementer Autonomy

This issue was authored without hands-on implementation — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description (e.g., "Issue suggested modifying X, but Y was more appropriate because...").
3. **Do not** silently drop requirements or weaken test coverage to make an alternative work. If a requirement seems wrong in light of what you've learned, flag it for discussion rather than ignoring it.

## Testing Requirements

This project follows **TDD (test-driven development)** practices. The implementation must include tests that validate the change.

### Test Approach

- **Test level:** <based on the test level guidelines in `test-infrastructure.instructions.md` and the type of behavior being tested>
- **Test project:** <the appropriate test project(s) for the affected code — discover by examining the workspace's test directory structure>
- **Relevant existing tests:** <list any existing test classes/files that cover nearby behavior>
- **Shared helpers to reuse:** <list applicable shared test helpers discovered in the workspace's test utilities or helpers directories>

### Prescribed Test Cases

<!-- Specific test cases we know are needed. Each becomes a RED test before implementation. -->

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `MethodUnderTest_Scenario_ExpectedBehavior` | <when this happens> | <then this should result> |
| 2 | ... | ... | ... |

### Discovering Additional Test Cases

The test cases above are a starting point based on what we know at issue-creation time. During implementation, **you are expected to discover and add additional test cases** as you encounter edge cases, error paths, or behaviors that the prescribed cases don't cover. Use the same naming convention (`MethodUnderTest_Scenario_ExpectedBehavior`) and the same RED→GREEN workflow. The goal is comprehensive coverage of the actual implementation, not just checking off the list above.

### TDD Workflow

Follow the `tdd-workflow` skill:
- **Baseline first**: Before writing any code, run the full test suite (using the project's test command from `copilot-instructions.md`) and record the pass count. This is your regression baseline — every test that passes before your work must still pass after.
- **Bug fix**: Write a failing test that reproduces the bug → fix the code → verify the test passes
- **Enhancement**: Write a failing test for the new behavior → implement → verify
- **Final check**: Run the full test suite again and compare against your baseline. If any previously-passing test now fails, fix the regression before committing.

All tests must pass the **Mandatory Validation Checkpoint** before being considered complete:
```
Test Validity Check:
  Invokes code under test: [YES]
  Breakage detection: [YES]
  Not a tautology: [YES]
```

## Additional Context

<!-- Any other information: screenshots, logs, related PRs, links to specs, design decisions -->

<additional context if any>
````

### Writing Guidelines

- **Be specific** — Reference actual class names, method names, and file paths from the codebase. Don't use vague language like "the data code" when you can say the specific class, method name, and module path.
- **Include code snippets** — If the issue involves specific code patterns, include relevant snippets to illustrate.
- **Separate requirements from guidance** — Requirements are testable outcomes the implementer must deliver. Implementation Guidance is the suggested approach based on what we know now. Never mix these — an implementer must be able to read the Requirements section alone and know exactly what "done" looks like.
- **Requirements must be testable** — Each requirement should map to at least one test case. If you can't write a test for it, it's not a requirement — it's a preference, and it belongs in Implementation Guidance.
- **Test cases are mandatory but not exhaustive** — Prescribe the test cases we know are needed. Explicitly tell the implementer to discover additional cases during implementation.
- **Match test level to assertion type** — Use the test infrastructure guidance to recommend the correct test level (unit/integration/UX).
- **Reference helpers** — When relevant shared test helpers or fixtures exist in the project's test utilities (discover by reading `test-infrastructure.instructions.md` and exploring the test directory), mention them so the implementer reuses infrastructure rather than reinventing it.
- **Autonomy section is mandatory** — Every issue must include the Implementer Autonomy section. This is not optional.

## Step 5: Present Draft for Confirmation

Show the user the complete issue (title, labels, and body) and ask for confirmation or adjustments before creating it.

Format the preview clearly:

```
📋 Issue Draft

Title: <title>
Label: <label>

<full body>
```

**Ask the user to confirm or request changes before proceeding.**

## Step 6: Create the Issue

Once the user confirms:

1. Create the issue using `gh`:
   ```powershell
   gh issue create --repo <owner>/<repo> `
     --title "<title>" `
     --body "<body>" `
     --label "<label>"
   ```
2. Report the created issue number and URL back to the user.

## Important Notes

- **Repository scope only** — Issues are created on the detected repository only. Do not add cross-project references.
- **Do not add issues to GitHub Projects** — The skill only creates the issue. Project board management is handled separately.
- **Available labels** — Use only labels that exist on the repo: `bug`, `enhancement`, `documentation`. Do not invent labels.
- **No duplicates** — Always check for existing similar issues in Step 3 before creating a new one.
- **TDD is non-negotiable** — Every bug and enhancement issue must include testing requirements. The project's testing philosophy is that tests are specifications encoding business intent.
- **Autonomy is structural** — The Requirements vs. Implementation Guidance separation and the Implementer Autonomy section are not decorative. They exist to prevent a remote worker from following a prescribed path off a cliff when they can see a better route. Always include them.
- **Single recommended approach** — The Implementation Guidance section must present ONE recommended approach, not multiple options. Multiple options create ambiguity and decision paralysis for the implementer. If you considered alternatives during research, pick the best one and commit to it. The Implementer Autonomy section already gives the implementer freedom to deviate if they find a better path during implementation.
- **Conversation mining** — When invoked after a discussion, extract *all* relevant findings from the conversation. Don't lose details that were uncovered during exploration.
