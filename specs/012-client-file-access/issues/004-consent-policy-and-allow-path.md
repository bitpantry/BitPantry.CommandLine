<!--
  STAGED ISSUE — not yet published to GitHub.
  Use /publish-issues to create this issue on GitHub.
  
  Staging Number: 004
  GitHub Issue Number: #54
-->

# File access consent policy and ConnectCommand --allow-path

**Labels**: enhancement, spec-012
**Blocked by**: None
**Implements**: FR-009, FR-011
**Covers**: US-004

## Summary

Implement `FileAccessConsentPolicy` — the client-side rule engine that determines whether a server-initiated file access request requires user consent based on `--allow-path` patterns configured at connect time. Also extend `ConnectCommand` with the `--allow-path` argument and store the patterns in the consent policy.

## Current Behavior

`ConnectCommand` has arguments for `Uri`, `ApiKey`, `TokenRequestEndpoint`, `ProfileName`, and `Force`. There is no mechanism for the user to pre-approve file access paths. No consent policy infrastructure exists.

## Expected Behavior

`ConnectCommand` accepts one or more `--allow-path` arguments (glob patterns). After connection, these patterns are stored in `FileAccessConsentPolicy`. The policy can evaluate any path against the allowed patterns: paths matching an allowed pattern don't require consent; all other paths require a prompt. With no allowed paths configured, every path requires consent.

## Affected Area

- **Project(s):** `BitPantry.CommandLine.Remote.SignalR.Client`
- **Key files:**
  - `BitPantry.CommandLine.Remote.SignalR.Client/FileAccessConsentPolicy.cs` — NEW
  - `BitPantry.CommandLine.Remote.SignalR.Client/Commands/Server/ConnectCommand.cs` — MODIFY: add --allow-path argument
  - `BitPantry.CommandLine.Remote.SignalR.Client/CommandLineApplicationBuilderExtensions.cs` — MODIFY: register policy in DI
- **Spec reference:** See `specs/012-client-file-access/spec.md`
- **Plan reference:** See `specs/012-client-file-access/plan.md`

## Requirements

- [ ] `FileAccessConsentPolicy` exists with `IsAllowed(string path)` method returning `bool` (FR-011)
- [ ] `FileAccessConsentPolicy` exists with `RequiresConsent(string path)` returning `!IsAllowed(path)` (FR-009)
- [ ] `FileAccessConsentPolicy` has `SetAllowedPatterns(IEnumerable<string> patterns)` to configure patterns (FR-011)
- [ ] When no patterns are configured, `IsAllowed` returns `false` for all paths (FR-009)
- [ ] Glob patterns with `*`, `**`, and `?` wildcards are supported (FR-011)
- [ ] Pattern matching is case-insensitive on Windows (FR-011)
- [ ] `ConnectCommand` has `--allow-path` / `-a` argument that accepts multiple values (FR-011)
- [ ] After successful connection, `ConnectCommand` stores allowed paths in `FileAccessConsentPolicy` (FR-011)
- [ ] `FileAccessConsentPolicy` is registered as singleton in client DI (FR-011)

## Prerequisites

No prerequisites — this issue can be started independently.

## Implementation Guidance

### FileAccessConsentPolicy

```csharp
public class FileAccessConsentPolicy
{
    private readonly List<string> _allowedPatterns = new();

    public void SetAllowedPatterns(IEnumerable<string> patterns)
    {
        _allowedPatterns.Clear();
        _allowedPatterns.AddRange(patterns);
    }

    public bool IsAllowed(string path)
    {
        if (_allowedPatterns.Count == 0) return false;
        // Use Matcher from Microsoft.Extensions.FileSystemGlobbing
        // or GlobPatternHelper for consistency
        return _allowedPatterns.Any(pattern => MatchesPattern(path, pattern));
    }

    public bool RequiresConsent(string path) => !IsAllowed(path);

    public IReadOnlyList<string> GetPathsRequiringConsent(IEnumerable<string> paths)
        => paths.Where(RequiresConsent).ToList();
}
```

For pattern matching, consider reusing `GlobPatternHelper.GlobPatternToRegex()` to convert each allowed pattern to a regex and match against the full path. This is consistent with how glob matching works elsewhere in the codebase.

### ConnectCommand Extension

Add to the existing arguments:

```csharp
[Argument(Name = "allow-path")]
[Alias('a')]
[Rest]  // collects multiple values: --allow-path c:\data\** --allow-path c:\backups\**
[Description("Client paths the server may access without prompting (glob patterns)")]
public string[] AllowPaths { get; set; }
```

After successful connection (at end of `Execute`), store in the policy:

```csharp
_consentPolicy.SetAllowedPatterns(AllowPaths ?? Array.Empty<string>());
```

### DI

```csharp
builder.Services.AddSingleton<FileAccessConsentPolicy>();
```

## Implementer Autonomy

This issue was authored from a specification and plan — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description.
3. **Do not** silently drop requirements or weaken test coverage.

## Testing Requirements

### Test Approach

- **Test level:** Unit (pure logic, no mocks needed for policy; ConnectCommand tested via integration later)
- **Test project:** `BitPantry.CommandLine.Tests.Remote.SignalR`
- **Existing fixtures to reuse:** None — pure unit tests

### Prescribed Test Cases

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `IsAllowed_NoPatterns_ReturnsFalse` | No patterns configured | Returns `false` for any path |
| 2 | `IsAllowed_ExactMatch_ReturnsTrue` | Pattern is `c:\data\file.txt`, path is same | Returns `true` |
| 3 | `IsAllowed_StarGlob_MatchesFiles` | Pattern `c:\data\*`, path `c:\data\file.txt` | Returns `true` |
| 4 | `IsAllowed_DoubleStarGlob_MatchesRecursive` | Pattern `c:\data\**`, path `c:\data\sub\deep\file.txt` | Returns `true` |
| 5 | `IsAllowed_QuestionMark_MatchesSingleChar` | Pattern `file?.txt`, path `file1.txt` | Returns `true` |
| 6 | `IsAllowed_NonMatchingPath_ReturnsFalse` | Pattern `c:\data\**`, path `c:\secrets\pw.txt` | Returns `false` |
| 7 | `IsAllowed_CaseInsensitive_Windows` | Pattern `C:\Data\**`, path `c:\data\file.txt` | Returns `true` (on Windows) |
| 8 | `RequiresConsent_AllowedPath_ReturnsFalse` | Path is allowed | `RequiresConsent` returns `false` |
| 9 | `RequiresConsent_UnallowedPath_ReturnsTrue` | Path not in allowed | `RequiresConsent` returns `true` |
| 10 | `SetAllowedPatterns_ReplacesExisting` | Set patterns, then set new ones | Only new patterns apply |
| 11 | `GetPathsRequiringConsent_MixedPaths_ReturnsOnlyUnapproved` | Some paths allowed, some not | Returns only the unapproved paths |
| 12 | `IsAllowed_MultiplePatterns_AnyMatch_ReturnsTrue` | Two patterns, path matches second | Returns `true` |

### Discovering Additional Test Cases

The test cases above are a starting point. During implementation, **discover and add additional test cases** as you encounter edge cases or error paths not covered above.

### TDD Workflow

Follow the `tdd-workflow` skill: write failing tests first (RED), implement (GREEN), refactor.
