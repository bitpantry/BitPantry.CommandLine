# Data Model: Command Groups Feature

## Overview

This document defines the data model changes required to implement the command groups feature, replacing the namespace-based organization with type-safe hierarchical groups.

---

## 1. New Entities

### GroupAttribute

**Purpose:** Marks a class as a command group container.

**Location:** `BitPantry.CommandLine/API/GroupAttribute.cs`

```csharp
using System;

namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Marks a class as a command group container. Groups organize related commands
    /// into hierarchical structures. Groups are non-executable and display help when invoked.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class GroupAttribute : Attribute
    {
        /// <summary>
        /// Optional group name override. If not specified, derived from the class name (lowercased, matching CommandAttribute behavior).
        /// Example: "UserManagement" class → "usermanagement"
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Optional description displayed in help output.
        /// If not provided, looks for [Description] attribute on the class.
        /// </summary>
        public string Description { get; set; }
    }
}
```

**Design Notes:**
- Uses same pattern as `CommandAttribute` (optional Name override)
- Name derived from class name if not specified (lowercased)
- Description is optional - can use `[Description]` attribute for consistency

---

### GroupInfo

**Purpose:** Runtime metadata for a registered command group.

**Location:** `BitPantry.CommandLine/Component/GroupInfo.cs`

```csharp
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Component
{
    /// <summary>
    /// Contains runtime information about a registered command group.
    /// </summary>
    public class GroupInfo
    {
        /// <summary>
        /// The group name derived from the marker class name (lowercased, matching CommandAttribute behavior).
        /// Example: "MathOperations" → "mathoperations"
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Human-readable description for help display.
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Parent group, or null for top-level groups.
        /// </summary>
        public GroupInfo Parent { get; }
        
        /// <summary>
        /// The marker class type decorated with [Group].
        /// </summary>
        public Type MarkerType { get; }
        
        /// <summary>
        /// Direct child groups nested within this group.
        /// </summary>
        public IReadOnlyList<GroupInfo> ChildGroups { get; }
        
        /// <summary>
        /// Commands directly contained in this group.
        /// </summary>
        public IReadOnlyList<CommandInfo> Commands { get; }
        
        /// <summary>
        /// The full hierarchical path (space-separated, matches invocation syntax).
        /// Example: "math advanced"
        /// </summary>
        public string FullPath => Parent == null 
            ? Name 
            : $"{Parent.FullPath} {Name}";
        
        /// <summary>
        /// Nesting depth (0 for top-level groups).
        /// </summary>
        public int Depth => Parent == null ? 0 : Parent.Depth + 1;
    }
}
```

**Relationships:**
- One-to-many with `CommandInfo` (a group contains commands)
- Self-referential one-to-many (parent-child groups)
- One-to-one with marker `Type`

---

## 2. Modified Entities

### CommandAttribute (Modified)

**Location:** `BitPantry.CommandLine/API/CommandAttribute.cs`

**Changes:**
- Remove `Namespace` property
- Add `Group` property (Type reference)

```csharp
using System;

namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Marks a class as a CLI command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CommandAttribute : Attribute
    {
        /// <summary>
        /// The command name. If not specified, derived from class name.
        /// </summary>
        public string Name { get; set; }
        
        // REMOVED: public string Namespace { get; set; }
        
        /// <summary>
        /// The group this command belongs to. Specify the Type of a class
        /// decorated with [Group]. Leave null for root-level commands.
        /// </summary>
        public Type Group { get; set; }
    }
}
```

**Migration Impact:**
- All existing `Namespace = "x"` usages must be replaced with `Group = typeof(XGroup)`

---

### CommandInfo (Modified)

**Location:** `BitPantry.CommandLine/Component/CommandInfo.cs`

**Changes:**
- Remove `Namespace` property
- Add `Group` property (GroupInfo reference)
- Update `FullyQualifiedName` computation

