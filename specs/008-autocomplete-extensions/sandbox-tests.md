# Sandbox Manual Testing Guide

**Feature Branch**: `008-autocomplete-extensions`  
**Created**: February 1, 2026  
**Purpose**: Manual validation of spec 008 autocomplete features via sandbox environment

---

## Overview

This document defines test commands and scenarios for manually validating the autocomplete extension system. Commands are split between local (SandboxClient) and remote (SandboxServer) to exercise both execution paths.

## Quick Start

```powershell
# Terminal 1: Start the server
cd sandbox/SandboxServer
dotnet run

# Terminal 2: Start the client
cd sandbox/SandboxClient
dotnet run
```

---

## Local Commands (SandboxClient)

### 1. `task` - Built-in Enum/Bool Handlers

**Features Tested**: FR-015, FR-016, FR-017 (Built-in implicit handlers)

```csharp
public enum Priority { Low, Medium, High, Critical }
public enum Status { Pending, Active, Completed, Cancelled }

[Command(Name = "task")]
public class TaskCommand : CommandBase
{
    [Argument] public Priority Priority { get; set; }
    [Argument] public Status Status { get; set; }
    [Argument] public bool Urgent { get; set; }
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"Task: {Priority}/{Status}, Urgent={Urgent}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| Enum ghost text | `task --priority ` | Ghost text shows "Critical" (first alphabetically) |
| Enum filtering | `task --priority h` | Ghost text shows "High" |
| Enum menu | `task --priority ` + Tab | Menu opens with Critical, High, Low, Medium |
| Bool ghost text | `task --urgent ` | Ghost text shows "false" |
| Bool menu | `task --urgent ` + Tab | Menu shows "false", "true" |

---

### 2. `chmod` - Flags Enum Handler

**Features Tested**: FR-015 (Enum handler with [Flags])

```csharp
[Flags]
public enum Permissions { None = 0, Read = 1, Write = 2, Execute = 4, Delete = 8 }

[Command(Name = "chmod")]
public class ChmodCommand : CommandBase
{
    [Argument] public Permissions Perms { get; set; }
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"Permissions: {Perms}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| Flags enum | `chmod --perms ` | Shows Delete, Execute, None, Read, Write |

---

### 3. `deploy` - Attribute Handler Override

**Features Tested**: FR-006 to FR-010 (Explicit attribute binding)

```csharp
public class EnvironmentHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        var envs = new[] { "Development", "Staging", "Production", "QA" };
        return Task.FromResult(envs
            .Where(e => e.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e)
            .Select(e => new AutoCompleteOption(e))
            .ToList());
    }
}

[Command(Name = "deploy")]
public class DeployCommand : CommandBase
{
    [Argument]
    [AutoComplete<EnvironmentHandler>]
    public string Environment { get; set; } = "";
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"Deploying to: {Environment}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| Custom handler | `deploy --environment ` | Shows Development, Production, QA, Staging |
| Prefix filter | `deploy --environment p` | Ghost text shows "Production" |

---

### 4. `travel` - Context-Aware Handler (ProvidedValues)

**Features Tested**: FR-027 (Context with provided argument values)

```csharp
public class CityHandler : IAutoCompleteHandler
{
    private static readonly Dictionary<string, string[]> CitiesByCountry = new()
    {
        ["USA"] = new[] { "New York", "Los Angeles", "Chicago" },
        ["UK"] = new[] { "London", "Manchester", "Edinburgh" },
        ["France"] = new[] { "Paris", "Lyon", "Marseille" }
    };

    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        var country = context.ProvidedValues
            .FirstOrDefault(kv => kv.Key.Name.Equals("Country", StringComparison.OrdinalIgnoreCase)).Value ?? "";
        
        var cities = CitiesByCountry.TryGetValue(country, out var c) ? c : Array.Empty<string>();
        
        return Task.FromResult(cities
            .Where(x => x.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .Select(x => new AutoCompleteOption(x))
            .ToList());
    }
}

[Command(Name = "travel")]
public class TravelCommand : CommandBase
{
    [Argument] public string Country { get; set; } = "";
    
