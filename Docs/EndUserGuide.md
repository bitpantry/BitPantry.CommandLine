# End User Guide

[← Back to Documentation Home](index.md)

This guide is for users who operate CLI applications built with BitPantry.CommandLine. No programming knowledge required.

## Table of Contents

- [Getting Started](#getting-started)
- [Command Syntax](#command-syntax)
- [Using the REPL](#using-the-repl)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [Tab Autocomplete](#tab-autocomplete)
- [Command History](#command-history)
- [Built-in Commands](#built-in-commands)
- [Connecting to Remote Servers](#connecting-to-remote-servers)

## Getting Started

A CLI (Command Line Interface) application lets you run commands by typing text. When you launch the application, you'll see a prompt where you can type commands:

```
> _
```

Type a command and press **Enter** to run it.

## Command Syntax

Commands follow this general pattern:

```
command-name [--argument value] [--option]
```

### Parts of a Command

| Part | Description | Example |
|------|-------------|---------|
| Command name | The command to run | `greet` |
| Arguments | Named values the command needs | `--name "John"` |
| Options | Flags that change behavior | `--verbose` |

### Examples

```bash
# Simple command with no arguments
list

# Command with a named argument
greet --name "World"

# Command with multiple arguments
copy --source "file.txt" --destination "backup.txt"

# Using argument aliases (single dash, single letter)
greet -n "World"

# Command with an option flag
process --verbose
```

### Command Groups

Commands can be organized into groups, separated by spaces:

```bash
# Commands in the "files" group
files list
files copy

# Nested groups
admin users create
```

## Using the REPL

The REPL (Read-Eval-Print Loop) is the interactive mode where you type commands one at a time. 

See [REPL](CommandLine/REPL.md) for advanced REPL features.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Enter** | Execute the current command |
| **Tab** | Autocomplete command or argument |
| **↑** (Up Arrow) | Previous command from history |
| **↓** (Down Arrow) | Next command in history |
| **Ctrl+C** | Cancel current command/input |
| **Backspace** | Delete character before cursor |
| **Delete** | Delete character at cursor |
| **Home** | Move cursor to start of line |
| **End** | Move cursor to end of line |
| **Left/Right Arrow** | Move cursor |

## Tab Autocomplete

Press **Tab** to autocomplete:

1. **Command names** - Start typing and press Tab to complete
2. **Argument names** - After `--` press Tab to see available arguments  
3. **Argument values** - For arguments with known values, Tab shows options

### Example

```
> gre<Tab>
> greet                    # Completes to "greet"

> greet --<Tab>
--name    --count          # Shows available arguments

> greet --name <Tab>
John    Jane    World      # Shows suggested values (if configured)
```

## Command History

Use **Up Arrow** and **Down Arrow** to navigate through previously executed commands.

- Commands are saved during your session
- Press **Up Arrow** to go to the previous command
- Press **Down Arrow** to go to the next command
- Press **Enter** to execute the shown command

## Built-in Commands

Most applications include these built-in commands:

### List Commands (`lc` or `listcommands`)

Lists all available commands:

```
> lc
```

Output shows command names, groups, and descriptions.

See [Built-in Commands](CommandLine/BuiltInCommands.md) for details.

## Connecting to Remote Servers

If your CLI supports remote connections, use these commands:

### Connect to a Server

```
> server.connect --uri https://server.example.com/cli
```

If authentication is required, you'll be prompted for credentials.

### Disconnect

```
> server.disconnect
```

See [Remote Built-in Commands](Remote/BuiltInCommands.md) for details.

## Tips

1. **Commands are case-insensitive** - `GREET`, `Greet`, and `greet` all work
2. **Use Tab liberally** - Autocomplete saves typing and prevents errors
3. **Check available commands** - Run `lc` when unsure what's available
4. **Use aliases** - Single-letter aliases are faster: `-n` instead of `--name`

## Getting Help

- Run `lc` to see all available commands
- Check the application's documentation for command-specific help
- Contact your system administrator for access issues

## See Also

- [REPL](CommandLine/REPL.md) - Interactive mode details
- [Command Syntax](CommandLine/CommandSyntax.md) - Complete syntax reference
- [Built-in Commands](CommandLine/BuiltInCommands.md) - Standard commands
- [Remote Built-in Commands](Remote/BuiltInCommands.md) - Connection commands