```csharp
public class CommandInfo
{
    // ... existing properties ...
    
    // REMOVED: public string Namespace { get; set; }
    
    /// <summary>
    /// The group this command belongs to, or null for root-level commands.
    /// </summary>
    public GroupInfo Group { get; set; }
    
    /// <summary>
    /// The fully qualified command name including group path (space-separated).
    /// Example: "math advanced sqrt"
    /// </summary>
    public string FullyQualifiedName => Group == null
        ? Name
        : $"{Group.FullPath} {Name}";
        
    // REMOVED: ValidateNamespace() method
}
```

---

### CommandRegistry (Modified)

**Location:** `BitPantry.CommandLine/CommandRegistry.cs`

**Changes:**
- Add group tracking
- Update lookup methods
- Remove namespace-based `Find()`

```csharp
public class CommandRegistry
{
    // Existing
    private readonly List<CommandInfo> _commands;
    
    // NEW: Group tracking
    private readonly List<GroupInfo> _groups;
    
    /// <summary>
    /// All registered groups (includes nested groups).
    /// </summary>
    public IReadOnlyList<GroupInfo> Groups => _groups.AsReadOnly();
    
    /// <summary>
    /// Top-level groups only (Parent == null).
    /// </summary>
    public IReadOnlyList<GroupInfo> RootGroups => 
        _groups.Where(g => g.Parent == null).ToList().AsReadOnly();
    
    /// <summary>
    /// Root-level commands (Group == null).
    /// </summary>
    public IReadOnlyList<CommandInfo> RootCommands =>
        _commands.Where(c => c.Group == null).ToList().AsReadOnly();
    
    // NEW: Group lookup
    public GroupInfo FindGroup(string name, GroupInfo parent = null);
    
    // MODIFIED: Command lookup
    public CommandInfo FindCommand(string name, GroupInfo group = null);
    
    // MODIFIED: Combined lookup for resolution
    public (GroupInfo group, CommandInfo command) FindGroupOrCommand(
        string name, GroupInfo currentGroup = null);
    
    // NEW: Register group
    public void RegisterGroup(Type markerType);
}
```

---

### ParsedCommand (Modified)

**Location:** `BitPantry.CommandLine/Processing/Parsing/ParsedCommand.cs`

**Changes:**
- Add group path tracking for space-separated syntax

```csharp
public class ParsedCommand
{
    // ... existing properties ...
    
    /// <summary>
    /// The parsed group path segments leading to the command.
    /// Example: Input "math advanced add" → GroupPath = ["math", "advanced"]
    /// </summary>
    public IReadOnlyList<string> GroupPath { get; }
    
    /// <summary>
    /// True if this represents a group invocation (no command, just group path).
    /// </summary>
    public bool IsGroupOnly { get; }
}
```

---

### ResolvedCommand (Modified)

**Location:** `BitPantry.CommandLine/Processing/Resolution/ResolvedCommand.cs`

**Changes:**
- Support group-only resolution for help display

```csharp
public class ResolvedCommand
{
    // ... existing properties ...
    
    /// <summary>
    /// The type of resolution - either a command or a group.
    /// </summary>
    public ResolvedType Type { get; }
    
    /// <summary>
    /// The resolved group (when Type == Group, or the command's group).
    /// </summary>
    public GroupInfo GroupInfo { get; }
}

/// <summary>
/// Indicates what type of entity was resolved.
/// </summary>
public enum ResolvedType
{
    /// <summary>
    /// A runnable command was resolved.
    /// </summary>
    Command,
    
    /// <summary>
    /// A group was resolved (will display help).
    /// </summary>
    Group
}
```

---

## 3. New Services

### IHelpFormatter

**Purpose:** Formats and displays help for commands and groups.

**Location:** `BitPantry.CommandLine/Help/IHelpFormatter.cs`

```csharp
public interface IHelpFormatter
{
    /// <summary>
    /// Display help for a specific command.
    /// </summary>
    void DisplayCommandHelp(CommandInfo command);
    
    /// <summary>
    /// Display help for a group (lists contained commands and subgroups).
    /// </summary>
    void DisplayGroupHelp(GroupInfo group);
    
    /// <summary>
    /// Display root application help (all groups and root commands).
    /// </summary>
    void DisplayRootHelp(CommandRegistry registry);
}
```

### HelpFormatter (Implementation)