    [Argument]
    [AutoComplete<CityHandler>]
    public string City { get; set; } = "";
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"Traveling to {City}, {Country}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| USA cities | `travel --country USA --city ` | Shows Chicago, Los Angeles, New York |
| UK cities | `travel --country UK --city ` | Shows Edinburgh, London, Manchester |
| France cities | `travel --country France --city ` | Shows Lyon, Marseille, Paris |
| No country | `travel --city ` | No suggestions (empty list) |

---

### 5. `paint` - Positional Arguments

**Features Tested**: FR-042 to FR-046 (Positional tracking and satisfaction)

```csharp
public enum Color { Red, Green, Blue, Yellow }
public enum Size { Small, Medium, Large, XLarge }

[Command(Name = "paint")]
public class PaintCommand : CommandBase
{
    [Argument(Position = 0)] public Color Color { get; set; }
    [Argument(Position = 1)] public Size Size { get; set; }
    [Argument] public bool Glossy { get; set; }
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"Painting {Size} {Color}, Glossy={Glossy}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| First positional | `paint ` | Ghost text shows "Blue" (enum autocomplete) |
| Second positional | `paint Red ` | Shows Size options: Large, Medium, Small, XLarge |
| After positionals | `paint Red Medium --` | Only `--glossy` available |
| Named blocks positional | `paint --color Red ` | No positional autocomplete after named |

---

### 6. `config` Group - Command/Group Syntax

**Features Tested**: FR-018 to FR-022 (Syntax autocomplete)

```csharp
[CommandGroup(Name = "config")]
public class ConfigGroup { }

[Command(Name = "show", Group = typeof(ConfigGroup))]
public class ConfigShowCommand : CommandBase
{
    [Argument] public string Key { get; set; } = "";
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"Config[{Key}] = (value)");
}

[Command(Name = "set", Group = typeof(ConfigGroup))]
public class ConfigSetCommand : CommandBase
{
    [Argument] public string Key { get; set; } = "";
    [Argument] public string Value { get; set; } = "";
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"Set Config[{Key}] = {Value}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| Group name | `con` | Ghost text shows "config" |
| Commands in group | `config ` | Shows "set", "show" |
| Command filter | `config s` | Shows "set", "show" (both match "s") |

---

### 7. `open` - Auto-Quoting Values with Spaces

**Features Tested**: FR-053, FR-054 (Value formatting with quotes)

```csharp
public class FilePathHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        var paths = new[] { "My Documents", "Program Files", "AppData", "simple.txt" };
        return Task.FromResult(paths
            .Where(p => p.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .Select(p => new AutoCompleteOption(p))
            .ToList());
    }
}

[Command(Name = "open")]
public class OpenCommand : CommandBase
{
    [Argument]
    [AutoComplete<FilePathHandler>]
    public string Path { get; set; } = "";
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"Opening: {Path}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| No space value | `open --path s` | Tab inserts `simple.txt` (no quotes) |
| Space value | `open --path M` | Tab inserts `"My Documents"` (auto-quoted) |
| Quote context | `open --path "P` | Tab inserts `"Program Files"` (continues quote) |

---

### 8. `calendar` - Menu Scroll and Type-to-Filter

**Features Tested**: FR-047 to FR-052 (Menu behavior)

```csharp
public enum Month { January, February, March, April, May, June, 
                    July, August, September, October, November, December }

[Command(Name = "calendar")]
public class CalendarCommand : CommandBase
{
    [Argument] public Month Month { get; set; }
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"Calendar for: {Month}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| Scroll indicator | `calendar --month ` + Tab | Menu shows 5 items + `▼ 7 more...` |
| Arrow wrap | Navigate down past December | Wraps to April (first visible) |
| Type-to-filter | Menu open, type "j" | Filters to January, July, June |
| Escape | Menu open + Escape | Menu closes, restores original text |
| Enter | Menu open + select + Enter | Accepts selection, closes menu |

---

## Remote Commands (SandboxServer)

### 9. `remote-task` - Remote Enum/Bool

**Features Tested**: RMT-004, RMT-005, RMT-UX-001, RMT-UX-002

```csharp
public enum RemotePriority { Low, Medium, High, Critical }
public enum RemoteStatus { Pending, Active, Completed, Cancelled }

[Command(Name = "remote-task")]
public class RemoteTaskCommand : CommandBase
{
    [Argument] public RemotePriority Priority { get; set; }
    [Argument] public RemoteStatus Status { get; set; }
    [Argument] public bool Urgent { get; set; }
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"[REMOTE] Task: {Priority}/{Status}, Urgent={Urgent}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| Remote ghost text | `connect` → `remote-task --priority ` | Ghost text appears via SignalR |
| Remote filtering | `remote-task --priority h` | "High" from server |
| Remote menu | `remote-task --priority ` + Tab | Menu with server options |

---

### 10. `remote-deploy` - Remote Attribute Handler

**Features Tested**: RMT-007 (Remote attribute handler resolution)

```csharp
public class RemoteEnvironmentHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        // Different values than local to prove server execution
        var envs = new[] { "AWS-Prod", "AWS-Staging", "Azure-Prod", "Azure-Staging", "Local" };
        return Task.FromResult(envs
            .Where(e => e.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e)
            .Select(e => new AutoCompleteOption(e))
            .ToList());
    }
}

[Command(Name = "remote-deploy")]
public class RemoteDeployCommand : CommandBase
{
    [Argument]
    [AutoComplete<RemoteEnvironmentHandler>]
    public string Environment { get; set; } = "";
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"[REMOTE] Deploying to: {Environment}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| Server handler | `remote-deploy --environment ` | Shows AWS-Prod, AWS-Staging, Azure-Prod, Azure-Staging, Local |
| Proves server | Compare to local `deploy` | Different options confirms server execution |

---

### 11. `remote-paint` - Remote Positional (CursorPosition)

**Features Tested**: RMT-008 (CursorPosition accuracy over remote)

```csharp
public enum RemoteColor { Cyan, Magenta, Yellow, Black }
public enum RemoteSize { Tiny, Small, Medium, Large, Huge }

[Command(Name = "remote-paint")]
public class RemotePaintCommand : CommandBase
{
    [Argument(Position = 0)] public RemoteColor Color { get; set; }
    [Argument(Position = 1)] public RemoteSize Size { get; set; }
    [Argument] public bool Matte { get; set; }
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"[REMOTE] Painting {Size} {Color}, Matte={Matte}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| Remote positional 1 | `remote-paint ` | Shows Black, Cyan, Magenta, Yellow |
| Remote positional 2 | `remote-paint Cyan ` | Shows Huge, Large, Medium, Small, Tiny |
| Remote named after | `remote-paint Cyan Small --` | Only `--matte` available |

---

### 12. `remote-search` - Remote Empty Results

**Features Tested**: RMT-009 (Empty list handling)

```csharp
public class EmptySearchHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        // Only return results if query starts with "valid"
        if (context.QueryString?.StartsWith("valid", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Task.FromResult(new List<AutoCompleteOption>
            {
                new("valid-result-1"),
                new("valid-result-2")
            });
        }
        return Task.FromResult(new List<AutoCompleteOption>()); // Empty list
    }
}

[Command(Name = "remote-search")]
public class RemoteSearchCommand : CommandBase
{
    [Argument]
    [AutoComplete<EmptySearchHandler>]
    public string Query { get; set; } = "";
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"[REMOTE] Search: {Query}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| No matches | `remote-search --query x` | No ghost text, no menu (empty list) |
| Has matches | `remote-search --query valid` | Shows valid-result-1, valid-result-2 |

---

### 13. `remote-files` - Remote Auto-Quoting

**Features Tested**: FR-053, FR-054 over SignalR

```csharp
public class RemoteFilePathHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct = default)
    {
        var paths = new[] { "Server Data", "Remote Logs", "Backup Files", "config.json" };
        return Task.FromResult(paths
            .Where(p => p.StartsWith(context.QueryString ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .Select(p => new AutoCompleteOption(p))
            .ToList());
    }
}

[Command(Name = "remote-files")]
public class RemoteFilesCommand : CommandBase
{
    [Argument]
    [AutoComplete<RemoteFilePathHandler>]
    public string Path { get; set; } = "";
    
