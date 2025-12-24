# Phase 0: Research - Command Groups Feature

## Overview

This document captures research findings for implementing the command groups feature as specified in [spec.md](spec.md).

---

## 1. Command Processing Pipeline

### Current Flow

```
User Input → ParsedInput → CommandResolver.Resolve() → ResolvedInput → Execution Loop
```

**Key Files:**
- [CommandLineApplicationCore.cs](../../BitPantry.CommandLine/Processing/Execution/CommandLineApplicationCore.cs) - Orchestrates the pipeline
- [ParsedInput.cs](../../BitPantry.CommandLine/Processing/Parsing/ParsedInput.cs) - Parses raw string into commands
- [CommandResolver.cs](../../BitPantry.CommandLine/Processing/Resolution/CommandResolver.cs) - Resolves parsed commands to registered commands
- [CommandRegistry.cs](../../BitPantry.CommandLine/CommandRegistry.cs) - Stores command registrations

### Run() Method Pipeline (lines 90-160)

```csharp
// 1. Parse commands
var parsedInput = new ParsedInput(inputStr);

// 2. Resolve commands
var resolvedInput = _resolver.Resolve(parsedInput);

// 3. Execute commands  
while (resolvedCmdStack.Count > 0)
{
    var cmd = resolvedCmdStack.Pop();
    // Execute...
}
```

### Help Interception Point

**Decision:** Help flag (`--help`/`-h`) interception should occur **after resolution but before execution**.

**Rationale:**
1. After parsing: We need to know what the user typed
2. After resolution: We need command/group metadata for help display
3. Before execution: Help should NOT run the command - it displays info and exits

**Implementation Location:** Between lines ~122 and ~131 in `CommandLineApplicationCore.Run()`:

```csharp
var resolvedInput = _resolver.Resolve(parsedInput);
// ... error handling ...

// NEW: Help flag interception
if (HelpFlagDetected(parsedInput))
{
    DisplayHelp(resolvedInput);
    return new RunResult { ResultCode = RunResultCode.Success };
}

// Execute commands...
```

This design keeps help logic **out of individual command classes**.

---

## 2. Current Namespace Implementation

### CommandAttribute (API/CommandAttribute.cs)

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandAttribute : Attribute
{
    public string Name { get; set; }
    public string Namespace { get; set; }  // TO BE REMOVED
}
```

### CommandInfo (Component/CommandInfo.cs)

```csharp
public class CommandInfo
{
    public string Name { get; set; }
    public string Namespace { get; set; }  // TO BE REPLACED with GroupInfo
    
    public string FullyQualifiedName => string.IsNullOrEmpty(Namespace)
        ? Name
        : $"{Namespace}.{Name}";
}
```

### CommandRegistry.Find() - BUG IDENTIFIED

```csharp
public CommandInfo Find(string fullyQualifiedCommandName)
{
    var parts = fullyQualifiedCommandName.Split(new char[] { '.' }, 2);
    
    if (parts.Length == 1)
        // Root command lookup
    else
        // Namespace.Command lookup - ONLY SPLITS ON FIRST DOT
}
```

**BUG:** For `my.math.add`, this produces:
- `namespace = "my"`
- `name = "math.add"`

This breaks nested namespaces. The new group implementation must handle arbitrary nesting.

---

## 3. New Data Model Design

### GroupAttribute (New)

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GroupAttribute : Attribute
{
    public string Description { get; set; }
}
```

### GroupInfo (New)

```csharp
public class GroupInfo
{
    public string Name { get; }          // From class name, e.g., "Math" → "math"
    public string Description { get; }   // From [Group] or [Description] attribute
    public GroupInfo Parent { get; }     // Null for top-level groups
    public Type MarkerType { get; }      // The [Group] class type
    
    public string FullPath => Parent == null 
        ? Name 
        : $"{Parent.FullPath}.{Name}";  // Internal representation only
}
```

### Updated CommandAttribute

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandAttribute : Attribute
{
    public string Name { get; set; }
    public Type Group { get; set; }  // NEW: Reference to [Group] marker class
}
```

### Updated CommandInfo

```csharp
public class CommandInfo
{
    public string Name { get; set; }
    public GroupInfo Group { get; set; }  // NEW: Replaces Namespace
    
