# Quickstart: Positional Arguments

**Feature**: 004-positional-arguments  
**Date**: December 24, 2025

## Overview

This guide provides quick examples for implementing and using positional arguments in BitPantry.CommandLine.

---

## For CLI Implementers

### Basic Positional Argument

```csharp
[Command(Name = "greet")]
[Description("Greet a person")]
public class GreetCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("Name of the person to greet")]
    public string Name { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        ctx.Console.WriteLine($"Hello, {Name}!");
    }
}
```

**Usage**: `greet Alice` → "Hello, Alice!"

---

### Multiple Positional Arguments

```csharp
[Command(Name = "copy")]
[Description("Copy a file to a destination")]
public class CopyCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("Source file path")]
    public string Source { get; set; }

    [Argument(Position = 1, IsRequired = true)]
    [Description("Destination file path")]
    public string Destination { get; set; }

    [Argument]
    [Alias('f')]
    [Flag]
    [Description("Overwrite if exists")]
    public bool Force { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        ctx.Console.WriteLine($"Copying {Source} to {Destination}");
        if (Force)
            ctx.Console.WriteLine("(overwriting if exists)");
    }
}
```

**Usage**: 
- `copy file.txt backup.txt`
- `copy file.txt backup.txt --force`
- `copy file.txt backup.txt -f`

---

### Variadic (Rest) Positional Argument

```csharp
[Command(Name = "rm")]
[Description("Remove files")]
public class RemoveCommand : CommandBase
{
    [Argument(Position = 0, IsRest = true, IsRequired = true)]
    [Description("Files to remove")]
    public string[] Files { get; set; }

    [Argument]
    [Alias('r')]
    [Flag]
    [Description("Recursive removal")]
    public bool Recursive { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        foreach (var file in Files)
            ctx.Console.WriteLine($"Removing {file}");
    }
}
```

**Usage**: `rm file1.txt file2.txt file3.txt`

---

### Mixed Positional and Rest

```csharp
[Command(Name = "move")]
[Description("Move files to a destination")]
public class MoveCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("Destination directory")]
    public string Destination { get; set; }

    [Argument(Position = 1, IsRest = true, IsRequired = true)]
    [Description("Files to move")]
    public string[] Files { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        foreach (var file in Files)
            ctx.Console.WriteLine($"Moving {file} to {Destination}");
    }
}
```

**Usage**: `move /backup file1.txt file2.txt file3.txt`

---

### Positional with Auto-Complete

```csharp
[Command(Name = "open")]
[Description("Open a project file")]
public class OpenCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true, AutoCompleteFunctionName = nameof(AutoComplete_File))]
    [Description("File to open")]
    public string File { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        ctx.Console.WriteLine($"Opening {File}");
    }

    public List<AutoCompleteOption> AutoComplete_File(AutoCompleteContext context)
    {
        return Directory.GetFiles(".", $"*{context.QueryString}*")
            .Select(f => new AutoCompleteOption(Path.GetFileName(f)))
            .ToList();
    }
}
```

---

### Repeated Named Options

```csharp
[Command(Name = "tag")]
[Description("Add tags to an item")]
public class TagCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("Item to tag")]
    public string Item { get; set; }

    [Argument]
    [Alias('t')]
    [Description("Tags to apply (can be repeated)")]
    public string[] Tags { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        ctx.Console.WriteLine($"Tagging {Item} with: {string.Join(", ", Tags)}");
    }
}
```

**Usage**: 
- `tag myfile.txt --tags "red;blue"` (delimiter syntax)
- `tag myfile.txt -t red -t blue` (repeated syntax)
- `tag myfile.txt --tags "red;blue" -t green` (mixed - all merge)

---

## For CLI Users

### Positional Syntax

```bash
# Instead of:
copy --source file.txt --dest backup.txt

# Now you can use:
copy file.txt backup.txt
```

### Multiple Values

```bash
# Remove multiple files:
rm file1.txt file2.txt file3.txt

# With options:
rm file1.txt file2.txt --recursive
```

### Values Starting with Dashes

Use the `--` separator to pass values that start with dashes:

```bash
# Remove a file named "-rf.txt":
rm -- -rf.txt

# This would NOT work (interpreted as option):
rm -rf.txt  # ERROR: Unknown option -r
```

### Repeated Options

For collection-type arguments, you can repeat the option:

```bash
# All of these are equivalent:
tag myfile --tags "red;blue;green"
tag myfile --tags red --tags blue --tags green
tag myfile -t red -t blue -t green
```

---

## Help Output Example

```
copy <source> <destination> [--force] [--verbose]

Copy a file to a destination

Arguments:
  <source>       Source file path (required)
  <destination>  Destination file path (required)

Options:
  --force, -f    Overwrite if exists
  --verbose, -v  Show detailed output
```

---

## Common Patterns

### Pattern: Command with Required Target and Optional Modifiers

```csharp
[Command(Name = "build")]
public class BuildCommand : CommandBase
{
    [Argument(Position = 0)]  // Optional positional
    [Description("Project to build (default: current directory)")]
    public string Project { get; set; } = ".";

    [Argument]
    [Description("Build configuration")]
    public string Configuration { get; set; } = "Release";
}
```

**Usage**:
- `build` → builds current directory
- `build myproject` → builds myproject
- `build myproject --configuration Debug`

### Pattern: Subcommand-Style with Rest

```csharp
[Command(Name = "exec")]
public class ExecCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("Command to execute")]
    public string Command { get; set; }

    [Argument(Position = 1, IsRest = true)]
    [Description("Arguments to pass to command")]
    public string[] Args { get; set; }
}
```

**Usage**: `exec git status --short`

---

## Validation Error Messages

If you misconfigure positional arguments, you'll see errors at startup:

| Error | Cause | Fix |
|-------|-------|-----|
| "Argument 'Files' has IsRest=true but property type 'string' is not a collection" | IsRest on scalar | Change to `string[]` or `List<string>` |
| "Argument 'Files' has IsRest=true but is not positional" | IsRest without Position | Add `Position = N` |
| "Command 'MyCommand' has multiple IsRest arguments" | Two+ IsRest args | Keep only one IsRest |
| "Argument 'Middle' has IsRest=true but is not the last positional" | IsRest not last | Reorder positions |
| "Command 'MyCommand' has non-contiguous positional indices: 0, 2" | Gap in positions | Use 0, 1, 2... |
| "Command 'MyCommand' has duplicate position 1" | Same Position twice | Use unique positions |