    public void Execute(CommandExecutionContext ctx) 
        => ctx.Console.WriteLine($"[REMOTE] File: {Path}");
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| Remote quoting | `remote-files --path S` | Tab inserts `"Server Data"` (quoted over remote) |

---

### 14. `remote-error` - Remote User-Facing Exception Handling

**Features Tested**: User-facing exception propagation over SignalR

```csharp
public enum ErrorType { UserFacing, Regular, WithInner, Custom }

public class CustomCommandFailedException : Exception, IUserFacingException
{
    public CustomCommandFailedException(string message) : base(message) { }
}

[Command(Name = "remoteerror")]
public class RemoteErrorCommand : CommandBase
{
    [Argument(Name = "type")] public ErrorType Type { get; set; } = ErrorType.UserFacing;
    [Argument(Name = "message")] public string Message { get; set; } = "Something went wrong";

    public void Execute(CommandExecutionContext ctx)
    {
        switch (Type)
        {
            case ErrorType.UserFacing:
                Fail(Message);
            case ErrorType.Regular:
                throw new InvalidOperationException(Message);
            case ErrorType.WithInner:
                Fail(Message, new InvalidOperationException("Inner details"));
            case ErrorType.Custom:
                throw new CustomCommandFailedException(Message);
        }
    }
}
```

| Scenario | Input | Expected Behavior |
|----------|-------|-------------------|
| User-facing exception | `remoteerror --type UserFacing --message "Invalid input"` | Client shows Spectre-formatted exception with message "Invalid input" |
| Regular exception | `remoteerror --type Regular --message "Failure"` | Client shows generic remote execution error (message hidden) |
| User-facing with inner | `remoteerror --type WithInner --message "Outer message"` | Client shows "Outer message" with inner exception details |
| Custom IUserFacingException | `remoteerror --type Custom --message "Custom error"` | Client shows "CustomCommandFailedException: Custom error" |

---

## Remote UX Scenarios

| Scenario | Test Case | How to Validate |
|----------|-----------|-----------------|
| Ghost text over remote | RMT-UX-001 | `connect` → `remote-task --priority ` → ghost text appears |
| Menu with server options | RMT-UX-002 | Tab on remote enum → menu opens with server values |
| Type-to-filter local | RMT-UX-003 | Open menu → type filter → instant (no network lag) |
| Connection failure | RMT-UX-004 | Stop server → `remote-task --priority ` → silent, no crash |
| Slow connection | RMT-UX-005 | Ghost text appears when response finally arrives |

---

## Manual Validation Checklist

### Local Autocomplete
- [ ] `task --priority ` → Ghost text "Critical", Tab shows 4 options
- [ ] `task --priority h` → Ghost text "High"
- [ ] `task --urgent ` → Ghost text "false", Tab shows true/false
- [ ] `chmod --perms ` → Shows flags: Delete, Execute, None, Read, Write
- [ ] `deploy --environment ` → Shows Development, Production, QA, Staging
- [ ] `travel --country USA --city ` → Shows Chicago, Los Angeles, New York
- [ ] `travel --country UK --city ` → Shows Edinburgh, London, Manchester
- [ ] `paint ` → Positional enum, Tab shows colors
- [ ] `paint Red ` → Next positional, Tab shows sizes
- [ ] `paint Red Medium --` → Only `--glossy` available
- [ ] `con` → Ghost text "config"
- [ ] `config ` → Shows "set", "show"
- [ ] `open --path M` → Tab inserts `"My Documents"` (quoted)
- [ ] `calendar --month ` → Menu with scroll indicator, 12 items
- [ ] Type "j" in calendar menu → Filters to January, July, June

### Remote Autocomplete
- [ ] `connect` → Connect to localhost:5000
- [ ] `remote-task --priority ` → Ghost text appears over SignalR
- [ ] `remote-task --priority h` → Filtered results from server
- [ ] `remote-deploy --environment ` → Shows AWS/Azure options (server-specific)
- [ ] `remote-paint ` → Positional works over remote
- [ ] `remote-paint Cyan ` → Second positional works
- [ ] `remote-search --query x` → No suggestions (empty list)
- [ ] `remote-search --query valid` → Shows valid-result-1, valid-result-2
- [ ] `remote-files --path S` → Tab inserts `"Server Data"` (quoted over remote)
- [ ] Stop server → `remote-task --priority ` → Silent failure, no crash

### Remote Error Handling
- [ ] `remoteerror --type UserFacing --message "Test error"` → Spectre exception with "Test error"
- [ ] `remoteerror --type Regular` → Generic error (message hidden for security)
- [ ] `remoteerror --type WithInner --message "Outer"` → Shows "Outer" with inner exception
- [ ] `remoteerror --type Custom --message "Custom"` → Shows "CustomCommandFailedException: Custom"

---

## Feature Requirements Coverage

| Feature | Requirement | Command(s) |
|---------|-------------|------------|
| Enum handler | FR-015 | task, chmod, paint, calendar |
| Bool handler | FR-016 | task |
| Attribute override | FR-006-010 | deploy, travel |
| Type handler extensibility | FR-013-014 | deploy (custom handler) |
| Command syntax | FR-018-022 | config group |
| Argument name filtering | FR-022 | paint (after positionals) |
| Context ProvidedValues | FR-027 | travel |
| Positional tracking | FR-042-046 | paint |
| Menu scroll | FR-049 | calendar |
| Menu navigation | FR-047-052 | calendar |
| Auto-quoting | FR-053-054 | open, remote-files |
| Case-insensitive | FR-057 | All commands |
| Remote invocation | RMT-004 | remote-task |
| Remote parity | RMT-005 | remote-task vs task |
| Remote handler resolution | RMT-006 | remote-deploy |
| Remote attribute handler | RMT-007 | remote-deploy |
| Remote cursor position | RMT-008 | remote-paint |
| Remote empty results | RMT-009 | remote-search |
| Remote ghost text | RMT-UX-001 | remote-task |
| Remote menu | RMT-UX-002 | remote-task |
| Local type-to-filter | RMT-UX-003 | remote-task (menu open) |
| Connection failure | RMT-UX-004 | Any remote (server stopped) |
| Remote user-facing error | RMT-ERR-001 | remoteerror --type UserFacing |
| Remote non-user error | RMT-ERR-002 | remoteerror --type Regular |
| Remote error with inner | RMT-ERR-003 | remoteerror --type WithInner |
| Remote custom IUserFacingException | RMT-ERR-004 | remoteerror --type Custom |