    public string FullyQualifiedName => Group == null
        ? Name
        : $"{Group.FullPath}.{Name}";  // Internal representation
}
```

---

## 4. Parsing Changes

### Current: Dot-Notation (Single Token)

```
Input: "math.add 1 2"
Parsed: Command="math.add", Args=["1", "2"]
```

### New: Space-Separated (Multiple Tokens)

```
Input: "math add 1 2"
Parsed: GroupPath=["math"], Command="add", Args=["1", "2"]
```

**Challenge:** Disambiguate between group path tokens and argument values.

**Solution:** Registry lookup during parsing:
1. Consume tokens as potential group/command path
2. Once command is found in registry, remaining tokens are arguments
3. If no command found, check if final token is a group (display group help)

### ParsedCommand Updates

```csharp
public class ParsedCommand
{
    public List<string> GroupPath { get; }  // NEW: e.g., ["math", "advanced"]
    public string CommandName { get; }       // e.g., "add"
    // ... existing argument handling
}
```

---

## 5. Resolution Changes

### New Resolution Logic

```csharp
public ResolvedCommand Resolve(ParsedCommand parsedCmd)
{
    // 1. Navigate group hierarchy
    GroupInfo currentGroup = null;
    foreach (var segment in parsedCmd.GroupPath)
    {
        currentGroup = _registry.FindGroup(segment, currentGroup);
        if (currentGroup == null)
            return Error(GroupNotFound);
    }
    
    // 2. Find command within group
    var cmdInfo = _registry.FindCommand(parsedCmd.CommandName, currentGroup);
    
    // 3. If not found, check if it's a group (for help display)
    if (cmdInfo == null)
    {
        var targetGroup = _registry.FindGroup(parsedCmd.CommandName, currentGroup);
        if (targetGroup != null)
            return new ResolvedCommand(ResolvedType.Group, targetGroup);
    }
    
    // ... continue with argument resolution
}
```

### ResolvedCommand Updates

```csharp
public class ResolvedCommand
{
    public ResolvedType Type { get; }  // NEW: Command or Group
    public CommandInfo CommandInfo { get; }
    public GroupInfo GroupInfo { get; }  // NEW: For group-only resolution
}

public enum ResolvedType
{
    Command,
    Group
}
```

---

## 6. Help Display Integration

### Help Flag Detection

**Important constraint:** Help must be an **explicit, standalone request** - not mixed with other arguments or pipelines.

**Valid help request:** Single command with ONLY the help flag (no other arguments).
**Invalid:** Help flag mixed with other arguments, or in a pipeline.

Add to `CommandLineApplicationCore`:

```csharp
private (bool isHelpRequest, string errorMessage) CheckHelpRequest(ParsedInput input)
{
    var commandsWithHelp = input.ParsedCommands
        .Where(cmd => HasHelpFlag(cmd))
        .ToList();
    
    if (commandsWithHelp.Count == 0)
        return (false, null);
    
    // Get the command with the help flag
    var cmd = commandsWithHelp.First();
    var slug = BuildCommandSlug(cmd);  // e.g., "file upload"
    
    // Help must be standalone - no pipeline, no other arguments
    bool isStandalone = input.ParsedCommands.Count == 1 
        && !cmd.Elements
            .Where(e => e.Value != "--help" && e.Value != "-h")
            .Where(e => e.ElementType != CommandElementType.Command)
            .Any();
    
    if (!isStandalone)
        return (false, $"error: --help cannot be combined with other arguments\nFor usage, run: {slug} --help");
    
    return (true, null);
}