**Location:** `BitPantry.CommandLine/Help/HelpFormatter.cs`

```csharp
public class HelpFormatter : IHelpFormatter
{
    private readonly IAnsiConsole _console;
    
    public HelpFormatter(IAnsiConsole console)
    {
        _console = console;
    }
    
    // Implementation uses Spectre.Console tables and formatting
}
```

---

## 4. Entity Relationships Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      CommandRegistry                         │
│  ┌──────────────┐                    ┌──────────────────┐   │
│  │ RootGroups   │                    │ RootCommands     │   │
│  └──────┬───────┘                    └────────┬─────────┘   │
└─────────┼────────────────────────────────────┼──────────────┘
          │                                    │
          ▼                                    ▼
    ┌───────────┐                       ┌─────────────┐
    │ GroupInfo │◄──────────────────────│ CommandInfo │
    │           │       Group           │             │
    │ - Name    │                       │ - Name      │
    │ - Parent ─┼───┐                   │ - Group ────┤
    │ - Children│   │                   │ - Arguments │
    │ - Commands├───┼──────────────────►│ - Aliases   │
    └───────────┘   │                   └─────────────┘
         ▲          │
         │          │ (self-reference)
         └──────────┘
```

---

## 5. Example Usage

### Defining Groups

```csharp
[Group]
[Description("Mathematical operations")]
public class Math { }

[Group]
[Description("Advanced mathematical operations")]
public class Advanced : Math { }  // Nested via inheritance
```

**Alternative nesting (inner classes):**

```csharp
[Group]
[Description("Mathematical operations")]
public class Math 
{
    [Group]
    [Description("Advanced mathematical operations")]
    public class Advanced { }
}
```

### Defining Commands

```csharp
[Command(Group = typeof(Math))]
[Description("Add two numbers")]
public class AddCommand : CommandBase
{
    // ...
}

[Command(Group = typeof(Math.Advanced))]
[Description("Calculate square root")]
public class SqrtCommand : CommandBase
{
    // ...
}

[Command]  // Root-level command
[Description("Display version")]
public class VersionCommand : CommandBase
{
    // ...
}
```

### Resulting Structure

```
root
├── version          (root command)
└── math/            (group)
    ├── add          (command)
    └── advanced/    (nested group)
        └── sqrt     (command)
```

### Invocation

```bash
$ myapp version                 # Root command
$ myapp math                    # Shows math group help
$ myapp math add 1 2            # Runs add command
$ myapp math advanced sqrt 16   # Runs sqrt command
$ myapp math advanced           # Shows advanced group help
$ myapp math add --help         # Shows add command help
```

---

## 6. Validation Rules

### Group Registration Validation

| Rule | Error |
|------|-------|
| Class must have `[Group]` attribute | `InvalidGroupDefinitionException` |
| Nested class parent must also be a group | `InvalidGroupHierarchyException` |
| No duplicate group names at same level | `DuplicateGroupException` |
| Group name cannot match command name at same level | `NameCollisionException` |

### Command Registration Validation

| Rule | Error |
|------|-------|
| `Group` type must have `[Group]` attribute | `InvalidGroupReferenceException` |
| Group must be registered before commands | `GroupNotFoundException` |
| No duplicate command names within same group | `DuplicateCommandException` |
| Command name cannot match group name at same level | `NameCollisionException` |

---

## 7. Migration Considerations

### Before (Namespace-based)

```csharp
[Command(Namespace = "math")]
public class AddCommand : CommandBase { }

[Command(Namespace = "math.advanced")]
public class SqrtCommand : CommandBase { }
```

### After (Group-based)

```csharp
[Group]
public class Math 
{
    [Group]
    public class Advanced { }
}

[Command(Group = typeof(Math))]
public class AddCommand : CommandBase { }

[Command(Group = typeof(Math.Advanced))]
public class SqrtCommand : CommandBase { }
```

### Breaking Changes

1. `Namespace` property removed from `CommandAttribute`
2. `Namespace` property removed from `CommandInfo`
3. Dot-notation parsing no longer supported (`math.add` → `math add`)
4. `CommandRegistry.Find(string)` signature changed