private bool HasHelpFlag(ParsedCommand cmd)
{
    return cmd.Elements.Any(e => 
        e.Value == "--help" || e.Value == "-h");
}
```

**Behavior:**

| Input | Result |
|-------|--------|
| `cmd --help` | ✅ Show help for cmd |
| `cmd -h` | ✅ Show help for cmd |
| `group cmd --help` | ✅ Show help for group/cmd |
| `group --help` | ✅ Show help for group |
| `cmd -f 'test.dat' --help` | ❌ `error: --help cannot be combined with other arguments`<br>`For usage, run: cmd --help` |
| `file upload -f 'c:\test.dat' -h` | ❌ `error: --help cannot be combined with other arguments`<br>`For usage, run: file upload --help` |
| `cmd1 --help \| cmd2` | ❌ `error: --help cannot be combined with other arguments`<br>`For usage, run: cmd1 --help` |
| `cmd1 \| cmd2 --help` | ❌ `error: --help cannot be combined with other arguments`<br>`For usage, run: cmd2 --help` |

### Help Display Logic

```csharp
private void DisplayHelp(ResolvedInput resolvedInput)
{
    var resolved = resolvedInput.ResolvedCommands.First();
    
    if (resolved.Type == ResolvedType.Group)
        _helpFormatter.DisplayGroupHelp(resolved.GroupInfo);
    else
        _helpFormatter.DisplayCommandHelp(resolved.CommandInfo);
}
```

### New HelpFormatter Service

```csharp
public interface IHelpFormatter
{
    void DisplayGroupHelp(GroupInfo group);
    void DisplayCommandHelp(CommandInfo command);
    void DisplayRootHelp(CommandRegistry registry);
}
```

Implementation uses `IAnsiConsole` (Spectre.Console) for rich formatting.

---

## 7. Files Requiring Modification

### Core Implementation Files

| File | Changes Required |
|------|------------------|
| `API/CommandAttribute.cs` | Remove `Namespace`, add `Group` property |
| `API/GroupAttribute.cs` | **NEW FILE** |
| `Component/CommandInfo.cs` | Replace `Namespace` with `Group` reference |
| `Component/GroupInfo.cs` | **NEW FILE** |
| `CommandRegistry.cs` | Add group registry, update `Find()` methods |
| `Processing/Parsing/ParsedInput.cs` | Handle space-separated group paths |
| `Processing/Parsing/ParsedCommand.cs` | Add `GroupPath` property |
| `Processing/Resolution/CommandResolver.cs` | Group-aware resolution |
| `Processing/Resolution/ResolvedCommand.cs` | Add group resolution type |
| `Processing/Execution/CommandLineApplicationCore.cs` | Help flag interception |
| `Commands/ListCommandsCommand.cs` | Display groups instead of namespaces |
| `Help/HelpFormatter.cs` | **NEW FILE** - Help display logic |
| `Help/IHelpFormatter.cs` | **NEW FILE** - Interface |

### Test Files Requiring Updates

| File | Changes Required |
|------|------------------|
| `DescribeCommandsTests.cs` | Update for group model |
| `ResolveCommandsTests.cs` | Update for group resolution |
| `ActivateCommandsTests.cs` | Update test commands |
| `AutoCompleteTests.cs` | Update for space-separated syntax |
| `NamespaceTests.cs` | **REMOVE** or rename to `GroupTests.cs` |
| All test command classes | Replace `Namespace` with `Group` |

### Documentation Files

| File | Changes Required |
|------|------------------|
| `README.md` | Update examples, terminology |
| `Docs/getting-started.md` | Update quick start guide |
| `Docs/syntax.md` | Update command syntax documentation |
| `Docs/advanced-topics.md` | Update advanced patterns |
| All XML documentation | Update code comments |

---

## 8. Constitution Check

### TDD Requirements
- ✅ All changes must have corresponding tests
- ✅ Tests written before implementation (red-green-refactor)
- ✅ FluentAssertions for assertions
- ✅ Moq for mocking

### DI Patterns
- ✅ `IHelpFormatter` injected via DI
- ✅ No static service location
- ✅ Follow existing `CommandActivator` patterns

### Security by Design
- ✅ Input validation on group paths
- ✅ No path traversal concerns (type-based references)

### Follow Existing Patterns
- ✅ Use `IAnsiConsole` for output
- ✅ Match `CommandInfo`/`CommandRegistry` patterns for `GroupInfo`/`GroupRegistry`
- ✅ Consistent naming conventions

---

## 9. Breaking Changes

### Removed
- `CommandAttribute.Namespace` property
- `CommandInfo.Namespace` property
- Dot-notation parsing (`math.add` → `math add`)

### Changed
- `CommandAttribute.Group` is now `Type` (was string `Namespace`)
- `CommandRegistry.Find()` signature and behavior
- Built-in `lc` command output format

### Migration Path
1. Create `[Group]` marker classes for each namespace
2. Update `[Command(Namespace = "x")]` to `[Command(Group = typeof(XGroup))]`
3. Update all documentation and user-facing examples

---

## 10. Open Questions (Resolved in Spec)

| Question | Resolution |
|----------|------------|
| Should groups be executable? | No (FR-001) |
| Exit code for group help? | 0 (FR-003) |
| Allow root commands? | Yes (FR-008) |
| Name collision handling? | Disallowed at same level (FR-013) |
| Case sensitivity? | Configurable, default sensitive (FR-014) |
| Help flag reservation? | `--help`/`-h` reserved at framework level (FR-016) |
